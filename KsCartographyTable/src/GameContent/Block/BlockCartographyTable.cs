using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using Kaisentlaia.KsCartographyTableMod.API.Utils;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Config;
using System.Linq;
using System.Collections.Generic;
using Vintagestory.API.Server;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
    public class BlockCartographyTable : Block
    {
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

        public override BlockEntityCartographyTable GetBlockEntity<BlockEntityCartographyTable>(BlockPos position)
        {
            return base.GetBlockEntity<BlockEntityCartographyTable>(position);
        }

        private bool OnInstantInteractionTakeQuill(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityCartographyTable beTable) {
            if (Variant["state"] != "filled") return false;
            ItemStack stack = new(world.GetItem(new AssetLocation(CartographyTableConstants.MOD_ID+":"+CartographyTableConstants.QUILL_ITEM_CODE)));
            if (byPlayer.InventoryManager.TryGiveItemstack(stack, true))
            {
                Block emptyBlock = world.GetBlock(CodeWithVariant("state", "empty"));
                world.BlockAccessor.ExchangeBlock(emptyBlock.BlockId, blockSel.Position);

                if (Sounds?.Place != null)
                {
                    world.PlaySoundAt(Sounds.Place, blockSel.Position, 0.1, byPlayer);
                }

                return true;
            }
            return false;
        }

        private bool OnInstantInteractionPutQuill(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityCartographyTable beTable) {
            if (Variant["state"] != "empty") return false;
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

        private bool OnBusyError(IPlayer byPlayer)
        {
            KsCartographyTableModSystem.DebugLog(api, $"OnBusyError {api.Side}");
            if (api.Side == EnumAppSide.Client)
            {
                (api as ICoreClientAPI).TriggerIngameError(byPlayer as IClientPlayer, "mapfailure", Lang.Get(CartographyTableLangCodes.FAILURE_BUSY));
            }
            return false;
        }

        private bool OnTimedInteractionWipeStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityCartographyTable beTable)
        {
            if (api.Side == EnumAppSide.Client)
            {
                if (beTable.IsBusy() && beTable.HasAnotherPlayerInteracting(byPlayer))
                {
                    return OnBusyError(byPlayer);
                }
                if (beTable.Map.Empty)
                {
                    KsCartographyTableModSystem.ShowChatMessage(api, byPlayer, Lang.Get(CartographyTableLangCodes.TABLE_MAP_ALREADY_EMPTY));
                }
                else
                {
                    KsCartographyTableModSystem.ShowChatMessage(api, byPlayer, Lang.Get(CartographyTableLangCodes.WIPE_STARTED));
                    beTable.SetWiping(true);
                }
            }
            return true;
        }

        private bool OnTimedInteractionPonderStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityCartographyTable beTable)
        {
            if (api.Side == EnumAppSide.Client)
            {
                if (beTable.IsBusy() && beTable.HasAnotherPlayerInteracting(byPlayer))
                {
                    return OnBusyError(byPlayer);
                }
                beTable.SetPondering(true);
            }
            return true;
        }

        private bool OnTimedInteractionUploadStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityCartographyTable beTable)
        {
            if (beTable.IsBusy() && beTable.HasAnotherPlayerInteracting(byPlayer) && api.Side == EnumAppSide.Client)
            {
                return OnBusyError(byPlayer);
            }
            return beTable.OnCartographySessionStart(CartographyAction.UploadMap, world, byPlayer, blockSel);
        }

        private bool OnTimedInteractionDownloadStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityCartographyTable beTable)
        {
            if (beTable.IsBusy() && beTable.HasAnotherPlayerInteracting(byPlayer) && api.Side == EnumAppSide.Client)
            {
                return OnBusyError(byPlayer);
            }
            return beTable.OnCartographySessionStart(CartographyAction.DownloadMap, world, byPlayer, blockSel);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            CartographyAction currentAction = GetPerformedAction(world, byPlayer, blockSel);

            BlockEntityCartographyTable beTable = FindBlockEntity(world, blockSel.Position);
            if (beTable == null)
            {
                KsCartographyTableModSystem.DebugLog(api, $"OnBlockInteractStart cannot find blockEntity!");
                return false;
            }
            CartographyAction? recentAction = beTable.GetRecentInteraction(byPlayer);
            if (recentAction.HasValue)
            {
                KsCartographyTableModSystem.DebugLog(api, $"OnBlockInteractStart has recent action by player, returning true");
                beTable.RegisterInteraction(byPlayer, recentAction.Value);
                return true;
            }

            Dictionary<CartographyAction, System.Func<IWorldAccessor, IPlayer, BlockSelection, BlockEntityCartographyTable, bool>> interactionHandlers = new()
            {
                [CartographyAction.None] = (world, byPlayer, blockSel, beTable) => false,
                [CartographyAction.TakeQuill] = OnInstantInteractionTakeQuill,
                [CartographyAction.PutQuill] = OnInstantInteractionPutQuill,
                [CartographyAction.WipeTable] = OnTimedInteractionWipeStart,
                [CartographyAction.PonderMap] = OnTimedInteractionPonderStart,
                [CartographyAction.UploadMap] = OnTimedInteractionUploadStart,
                [CartographyAction.DownloadMap] = OnTimedInteractionDownloadStart,
            };

            bool canStart = interactionHandlers.TryGetValue(currentAction, out var handler) ? handler(world, byPlayer, blockSel, beTable) : base.OnBlockInteractStart(world, byPlayer, blockSel);

            KsCartographyTableModSystem.DebugLog(api, $"OnBlockInteractStart {currentAction}, {canStart}");

            if (canStart && currentAction != CartographyAction.None)
            {
                beTable.RegisterInteraction(byPlayer, currentAction);
            }
            
            return canStart;
        }

        private bool OnTimedInteractionWipeStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityCartographyTable beTable)
        {
            if (secondsUsed >= 3 && !beTable.Map.Empty)
            {
                beTable.OnWipeTableMap(byPlayer);
            }
            return true;
        }

        private bool OnTimedInteractionPonderStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityCartographyTable beTable)
        {
            if (secondsUsed >= 3 && !beTable.Map.Empty)
            {
                beTable.OnPonderMap(byPlayer);
            }
            return true;
        }

        private bool OnTimedInteractionUploadStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityCartographyTable beTable)
        {
            return beTable.OnCartographySessionStep(CartographyAction.UploadMap, secondsUsed, world, byPlayer, blockSel);
        }
        
        private bool OnTimedInteractionDownloadStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityCartographyTable beTable)
        {
            return beTable.OnCartographySessionStep(CartographyAction.DownloadMap, secondsUsed, world, byPlayer, blockSel);
        }
        
        private bool OnTimedInteractionInvalidStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityCartographyTable beTable)
        {
            beTable.SetIdle(byPlayer, this);
            return false;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            CartographyAction currentAction = GetPerformedAction(world, byPlayer, blockSel);
            BlockEntityCartographyTable beTable = FindBlockEntity(world, blockSel.Position);
            if (beTable == null)
            {
                api.Logger.Error($"{CartographyTableConstants.MAP_EVENT} OnBlockInteractStep cannot find blockEntity!");
                return false;
            }
            if (currentAction != CartographyAction.None)
            {
                beTable.RegisterInteraction(byPlayer, currentAction);
            }
            Dictionary<CartographyAction, System.Func<float, IWorldAccessor, IPlayer, BlockSelection, BlockEntityCartographyTable, bool>> interactionHandlers = new()
            {
                [CartographyAction.None] = OnTimedInteractionInvalidStep,
                [CartographyAction.TakeQuill] = OnTimedInteractionInvalidStep,
                [CartographyAction.PutQuill] = OnTimedInteractionInvalidStep,
                [CartographyAction.WipeTable] = OnTimedInteractionWipeStep,
                [CartographyAction.PonderMap] = OnTimedInteractionPonderStep,
                [CartographyAction.UploadMap] = OnTimedInteractionUploadStep,
                [CartographyAction.DownloadMap] = OnTimedInteractionDownloadStep,
            };

            bool canContinue = interactionHandlers.TryGetValue(currentAction, out var handler) ? handler(secondsUsed, world, byPlayer, blockSel, beTable) : base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
            
            if (!canContinue)
            {
                KsCartographyTableModSystem.DebugLog(api, $"OnBlockInteractStep {currentAction}, {canContinue}");
            }
            
            return canContinue;
        }

        private void OnTimedInteractionWipeStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityCartographyTable beTable = FindBlockEntity(world, blockSel.Position);
            if (beTable != null && secondsUsed >= 3 && !beTable.Map.Empty)
            {
                beTable.OnWipeTableMap(byPlayer);
            }
        }

        private void OnTimedInteractionPonderStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityCartographyTable beTable = FindBlockEntity(world, blockSel.Position);
            if (beTable != null && secondsUsed >= 3 && !beTable.Map.Empty)
            {
                beTable.OnPonderMap(byPlayer);
            }
        }

        private void OnTimedInteractionUploadStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityCartographyTable beTable = FindBlockEntity(world, blockSel.Position);
            beTable?.OnCartographySessionStop(CartographyAction.UploadMap, world, byPlayer, blockSel);
        }
        
        private void OnTimedInteractionDownloadStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityCartographyTable beTable = FindBlockEntity(world, blockSel.Position);
            beTable?.OnCartographySessionStop(CartographyAction.DownloadMap, world, byPlayer, blockSel);
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            CartographyAction currentAction = GetPerformedAction(world, byPlayer, blockSel);
            Dictionary<CartographyAction, System.Action<float, IWorldAccessor, IPlayer, BlockSelection>> interactionHandlers = new()
            {
                [CartographyAction.None] = (secondsUsed, world, byPlayer, blockSel) => { },
                [CartographyAction.TakeQuill] = (secondsUsed, world, byPlayer, blockSel) => { },
                [CartographyAction.PutQuill] = (secondsUsed, world, byPlayer, blockSel) => { },
                [CartographyAction.WipeTable] = OnTimedInteractionWipeStop,
                [CartographyAction.PonderMap] = OnTimedInteractionPonderStop,
                [CartographyAction.UploadMap] = OnTimedInteractionUploadStop,
                [CartographyAction.DownloadMap] = OnTimedInteractionDownloadStop,
            };

            KsCartographyTableModSystem.DebugLog(api, $"OnBlockInteractStop {currentAction}");
            
            if (interactionHandlers.TryGetValue(currentAction, out var handler))
            {
                handler(secondsUsed, world, byPlayer, blockSel);
            }
            
            base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
        }

        private bool OnTimedInteractionWipeCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityCartographyTable beTable)
        {
            if (secondsUsed >= 3 && !beTable.Map.Empty)
            {
                beTable.OnWipeTableMap(byPlayer);
            }
            else
            {
                beTable.SetWiping(false);
            }
            return true;
        }

        private bool OnTimedInteractionPonderCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityCartographyTable beTable)
        {
            if (secondsUsed >= 3 && !beTable.Map.Empty)
            {
                beTable.OnPonderMap(byPlayer);
            }
            else
            {                
                beTable.SetPondering(false);
            }
            return true;
        }

        private bool OnTimedInteractionUploadCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityCartographyTable beTable)
        {
            beTable.OnCartographySessionStop(CartographyAction.UploadMap, world, byPlayer, blockSel);
            return true;
        }
        
        private bool OnTimedInteractionDownloadCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityCartographyTable beTable)
        {
            beTable.OnCartographySessionStop(CartographyAction.DownloadMap, world, byPlayer, blockSel);
            return true;
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            BlockEntityCartographyTable beTable = FindBlockEntity(world, blockSel.Position);
            if (beTable == null)
            {
                api.Logger.Error($"{CartographyTableConstants.MAP_EVENT} OnBlockInteractCancel cannot find blockEntity!");
                return true;
            }
            CartographyAction currentAction = GetPerformedAction(world, byPlayer, blockSel);
            CartographyAction action = beTable?.GetRecentInteraction(byPlayer) ?? CartographyAction.None;
            Block newSelectedBlock = byPlayer.CurrentBlockSelection?.Block;
            bool isAdvancedBlock = newSelectedBlock is BlockAdvancedCartographyTable || newSelectedBlock is BlockAdvancedCartographyTablePart;
            bool preserveSession = cancelReason == EnumItemUseCancelReason.MovedAway 
                && isAdvancedBlock 
                && (action == CartographyAction.UploadMap || action == CartographyAction.DownloadMap)
                && byPlayer.CurrentBlockSelection?.SelectionBoxIndex == CartographyTableSelectionBoxesEnum.MapArea;
            
            if (preserveSession)
            {
                // Refresh the timestamp so the companion Start will see its
                beTable.RegisterInteraction(byPlayer, action);
                KsCartographyTableModSystem.DebugLog(api, $"OnBlockInteractCancel preserving {action} session");
                return true;
            }
            beTable.ClearRecentInteraction(byPlayer);
            
            Dictionary<CartographyAction, System.Func<float, IWorldAccessor, IPlayer, BlockSelection, BlockEntityCartographyTable, bool>> interactionHandlers = new()
            {
                [CartographyAction.None] = (secondsUsed, world, byPlayer, blockSel, beTable) => true,
                [CartographyAction.TakeQuill] = (secondsUsed, world, byPlayer, blockSel, beTable) => true,
                [CartographyAction.PutQuill] = (secondsUsed, world, byPlayer, blockSel, beTable) => true,
                [CartographyAction.WipeTable] = OnTimedInteractionWipeCancel,
                [CartographyAction.PonderMap] = OnTimedInteractionPonderCancel,
                [CartographyAction.UploadMap] = OnTimedInteractionUploadCancel,
                [CartographyAction.DownloadMap] = OnTimedInteractionDownloadCancel,
            };

            bool canCancel = interactionHandlers.TryGetValue(currentAction, out var handler) ? handler(secondsUsed, world, byPlayer, blockSel, beTable) : base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);

            KsCartographyTableModSystem.DebugLog(api, $"OnBlockInteractCancel {currentAction}, {canCancel}, {cancelReason}");
            
            return true;
        }

        private bool IsCompanionBlock(BlockPos targetPos, BlockPos fromPos, IWorldAccessor world)
        {
            if (targetPos == null) return false;
            
            Block targetBlock = world.BlockAccessor.GetBlock(targetPos);
            Block fromBlock = world.BlockAccessor.GetBlock(fromPos);
            
            // From main to part
            if (fromBlock is BlockAdvancedCartographyTable advancedTable)
            {
                BlockPos expectedCompanion = advancedTable.GetCompanionPosition(fromPos);
                return targetPos.Equals(expectedCompanion) && targetBlock is BlockAdvancedCartographyTablePart;
            }
            
            // From part to main
            if (fromBlock is BlockAdvancedCartographyTablePart part)
            {
                part.EnsureParent(world, fromPos);
                return part.Parent?.Position != null && targetPos.Equals(part.Parent.Position) 
                    && targetBlock is BlockAdvancedCartographyTable;
            }
            
            return false;
        }

        private static CartographyAction GetPerformedAction(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
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
            if (beTable == null)
            {
                return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
            }
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

        public static bool IsWriting(IWorldAccessor world, BlockSelection blockSel)
        {
            BlockEntityCartographyTable beTable = FindBlockEntity(world, blockSel.Position);
            return beTable?.Map?.IsWriting ?? false;
        }
    }
}