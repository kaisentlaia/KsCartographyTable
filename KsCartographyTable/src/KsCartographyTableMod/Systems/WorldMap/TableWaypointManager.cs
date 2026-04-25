
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
	public class WaypointsUpdated
    {
        public int Added;
		public int Edited;
		public int Deleted;

		public bool Updated;

		public WaypointsUpdated(int added, int edited, int deleted)
        {
            Added = added;
			Edited = edited;
			Deleted = deleted;
			Updated = Added > 0 || Edited > 0 || Deleted > 0;
        }
    }
	public class TableWaypointManager
	{
		public ICoreServerAPI CoreServerAPI;
		WorldMapManager WorldMapManager;
		WaypointMapLayer waypointMapLayer;
		public WaypointMapLayer WaypointMapLayer
		{
			get
			{
				if (waypointMapLayer == null)
				{
					WorldMapManager = CoreServerAPI.ModLoader.GetModSystem<WorldMapManager>();
					if (WorldMapManager != null)
					{
						waypointMapLayer = WorldMapManager.MapLayers.FirstOrDefault((MapLayer ml) => ml is WaypointMapLayer) as WaypointMapLayer;
					}
				}

				return waypointMapLayer;
			}
		}
		string modDataPath = Path.Combine(
			GamePaths.DataPath,
			"ModData",
			CartographyTableConstants.MOD_ID
		);

		public TableWaypointManager(ICoreServerAPI api)
		{
			CoreServerAPI = api;
            GamePaths.EnsurePathExists(modDataPath);
		}

		private List<Waypoint> GetPlayerWaypoints(IServerPlayer player)
		{
			List<Waypoint> waypoints = new List<Waypoint>();
			if (WaypointMapLayer != null)
			{
				waypoints = WaypointMapLayer.Waypoints.FindAll(PlayerWaypoint => PlayerWaypoint.OwningPlayerUid == player.PlayerUID);
			}
			return waypoints;
		}

		public WaypointsUpdated SendWaypointsToPlayer(CartographyMap map, IServerPlayer player)
		{
			var playerWaypoints = GetPlayerWaypoints(player);
			var tableDeletedWaypoints = map.DeletedWaypoints;

			var playerGuids = playerWaypoints.Select(pw => pw.Guid).ToHashSet();
			var tableGuids = map.Waypoints.Select(wp => wp.Guid).ToHashSet();
			var deletedGuids = tableDeletedWaypoints.Select(dw => dw.Guid).ToHashSet();

			var onlyOnTableMapToAdd = map.Waypoints.Where(SharedWaypoint =>
				!playerGuids.Contains(SharedWaypoint.Guid)).ToList();

			var onBothMapsToEdit = map.Waypoints.Where(SharedWaypoint =>
			{
				var existingWaypoint = playerWaypoints.FirstOrDefault(pw => pw.Guid == SharedWaypoint.Guid);
				return existingWaypoint != null && (
					existingWaypoint.Color != SharedWaypoint.Color ||
					existingWaypoint.Icon != SharedWaypoint.Icon ||
					existingWaypoint.Pinned != SharedWaypoint.Pinned ||
					existingWaypoint.Title != SharedWaypoint.Title
				);
			}).ToList();

			var onlyOnPlayerMapToDelete = playerWaypoints.Where(PlayerWaypoint =>
			{
				bool notOnTable = !tableGuids.Contains(PlayerWaypoint.Guid);
				bool markedDeletedOnTable = deletedGuids.Contains(PlayerWaypoint.Guid);
				return notOnTable && markedDeletedOnTable;
			}).ToList();

			onlyOnTableMapToAdd.ForEach(SharedWaypoint =>
			{
				Waypoint waypoint = new Waypoint()
				{
					Color = SharedWaypoint.Color,
					OwningPlayerUid = player.PlayerUID,
					Position = SharedWaypoint.Position,
					Title = SharedWaypoint.Title,
					Text = SharedWaypoint.Text,
					Icon = SharedWaypoint.Icon,
					Pinned = SharedWaypoint.Pinned,
					Guid = SharedWaypoint.Guid,
				};
				WaypointMapLayer.AddWaypoint(waypoint, player);
			});

			onBothMapsToEdit.ForEach(SharedWaypoint =>
			{
				var PlayerWaypoint = playerWaypoints.FirstOrDefault(pw => pw.Guid == SharedWaypoint.Guid);
				if (PlayerWaypoint != null)
				{
					PlayerWaypoint.Color = SharedWaypoint.Color;
					PlayerWaypoint.Icon = SharedWaypoint.Icon;
					PlayerWaypoint.Pinned = SharedWaypoint.Pinned;
					PlayerWaypoint.Title = SharedWaypoint.Title;
					PlayerWaypoint.OwningPlayerUid = player.PlayerUID;
				}
			});

			bool anyDeleted = false;
			onlyOnPlayerMapToDelete.ForEach(PlayerWaypoint =>
			{
				var toDelete = WaypointMapLayer.Waypoints.FirstOrDefault(wp => wp.Guid == PlayerWaypoint.Guid);
				if (toDelete != null)
				{
					WaypointMapLayer.Waypoints.Remove(toDelete);
					anyDeleted = true;
				}
			});

			if (onlyOnTableMapToAdd.Count > 0 || onBothMapsToEdit.Count > 0 || anyDeleted)
			{
				ResendWaypointsToPlayer(player);
				return new WaypointsUpdated(onlyOnTableMapToAdd.Count, onBothMapsToEdit.Count, anyDeleted ? onlyOnPlayerMapToDelete.Count : 0);
			}
			return new WaypointsUpdated(0, 0, 0);
		}

		public void ResendWaypointsToPlayer(IServerPlayer toPlayer)
		{
			Dictionary<int, PlayerGroupMembership> playerGroupMemberships = toPlayer.ServerData.PlayerGroupMemberships;
			List<Waypoint> list = new List<Waypoint>();
			foreach (Waypoint waypoint in WaypointMapLayer.Waypoints)
			{
				if (!(toPlayer.PlayerUID != waypoint.OwningPlayerUid) || playerGroupMemberships.ContainsKey(waypoint.OwningPlayerGroupId))
				{
					list.Add(waypoint);
				}
			}
			WorldMapManager.SendMapDataToClient(WaypointMapLayer, toPlayer, SerializerUtil.Serialize(list));
		}

		internal WaypointsUpdated Update(CartographyMap map, IServerPlayer player)
		{
			// BUG with lots of waypoints odd behaviors happen: at the first interaction the message says edited and deleted when the player added some waypoints and deleted other waypoints. At the second interaction the message says added. Further interactions keep displaying different values.
			var playerWaypoints = GetPlayerWaypoints(player);
			var playerDeletedWaypoints = GetPlayerDeletedWaypoints(player);

			var tableGuids = map.Waypoints.Select(wp => wp.Guid).ToHashSet();

			var toAdd = playerWaypoints.Where(PlayerWaypoint => !tableGuids.Contains(PlayerWaypoint.Guid)).ToList();
			var toUpdate = playerWaypoints.Where(PlayerWaypoint =>
			{
				var tableWaypoint = map.Waypoints.FirstOrDefault(wp => wp.Guid == PlayerWaypoint.Guid);
				if (tableWaypoint == null) return false;
				return tableWaypoint.Icon != PlayerWaypoint.Icon ||
					   tableWaypoint.Color != PlayerWaypoint.Color ||
					   tableWaypoint.Title != PlayerWaypoint.Title ||
					   tableWaypoint.Pinned != PlayerWaypoint.Pinned;
			}).ToList();
			var toDelete = playerDeletedWaypoints.Where(PlayerWaypoint => tableGuids.Contains(PlayerWaypoint.Guid)).ToList();

			toAdd.ForEach(PlayerWaypoint =>
			{
				map.CreateOrUpdate(PlayerWaypoint, player);
			});

			toUpdate.ForEach(PlayerWaypoint =>
			{
				map.Update(PlayerWaypoint, player);
			});

			toDelete.ForEach(waypoint =>
			{
				map.Delete(waypoint);
				ClearDeletedWaypoint(waypoint);
			});

			if (toAdd.Count > 0 || toUpdate.Count > 0 || toDelete.Count > 0)
			{
				return new WaypointsUpdated(toAdd.Count, toUpdate.Count, toDelete.Count);
			}
			return new WaypointsUpdated(0, 0, 0);
		}

		private List<Waypoint> GetPlayerDeletedWaypoints(IPlayer player = null)
		{
			byte[] data = CoreServerAPI.WorldManager.SaveGame.GetData("deletedWaypoints");
			var deletedWaypoints = data == null ? new List<Waypoint>() : SerializerUtil.Deserialize<List<Waypoint>>(data);
			if (player != null)
			{
				return deletedWaypoints.Where(waypoint => waypoint.OwningPlayerUid == player.PlayerUID).ToList();
			}
			return deletedWaypoints;
		}

		public void ClearDeletedWaypoint(Waypoint waypoint)
		{
			var deletedWaypoints = GetPlayerDeletedWaypoints();
			var toClear = deletedWaypoints.FindAll(dwp => dwp.Guid == waypoint.Guid);
			toClear.ForEach(wp => deletedWaypoints.Remove(wp));
			CoreServerAPI.WorldManager.SaveGame.StoreData("deletedWaypoints", SerializerUtil.Serialize(deletedWaypoints));
		}

		public void ClearAllDeletedWaypoints()
		{
			CoreServerAPI.WorldManager.SaveGame.StoreData("deletedWaypoints", SerializerUtil.Serialize(new List<Waypoint>()));
		}

		public int Wipe(CartographyMap map)
		{
			int wiped = 0;
			if (map?.Waypoints.Count > 0)
			{
				wiped += map.Waypoints.Count;
				map.Waypoints.Clear();
			}
			if (map?.DeletedWaypoints.Count > 0)
			{
				wiped += map.Waypoints.Count;
				map.DeletedWaypoints.Clear();
			}
			return wiped;
		}

		public List<Waypoint> GetWaypointsWithGroupId()
		{
			return WaypointMapLayer.Waypoints.FindAll(PlayerWaypoint => PlayerWaypoint.OwningPlayerGroupId != -1);
		}
		public void ClearAllWaypoints()
		{
			WaypointMapLayer.Waypoints.Clear();
			WaypointMapLayer.ownWaypoints.Clear();
		}

        internal List<CartographyWaypoint> GetNewWaypoints(IPlayer forPlaye, CartographyMap map)
        {
            throw new NotImplementedException();
        }

        internal List<CartographyWaypoint> GetEditedWaypoints(IPlayer forPlayer, CartographyMap map)
        {
            throw new NotImplementedException();
        }

        internal List<CartographyWaypoint> GetDeletedWaypoints(IPlayer forPlayer, CartographyMap map)
        {
            throw new NotImplementedException();
        }

		internal void AddDeletedWaypointId(Waypoint deletedWaypoint, IServerPlayer byPlayer)
		{
            List<string> deletedWaypoints = GetDeletedWaypointsIds(byPlayer);
            deletedWaypoints.Add(deletedWaypoint.Guid);
            SaveDeletedWaypointsIds(deletedWaypoints, byPlayer);
		}

        private List<string> GetDeletedWaypointsIds(IServerPlayer byPlayer)
        {
            string deletedWaypointsFilePath = Path.Combine(modDataPath, byPlayer.PlayerUID + ".json");
            if (!File.Exists(deletedWaypointsFilePath)) return [];

            try
            {
                string json = File.ReadAllText(deletedWaypointsFilePath);
                var ids = JsonUtil.FromString<List<string>>(json);
                if (ids != null)
                {
                    return ids;
                }
                return [];
            }
            catch (Exception ex)
            {
                CoreServerAPI.Logger.Error("Failed to load player deleted waypoints: {0}", ex);
                return [];
            }
        }
		
        private void SaveDeletedWaypointsIds(List<string> deletedWaypointIds, IServerPlayer byPlayer)
        {
            string deletedWaypointsFilePath = Path.Combine(modDataPath, byPlayer.PlayerUID + ".json");
            try
            {
                string json = JsonUtil.ToString(deletedWaypointIds.ToList());
                File.WriteAllText(deletedWaypointsFilePath, json);
            }
            catch (Exception ex)
            {
                CoreServerAPI.Logger.Error("Failed to save player deleted waypoints: {0}", ex);
            }
        }
    }
}