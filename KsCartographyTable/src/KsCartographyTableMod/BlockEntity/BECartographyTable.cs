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
        EnumCartographyTableCloseSoundTypes finalSoundType = EnumCartographyTableCloseSoundTypes.None;
        static SimpleParticleProperties InkParticles;
        static SimpleParticleProperties PaperDustParticles;
        protected ILoadedSound ambientSound;
        protected bool SpawnParticles = false;
        private ICoreServerAPI CoreServerAPI;
        private ICoreClientAPI CoreClientAPI;
        public EnumAppSide Side;
        public CartographyMap Map;

        public enum EnumCartographyTableCloseSoundTypes
        {
            NothingWritten,
            SomethingWritten,
            Unknown,
            None
        }

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
            if (Side == EnumAppSide.Server)
            {
                CoreServerAPI = Api as ICoreServerAPI;
            }
            if (Side == EnumAppSide.Client)
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
            if (CoreServerAPI != null)
            {
                KsCartographyTableModSystem.ServerCartographyService.WipeTableMap(Block, byPlayer, blockPos);
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
            Map.ExploredAreasIds = [.. piecesIds.Select(pieceId => pieceId.ToChunkIndex())];
            MarkDirty();
        }

        internal void UpdateMapWaypointCount(int waypointCount)
        {
            EnsureMap();
            Map.WaypointCount = waypointCount;
            MarkDirty();
        }

        internal DateTime GetPlayerLastDownload(IPlayer forPlayer)
        {
            EnsureMap();
            return Map.GetPlayerLastDownload(forPlayer);
        }

        internal void SetPlayerLastDownload(IPlayer player)
        {
            EnsureMap();
            Map.SetPlayerLastDownload(player);
            MarkDirty();
        }

        internal void UpdateFinalSoundType(EnumCartographyTableCloseSoundTypes soundType)
        {
            if (Api.Side == EnumAppSide.Server)
            {
                finalSoundType = soundType;
                MarkDirty();
            }
        }

        internal bool OnCartographySessionStart(CartographyAction action, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (action == CartographyAction.UploadMap && CoreClientAPI != null)
            {
                bool uploadStarted = KsCartographyTableModSystem.ClientCartographyService.StartCartographyUploadSession(action, Map, world, byPlayer, blockSel);
                if (uploadStarted) {
                    StartSoundAndParticles();
                }
                return uploadStarted;
            }
            if (action == CartographyAction.DownloadMap)
            {
                if (CoreServerAPI != null)
                {
                    bool downloadStarted = KsCartographyTableModSystem.ServerCartographyService.StartCartographyDownloadSession(action, world, byPlayer, blockSel);
                    return downloadStarted;
                }
                else
                {
                    // sound needs to happen client side
                    StartSoundAndParticles();
                    return true;
                }
            }
            return false;
        }

        internal bool OnCartographySessionStep(CartographyAction action, float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (action == CartographyAction.UploadMap && Api.Side == EnumAppSide.Client)
            {
                if ((int)(secondsUsed * 20) % 4 == 0)
                {
                    SpawnMoreParticles(world);
                }
                return KsCartographyTableModSystem.ClientCartographyService.ContinueCartographyUploadSession(byPlayer, secondsUsed, blockSel.Block, this);
            }
            if (action == CartographyAction.DownloadMap)
            {
                if (CoreServerAPI != null)
                {
                    blockSel.Block = world.BlockAccessor.GetBlock(blockSel.Position); 
                    return KsCartographyTableModSystem.ServerCartographyService.ContinueCartographyDownloadSession(byPlayer, secondsUsed, blockSel.Block, this);
                }
                else
                {
                    if ((int)(secondsUsed * 20) % 4 == 0)
                    {
                        SpawnMoreParticles(world);
                    }
                    return true;
                }
            }
            return false;
        }

        internal void OnCartographySessionStop(CartographyAction action, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (action == CartographyAction.UploadMap && Api.Side == EnumAppSide.Client)
            {
                KsCartographyTableModSystem.ClientCartographyService.EndCartographyUploadSession(byPlayer, blockSel.Block);
            }
            if (action == CartographyAction.DownloadMap)
            {
                if (Api.Side == EnumAppSide.Server)
                {
                    KsCartographyTableModSystem.ServerCartographyService.EndCartographyDownloadSession(world, byPlayer, world.BlockAccessor.GetBlock(blockSel.Position));
                }               
            }
            StopSoundAndParticles(EnumCartographyTableCloseSoundTypes.Unknown);
        }
        public void StartSoundAndParticles()
        {
            if (ambientSound == null && Api?.Side == EnumAppSide.Client)
            {
                // TODO bugfix no sound on download sessions (server side)
                ambientSound = (Api as ICoreClientAPI).World.LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("kscartographytable:sounds/effect/mapwriting.ogg"),
                    ShouldLoop = true,
                    Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 0.75f
                });

                ambientSound.Start();
                // TODO fix particles size and collision with book before reenabling them
                // SpawnParticles = true;

                MarkDirty();
            }
        }

        private void SpawnMoreParticles(IWorldAccessor world)
        {
            if (SpawnParticles)
            {
                ItemStack inkColorStack = ItemDetectorService.GetItemStacks(world, "charcoal")[0];
                InkParticles.Color = InkParticles.Color = inkColorStack.Collectible.GetRandomColor(Api as ICoreClientAPI, inkColorStack);
                InkParticles.Color &= 0xffffff;
                InkParticles.Color |= (200 << 24);
                InkParticles.MinQuantity = 1;
                InkParticles.AddQuantity = 5;
                InkParticles.MinVelocity.Set(-0.1f, 0, -0.1f);
                InkParticles.AddVelocity.Set(0.2f, 0.2f, 0.2f);

                PaperDustParticles.AddQuantity = 1;
                PaperDustParticles.MinQuantity = 2;

                if (Block is not BlockAdvancedCartographyTable)
                {
                    InkParticles.MinPos.Set(Pos.X - 1 / 32f , Pos.Y + 16 / 16f, Pos.Z - 1 / 32f);
                    PaperDustParticles.MinPos.Set(Pos.X - 1 / 32f, Pos.Y + 16 / 16f, Pos.Z - 1 / 32f);
                }
                else
                {
                    InkParticles.MinPos.Set(Pos.X - 10 / 16f, Pos.Y + 18 / 16f, Pos.Z - 1 / 32f);
                    PaperDustParticles.MinPos.Set(Pos.X - 10 / 16f, Pos.Y + 18 / 16f, Pos.Z - 1 / 32f);
                }
                Api.World.SpawnParticles(InkParticles);
                Api.World.SpawnParticles(PaperDustParticles);
            }
        }

        public void StopSoundAndParticles(EnumCartographyTableCloseSoundTypes soundType)
        {
            if (ambientSound != null)
            {
                ambientSound.Stop();
                ambientSound.Dispose();
                ambientSound = null;
                // TODO fix particles size and collision with book before reenabling them
                // SpawnParticles = false;
                if (Api.Side == EnumAppSide.Client)
                {
                    AssetLocation location = null;
                    if (soundType == EnumCartographyTableCloseSoundTypes.NothingWritten || finalSoundType == EnumCartographyTableCloseSoundTypes.NothingWritten)
                    {
                        location = new AssetLocation("kscartographytable:sounds/effect/mapclose");
                    }
                    else if (soundType == EnumCartographyTableCloseSoundTypes.SomethingWritten || finalSoundType == EnumCartographyTableCloseSoundTypes.SomethingWritten)
                    {                        
                        location = new AssetLocation("kscartographytable:sounds/effect/mapwriteandclose");
                    }
                    else if (soundType != EnumCartographyTableCloseSoundTypes.None || finalSoundType != EnumCartographyTableCloseSoundTypes.None)
                    {
                        // fallback
                        location = new AssetLocation("game:sounds/held/bookclose1");
                    }

                    if (location != null)
                    {
                        // One last sound to confirm session is complete, for when the session doesn't last long enough so the sound doesn't really play
                        ILoadedSound finalAmbientSound = (Api as ICoreClientAPI).World.LoadSound(new SoundParams()
                        {
                            Location = location,
                            ShouldLoop = false,
                            Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                            DisposeOnFinish = true,
                            Volume = 0.75f
                        });

                        finalAmbientSound.Start();

                        finalSoundType = EnumCartographyTableCloseSoundTypes.None;
                    }
                }
                MarkDirty();
            }
        }
    }
}