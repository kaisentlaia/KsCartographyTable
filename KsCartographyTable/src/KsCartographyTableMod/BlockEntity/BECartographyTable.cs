using System;
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
        public EnumAppSide Side;
        public CartographyMap Map;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            Side = Api.Side;
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
            if (Map == null)
            {
                Map = new CartographyMap(Api);
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
                KsCartographyTableModSystem.ClientCartographyService.Ponder(Map, byPlayer as IClientPlayer);
            }

            return true;
        }

        internal bool OnWipeTableMap(IPlayer byPlayer, BlockPos blockPos)
        {
            EnsureMap();
            if (CoreServerAPI != null)
            {
                KsCartographyTableModSystem.ServerCartographyService.WipeTableMap(Map, Block, byPlayer, blockPos);
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
                KsCartographyTableModSystem.ServerCartographyService.UpdateTableMap(Map, byPlayer as IServerPlayer);
                MarkDirty();
            }
            if (CoreClientAPI != null)
            {
                CoreClientAPI.World.Player.TriggerFpAnimation(EnumHandInteract.BlockInteract);
                KsCartographyTableModSystem.ClientCartographyService.UpdateTableMap(Map, Block, blockSel.Position);
            }

            return true;
        }

        internal bool OnUpdatePlayerMap(IPlayer byPlayer, BlockSelection blockSel)
        {
            EnsureMap();
            if (CoreServerAPI != null)
            {
                KsCartographyTableModSystem.ServerCartographyService.UpdatePlayerMap(Map, byPlayer as IServerPlayer, Block, blockSel.Position);
            }
            if (CoreClientAPI != null)
            {
                CoreClientAPI.World.Player.TriggerFpAnimation(EnumHandInteract.BlockInteract);
            }

            return true;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (Block is not BlockAdvancedCartographyTable && Map != null && Map.Waypoints.Count > 0)
            {
                dsc.AppendLine(Lang.Get(CartographyTableLangCodes.GUI_TABLE_WAYPOINTS, Map.Waypoints.Count));
            } else if (Block is BlockAdvancedCartographyTable && Map != null && (Map.Waypoints.Count > 0 || Map.ExploredAreasIds.Count > 0)) {
                double km2 = Map.ExploredAreasIds.Count * 0.001024;
                dsc.AppendLine(Lang.Get(CartographyTableLangCodes.GUI_TABLE_MAP_WAYPOINTS, Map.Waypoints.Count, $"{km2:F1}"));
            } else
            {
                dsc.AppendLine(Lang.Get(CartographyTableLangCodes.GUI_TABLE_EMPTY));
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (Map != null)
            {
                Map.Serialize(tree);
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            EnsureMap();
            Map.Deserialize(tree);
        }
        
        public void UpdateMapExploredAreasIds(List<FastVec2i> piecesIds)
        {
            EnsureMap();
            Map.ExploredAreasIds = piecesIds.Select(pieceId => pieceId.ToChunkIndex()).ToList();
            MarkDirty();
        }

        internal bool onCartographySessionStart(CartographyAction action, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (action == CartographyAction.UploadMap && Api.Side == EnumAppSide.Client)
            {
                return KsCartographyTableModSystem.ClientCartographyService.StartCartographyUploadSession(action, Map, world, byPlayer, blockSel.Block);
            }
            if (action == CartographyAction.DownloadMap && Api.Side == EnumAppSide.Server)
            {
                return KsCartographyTableModSystem.ServerCartographyService.StartCartographyDownloadSession(action, Map, world, byPlayer, blockSel.Block);
            }
            return false;
        }

        internal bool onCartographySessionStep(CartographyAction action, float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (action == CartographyAction.UploadMap && Api.Side == EnumAppSide.Client)
            {
                return KsCartographyTableModSystem.ClientCartographyService.ContinueCartographyUploadSession(byPlayer, blockSel.Block);
            }
            if (action == CartographyAction.DownloadMap && Api.Side == EnumAppSide.Server)
            {
                return KsCartographyTableModSystem.ServerCartographyService.continueCartographyDownloadSession(Map, secondsUsed, world, byPlayer, blockSel.Block);
            }
            throw new NotImplementedException();
        }

        internal void onCartographySessionStop(CartographyAction action, float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (action == CartographyAction.UploadMap && Api.Side == EnumAppSide.Client)
            {
                KsCartographyTableModSystem.ClientCartographyService.EndCartographyUploadSession(byPlayer, blockSel.Block);
            }
            if (action == CartographyAction.DownloadMap && Api.Side == EnumAppSide.Server)
            {
                KsCartographyTableModSystem.ServerCartographyService.endCartographyDownloadSession(Map, secondsUsed, world, byPlayer, blockSel.Block);
            }
            throw new NotImplementedException();
        }
    }
}