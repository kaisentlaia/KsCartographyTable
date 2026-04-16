using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Kaisentlaia.CartographyTable.Blocks
{
    internal class BlockAdvancedCartographyTablePart : Block
    {
        // Cache the selection boxes since they don't change
        private Cuboidf[] selectionBoxes;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            
            // Build selection boxes from JSON data
            var boxes = new List<Cuboidf>();
            if (CollisionBoxes != null)
            {
                foreach (var box in CollisionBoxes)
                {
                    boxes.Add(box);
                }
            }
            selectionBoxes = boxes.ToArray();
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

        /// <summary>
        /// Finds the parent Advanced Cartography Table block position based on this part block's position and neighboring blocks
        /// </summary>
        private BlockPos GetParentPosition(IWorldAccessor world, BlockPos pos)
        {
            // Check all 4 horizontal neighbors to find the main table block
            BlockPos[] neighbors = new[]
            {
                pos.WestCopy(),  // West neighbor
                pos.EastCopy(),  // East neighbor
                pos.NorthCopy(), // North neighbor
                pos.SouthCopy()  // South neighbor
            };

            foreach (BlockPos neighborPos in neighbors)
            {
                Block block = world.BlockAccessor.GetBlock(neighborPos);
                if (block.Code.Path.StartsWith("advancedcartographytable-") && 
                    !block.Code.Path.Contains("part"))
                {
                    return neighborPos;
                }
            }

            return null;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockPos parentPos = GetParentPosition(world, blockSel.Position);
            if (parentPos == null) return base.OnBlockInteractStart(world, byPlayer, blockSel);

            Block parentBlock = world.BlockAccessor.GetBlock(parentPos);
            
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
                Position = parentPos,
                Face = blockSel.Face,
                HitPosition = blockSel.HitPosition,
                SelectionBoxIndex = parentBoxIndex
            };

            return parentBlock.OnBlockInteractStart(world, byPlayer, parentSel);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            // When the part is broken, break the parent block instead (which will handle drops and remove this part)
            BlockPos parentPos = GetParentPosition(world, pos);
            if (parentPos != null)
            {
                Block parentBlock = world.BlockAccessor.GetBlock(parentPos);
                parentBlock.OnBlockBroken(world, parentPos, byPlayer, dropQuantityMultiplier);
            }
            else
            {
                base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
            }
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            // Drops come from the parent block when broken via OnBlockBroken
            // Return empty if broken directly (shouldn't happen normally)
            return System.Array.Empty<ItemStack>();
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            BlockPos parentPos = GetParentPosition(world, selection.Position);
            if (parentPos == null) return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);

            Block parentBlock = world.BlockAccessor.GetBlock(parentPos);
            
            // Map selection box index for proper interaction help
            int parentBoxIndex = selection.SelectionBoxIndex switch
            {
                0 => 0, // Table base
                1 => 2, // Disabled ink -> Book
                2 => 2, // Book -> Book
                _ => 0
            };

            BlockSelection parentSel = new BlockSelection
            {
                Position = parentPos,
                Face = selection.Face,
                HitPosition = selection.HitPosition,
                SelectionBoxIndex = parentBoxIndex
            };

            return parentBlock.GetPlacedBlockInteractionHelp(world, parentSel, forPlayer);
        }
    }
}