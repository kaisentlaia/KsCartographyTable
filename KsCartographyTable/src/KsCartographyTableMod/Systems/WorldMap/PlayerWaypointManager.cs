using Vintagestory.API.Client;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
	public class PlayerWaypointManager
	{
		public ICoreClientAPI CoreClientAPI;
		public PlayerWaypointManager(ICoreClientAPI api) {
			CoreClientAPI = api;
		}
	}
}