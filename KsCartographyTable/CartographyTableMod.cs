using System.Reflection;
using HarmonyLib;
using Kaisentlaia.CartographyTable.BlockEntities;
using Kaisentlaia.CartographyTable.Blocks;
using Kaisentlaia.CartographyTable.Utilities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Kaisentlaia.CartographyTable;

[HarmonyPatch]
public class KsCartographyTableModSystem : ModSystem
{
        public static ICoreAPI CoreAPI;
        public static ICoreServerAPI CoreServerAPI;
        public static ICoreClientAPI CoreClientAPI;
        public Harmony harmony;
        protected const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        public static CartographyHelper CartographyHelper;
        public static bool purgeWpGroups = false;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            CoreAPI = api;
            api.RegisterBlockEntityClass(Mod.Info.ModID + ".cartography-table-entity", typeof(BlockEntityCartographyTable));
            api.RegisterBlockClass(Mod.Info.ModID + ".cartography-table", typeof(BlockCartographyTable));
        }        
        
        /// <summary>
        /// Server-specific intialization
        /// </summary>
        public override void StartServerSide(ICoreServerAPI api)
        {
            CoreServerAPI = api;
            CartographyHelper = new CartographyHelper(CoreServerAPI);
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
                CartographyHelper.ClearAllDeletedWaypoints();
                return TextCommandResult.Success("Data cleared.");
            });
            api.ChatCommands.Create("wipewaypoints")
            .WithDescription("Wipes all the waypoints")
            .RequiresPrivilege(Privilege.root)
            .RequiresPlayer()
            .HandleWith((args) => {
                CartographyHelper.WipeWaypoints();
                CartographyHelper.ResendWaypoints(args.Caller.Player as IServerPlayer);
                return TextCommandResult.Success("Waypoints wiped");
            });
            if (!Harmony.HasAnyPatches(Mod.Info.ModID)) {
                harmony = new Harmony(Mod.Info.ModID);
                harmony.PatchAll(); // Applies all harmony patches
            }
        }

        /// <summary>
        /// Client-specific initialization
        /// </summary>
        public override void StartClientSide(ICoreClientAPI api)
        {
            CoreClientAPI = api;
        }

        private static bool IsMapDisallowed()
        {
            if (!CoreAPI.World.Config.GetBool("allowMap", defaultValue: true))
            {
                return true;
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WaypointMapLayer), "OnCmdWayPointRemove")]
        public static void PreOnCmdWayPointRemove(TextCommandCallingArgs args) {
            CoreServerAPI.Logger.Notification("user deleting waypoint");
            if (!IsMapDisallowed() && !args.Parsers[0].IsMissing) {
                int index = (int)args.Parsers[0].GetValue();
                IServerPlayer player = args.Caller.Player as IServerPlayer;
                CartographyHelper.MarkDeleted(player, index);
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
