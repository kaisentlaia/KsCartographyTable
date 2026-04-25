using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
	public class PlayerWaypointManager
	{
		public ICoreClientAPI CoreClientAPI;
		WaypointMapLayer waypointMapLayer;
		public WaypointMapLayer WaypointMapLayer
		{
			get
			{
				if (waypointMapLayer == null)
				{
					WorldMapManager wmm = CoreClientAPI.ModLoader.GetModSystem<WorldMapManager>();
					if (wmm != null)
					{
						waypointMapLayer = wmm.MapLayers.FirstOrDefault((MapLayer ml) => ml is WaypointMapLayer) as WaypointMapLayer;
					}
				}

				return waypointMapLayer;
			}
		}

		public PlayerWaypointManager(ICoreClientAPI api)
		{
			CoreClientAPI = api;
		}

		private List<Waypoint> GetPlayerWaypoints()
		{
			List<Waypoint> waypoints = new List<Waypoint>();
			if (WaypointMapLayer != null)
			{
				waypoints = WaypointMapLayer.Waypoints.FindAll(PlayerWaypoint => PlayerWaypoint.OwningPlayerUid == CoreClientAPI.World.Player.PlayerUID);
			}
			return waypoints;
		}

		internal List<CartographyWaypoint> GetEditedWaypoints(CartographyMap map)
		{
			List<CartographyWaypoint> deletedWaypoints = map.DeletedWaypoints;
			List<CartographyWaypoint> existingWaypoints = map.Waypoints;

			return GetPlayerWaypoints()
				.Where(waypoint =>
					!deletedWaypoints.Any(cartographyWaypoint => cartographyWaypoint.CorrespondsTo(waypoint)) &&
					existingWaypoints.Any(cartographyWaypoint => (cartographyWaypoint.SamePositionAs(waypoint) || cartographyWaypoint.CorrespondsTo(waypoint)) && !cartographyWaypoint.ContentEqualTo(waypoint))
				)
				.Select(waypoint => new CartographyWaypoint(waypoint, CoreClientAPI.World.Player))
				.ToList();
		}

		internal List<CartographyWaypoint> GetNewWaypoints(CartographyMap map)
		{
			List<CartographyWaypoint> deletedWaypoints = map.DeletedWaypoints;
			List<CartographyWaypoint> existingWaypoints = map.Waypoints;

			return GetPlayerWaypoints()
				.Where(waypoint =>
					!deletedWaypoints.Any(cartographyWaypoint => cartographyWaypoint.CorrespondsTo(waypoint)) &&
					!existingWaypoints.Any(cartographyWaypoint => cartographyWaypoint.SamePositionAs(waypoint) || cartographyWaypoint.CorrespondsTo(waypoint))
				)
				.Select(waypoint => new CartographyWaypoint(waypoint, CoreClientAPI.World.Player))
				.ToList();
		}

        internal List<CartographyWaypoint> GetDeletedWaypoints(CartographyMap map)
        {
            throw new NotImplementedException();
        }
    }
}