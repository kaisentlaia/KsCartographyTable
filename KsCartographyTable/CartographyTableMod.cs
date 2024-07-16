using System.Reflection;
using HarmonyLib;
using Kaisentlaia.CartographyTable.BlockEntities;
using Kaisentlaia.CartographyTable.Blocks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Kaisentlaia.CartographyTable;

[HarmonyPatch]
public class KsCartographyTableModSystem : ModSystem
{
        public static ICoreAPI CoreAPI;
        public static ICoreServerAPI CoreServerAPI;
        public static ICoreClientAPI CoreClientAPI;
        public Harmony harmony;
        protected const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

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
            //api.Event.PlayerJoin += FixWaypoints;
            api.ChatCommands.Create("purgewpgroups")
            .WithDescription("removes groups from all the waypoints created by other mods on the next cartography table interaction")
            .RequiresPrivilege(Privilege.chat)
            .RequiresPlayer()
            .HandleWith((args) => {
                purgeWpGroups = true;
                return TextCommandResult.Success("Groups set to be purged from all waypoints. Interact with a cartography table to apply.");
            });
        }

        /// <summary>
        /// Client-specific initialization
        /// </summary>
        public override void StartClientSide(ICoreClientAPI api)
        {
            CoreClientAPI = api;
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
