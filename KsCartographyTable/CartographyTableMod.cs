using Kaisentlaia.CartographyTable.BlockEntities;
using Kaisentlaia.CartographyTable.Blocks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Kaisentlaia.CartographyTable;

public class KsCartographyTableModSystem : ModSystem
{
        public static ICoreAPI CoreAPI;
        public static ICoreServerAPI CoreServerAPI;
        public static ICoreClientAPI CoreClientAPI;

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
        }
}
