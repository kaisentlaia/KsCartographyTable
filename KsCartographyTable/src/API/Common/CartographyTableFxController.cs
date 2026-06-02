using System;
using Kaisentlaia.KsCartographyTableMod.API.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Kaisentlaia.KsCartographyTableMod.API.Common
{
    public class CartographyTableFxController
    {
        readonly SimpleParticleProperties InkParticles;
        readonly SimpleParticleProperties PaperDustParticles;
        private readonly ICoreClientAPI Api;
        private readonly BlockPos Pos;
        private readonly bool IsAdvanced;
        
        private ILoadedSound ambientSound;
        private ILoadedSound finalSound;
        
        private FxState currentState = FxState.Idle;
        private float stateTimer;
        private float particleAccumulator;
        
        private enum FxState { Idle, Wiping, Writing, Pondering }

        public CartographyTableFxController(ICoreClientAPI api, BlockPos pos, bool isAdvanced)
        {
            Api = api;
            Pos = pos;
            IsAdvanced = isAdvanced;

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
        
        // Called once per sync from server
        public void OnStateChanged(bool wasWiping, bool isWiping,
                                bool wasWriting, bool isWriting,
                                bool wasPondering, bool isPondering,
                                bool hasWrittenData)
        {
            FxState oldState = ToFxState(wasWiping, wasWriting, wasPondering);
            FxState newState = ToFxState(isWiping, isWriting, isPondering);
            
            if (oldState == newState) return;
            
            ExitState(oldState, hasWrittenData);
            EnterState(newState);
            currentState = newState;
            stateTimer = 0;
            particleAccumulator = 0;
        }
        
        // Called every client tick (50ms)
        public void Tick(float dt)
        {
            if (currentState == FxState.Idle) return;
            
            stateTimer += dt;
            particleAccumulator += dt;
            
            switch (currentState)
            {
                case FxState.Wiping:
                    if (particleAccumulator >= 0.2f)
                    {
                        // SpawnWipingParticles();
                        particleAccumulator = 0;
                    }
                    break;
                    
                case FxState.Writing:
                    if (particleAccumulator >= 0.2f)
                    {
                        // SpawnWritingParticles();
                        particleAccumulator = 0;
                    }
                    break;
                    
                case FxState.Pondering:
                    if (particleAccumulator >= 0.2f)
                    {
                        // SpawnPonderingParticles();
                        particleAccumulator = 0;
                    }
                    break;
            }
        }

        private void EnterState(FxState state)
        {
            switch (state)
            {
                case FxState.Wiping:
                    ambientSound = LoadSound("game:sounds/player/scrape", loop: true);
                    ambientSound?.Start();
                    break;
                    
                case FxState.Writing:
                    string path = !IsAdvanced ? "game:sounds/effect/writing" : CartographyTableConstants.MOD_ID + ":sounds/effect/mapwriting";
                    ambientSound = LoadSound(path, loop: true);
                    ambientSound?.Start();
                    break;
                    
                case FxState.Pondering:
                    // ambientSound = LoadSound(CartographyTableConstants.MOD_ID + ":sounds/effect/pondering", loop: true);
                    // ambientSound?.Start();
                    break;
            }
        }
        
        private void ExitState(FxState state, bool hasWrittenData)
        {
            // Always stop ambient sound
            if (ambientSound != null)
            {
                ambientSound.Stop();
                ambientSound.Dispose();
                ambientSound = null;
            }
            
            switch (state)
            {
                case FxState.Writing:
                    // Play close sound based on table type + whether data was written
                    string closeSound = (IsAdvanced, hasWrittenData) switch
                    {
                        (false, false) => "game:sounds/held/bookturn1",
                        (false, true)  => "game:sounds/effect/writing",
                        (true, false)  => CartographyTableConstants.MOD_ID + ":sounds/effect/mapclose",
                        (true, true)   => CartographyTableConstants.MOD_ID + ":sounds/effect/mapclose"
                    };
                    finalSound = LoadSound(closeSound, loop: false);
                    finalSound?.Start();
                    break;
                    
                case FxState.Pondering when stateTimer >= 3.0f:
                    // finalSound = LoadSound("mod:sounds/effect/pondercomplete", loop: false);
                    // finalSound?.Start();
                    break;
                    
                // Wipe: no final sound
                // Ponder canceled (<3s): no final sound
            }
        }
        
        private static FxState ToFxState(bool wiping, bool writing, bool pondering)
        {
            if (wiping) return FxState.Wiping;
            if (writing) return FxState.Writing;
            if (pondering) return FxState.Pondering;
            return FxState.Idle;
        }
        
        private ILoadedSound LoadSound(string path, bool loop)
        {
            return Api.World.LoadSound(new SoundParams
            {
                Location = new AssetLocation(path),
                ShouldLoop = loop,
                Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                DisposeOnFinish = !loop,
                Volume = 1f
            });
        }

        private void SpawnPonderingParticles()
        {
            throw new NotImplementedException();
        }

        private void SpawnWritingParticles()
        {
            // Api.World.SpawnCubeParticles(blockSel.Position.ToVec3d().Add(blockSel.HitPosition), slot.Itemstack, 0.25f, 1, 0.5f, byPlayer, new Vec3f(0, 1, 0));
            ItemStack inkColorStack = ItemDetectorService.GetItemStacks(Api.World, "charcoal")[0];
            InkParticles.Color = InkParticles.Color = inkColorStack.Collectible.GetRandomColor(Api as ICoreClientAPI, inkColorStack);
            InkParticles.Color &= 0xffffff;
            InkParticles.Color |= (200 << 24);
            InkParticles.MinQuantity = 1;
            InkParticles.AddQuantity = 5;
            InkParticles.MinVelocity.Set(-0.1f, 0, -0.1f);
            InkParticles.AddVelocity.Set(0.2f, 0.2f, 0.2f);       
            
            if (!IsAdvanced)
            {
                InkParticles.MinPos.Set(Pos.X - 1 / 32f, Pos.Y + 16 / 16f, Pos.Z - 1 / 32f);
            }
            else
            {
                InkParticles.MinPos.Set(Pos.X - 10 / 16f, Pos.Y + 18 / 16f, Pos.Z - 1 / 32f);
            }
            Api.World.SpawnParticles(InkParticles);
        }

        private void SpawnWipingParticles()
        {
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

            PaperDustParticles.MinPos.Set(Pos.X - 1 / 32f, Pos.Y + 16 / 16f, Pos.Z - 1 / 32f);
            Api.World.SpawnParticles(PaperDustParticles);
        }
    }
}