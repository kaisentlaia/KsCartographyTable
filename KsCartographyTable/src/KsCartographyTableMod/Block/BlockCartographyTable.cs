using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using Kaisentlaia.KsCartographyTableMod.API.Utils;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Config;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
    public class BlockCartographyTable : Block
    {
        private CartographyAction currentAction = CartographyAction.None;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public override bool DoPartialSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }
        public bool Empty
        {
            get { return Variant["state"] == "empty"; }
        }

        public static BlockEntityCartographyTable FindBlockEntity(IWorldAccessor world, BlockPos pos)
        {
            if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCartographyTable entity)
            {
                return entity;
            }

            return null;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityCartographyTable beTable = FindBlockEntity(world, blockSel.Position);
            currentAction = GetPerformedAction(world, byPlayer, blockSel);
            if (currentAction == CartographyAction.None)
            {
                return false;
            }
            if (currentAction == CartographyAction.TakeQuill)
            {
                ItemStack stack = new ItemStack(world.GetItem(new AssetLocation(CartographyTableConstants.MOD_ID+":"+CartographyTableConstants.QUILL_ITEM_CODE)));
                if (byPlayer.InventoryManager.TryGiveItemstack(stack, true))
                {
                    Block filledBlock = world.GetBlock(CodeWithVariant("state", "empty"));
                    world.BlockAccessor.ExchangeBlock(filledBlock.BlockId, blockSel.Position);

                    if (Sounds?.Place != null)
                    {
                        world.PlaySoundAt(Sounds.Place, blockSel.Position, 0.1, byPlayer);
                    }

                    return true;
                }
                return false;
            }
            if (currentAction == CartographyAction.PutQuill)
            {
                ItemStack heldStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
                if (heldStack != null && heldStack.Collectible.Code.Path.Equals(CartographyTableConstants.QUILL_ITEM_CODE))
                {
                    byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
                    byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();

                    Block filledBlock = world.GetBlock(CodeWithVariant("state", "filled"));
                    world.BlockAccessor.ExchangeBlock(filledBlock.BlockId, blockSel.Position);

                    if (Sounds?.Place != null)
                    {
                        world.PlaySoundAt(Sounds.Place, blockSel.Position, 0.1, byPlayer);
                    }
                    return true;
                }
                return false;
            }
            if (currentAction == CartographyAction.WipeTable)
            {
                if (beTable.Map.Empty)
                {
                    if (api.Side == EnumAppSide.Client)
                    {
                        (api as ICoreClientAPI).ShowChatMessage(Lang.Get(CartographyTableLangCodes.TABLE_MAP_ALREADY_EMPTY));
                    }
                    return true;
                }
                if (api.Side == EnumAppSide.Client)
                {
                    (api as ICoreClientAPI).ShowChatMessage(Lang.Get(CartographyTableLangCodes.WIPE_STARTED));
                }
                return true;
            }
            if (currentAction != CartographyAction.PonderMap)
            {
                return beTable.OnCartographySessionStart(currentAction, world, byPlayer, blockSel);
            }
            return true;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (new[] { CartographyAction.None, CartographyAction.PonderMap }.Contains(currentAction))
            {
                return true;
            }
            BlockEntityCartographyTable beTable = FindBlockEntity(world, blockSel.Position);

            if (currentAction == CartographyAction.WipeTable)
            {
                // TODO play looping scraping sound
                if (secondsUsed >= 3 && !beTable.Map.Empty)
                {
                    beTable.OnWipeTableMap(byPlayer, blockSel.Position);
                }
                return true;
            }

            return beTable.OnCartographySessionStep(currentAction, secondsUsed, world, byPlayer, blockSel);
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (currentAction == CartographyAction.None)
            {
                return;
            }
            BlockEntityCartographyTable beTable = FindBlockEntity(world, blockSel.Position);

            if (currentAction == CartographyAction.WipeTable)
            {
                if (secondsUsed >= 3 && !beTable.Map.Empty)
                {
                    beTable.OnWipeTableMap(byPlayer, blockSel.Position);
                }

                // TODO stop looping scraping sound
                currentAction = CartographyAction.None;
                return;
            }

            if (currentAction == CartographyAction.PonderMap)
            {
                beTable.OnPonderMap(byPlayer);
                return;
            }
            beTable.OnCartographySessionStop(currentAction, world, byPlayer, blockSel);
            currentAction = CartographyAction.None;
        }

        private CartographyAction GetPerformedAction(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityCartographyTable beTable = FindBlockEntity(world, blockSel.Position);

            if (beTable == null)
            {
				return CartographyAction.None;
            }

            if (blockSel.SelectionBoxIndex == CartographyTableSelectionBoxesEnum.MapArea)
            {
                if (byPlayer?.InventoryManager.ActiveTool == EnumTool.Knife)
                {
                    return CartographyAction.WipeTable;
                }

                if (ItemDetectorService.HasItemInHand(byPlayer, CartographyTableConstants.QUILL_ITEM_CODE))
                {
                    return byPlayer.Entity.Controls.Sprint ? CartographyAction.DownloadMap : CartographyAction.UploadMap;
                }
                
                if (ItemDetectorService.HasItemInHand(byPlayer, CartographyTableConstants.PALANTIR_BLOCK_CODE))
                {
				    return CartographyAction.PonderMap;
                }
			}

			if (blockSel.SelectionBoxIndex == CartographyTableSelectionBoxesEnum.InkAndQuill && ItemDetectorService.HasEmptyHand(byPlayer))
			{
                return CartographyAction.TakeQuill;
			}

			if (blockSel.SelectionBoxIndex == CartographyTableSelectionBoxesEnum.InkAndQuill && ItemDetectorService.HasItemInHand(byPlayer, "quill"))
			{
                return CartographyAction.PutQuill;
			}

			return  CartographyAction.None;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            BlockEntityCartographyTable beTable = FindBlockEntity(world, selection.Position);
            return InteractionHelpProvider.GetHelpText(world, selection.SelectionBoxIndex, Empty, beTable.Map.Empty).Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
        
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            // Only cleanup if creative mode or no player (fire/explosion)
            bool shouldCleanup = byPlayer?.WorldData?.CurrentGameMode == EnumGameMode.Creative || byPlayer == null;

            if (shouldCleanup)
            {
                KsCartographyTableModSystem.ServerCartographyService?.CleanupMapData(this);
            }

            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            var drops = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
            
            if (drops.Length > 0)
            {
                var be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityCartographyTable;
                if (be != null)
                {
                    // Serialize BE data into a subtree
                    var beData = new TreeAttribute();
                    be.ToTreeAttributes(beData);
                    
                    // Store in item's attributes
                    drops[0].Attributes["BlockEntityCartographyData"] = beData;
                }
            }
            
            return drops;
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, pos, byItemStack);
            
            if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCartographyTable be)
            {
                if (byItemStack?.Attributes?["BlockEntityCartographyData"] is ITreeAttribute tree)
                {
                    be.FromTreeAttributes(tree, world);
                }
                else
                {
                    be.EnsureMap(); // Only create new map if no data to restore
                }
            }
        }
    }
}