

using System;
using System.Collections.Generic;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Kaisentlaia.KsCartographyTableMod.API.Utils
{
    public static class InteractionHelpProvider
    {
        public static WorldInteraction[] GetHelpText(IWorldAccessor world, int selectionBoxIndex, bool empty, bool mapEmpty)
        {
            return selectionBoxIndex switch
            {
                0 => GetTableHelp(),
                1 => GetInkAndQuillHelp(world, empty),
                2 => GetMapHelp(world, mapEmpty),
                _ => []
            };
        }

        private static WorldInteraction[] GetTableHelp()
        {
            var interactions = new List<WorldInteraction>();
            return interactions.ToArray();
        }

        private static WorldInteraction[] GetInkAndQuillHelp(IWorldAccessor world, bool empty)
        {
            if (empty)
            {
                return
                [
                    new WorldInteraction()
                    {
                        ActionLangCode = CartographyTableLangCodes.INTERACTION_ADD_QUILL,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = [new ItemStack(world.GetItem(new AssetLocation(CartographyTableConstants.MOD_ID+":"+CartographyTableConstants.QUILL_ITEM_CODE)))]
                    }
                ];
            } else
            {
                return
                [
                    new WorldInteraction()
                    {
                        ActionLangCode = CartographyTableLangCodes.INTERACTION_REMOVE_QUILL,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = null
                    }
                ];
            }
        }

        private static WorldInteraction[] GetMapHelp(IWorldAccessor world, bool mapEmpty)
        {
            var interactions = new List<WorldInteraction>
            {
                new()
                {
                    ActionLangCode = CartographyTableLangCodes.INTERACTION_TABLE_UPDATE,
                    HotKeyCode = null,
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = ItemDetectorService.GetItemStacks(world, CartographyTableConstants.QUILL_ITEM_CODE)
                },
                new()
                {
                    ActionLangCode = CartographyTableLangCodes.INTERACTION_USER_UPDATE,
                    HotKeyCode = "sprint",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = ItemDetectorService.GetItemStacks(world, CartographyTableConstants.QUILL_ITEM_CODE)
                }
            };

            if (!mapEmpty)
            {
                interactions.Add(new()
                    {
                        ActionLangCode = CartographyTableLangCodes.INTERACTION_TABLE_WIPE,
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = ObjectCacheUtil.GetToolStacks(world.Api, EnumTool.Knife)
                    }
                );
            }

            if (KsCartographyTableModSystem.ModCompatibilityManager.IsPalantirEnabled)
            {
                interactions.Add(new WorldInteraction()
                {
                    ActionLangCode = CartographyTableLangCodes.INTERACTION_TABLE_PONDER,
                    HotKeyCode = null,
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = ItemDetectorService.GetItemStacks(world, CartographyTableConstants.PALANTIR_BLOCK_CODE)
                });
            }

            return [.. interactions];
        }
    }
}