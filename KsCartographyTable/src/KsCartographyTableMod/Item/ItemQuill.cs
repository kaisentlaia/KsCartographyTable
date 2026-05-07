
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
                Block block = api.World.BlockAccessor.GetBlock(pos);
                BlockEntity blockEntity = api.World.BlockAccessor.GetBlockEntity(pos);
                if (block is BlockCartographyTable && plr?.BlockSelection.SelectionBoxIndex == CartographyTableSelectionBoxesEnum.MapArea && blockEntity is BlockEntityCartographyTable && (blockEntity as BlockEntityCartographyTable).Map.HasWrittenData)
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
    }
}