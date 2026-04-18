

using System;
using System.Collections.Generic;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Kaisentlaia.KsCartographyTableMod.API.Utils
{
  public static class InteractionHelpProvider
  {
    public static WorldInteraction[] GetHelpText(IWorldAccessor world, int selectionBoxIndex)
    {
      return selectionBoxIndex switch
      {
        0 => GetTableHelp(),
        1 => GetInkAndQuillHelp(),
        2 => GetMapHelp(world),
        _ => Array.Empty<WorldInteraction>()
      };
    }

    private static WorldInteraction[] GetTableHelp()
    {
      var interactions = new List<WorldInteraction>();
      return interactions.ToArray();
    }

    private static WorldInteraction[] GetInkAndQuillHelp()
    {
      var interactions = new List<WorldInteraction>
      {
        new WorldInteraction()
        {
          ActionLangCode = CartographyTableLangCodes.INTERACTION_TABLE_UPDATE,
          HotKeyCode = null,
          MouseButton = EnumMouseButton.Right,
        },
        new WorldInteraction()
        {
          ActionLangCode = CartographyTableLangCodes.INTERACTION_USER_UPDATE,
          HotKeyCode = "sprint",
          MouseButton = EnumMouseButton.Right,
        }
      };
      return interactions.ToArray();
    }

    private static WorldInteraction[] GetMapHelp(IWorldAccessor world)
    {
      var interactions = new List<WorldInteraction>
      {
        new WorldInteraction()
        {
          ActionLangCode = CartographyTableLangCodes.INTERACTION_TABLE_WIPE,
          HotKeyCode = null,
          MouseButton = EnumMouseButton.Right,
          Itemstacks = ItemDetector.GetItemStacks(world, "resin")
        }
      };

      if (KsCartographyTableModSystem.ModCompatibilityManager.IsPalantirEnabled)
      {
          interactions.Add(new WorldInteraction()
          {
              ActionLangCode = CartographyTableLangCodes.INTERACTION_TABLE_PONDER,
              HotKeyCode = null,
              MouseButton = EnumMouseButton.Right,
              Itemstacks = ItemDetector.GetItemStacks(world, CartographyTableConstants.PALANTIR_BLOCK_CODE)
          });
      }

      return interactions.ToArray();
    }
  }
}