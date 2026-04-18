using System;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Kaisentlaia.KsCartographyTableMod.GameContent;
using Vintagestory.API.Common;

namespace Kaisentlaia.KsCartographyTableMod.API.Utils
{
	public class BlockInteractionRouterService
	{
		private InteractionCooldownService cooldownManager;

		public BlockInteractionRouterService(InteractionCooldownService cooldownManager)
		{
			this.cooldownManager = cooldownManager;
		}

		public bool TryRouteInteraction(
			IPlayer byPlayer,
			BlockSelection blockSel,
			BlockEntityCartographyTable beTable,
			Action<BlockSelection> onWipeMap,
			Action<BlockSelection> onPalantir,
			Action<BlockSelection> onUpdatePlayerMap,
			Action<BlockSelection> onUpdateTableMap)
		{
			if (!cooldownManager.CanInteract(byPlayer))
				return false;

			if (beTable == null)
				return false;

			// Box 2: Map area - wipe with resin
			if (blockSel.SelectionBoxIndex == CartographyTableSelectionBoxesEnum.MapArea && ItemDetectorService.HasItemInHand(byPlayer, "resin"))
			{
				onWipeMap(blockSel);
				return true;
			}

			// Palantir interaction
			if (blockSel.SelectionBoxIndex == CartographyTableSelectionBoxesEnum.MapArea && ItemDetectorService.HasItemInHand(byPlayer, CartographyTableConstants.PALANTIR_BLOCK_CODE))
			{
				onPalantir(blockSel);
				return true;
			}

			// Box 1: Ink and quill interaction
			if (blockSel.SelectionBoxIndex == CartographyTableSelectionBoxesEnum.InkAndQuill && ItemDetectorService.HasEmptyHand(byPlayer))
			{
				if (byPlayer.Entity.Controls.Sprint)
				{
					onUpdatePlayerMap(blockSel);
				}
				else
				{
					onUpdateTableMap(blockSel);
				}
				return true;
			}

			return false;
		}
	}
}