using Kaisentlaia.KsCartographyTableMod.API.Common;
using Vintagestory.API.Client;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
  public class ModCompatibilityManager
  {
    private bool enablePalantir;

    public ModCompatibilityManager(ICoreClientAPI api)
    {
      enablePalantir = api.ModLoader.IsModEnabled(CartographyTableConstants.PALANTIR_MOD_ID);
    }

    public bool IsPalantirEnabled => enablePalantir;
  }
}