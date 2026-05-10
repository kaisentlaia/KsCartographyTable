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

        public bool IsAdvanced
        {
            get
            {
                return Block is BlockAdvancedCartographyTable || Block is BlockAdvancedCartographyTablePart;
            }
        }

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
                KsCartographyTableModSystem.ClientCartographyService.Ponder(byPlayer as IClientPlayer, this);
            }

            return true;
        }

        internal bool OnWipeTableMap(IPlayer byPlayer)
        {
            EnsureMap();
            if (Side == EnumAppSide.Server)
            {
                CoreServerAPI.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} OnWipeTableMap server");
                KsCartographyTableModSystem.ServerCartographyService.WipeTableMap(Block, byPlayer, this);
            }

            if (Side == EnumAppSide.Client)
            {
                CoreClientAPI.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} OnWipeTableMap client");
                CoreClientAPI.World.Player.TriggerFpAnimation(EnumHandInteract.BlockInteract);
            }

            MarkDirty();

            return true;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (!IsAdvanced && Map != null && Map.WaypointCount > 0)
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
            Map?.Serialize(tree);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            EnsureMap();
            bool wasWriting = Map.IsWriting;
            Map.Deserialize(tree);

            if (wasWriting != Map.IsWriting && Side == EnumAppSide.Client)
            {
                UpdateWritingSoundState(Map.IsWriting);
            }
        }
        
        public void UpdateMapExploredAreasIds(List<FastVec2i> piecesIds)
        {
            EnsureMap();
            Map.ExploredAreasIds = [.. piecesIds.Select(pieceId => pieceId.ToChunkIndex())];
            Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} UpdateMapExploredAreasIds {Map.ExploredAreasIds.Count}");
            MarkDirty();
        }

        internal void UpdateMapWaypointCount(int waypointCount)
        {
            EnsureMap();
            Map.WaypointCount = waypointCount;
            Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} UpdateMapWaypointCount {Map.WaypointCount}");
            MarkDirty();
        }

        internal void SetPalantirWaypointPositions(List<Vec3d> palantirWaypoints)
        {
            EnsureMap();
            Map.PalantirWaypoints = palantirWaypoints;
            Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} UpdatePalantirWaypoints {Map.PalantirWaypoints.Count}");
            MarkDirty();
        }

        internal DateTime GetPlayerLastDownload(IPlayer forPlayer)
        {
            EnsureMap();
            return Map.GetPlayerLastSync(forPlayer);
        }

        internal void SetPlayerSyncToNow(IPlayer player)
        {
            EnsureMap();
            Map.SetPlayerLastSync(player);
            MarkDirty();
        }

        internal bool OnCartographySessionStart(CartographyAction action)
        {
            if (action == CartographyAction.UploadMap && Side == EnumAppSide.Client)
            {
                return true;
            }
            if (action == CartographyAction.DownloadMap)
            {
                return true;
            }
            return false;
        }

        internal bool OnCartographySessionStep(CartographyAction action, float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (action == CartographyAction.UploadMap)
            {
                if (Side == EnumAppSide.Client)
                {
                    if ((int)(secondsUsed * 20) % 4 == 0)
                    {
                        SpawnWritingParticles(world);
                    }
                    bool hasSession = KsCartographyTableModSystem.ClientCartographyService.HasCartographyUploadSession(byPlayer, Block);
                    if (!hasSession && secondsUsed > 0.25)
                    {
                        KsCartographyTableModSystem.ClientCartographyService.StartCartographyUploadSession(action, world, byPlayer, blockSel.Position, Block, this);
                    }
                    else if (hasSession)
                    {                        
                        KsCartographyTableModSystem.ClientCartographyService.ContinueCartographyUploadSession(byPlayer, secondsUsed, Block);
                    }
                    if (!Map.IsWriting && hasSession)
                    {
                        UpdateWritingSoundState(false);
                    }
                }
                // always return true even when the session is complete to keep the interaction going until stopped by the player
                return true;
            }
            if (action == CartographyAction.DownloadMap)
            {
                if (Side == EnumAppSide.Server)
                {
                    bool hasSession = KsCartographyTableModSystem.ServerCartographyService.HasCartographyDownloadSession(byPlayer, Block);
                    if (!hasSession && secondsUsed > 0.25)
                    {
                        KsCartographyTableModSystem.ServerCartographyService.StartCartographyDownloadSession(action,  world, byPlayer, Block, blockSel.Position, this);
                    }
                    else if (hasSession)
                    {
                        KsCartographyTableModSystem.ServerCartographyService.ContinueCartographyDownloadSession(byPlayer, secondsUsed, Block, this);
                    }
                }
                else
                {
                    if ((int)(secondsUsed * 20) % 4 == 0)
                    {
                        SpawnWritingParticles(world);
                    }
                }
                // always return true even when the session is complete to keep the interaction going until stopped by the player
                return true;
            }
            return false;
        }

        internal void OnCartographySessionStop(CartographyAction action, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (action == CartographyAction.UploadMap && Side == EnumAppSide.Client)
            {
                KsCartographyTableModSystem.ClientCartographyService.EndCartographyUploadSession(byPlayer, Block, this);
                if (!Map.IsWriting)
                {
                    UpdateWritingSoundState(false);
                }
            }
            if (action == CartographyAction.DownloadMap && Side == EnumAppSide.Server)
            {
                KsCartographyTableModSystem.ServerCartographyService.EndCartographyDownloadSession(byPlayer, world.BlockAccessor.GetBlock(blockSel.Position), this);
            }
            if (action == CartographyAction.DownloadMap && Side == EnumAppSide.Client)
            {
                if (!Map.IsWriting)
                {
                    UpdateWritingSoundState(false);
                }
            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} OnReceivedServerPacket {data} {Side}");
            base.OnReceivedServerPacket(packetid, data);
        }
        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} OnReceivedClientPacket {data} {Side}");
            base.OnReceivedClientPacket(fromPlayer, packetid, data);
        }

        public void SetWriting(bool writing)
        {
            bool nowWriting = writing;

            if (nowWriting != Map.IsWriting)
            {
                Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} SetWriting {writing} {Side}");
                UpdateWritingSoundState(nowWriting);

                Map.IsWriting = nowWriting;

                if (Api.Side == EnumAppSide.Server)
                {
                    MarkDirty();
                }
            }
        }

        bool beforeWiping;
        public void SetWiping(bool wiping)
        {
            bool nowWiping = wiping;

            if (nowWiping != beforeWiping)
            {
                UpdateWipingSoundState(nowWiping);

                beforeWiping = nowWiping;

                if (Api.Side == EnumAppSide.Server)
                {
                    MarkDirty();
                }
            }
        }

        public void SetWritten(bool written)
        {
            Map.HasWrittenData = written;
            Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} SetWritten {written} {Side}");

            if (Api.Side == EnumAppSide.Server)
            {
                MarkDirty();
            }
        }

        private void UpdateWritingSoundState(bool nowWriting)
        {
            if (nowWriting) {
                StartWritingSoundAndParticles();
                return;
            }

            Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} UpdateWritingSoundState data sent {Map.HasWrittenData} {Side}");
            StopWritingSoundAndParticles(Map.HasWrittenData ? EnumCartographyTableCloseSoundTypes.SomethingWritten : EnumCartographyTableCloseSoundTypes.NothingWritten);
        }

        private void UpdateWipingSoundState(bool nowWiping)
        {
            if (nowWiping) StartWipingSoundAndParticles();
            else StopWipingSoundAndParticles();
        }

        private void StartWritingSoundAndParticles()
        {
            if (ambientSound == null && Side == EnumAppSide.Client)
            {
                // BUG the sound doesn't seem to play on download sessions on clients that are running on a different machine from the server
                // when the server is on the same machine as the client (es. I'm playing and hosting the game) the sound works perfectly.
                ambientSound = (Api as ICoreClientAPI).World.LoadSound(new SoundParams()
                {
                    Location = new AssetLocation(!IsAdvanced ? "game:sounds/effect/writing" : CartographyTableConstants.MOD_ID + ":sounds/effect/mapwriting"),
                    ShouldLoop = true,
                    Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 1f
                });

                ambientSound.Start();
                // TODO fix particles size and collision with book before reenabling them
                // SpawnParticles = true;
            }
        }

        private void StartWipingSoundAndParticles()
        {
            if (ambientSound == null && Side == EnumAppSide.Client)
            {
                ambientSound = (Api as ICoreClientAPI).World.LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("game:sounds/player/scrape"),
                    ShouldLoop = true,
                    Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 1f
                });

                ambientSound.Start();
                // TODO fix particles size and collision with book before reenabling them
                // SpawnParticles = true;
            }
        }

        private void SpawnWritingParticles(IWorldAccessor world)
        {
            if (SpawnParticles && Side == EnumAppSide.Client)
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

                if (!IsAdvanced)
                {
                    PaperDustParticles.MinPos.Set(Pos.X - 1 / 32f, Pos.Y + 16 / 16f, Pos.Z - 1 / 32f);
                }
                else
                {
                    InkParticles.MinPos.Set(Pos.X - 10 / 16f, Pos.Y + 18 / 16f, Pos.Z - 1 / 32f);
                    PaperDustParticles.MinPos.Set(Pos.X - 10 / 16f, Pos.Y + 18 / 16f, Pos.Z - 1 / 32f);
                }
                world.SpawnParticles(InkParticles);
                world.SpawnParticles(PaperDustParticles);
            }
        }

        public void SpawnWipingParticles(IWorldAccessor world)
        {
            if (SpawnParticles && Side == EnumAppSide.Client)
            {
                PaperDustParticles.AddQuantity = 1;
                PaperDustParticles.MinQuantity = 2;

                if (!IsAdvanced)
                {
                    PaperDustParticles.MinPos.Set(Pos.X - 1 / 32f, Pos.Y + 16 / 16f, Pos.Z - 1 / 32f);
                }
                else
                {
                    PaperDustParticles.MinPos.Set(Pos.X - 10 / 16f, Pos.Y + 18 / 16f, Pos.Z - 1 / 32f);
                }
                world.SpawnParticles(PaperDustParticles);
            }
        }
        public void StopWipingSoundAndParticles()
        {
            if (ambientSound != null)
            {
                ambientSound.Stop();
                ambientSound.Dispose();
                ambientSound = null;
                // TODO fix particles size and collision with book before reenabling them
                // SpawnParticles = false;
            }
        }

        public void StopWritingSoundAndParticles(EnumCartographyTableCloseSoundTypes soundType)
        {
            if (ambientSound != null)
            {
                ambientSound.Stop();
                ambientSound.Dispose();
                ambientSound = null;
                // TODO fix particles size and collision with book before reenabling them
                // SpawnParticles = false;
                AssetLocation location = null;
                if (soundType == EnumCartographyTableCloseSoundTypes.NothingWritten)
                {
                    Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} playing map close sound (nothing written) {Side}");
                    location = new AssetLocation(!IsAdvanced ? "game:sounds/held/bookturn1" : CartographyTableConstants.MOD_ID + ":sounds/effect/mapclose");
                }
                else if (soundType == EnumCartographyTableCloseSoundTypes.SomethingWritten)
                {
                    Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} playing {(!IsAdvanced ? "written sound":"written and close sound")} {Side}");            
                    location = new AssetLocation(!IsAdvanced ? "game:sounds/effect/writing" : CartographyTableConstants.MOD_ID + ":sounds/effect/mapclose");
                }

                if (location != null)
                {
                    // One last sound to confirm session is complete, for when the session doesn't last long enough and the sound doesn't have enough time to play
                    ILoadedSound finalAmbientSound = (Api as ICoreClientAPI).World.LoadSound(new SoundParams()
                    {
                        Location = location,
                        ShouldLoop = false,
                        Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                        DisposeOnFinish = true,
                        Volume = 1f
                    });

                    finalAmbientSound.Start();
                }
            }

            Map.HasWrittenData = false;
            Map.IsWriting = false;

            if (Side == EnumAppSide.Server)
            {
                MarkDirty();
            }
        }
    }
}