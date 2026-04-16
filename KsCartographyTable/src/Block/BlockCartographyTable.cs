using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using System.Collections.Generic;
using Kaisentlaia.CartographyTable.BlockEntities;
using System;

namespace Kaisentlaia.CartographyTable.Blocks
{
    internal class BlockCartographyTable : Block
    {
        protected WorldInteraction[] interactions;
        private Dictionary<string, long> lastInteractionTimes = new Dictionary<string, long>();
        private const long InteractionCooldownMs = 500;
        private const long EntryExpirationMs = 60000;

        private bool enablePalantir = false;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;
            
            enablePalantir = capi.ModLoader.IsModEnabled("palantir");

            interactions = ObjectCacheUtil.GetOrCreate(api, "cartographyTableBlockInteractions", () => Array.Empty<WorldInteraction>());

        }

        public bool CanInteract(IPlayer player)
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

        public static BlockEntityCartographyTable FindBlockEntity(IWorldAccessor world, BlockPos pos)
        {
            var entity = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityCartographyTable;
            if (entity != null) return entity;

            // Secondary multiblock position — search adjacent blocks for the entity
            BlockPos[] adjacents = [
                pos.AddCopy(1, 0, 0), pos.AddCopy(-1, 0, 0),
                pos.AddCopy(0, 0, 1), pos.AddCopy(0, 0, -1)
            ];
            foreach (var adjacent in adjacents)
            {
                entity = world.BlockAccessor.GetBlockEntity(adjacent) as BlockEntityCartographyTable;
                if (entity != null) return entity;
            }
            return null;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (!CanInteract(byPlayer)) return true;

            BlockEntityCartographyTable beTable = FindBlockEntity(world, blockSel.Position);
            if (beTable == null) return base.OnBlockInteractStart(world, byPlayer, blockSel);

            // Box 2: Map area - wipe with resin (also checks for resin in hand)
            if (blockSel.SelectionBoxIndex == 2 && HasItemInHand(byPlayer, "resin")) {
                return beTable.OnWipeTableMap(world, byPlayer, blockSel);
            }

            if (blockSel.SelectionBoxIndex == 2 && HasItemInHand(byPlayer, "palantir")) {
                return beTable.OnPonderMap(world, byPlayer, blockSel);
            }
            
            // Box 1: Ink and quill area - update maps
            if (blockSel.SelectionBoxIndex == 1 && HasEmptyHand(byPlayer))
            {
                return beTable.OnPlayerInteract(world, byPlayer, blockSel);            
            }

            // table - command to purge waypoint groups
            if(KsCartographyTableModSystem.purgeWpGroups) 
            {
                return beTable.OnPurgeWaypointGroups(world, byPlayer, blockSel);
            }                    

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
        
        public override bool DoPartialSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            List<WorldInteraction> help = new List<WorldInteraction>();
            
            switch (selection.SelectionBoxIndex)
            {
                case 0: // Table base - no specific interaction help, or add default
                    break;
                    
                case 1: // Ink and quill
                    help.Add(new WorldInteraction()
                    {
                        ActionLangCode = "kscartographytable:blockhelp-cartography-table-share-map",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                    });
                    help.Add(new WorldInteraction()
                    {
                        ActionLangCode = "kscartographytable:blockhelp-cartography-table-update-map",
                        HotKeyCode = "sprint",
                        MouseButton = EnumMouseButton.Right,
                    });
                    break;
                    
                case 2: // Map
                    help.Add(new WorldInteraction()
                    {
                        ActionLangCode = "kscartographytable:blockhelp-cartography-table-wipe-map",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = GetResinStacks(world)
                    });
                    if (enablePalantir)
                    {
                        help.Add(new WorldInteraction()
                        {
                            ActionLangCode = "kscartographytable:blockhelp-cartography-table-ponder",
                            HotKeyCode = null,
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = GetPalantirStacks(world)
                        });
                    }
                    break;
            }

            // Convert list to array and append base interactions
            return help.ToArray().Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }

        // Helper methods
        public bool HasItemInHand(IPlayer player, string codePart)
        {
            var slot = player.InventoryManager.ActiveHotbarSlot;
            return slot?.Itemstack != null && slot.Itemstack.Collectible.FirstCodePart() == codePart;
        }
        public bool HasEmptyHand(IPlayer player)
        {
            var slot = player.InventoryManager.ActiveHotbarSlot;
            return slot?.Itemstack == null;
        }

        public ItemStack[] GetResinStacks(IWorldAccessor world)
        {
            // Similar to your resin loading logic
            var ink = world.Collectibles.Find(obj => obj.FirstCodePart() == "resin");
            return ink?.GetHandBookStacks(world.Api as ICoreClientAPI)?.ToArray();
        }

        public ItemStack[] GetPalantirStacks(IWorldAccessor world)
        {
            // Similar to your resin loading logic
            var ink = world.Collectibles.Find(obj => obj.FirstCodePart() == "palantir");
            return ink?.GetHandBookStacks(world.Api as ICoreClientAPI)?.ToArray();
        }
    }
}