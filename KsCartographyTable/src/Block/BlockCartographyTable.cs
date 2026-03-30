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
        private Dictionary<string, long> lastInteractionTimes = new Dictionary<string, long>();
        private const long InteractionCooldownMs = 500;
        private const long EntryExpirationMs = 60000;

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

                    List<ItemStack> resinStackList = new List<ItemStack>();
                    var resin = api.World.Collectibles.Find(obj => obj.FirstCodePart() == "resin");
                    if(resin != null) {
                        List<ItemStack> stacks = resin.GetHandBookStacks(capi);
                        if (stacks != null) resinStackList.AddRange(stacks);
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

        private bool CanInteract(IPlayer player)
        {
            string playerKey = player.PlayerUID;
            long currentTime = api.World.ElapsedMilliseconds;

            if (lastInteractionTimes.TryGetValue(playerKey, out long lastTime))
            {
                if (currentTime - lastTime < InteractionCooldownMs)
                {
                    return false;
                }
            }

            CleanupExpiredEntries(currentTime);
            lastInteractionTimes[playerKey] = currentTime;
            return true;
        }

        private void CleanupExpiredEntries(long currentTime)
        {
            var keysToRemove = new List<string>();
            foreach (var kvp in lastInteractionTimes)
            {
                if (currentTime - kvp.Value > EntryExpirationMs)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                lastInteractionTimes.Remove(key);
            }
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            // Prevent multiple rapid interactions
            if (!CanInteract(byPlayer))
            {
                return true; // Block the interaction but return true to prevent other handlers
            }

            BlockEntityCartographyTable BlockEntityCartographyTable = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityCartographyTable;

            if (BlockEntityCartographyTable != null) {
                ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

                // Wipe table map with resin
                if (slot?.Itemstack != null && slot.Itemstack.Collectible.FirstCodePart() == "resin") 
                {
                    return BlockEntityCartographyTable.OnWipeTableMap(world, byPlayer, blockSel);
                }
                // Update cartography table or player map with ink and quill
                else if (slot?.Itemstack != null && 
                         slot.Itemstack.Collectible.FirstCodePart() == "inkandquill" && 
                         !byPlayer.Entity.Controls.Sneak) 
                {      
                    return BlockEntityCartographyTable.OnPlayerInteract(world, byPlayer, blockSel);
                }
                // Purge waypoint groups (command-triggered)
                else if(KsCartographyTableModSystem.purgeWpGroups) 
                {
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