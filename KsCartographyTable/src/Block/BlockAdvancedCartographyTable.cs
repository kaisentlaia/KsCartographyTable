using System.Collections.Generic;
using Kaisentlaia.CartographyTable.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Kaisentlaia.CartographyTable.Blocks
{
    internal class BlockAdvancedCartographyTable : BlockCartographyTable
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            interactions = ObjectCacheUtil.GetOrCreate(api, "advancedCartographyTableBlockInteractions", () =>
                {

                    List<ItemStack> resinStackList = new List<ItemStack>();
                    var resin = api.World.Collectibles.Find(obj => obj.FirstCodePart() == "resin");
                    if (resin != null)
                    {
                        List<ItemStack> stacks = resin.GetHandBookStacks(capi);
                        if (stacks != null) resinStackList.AddRange(stacks);
                    }

                    return new WorldInteraction[]
                    {
                        new WorldInteraction()
                        {
                            ActionLangCode = "kscartographytable:blockhelp-cartography-table-share-map",
                            HotKeyCode = null,
                            MouseButton = EnumMouseButton.Right
                        },
                        new WorldInteraction()
                        {
                            ActionLangCode = "kscartographytable:blockhelp-cartography-table-update-map",
                            HotKeyCode = "sprint",
                            MouseButton = EnumMouseButton.Right
                        },
                        new WorldInteraction()
                        {
                            ActionLangCode = "kscartographytable:blockhelp-cartography-table-wipe-map",
                            HotKeyCode = null,
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = resinStackList.ToArray()
                        }
                    };
                }
            );
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            // Prevent multiple rapid interactions
            if (!CanInteract(byPlayer))
            {
                return true; // Block the interaction but return true to prevent other handlers
            }

            BlockEntityCartographyTable BlockEntityCartographyTable = FindBlockEntity(world, blockSel.Position);

            if (BlockEntityCartographyTable != null) {
                ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

                // Wipe table map with resin
                if (slot?.Itemstack != null && slot.Itemstack.Collectible.FirstCodePart() == "resin") 
                {
                    return BlockEntityCartographyTable.OnWipeTableMap(world, byPlayer, blockSel);
                }
                // Update cartography table or player map
                else
                {      
                    return BlockEntityCartographyTable.OnPlayerInteract(world, byPlayer, blockSel);
                }
            }
            
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}