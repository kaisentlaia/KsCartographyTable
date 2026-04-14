using System.Reflection;
using HarmonyLib;
using Kaisentlaia.CartographyTable.BlockEntities;
using Kaisentlaia.CartographyTable.Blocks;
using Kaisentlaia.CartographyTable.Client;
using Kaisentlaia.CartographyTable.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Kaisentlaia.CartographyTable;

[HarmonyPatch]
public class KsCartographyTableModSystem : ModSystem
{
    // TODO continuous animation and repeating scribble sound while the explored map is being uploaded/downloaded + block player interactions
    // TODO probably don't allow two players uploading/downloading at once - to test
    // TODO change interaction to avoid uploading by mistake? maybe adding a modifier to the upload, download already has a modifier and that's fine
    // TODO an alternative might be to keep pressing, like when using the quern. Chunks get uploaded only while the interaction continues
    // TODO check that when a second player uploads their map it won't resend all data, only the chunks that aren't on the table yet (already works for the first player, uploading a second time does nothing)
    // TODO grey out the chunks uploaded by other players and never seen by the player who downloads the map (overlay fog of war color)
    // TODO test with huge maps
    // TODO alternative recipe (2 cartography tables, 1 ink and quill, 1 candle)
    // TODO add fire particles to the candle
    // TODO wipe server side map db and table chunk id list on wipe with resin
    // TODO consider if it would be safer to add a delay between packets
    // TODO test recipes in survival
    // TODO update guide
    // TODO update table description

    // TODO test behaviors:
    // when player 1 first saves their map on a new table, all the data gets uploaded
    // when player 2 first saves their map on a new table, only the data which isn't already on the table gets uploaded
    // when any player updates their map, only chunks they never saw get downloaded 
    // when any player saves their map after exploring new chunks on a table where they already uploaded data, only the new chunks get uploaded
    // the candle emits light properly
    // the cartography table is craftable as before
    // the advanced cartography table is craftable (2 alternative recipes)
    // any player can wipe the map data (waypoints get wiped from the table, chunk ids get wiped from the table, server side db is dumped)

    public static ICoreAPI CoreAPI;
    public static ICoreServerAPI CoreServerAPI;
    public static ICoreClientAPI CoreClientAPI;
    public Harmony harmony;
    protected const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    public static ServerCartographyHelper ServerCartographyHelper;
    public static ClientCartographyHelper ClientCartographyHelper;
    public static bool purgeWpGroups = false;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        CoreAPI = api;
        api.RegisterBlockEntityClass(Mod.Info.ModID + ".cartography-table-entity", typeof(BlockEntityCartographyTable));
        api.RegisterBlockClass(Mod.Info.ModID + ".cartography-table", typeof(BlockCartographyTable));
        api.RegisterBlockClass(Mod.Info.ModID + ".advanced-cartography-table", typeof(BlockAdvancedCartographyTable));
    }

    /// <summary>
    /// Server-specific intialization
    /// </summary>
    public override void StartServerSide(ICoreServerAPI api)
    {
        CoreServerAPI = api;
        ServerCartographyHelper = new ServerCartographyHelper(CoreServerAPI);
        api.ChatCommands.Create("purgewpgroups")
        .WithDescription("Removes groups from all the waypoints created by other mods on the next cartography table interaction")
        .RequiresPrivilege(Privilege.chat)
        .RequiresPlayer()
        .HandleWith((args) => {
            purgeWpGroups = true;
            return TextCommandResult.Success("Groups set to be purged from all waypoints. Interact with a cartography table to apply.");
        });
        api.ChatCommands.Create("clearcartographydata")
        .WithDescription("Clears the cartography table mod data from the savegame")
        .RequiresPrivilege(Privilege.root)
        .RequiresPlayer()
        .HandleWith((args) => {
            ServerCartographyHelper.ClearAllDeletedWaypoints();
            return TextCommandResult.Success("Data cleared.");
        });
        api.ChatCommands.Create("wipewaypoints")
        .WithDescription("Wipes all the waypoints")
        .RequiresPrivilege(Privilege.root)
        .RequiresPlayer()
        .HandleWith((args) => {
            ServerCartographyHelper.WipeWaypoints();
            ServerCartographyHelper.ResendWaypoints(args.Caller.Player as IServerPlayer);
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
        ClientCartographyHelper = new ClientCartographyHelper(CoreClientAPI);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(WaypointMapLayer), "OnCmdWayPointRemove")]
    public static void PreOnCmdWayPointRemove(TextCommandCallingArgs args) {
        CoreServerAPI.Logger.Notification("user deleting waypoint");
        if (!IsMapDisallowed() && !args.Parsers[0].IsMissing) {
            int index = (int)args.Parsers[0].GetValue();
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            ServerCartographyHelper.MarkDeleted(player, index);
        }
    }

    /// <summary>
    /// Unapplies Harmony patches and disposes of all static variables in the ModSystem.
    /// </summary>
    public override void Dispose()
    {
        if (CoreClientAPI != null)
        {
            CoreClientAPI = null;
        }
        CoreAPI = null;
        CoreServerAPI = null;
        harmony?.UnpatchAll(Mod.Info.ModID);
    }
}
