using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
    public class BlockEntityCartographyTable : BlockEntity
    {
        private ICoreServerAPI CoreServerAPI;
        private ICoreClientAPI CoreClientAPI;
        private CartographyMap map;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            if (Api.Side == EnumAppSide.Server)
            {
                CoreServerAPI = Api as ICoreServerAPI;
            }
            if (Api.Side == EnumAppSide.Client)
            {
                CoreClientAPI = Api as ICoreClientAPI;
            }
        }

        public void EnsureMap()
        {
            if (map == null)
            {
                map = new CartographyMap(Api);
            }
        }

        internal bool OnPurgeWaypointGroups(IPlayer byPlayer)
        {
            if (CoreServerAPI != null && KsCartographyTableModSystem.purgeWpGroups)
            {
                KsCartographyTableModSystem.ServerCartographyService.PurgeWaypointGroups(byPlayer);
            }
            return true;
        }

        internal bool OnPonderMap(IPlayer byPlayer)
        {
            EnsureMap();
            if (CoreClientAPI != null)
            {
                KsCartographyTableModSystem.ClientCartographyService.Ponder(map, byPlayer as IClientPlayer);
            }

            return true;
        }

        internal bool OnWipeTableMap(IPlayer byPlayer, BlockPos blockPos)
        {
            EnsureMap();
            if (CoreServerAPI != null)
            {
                KsCartographyTableModSystem.ServerCartographyService.WipeTableMap(map, Block, byPlayer, blockPos);
                MarkDirty();
            }

            if (CoreClientAPI != null)
            {
                CoreClientAPI.World.Player.TriggerFpAnimation(EnumHandInteract.BlockInteract);
            }

            return true;
        }

        internal bool OnUpdateTableMap(IPlayer byPlayer, BlockSelection blockSel)
        {
            EnsureMap();
            if (CoreServerAPI != null)
            {
                KsCartographyTableModSystem.ServerCartographyService.UpdateTableMap(map, byPlayer as IServerPlayer);
                MarkDirty();
            }
            if (CoreClientAPI != null)
            {
                CoreClientAPI.World.Player.TriggerFpAnimation(EnumHandInteract.BlockInteract);
                KsCartographyTableModSystem.ClientCartographyService.UpdateTableMap(map, Block, blockSel.Position);
            }

            return true;
        }

        internal bool OnUpdatePlayerMap(IPlayer byPlayer, BlockSelection blockSel)
        {
            EnsureMap();
            if (CoreServerAPI != null)
            {
                KsCartographyTableModSystem.ServerCartographyService.UpdatePlayerMap(map, byPlayer as IServerPlayer, Block, blockSel.Position);
            }
            if (CoreClientAPI != null)
            {
                CoreClientAPI.World.Player.TriggerFpAnimation(EnumHandInteract.BlockInteract);
                KsCartographyTableModSystem.ClientCartographyService.UpdateTableMap(map, Block, blockSel.Position);
            }

            return true;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (!(Block is BlockAdvancedCartographyTable) && map != null && map.Waypoints.Count > 0)
            {
                dsc.AppendLine(Lang.Get(CartographyTableLangCodes.GUI_TABLE_WAYPOINTS, map.Waypoints.Count));
            } else if (Block is BlockAdvancedCartographyTable && map != null && (map.Waypoints.Count > 0 || map.ExploredAreasIds.Count > 0)) {
                double km2 = map.ExploredAreasIds.Count * 0.001024;
                dsc.AppendLine(Lang.Get(CartographyTableLangCodes.GUI_TABLE_MAP_WAYPOINTS, map.Waypoints.Count, $"{km2:F1}"));
            } else
            {
                dsc.AppendLine(Lang.Get(CartographyTableLangCodes.GUI_TABLE_EMPTY));
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (map != null)
            {
                map.Serialize(tree);
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            EnsureMap();
            map.Deserialize(tree);
        }
        
        public void UpdateMapExploredAreasIds(List<FastVec2i> piecesIds)
        {
            EnsureMap();
            map.ExploredAreasIds = piecesIds.Select(pieceId => pieceId.ToChunkIndex()).ToList();
            MarkDirty();
        }
    }
}