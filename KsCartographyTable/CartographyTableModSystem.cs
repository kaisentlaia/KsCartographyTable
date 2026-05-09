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
                api.Logger.Error($"Failed to load {modId}.json: {ex.Message}. Using defaults.");
            }
        }

        // Apply boolean toggles from settings file (or use defaults)
        if (settingsFile != null)
        {
            Settings.ImmersiveMode = settingsFile.ImmersiveMode;
        } else
        {
            Settings.ImmersiveMode = false;
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

        // TODO add handbook entry
        api.ChatCommands.Create("wipewaypoints")
        .WithDescription("Wipes all the waypoints")
        .RequiresPrivilege(Privilege.root)
        .RequiresPlayer()
        .HandleWith((args) => {
            ServerCartographyService.WipeWaypoints();
            ServerCartographyService.ResendWaypointsToPlayer(args.Caller.Player as IServerPlayer);
            return TextCommandResult.Success("Waypoints wiped");
        });
        if (!Harmony.HasAnyPatches(Mod.Info.ModID)) {
            harmony = new Harmony(Mod.Info.ModID);
            harmony.PatchAll(); // Applies all harmony patches
        }
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
            if (api.Side == EnumAppSide.Client)
            {
                (api as ICoreClientAPI).SendChatMessage(Lang.Get(messageIdentifier, data));
            }
            else if (api.Side == EnumAppSide.Server)
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
        ServerCartographyService?.Dispose();
        CoreClientAPI = null;
        CoreAPI = null;
        CoreServerAPI = null;
        harmony?.UnpatchAll(Mod.Info.ModID);
    }
}
