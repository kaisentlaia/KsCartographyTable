using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using Kaisentlaia.KsCartographyTableMod.API.Utils;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
    // TODO when deleting the table close the connection and delete the db file
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
            if (entity != null) return entity;

            // TODO is this needed?
            // Secondary multiblock position — search adjacent blocks for the entity
            // BlockPos[] adjacents = [
            //     pos.AddCopy(1, 0, 0), pos.AddCopy(-1, 0, 0),
            //     pos.AddCopy(0, 0, 1), pos.AddCopy(0, 0, -1)
            // ];
            // foreach (var adjacent in adjacents)
            // {
            //     entity = world.BlockAccessor.GetBlockEntity(adjacent) as BlockEntityCartographyTable;
            //     if (entity != null) return entity;
            // }
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
    }
}