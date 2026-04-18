using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using Kaisentlaia.KsCartographyTableMod.API.Utils;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Vintagestory.API.Datastructures;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
    internal class BlockCartographyTable : Block
    {
        private BlockInteractionRouterService blockInteractionRouterService;
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            blockInteractionRouterService = new BlockInteractionRouterService(new InteractionCooldownService(api));

            if (api.Side != EnumAppSide.Client) return;
        }

        public override bool DoPartialSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }

        public static BlockEntityCartographyTable FindBlockEntity(IWorldAccessor world, BlockPos pos)
        {
            var entity = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityCartographyTable;
            if (entity != null) {
                return entity;
            }

            return null;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityCartographyTable beTable = FindBlockEntity(world, blockSel.Position);
            return blockInteractionRouterService.TryRouteInteraction(
                byPlayer,
                blockSel,
                beTable,
                sel => beTable?.OnWipeTableMap(byPlayer, blockSel.Position),
                sel => beTable?.OnPonderMap(byPlayer),
                sel => beTable?.OnUpdatePlayerMap(byPlayer, sel),
                sel => beTable?.OnUpdateTableMap(byPlayer, sel)
            );
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return InteractionHelpProvider.GetHelpText(world, selection.SelectionBoxIndex).Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
        
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            // Only cleanup if creative mode or no player (fire/explosion)
            bool shouldCleanup = byPlayer?.WorldData?.CurrentGameMode == EnumGameMode.Creative || byPlayer == null;

            if (shouldCleanup && world.Side == EnumAppSide.Server)
            {
                KsCartographyTableModSystem.ServerCartographyService.CleanupMapData(this, pos);
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