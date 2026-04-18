using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using Kaisentlaia.KsCartographyTableMod.API.Utils;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
    internal class BlockCartographyTable : Block
    {
        private InteractionCooldownManager interactionCooldownManager;

        private BlockInteractionRouter blockInteractionRouter;
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (api.Side != EnumAppSide.Client) return;
        }
        
        public override bool DoPartialSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }

        public static BlockEntityCartographyTable FindBlockEntity(IWorldAccessor world, BlockPos pos)
        {
            var entity = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityCartographyTable;
            if (entity != null) return entity;

            // TODO is this needed?
            // Secondary multiblock position — search adjacent blocks for the entity
            BlockPos[] adjacents = [
                pos.AddCopy(1, 0, 0), pos.AddCopy(-1, 0, 0),
                pos.AddCopy(0, 0, 1), pos.AddCopy(0, 0, -1)
            ];
            foreach (var adjacent in adjacents)
            {
                entity = world.BlockAccessor.GetBlockEntity(adjacent) as BlockEntityCartographyTable;
                if (entity != null) return entity;
            }
            return null;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (interactionCooldownManager == null)
            {
                interactionCooldownManager = new InteractionCooldownManager(api);
            }
            if (blockInteractionRouter == null)
            {
                blockInteractionRouter = new BlockInteractionRouter(interactionCooldownManager);
            }
            BlockEntityCartographyTable beTable = FindBlockEntity(world, blockSel.Position);
            if (blockInteractionRouter == null)
            {
                return base.OnBlockInteractStart(world, byPlayer, blockSel);
            }
            return blockInteractionRouter.TryRouteInteraction(
                byPlayer,
                blockSel,
                beTable,
                sel => beTable?.OnWipeTableMap(world, byPlayer, sel),
                sel => beTable?.OnPonderMap(world, byPlayer, sel),
                sel => beTable?.OnUpdatePlayerMap(world, byPlayer, sel),
                sel => beTable?.OnUpdateTableMap(world, byPlayer, sel)
            );
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return InteractionHelpProvider.GetHelpText(world, selection.SelectionBoxIndex).Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}