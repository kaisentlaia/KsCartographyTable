using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Kaisentlaia.KsCartographyTableMod.API.Utils;
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
        static SimpleParticleProperties InkParticles;
        static SimpleParticleProperties PaperDustParticles;
        protected ILoadedSound ambientSound;
        protected bool SpawnParticles = false;
        private ICoreServerAPI CoreServerAPI;
        private ICoreClientAPI CoreClientAPI;
        public EnumAppSide Side;
        public CartographyMap Map;

        static BlockEntityCartographyTable()
        {
            InkParticles = new SimpleParticleProperties(1, 3, ColorUtil.ToRgba(200, 20, 20, 60), new Vec3d(), new Vec3d(), new Vec3f(-0.25f, -0.25f, -0.25f), new Vec3f(0.25f, 0.25f, 0.25f), 1, 1, 0.1f, 0.3f, EnumParticleModel.Quad);
            InkParticles.AddPos.Set(1 + 2 / 32f, 0, 1 + 2 / 32f);
            InkParticles.AddQuantity = 20;
            InkParticles.MinVelocity.Set(-0.25f, 0, -0.25f);
            InkParticles.AddVelocity.Set(0.5f, 1, 0.5f);
            InkParticles.WithTerrainCollision = true;
            InkParticles.SelfPropelled = true;
            InkParticles.GravityEffect = 0.5f;
            InkParticles.ParticleModel = EnumParticleModel.Cube;
            InkParticles.LifeLength = 1.5f;
            InkParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.4f);

            PaperDustParticles = new SimpleParticleProperties(
                1,
                3,
                ColorUtil.ToRgba(200, 200, 170, 100),
                new Vec3d(),
                new Vec3d(),
                new Vec3f(-0.25f, -0.25f, -0.25f),
                new Vec3f(0.25f, 0.25f, 0.25f),
                1,
                1,
                0.1f,
                0.2f,
                EnumParticleModel.Quad
            );
            PaperDustParticles.AddPos.Set(1 + 2 / 32f, 0, 1 + 2 / 32f);
            PaperDustParticles.AddQuantity = 1;
            PaperDustParticles.MinVelocity.Set(-0.05f, 0, -0.05f);
            PaperDustParticles.AddVelocity.Set(0.1f, 0.2f, 0.1f);
            PaperDustParticles.WithTerrainCollision = false;
            PaperDustParticles.ParticleModel = EnumParticleModel.Quad;
            PaperDustParticles.LifeLength = 0.5f;
            PaperDustParticles.SelfPropelled = true;
            PaperDustParticles.GravityEffect = 0;
            PaperDustParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, 0.4f);
            PaperDustParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f);
        }
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
                KsCartographyTableModSystem.ClientCartographyService.Ponder(byPlayer as IClientPlayer);
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
                bool uploadStarted = KsCartographyTableModSystem.ClientCartographyService.StartCartographyUploadSession(action, Map, world, byPlayer, blockSel);
                if (uploadStarted) {
                    StartSoundAndParticles();
                }
                return uploadStarted;
            }
            if (action == CartographyAction.DownloadMap && Api.Side == EnumAppSide.Server)
            {
                bool uploadStarted = KsCartographyTableModSystem.ServerCartographyService.StartCartographyDownloadSession(action, Map, world, byPlayer, blockSel);
                if (uploadStarted) {
                    StartSoundAndParticles();
                }
                return uploadStarted;
            }
            return false;
        }

        internal bool OnCartographySessionStep(CartographyAction action, float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (action == CartographyAction.UploadMap && Api.Side == EnumAppSide.Client)
            {
                if (SpawnParticles)
                {
                    ItemStack inkColorStack = ItemDetectorService.GetItemStacks(world, "charcoal")[0];
                    InkParticles.Color = InkParticles.Color = inkColorStack.Collectible.GetRandomColor(Api as ICoreClientAPI, inkColorStack);
                    InkParticles.Color &= 0xffffff;
                    InkParticles.Color |= (200 << 24);
                    InkParticles.MinQuantity = 1;
                    InkParticles.AddQuantity = 5;
                    InkParticles.MinPos.Set(Pos.X - 10 / 16f, Pos.Y + 18 / 16f, Pos.Z - 1 / 32f);
                    InkParticles.MinVelocity.Set(-0.1f, 0, -0.1f);
                    InkParticles.AddVelocity.Set(0.2f, 0.2f, 0.2f);
                    PaperDustParticles.MinPos.Set(Pos.X - 10 / 16f, Pos.Y + 18 / 16f, Pos.Z - 1 / 32f);
                    PaperDustParticles.AddQuantity = 1;
                    PaperDustParticles.MinQuantity = 2;
                    Api.World.SpawnParticles(InkParticles);
                    Api.World.SpawnParticles(PaperDustParticles);
                }
                return KsCartographyTableModSystem.ClientCartographyService.ContinueCartographyUploadSession(byPlayer, secondsUsed, blockSel.Block, this);
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
                KsCartographyTableModSystem.ClientCartographyService.EndCartographyUploadSession(byPlayer, blockSel.Block);
            }
            if (action == CartographyAction.DownloadMap && Api.Side == EnumAppSide.Server)
            {
                KsCartographyTableModSystem.ServerCartographyService.EndCartographyDownloadSession(Map, secondsUsed, world, byPlayer, blockSel.Block);
            }
            StopSoundAndParticles();
        }
        public void StartSoundAndParticles()
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
                SpawnParticles = true;
            }
        }

        public void StopSoundAndParticles()
        {
            if (ambientSound != null)
            {
                ambientSound.Stop();
                ambientSound.Dispose();
                ambientSound = null;
                SpawnParticles = false;
            }
        }
    }
}