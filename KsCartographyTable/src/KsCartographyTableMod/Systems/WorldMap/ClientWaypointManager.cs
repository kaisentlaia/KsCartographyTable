using System;
using System.Collections.Generic;
using System.Linq;
using Kaisentlaia.KsCartographyTableMod.API.Client;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
	public class ClientWaypointManager
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

		public ClientWaypointManager(ICoreClientAPI api)
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

        public List<CoordsPacket> GetPalantirWaypoints()
        {
            return GetPlayerWaypoints().Where(waypoint => waypoint.Icon == "palantir-manual").Select(waypoint => new CoordsPacket(waypoint.Position.X, waypoint.Position.Y, waypoint.Position.Z)).ToList();
        }
    }
}