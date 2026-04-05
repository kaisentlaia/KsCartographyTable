using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace Kaisentlaia.CartographyTable.GameContent
{
    public class GuiDialogCartographyMap : GuiDialogWorldMap
    {
        public GuiDialogCartographyMap(OnViewChangedDelegate viewChanged, OnViewChangedSyncDelegate viewChangedSync, ICoreClientAPI capi, List<string> tabnames) : base(viewChanged, viewChangedSync, capi, tabnames)
        {
        }
    }
}