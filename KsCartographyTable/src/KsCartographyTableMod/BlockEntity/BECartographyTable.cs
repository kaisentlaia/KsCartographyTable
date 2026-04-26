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
        protected ILoadedSound ambientSound;
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

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (Block is not BlockAdvancedCartographyTable && Map != null && Map.WaypointCount > 0)
            {
                dsc.AppendLine(Lang.Get(CartographyTableLangCodes.GUI_TABLE_WAYPOINTS, Map.WaypointCount));
            } else if (Block is BlockAdvancedCartographyTable && Map != null && (Map.WaypointCount > 0 || Map.ExploredAreasIds.Count > 0)) {
                double km2 = Map.ExploredAreasIds.Count * 0.001024;
                dsc.AppendLine(Lang.Get(CartographyTableLangCodes.GUI_TABLE_MAP_WAYPOINTS, Map.WaypointCount, $"{km2:F1}"));
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

        internal bool OnCartographySessionStart(CartographyAction action, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (action == CartographyAction.UploadMap && Api.Side == EnumAppSide.Client)
            {
                MarkDirty();
                startSound();
                return KsCartographyTableModSystem.ClientCartographyService.StartCartographyUploadSession(action, Map, world, byPlayer, blockSel, this);
            }
            if (action == CartographyAction.DownloadMap && Api.Side == EnumAppSide.Server)
            {
                MarkDirty();
                startSound();
                return KsCartographyTableModSystem.ServerCartographyService.StartCartographyDownloadSession(action, Map, world, byPlayer, blockSel);
            }
            return false;
        }

        internal bool OnCartographySessionStep(CartographyAction action, float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            // TODO session should receive secondsUsed and send a packet every x seconds
            if (action == CartographyAction.UploadMap && Api.Side == EnumAppSide.Client)
            {
                return KsCartographyTableModSystem.ClientCartographyService.ContinueCartographyUploadSession(byPlayer, blockSel.Block);
            }
            if (action == CartographyAction.DownloadMap && Api.Side == EnumAppSide.Server)
            {
                return KsCartographyTableModSystem.ServerCartographyService.ContinueCartographyDownloadSession(Map, secondsUsed, world, byPlayer, blockSel.Block);
            }
            throw new NotImplementedException();
        }

        internal void OnCartographySessionStop(CartographyAction action, float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (action == CartographyAction.UploadMap && Api.Side == EnumAppSide.Client)
            {
                stopSound();
                KsCartographyTableModSystem.ClientCartographyService.EndCartographyUploadSession(byPlayer, blockSel.Block);
            }
            if (action == CartographyAction.DownloadMap && Api.Side == EnumAppSide.Server)
            {
                stopSound();
                KsCartographyTableModSystem.ServerCartographyService.EndCartographyDownloadSession(Map, secondsUsed, world, byPlayer, blockSel.Block);
            }
        }
        public void startSound()
        {
            if (ambientSound == null && Api?.Side == EnumAppSide.Client)
            {
                ambientSound = (Api as ICoreClientAPI).World.LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("game:sounds/effect/writing.ogg"),
                    ShouldLoop = true,
                    Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 0.75f
                });

                ambientSound.Start();
            }
        }

        public void stopSound()
        {
            if (ambientSound != null)
            {
                ambientSound.Stop();
                ambientSound.Dispose();
                ambientSound = null;
            }
        }
    }
}