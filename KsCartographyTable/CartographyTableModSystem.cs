using System.Reflection;
using HarmonyLib;
using Kaisentlaia.KsCartographyTableMod.API.Client;
using Kaisentlaia.KsCartographyTableMod.GameContent;
using Kaisentlaia.KsCartographyTableMod.API.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using System.IO;
using System;
using Vintagestory.API.Config;

namespace Kaisentlaia.KsCartographyTableMod.API.Common;

/// <summary>
/// Represents the settings.json file structure.
/// </summary>
public class Settings
{
    public bool ImmersiveMode { get; set; } = false;
    public int ChunksPerPacket { get; set; } = 25;
    public double PacketDelay { get; set; } = 0.2;
    public bool VerboseDebug { get; set; } = false;
}

[HarmonyPatch]
public class KsCartographyTableModSystem : ModSystem
{

    // TODO adjust collision boxes
    // TODO update it labels

    // TODO test behaviors:
    // when player 1 first saves their map on a new table, all the data gets uploaded
    // when player 2 first saves their map on a new table, only the data which isn't already on the table gets uploaded
    // when any player updates their map, only chunks they never saw get downloaded 
    // when any player saves their map after exploring new chunks on a table where they already uploaded data, only the new chunks get uploaded
    // the candle emits light properly ✔
    // old cartography table is correctly replaced
    // the cartography table is craftable
    // the advanced cartography table is craftable
    // any player can wipe the map data (waypoints get wiped from the table, chunk ids get wiped from the table, server side db is dumped)

    public static ICoreAPI CoreAPI;
    public static ICoreServerAPI CoreServerAPI;
    public static ICoreClientAPI CoreClientAPI;
    public Harmony harmony;
    protected const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    public static ServerCartographyService ServerCartographyService;
    public static ClientCartographyService ClientCartographyService;
    public static ModCompatibilityManager ModCompatibilityManager;

    public static Settings Settings { get; private set; }

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        CoreAPI = api;
        api.RegisterBlockEntityClass(Mod.Info.ModID + ".cartography-table-entity", typeof(BlockEntityCartographyTable));
        api.RegisterBlockClass(Mod.Info.ModID + ".cartography-table", typeof(BlockCartographyTable));
        api.RegisterBlockClass(Mod.Info.ModID + ".advanced-cartography-table", typeof(BlockAdvancedCartographyTable));
        api.RegisterBlockClass(Mod.Info.ModID + ".advanced-cartography-table-part", typeof(BlockAdvancedCartographyTablePart));
        api.RegisterItemClass(Mod.Info.ModID + ".item-quill", typeof(ItemQuill));
        ReadSettings(api, Mod.Info.ModID);        
    }

    public static void ReadSettings(ICoreAPI api, string modId)
    {
        string settingsPath = Path.Combine(api.GetOrCreateDataPath("ModConfig"), $"{modId}.json");
        Settings settingsFile = null;
        Settings = new();
        if (File.Exists(settingsPath))
        {
            try
            {
                string json = File.ReadAllText(settingsPath);
                settingsFile = JsonUtil.FromString<Settings>(json);
            }
            catch (Exception ex)
            {
                api.Logger.Error($"{CartographyTableConstants.MAP_EVENT} Failed to load {modId}.json: {ex.Message}. Using defaults.");
            }
        }

        // Apply boolean toggles from settings file (or use defaults)
        if (settingsFile != null)
        {
            Settings.ImmersiveMode = settingsFile.ImmersiveMode;
            Settings.ChunksPerPacket = settingsFile.ChunksPerPacket;
            Settings.PacketDelay = settingsFile.PacketDelay;
            Settings.VerboseDebug = settingsFile.VerboseDebug;
        } else
        {
            Settings.ImmersiveMode = false;
            Settings.ChunksPerPacket = 25;
            Settings.PacketDelay = 0.2;
            Settings.VerboseDebug = false;
            string json = JsonUtil.ToString(Settings);
            File.WriteAllText(settingsPath, json);
        }
    }

    /// <summary>
    /// Server-specific intialization
    /// </summary>
    public override void StartServerSide(ICoreServerAPI api)
    {
        CoreServerAPI = api;
        ServerCartographyService = new ServerCartographyService(api);

        IChatCommand kctCommand = CoreServerAPI.ChatCommands
            .Create("kct")
            .WithDescription("K's Cartography Table commands")
            .RequiresPrivilege(Privilege.root);

        IChatCommand waypointsCommand = kctCommand
            .BeginSubCommand("waypoints")
            .WithDescription("Waypoint management commands")
            .RequiresPrivilege(Privilege.root);

        var parsers = CoreServerAPI.ChatCommands.Parsers;

        waypointsCommand
            .BeginSubCommand("wipe")
            .WithDescription("Deletes all waypoints from the maps of all players. Use with caution.<br><br>Running the command with 'maponly' will delete the waypoints from the player's maps, but will allow the players to get them back from the cartography table. Running it with 'mapandtable' will delete them also from the cartography table the next time a player transcribes their waypoints on it.<br><br Without the 'confirm' arg, does a dry-run only!")
            .RequiresPrivilege(Privilege.root)
            .WithArgs(parsers.WordRange("mode", ["maponly", "mapandtable"]), parsers.OptionalWordRange("confirm", ["confirm", "dryrun"]))
            .HandleWith((args) => {
                bool confirmed = !args.Parsers[1].IsMissing && ((string)args.Parsers[0].GetValue()).Equals("confirm", StringComparison.OrdinalIgnoreCase);
                bool mapOnly = !args.Parsers[0].IsMissing && ((string)args.Parsers[0].GetValue()).Equals("maponly", StringComparison.OrdinalIgnoreCase);

                TextCommandResult result = ServerCartographyService.WipeWaypoints(!confirmed, null, mapOnly);

                return result;
            })
            .EndSubCommand();

        if (!Harmony.HasAnyPatches(Mod.Info.ModID)) {
            harmony = new Harmony(Mod.Info.ModID);
            harmony.PatchAll(); // Applies all harmony patches
        }

        CoreServerAPI.Event.PlayerDisconnect += OnPlayerDisconnect;
    }

    private void OnPlayerDisconnect(IServerPlayer player)
    {
        ServerCartographyService.CleanupPlayerSessions(player);
    }

    private static bool IsMapDisallowed()
    {
        return !CoreAPI.World.Config.GetBool("allowMap", defaultValue: true);
    }
    
    public override void StartClientSide(ICoreClientAPI api)
    {
        CoreClientAPI = api;
        ModCompatibilityManager = new ModCompatibilityManager(CoreClientAPI);
        ClientCartographyService = new ClientCartographyService(CoreClientAPI);
        CoreClientAPI.Event.LeaveWorld += OnLeaveWorld;

        var parsers = CoreClientAPI.ChatCommands.Parsers;

        var kctClientCommand = CoreClientAPI.ChatCommands
            .Create("kct")
            .WithDescription("K's Cartography Table commands");

        IChatCommand waypointsCommand = kctClientCommand
            .BeginSubCommand("waypoints")
            .WithDescription("Waypoint management commands");

        waypointsCommand
            .BeginSubCommand("wipe")
            .WithDescription("Deletes all waypoints from your map.<br><br>Running the command with 'maponly' will delete the waypoints from your map, but will let you get them back from the cartography table. Running it with 'mapandtable' will let you delete them also from the cartography table the next time you transcribe your waypoints on it.<br><br>Without the 'confirm' arg, does a dry-run only!")
            .RequiresPlayer()
            .WithArgs(parsers.WordRange("mode", ["maponly", "mapandtable"]), parsers.OptionalWordRange("confirm", ["confirm", "dryrun"]))
            .HandleWith((args) => {
                bool confirmed = !args.Parsers[1].IsMissing && ((string)args.Parsers[0].GetValue()).Equals("confirm", StringComparison.OrdinalIgnoreCase);
                bool mapOnly = !args.Parsers[0].IsMissing && ((string)args.Parsers[0].GetValue()).Equals("maponly", StringComparison.OrdinalIgnoreCase);

                ClientCartographyService.WipeWaypoints(!confirmed, mapOnly);

                return TextCommandResult.Success();
            })
            .EndSubCommand();
    }

    private void OnLeaveWorld()
    {
        DebugLog(CoreClientAPI, "leaving world, disposing db connections");
        ClientCartographyService?.Dispose();
        ClientCartographyService = null;
    }

    public static void DebugLog(ICoreAPI api, string message)
    {
        if (Settings.VerboseDebug)
        {            
            api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} ${message}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(WaypointMapLayer), "OnCmdWayPointRemove")]
    public static void PreOnCmdWayPointRemove(TextCommandCallingArgs args) {
        if (!IsMapDisallowed() && !args.Parsers[0].IsMissing) {
            int index = (int)args.Parsers[0].GetValue();
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            ServerCartographyService.MarkWaypointDeleted(player, index);
        }
    }

    public static void ShowChatMessage(ICoreAPI api, IPlayer player, string messageIdentifier, string data = "")
    {
        if (!Settings.ImmersiveMode)
        {
            if (api.Side == EnumAppSide.Client && player is IClientPlayer)
            {
                (api as ICoreClientAPI).ShowChatMessage(Lang.Get(messageIdentifier, data));
            }
            else if (api.Side == EnumAppSide.Server && player is IServerPlayer)
            {
                (api as ICoreServerAPI).SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get(messageIdentifier, data), EnumChatType.Notification);
            }
        }
    }

    /// <summary>
    /// Unapplies Harmony patches and disposes of all static variables in the ModSystem.
    /// </summary>
    public override void Dispose()
    {
        ClientCartographyService?.Dispose();
        ClientCartographyService = null;
        ServerCartographyService?.Dispose();
        ServerCartographyService = null;

        CoreClientAPI = null;
        CoreAPI = null;
        CoreServerAPI = null;
        harmony?.UnpatchAll(Mod.Info.ModID);
    }
}
