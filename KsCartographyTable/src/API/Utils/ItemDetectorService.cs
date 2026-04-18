using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Kaisentlaia.KsCartographyTableMod.API.Utils
{
  public class ItemDetectorService
  {
    public static bool HasItemInHand(IPlayer player, string codePart)
    {
      ItemStack itemStack = player?.InventoryManager?.ActiveHotbarSlot?.Itemstack;
      return itemStack?.Item?.Code?.Path.Contains(codePart) ?? false;
    }

    public static bool HasEmptyHand(IPlayer player)
    {
      return player?.InventoryManager?.ActiveHotbarSlot?.Empty ?? true;
    }

    public static ItemStack[] GetItemStacks(IWorldAccessor world, string codePart)
    {
      var ink = world.Collectibles.Find(obj => obj.FirstCodePart() == codePart);
      return ink?.GetHandBookStacks(world.Api as ICoreClientAPI)?.ToArray();
    }
  }
}