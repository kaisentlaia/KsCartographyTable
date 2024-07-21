using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using System.Collections.Generic;
using Kaisentlaia.CartographyTable.BlockEntities;


namespace Kaisentlaia.CartographyTable.Blocks
{
    internal class BlockCartographyTable : Block
    {
        WorldInteraction[] interactions;
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            interactions = ObjectCacheUtil.GetOrCreate(api, "cartographyTableBlockInteractions", () =>
                {
                    List<ItemStack> inkAndQuillStackList = new List<ItemStack>();
                    var inkAndQuill = api.World.Collectibles.Find(obj => obj.FirstCodePart() == "inkandquill");
                    if(inkAndQuill != null) {
                        List<ItemStack> stacks = inkAndQuill.GetHandBookStacks(capi);
                        if (stacks != null) inkAndQuillStackList.AddRange(stacks);
                    }
                    return new WorldInteraction[]
                    {
                        new WorldInteraction()
                        {
                            ActionLangCode = "kscartographytable:blockhelp-cartography-table-share-map",
                            HotKeyCode = null,
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = inkAndQuillStackList.ToArray()
                        },
                        new WorldInteraction()
                        {
                            ActionLangCode = "kscartographytable:blockhelp-cartography-table-update-map",
                            HotKeyCode = "sprint",                        
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = inkAndQuillStackList.ToArray()
                        },
                    };
                }
            );

        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            // TODO prevent multiple consequent executions
            BlockEntityCartographyTable BlockEntityCartographyTable = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityCartographyTable;

            if (BlockEntityCartographyTable != null) {
                ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

                if (slot.Itemstack != null && slot.Itemstack.Collectible.FirstCodePart() == "inkandquill" && !byPlayer.Entity.Controls.Sneak) {      
                    return BlockEntityCartographyTable.OnPlayerInteract(world, byPlayer, blockSel);
                } else if(byPlayer.Entity.LeftHandItemSlot?.Itemstack?.Collectible?.FirstCodePart() == "resin" && slot.Itemstack != null && slot.Itemstack.Collectible.FirstCodePart() == "parchment") {   
                    // TODO wipe table map  
                    return base.OnBlockInteractStart(world, byPlayer, blockSel);
                } else if(KsCartographyTableModSystem.purgeWpGroups) {
                    return BlockEntityCartographyTable.OnPurgeWaypointGroups(world, byPlayer, blockSel);
                }
                return base.OnBlockInteractStart(world, byPlayer, blockSel);
            }
            
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}