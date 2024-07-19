//Here are the imports for this script. Most of these will add automatically.
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using System.Collections.Generic;
using Kaisentlaia.CartographyTable.BlockEntities;

/*
* The namespace the class will be in. This is essentially the folder the script is found in.
* If you need to use the BlockCartographyTable class in any other script, you will have to add 'using VSTutorial.Blocks' to that script.
*/
namespace Kaisentlaia.CartographyTable.Blocks
{
    /*
    * The class definition. Here, you define BlockCartographyTable as a child of Block, which
    * means you can 'override' many of the functions within the general Block class. 
    */
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