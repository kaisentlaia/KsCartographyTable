using Kaisentlaia.KsCartographyTableMod.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
    public class BlockAdvancedCartographyTable : BlockCartographyTable
    {
        internal Vec3f candleWickPosition = new(0.1875f, 1.26f, 0.1875f);

        readonly Vec3f[] candleWickPositionsByRot = new Vec3f[4];

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            initRotations();
        }

        internal void initRotations()
        {
            for (int i = 0; i < 4; i++)
            {
                Matrixf m = new Matrixf();
                m.Translate(0.5f, 0.5f, 0.5f);
                m.RotateYDeg(-i * 90);  // ← Negated: clockwise to match VS block orientation
                m.Translate(-0.5f, -0.5f, -0.5f);

                Vec4f rotated = m.TransformVector(new Vec4f(candleWickPosition.X, candleWickPosition.Y, candleWickPosition.Z, 1));
                candleWickPositionsByRot[i] = new Vec3f(rotated.X, rotated.Y, rotated.Z);
            }
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            string side = GetPlayerFacingSide(byPlayer);
            BlockPos companionPos = GetCompanionPosition(blockSel.Position, side);
            
            world.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} TryPlaceBlock Block position: {blockSel.Position} companion position: {companionPos} side: {side}");

            Block blockAtMain = world.BlockAccessor.GetBlock(blockSel.Position);
            Block blockAtCompanion = world.BlockAccessor.GetBlock(companionPos);

            // Check main position
            if (!blockAtMain.IsReplacableBy(this))
            {
                world.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} Main position block isn't replaceable");
                failureCode = "notenoughspace";
                return false;
            }

            // Check companion position
            if (!blockAtCompanion.IsReplacableBy(this))
            {
                world.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} Companion position block isn't replaceable");
                failureCode = "notenoughspace";
                return false;
            }

            return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, pos, byItemStack);

            // Place companion block based on orientation
            BlockPos companionPos = GetCompanionPosition(pos);
            Block companionBlock = world.GetBlock(new AssetLocation(CartographyTableConstants.MOD_ID + ":" + CartographyTableConstants.ADVANCED_PREFIX + CartographyTableConstants.ADVANCED_PART_SUFFIX + LastCodePart()));

            if (companionBlock == null)
            {
                world.Logger.Error("Companion block for advanced cartography table not found");
                return;
            }
            world.BlockAccessor.SetBlock(companionBlock.BlockId, companionPos);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            // Remove companion block
            BlockPos companionPos = GetCompanionPosition(pos);
            if (world.BlockAccessor.GetBlock(companionPos).Code.Path == CartographyTableConstants.ADVANCED_PREFIX + CartographyTableConstants.ADVANCED_PART_SUFFIX + LastCodePart())
            {
                world.BlockAccessor.SetBlock(0, companionPos);
            }

            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        private string GetPlayerFacingSide(IPlayer byPlayer)
        {
            float yaw = byPlayer.Entity.Pos.Yaw;
            BlockFacing facing = BlockFacing.HorizontalFromYaw(yaw);
            return facing.ToString().ToLower();
        }

        private BlockPos GetCompanionPosition(BlockPos pos)
        {
            return GetCompanionPosition(pos, Variant["side"]);
        }

        // Core logic shared by both
        private BlockPos GetCompanionPosition(BlockPos pos, string side)
        {
            return side switch
            {
                "north" => pos.EastCopy(),
                "south" => pos.WestCopy(),
                "east" => pos.SouthCopy(),
                "west" => pos.NorthCopy(),
                _ => pos.EastCopy()
            };
        }

        public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
        {
            if (ParticleProperties != null && ParticleProperties.Length > 0)
            {
                string side = Variant["side"];
                int rotIndex = side switch
                {
                    "east" => 1,
                    "south" => 2,
                    "west" => 3,
                    _ => 0
                };

                Vec3f wickPos = candleWickPositionsByRot[rotIndex];

                for (int i = 0; i < ParticleProperties.Length; i++)
                {
                    AdvancedParticleProperties bps = ParticleProperties[i];
                    bps.WindAffectednesAtPos = windAffectednessAtPos;
                    bps.basePos.X = pos.X + wickPos.X;
                    bps.basePos.Y = pos.InternalY + wickPos.Y;
                    bps.basePos.Z = pos.Z + wickPos.Z;
                    manager.Spawn(bps);
                }
            }
        }
    }
}