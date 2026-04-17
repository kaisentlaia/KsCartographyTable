using Kaisentlaia.KsCartographyTableMod.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
    internal class Parent
    {
        public BlockPos Position;
        public Block Block;

        public Parent(IWorldAccessor world, BlockPos pos)
        {
            // Check all 4 horizontal neighbors to find the main table block
            BlockPos[] neighbors =
            [
                pos.WestCopy(),  // West neighbor
                pos.EastCopy(),  // East neighbor
                pos.NorthCopy(), // North neighbor
                pos.SouthCopy()  // South neighbor
            ];

            foreach (BlockPos neighborPos in neighbors)
            {
                Block block = world.BlockAccessor.GetBlock(neighborPos);
                if (block.Code.Path.StartsWith(CartographyTableConstants.ADVANCED_PREFIX) && 
                    !block.Code.Path.Contains(CartographyTableConstants.ADVANCED_PART_SUFFIX))
                {
                    Position = neighborPos;
                    Block = world.BlockAccessor.GetBlock(Position);
                    break;
                }
            }
        }

        public BlockSelection GetSelection(BlockSelection blockSel)
        {
            
            // Create a new BlockSelection for the parent block, preserving the hit position
            // Map the selection box index: part block box index -> parent block box index
            int parentBoxIndex = blockSel.SelectionBoxIndex switch
            {
                0 => 0, // Table base -> Table base
                1 => 2, // Disabled ink box on part -> Book box on parent (or adjust as needed)
                2 => 2, // Book -> Book
                _ => 0
            };

            BlockSelection parentSel = new BlockSelection
            {
                Position = Position,
                Face = blockSel.Face,
                HitPosition = blockSel.HitPosition,
                SelectionBoxIndex = parentBoxIndex
            };

            return parentSel;
        }
    }
    internal class BlockAdvancedCartographyTablePart : Block
    {
        public Parent Parent;

        public void EnsureParent(IWorldAccessor world, BlockPos pos)
        {
            if (Parent == null)
            {
                Parent = new Parent(world, pos);
            }
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, pos, byItemStack);
        }

        public override bool DoPartialSelection(IWorldAccessor world, BlockPos pos)
        {
            return true; // Essential for boxes outside 0-1 range
        }

        /// <summary>
        /// Override GetSelectionBoxes to return multiple boxes for this block
        /// </summary>
        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            // Return the collision boxes as selection boxes
            return CollisionBoxes ?? base.GetSelectionBoxes(blockAccessor, pos);
        }


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            EnsureParent(world, blockSel.Position);

            if (Parent == null || Parent.Block == null || Parent.Position == null) return base.OnBlockInteractStart(world, byPlayer, blockSel);

            return Parent.Block.OnBlockInteractStart(world, byPlayer, Parent.GetSelection(blockSel));
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            EnsureParent(world, pos);

            if (Parent != null && Parent.Block != null && Parent.Position != null)
            {
                Parent.Block.OnBlockBroken(world, Parent.Position, byPlayer, dropQuantityMultiplier);
                return;
            }
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            // Drops come from the parent block when broken via OnBlockBroken
            // Return empty if broken directly (shouldn't happen normally)
            return [];
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            EnsureParent(world, pos);

            if (Parent != null && Parent.Block != null && Parent.Position != null)
            {
                return Parent.Block.GetPlacedBlockInfo(world, Parent.Position, forPlayer);
            }
            
            return base.GetPlacedBlockInfo(world, pos, forPlayer);
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            EnsureParent(world, selection.Position);
        
            if (Parent != null && Parent.Block != null && Parent.Position != null)
            {
                return Parent.Block.GetPlacedBlockInteractionHelp(world, Parent.GetSelection(selection), forPlayer);
            }

            return [];
        }
    }
}