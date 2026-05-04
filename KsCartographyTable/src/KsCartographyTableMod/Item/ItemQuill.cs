
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
    public class ItemQuill : Item
    {
        public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
        {
            return getMapWriteAnim(forEntity) ?? base.GetHeldTpUseAnimation(activeHotbarSlot, forEntity);
        }

        public string getMapWriteAnim(Entity byEntity)
        {
            var plr = byEntity as EntityPlayer;
            var pos = plr?.BlockSelection?.Position;
            if (pos != null && (plr.Controls.HandUse != EnumHandInteract.None || plr.Controls.RightMouseDown))
            {
                Block block = api.World.BlockAccessor.GetBlock(pos);
                if (block is BlockCartographyTable && plr?.BlockSelection.SelectionBoxIndex == CartographyTableSelectionBoxesEnum.MapArea)
                {
                    return "clayform";
                }
            }
            return null;
        }
    }
}