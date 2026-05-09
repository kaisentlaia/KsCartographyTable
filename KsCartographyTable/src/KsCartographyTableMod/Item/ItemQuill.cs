
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
    public class ItemQuill : Item
    {
        public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
        {
            return GetMapWriteAnim(byEntity) ?? base.GetHeldTpHitAnimation(slot, byEntity);
        }
        public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
        {
            return GetMapWriteAnim(forEntity) ?? base.GetHeldTpUseAnimation(activeHotbarSlot, forEntity);
        }

        public string GetMapWriteAnim(Entity byEntity)
        {
            var plr = byEntity as EntityPlayer;
            var pos = plr?.BlockSelection?.Position;
            if (pos != null && (plr.Controls.HandUse != EnumHandInteract.None || plr.Controls.RightMouseDown))
            {
                Block selectedBlock = api.World.BlockAccessor.GetBlock(pos);
                Block block = selectedBlock is BlockAdvancedCartographyTablePart ? (selectedBlock as BlockAdvancedCartographyTablePart).Parent.Block : selectedBlock;
                BlockEntity blockEntity = api.World.BlockAccessor.GetBlockEntity(selectedBlock is BlockAdvancedCartographyTablePart ? (selectedBlock as BlockAdvancedCartographyTablePart).Parent.Position : pos);
                if (block is BlockCartographyTable && plr?.BlockSelection.SelectionBoxIndex == CartographyTableSelectionBoxesEnum.MapArea && blockEntity is BlockEntityCartographyTable && (blockEntity as BlockEntityCartographyTable).Map.IsWriting)
                {
                    return "clayform";
                }
            }
            return null;
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (blockSel == null) return;
            OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null) return false;
            return OnHeldAttackStep(secondsUsed, slot, byEntity, blockSel, entitySel);
        }
        
    }
}