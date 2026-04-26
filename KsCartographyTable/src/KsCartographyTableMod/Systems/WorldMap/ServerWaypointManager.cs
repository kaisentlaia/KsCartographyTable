
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
	public class WaypointSyncResult
    {
        public int Added;
		public int Edited;
		public int Deleted;
		public int Rejected;
		public bool Synced;

		public WaypointSyncResult(int added, int edited, int rejected, int deleted)
        {
            Added = added;
			Edited = edited;
			Rejected = rejected;
			Deleted = deleted;
			Synced = Added > 0 || Edited > 0 || Deleted > 0;
        }
    }
	public class ServerWaypointManager
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
		string modDataPath;

		public ServerWaypointManager(ICoreServerAPI api)
		{
			CoreServerAPI = api;
            modDataPath = Path.Combine(
                GamePaths.DataPath,
                "ModData",
                CoreServerAPI.World.SavegameIdentifier,
                CartographyTableConstants.MOD_ID
            );
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

		public List<Waypoint> GetWaypointsWithGroupId()
		{
			return WaypointMapLayer.Waypoints.FindAll(PlayerWaypoint => PlayerWaypoint.OwningPlayerGroupId != -1);
		}
		public void ClearAllWaypoints()
		{
			WaypointMapLayer.Waypoints.Clear();
			WaypointMapLayer.ownWaypoints.Clear();
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

        internal WaypointSyncResult UpdateTableWaypoints(IServerPlayer fromPlayer, BlockPos blockPos, ServerMapDB mapDB)
        {
            BlockEntityCartographyTable blockEntity = (BlockEntityCartographyTable)CoreServerAPI.World.BlockAccessor.GetBlockEntity(blockPos);
            if (blockEntity != null)
            {
                DateTime playerLastDownload = blockEntity.Map.getPlayerLastDownload(fromPlayer);
                List<CartographyWaypoint> playerSharedDbWaypoints = mapDB.GetPlayerSharedWaypoints(fromPlayer);
                List<Waypoint> playerCurrentWaypoints = GetPlayerWaypoints(fromPlayer);

                List<CartographyWaypoint> newWaypoints = [.. playerCurrentWaypoints.Where(w => playerSharedDbWaypoints.Find(sw => sw.Guid == w.Guid) == null).Select(waypoint => new CartographyWaypoint(waypoint))];

                newWaypoints.ForEach(waypoint =>
                {
                    CartographyWaypoint matching = mapDB.GetMatchingWaypoint(waypoint);
                    if (matching != null)
                    {
                        waypoint.ParentGuid = matching.Guid;
                        waypoint.LastUpdated = matching.LastUpdated;
                    }
                });
                var duplicateGuids = playerCurrentWaypoints
                    .GroupBy(w => w.Guid)
                    .Where(g => g.Count() > 1)
                    .Select(g => new { Guid = g.Key, Count = g.Count(), Titles = g.Select(w => w.Title).ToList() });

                foreach (var dup in duplicateGuids)
                {
                    CoreServerAPI.Logger.Error("DUPLICATE WAYPOINT: GUID={0}, Count={1}, Titles={2}", 
                        dup.Guid, dup.Count, string.Join(", ", dup.Titles));
                }
                mapDB.CreateWaypoints(newWaypoints);

                List<CartographyWaypoint> changedWaypoints = [.. playerCurrentWaypoints.Where(w => playerSharedDbWaypoints.Find(sw => sw.Guid == w.Guid && (sw.Color != w.Color || sw.Title != w.Title || sw.Position != w.Position || sw.Icon != w.Icon)) != null).Select(waypoint => new CartographyWaypoint(waypoint))];

                List<CartographyWaypoint> rejectedWaypoints = [.. changedWaypoints.Where(w => w.LastUpdated < playerLastDownload)];

                List<CartographyWaypoint> updatedWaypoints = [.. changedWaypoints.Where(w => w.LastUpdated >= playerLastDownload)];

                mapDB.UpdateWaypoints(updatedWaypoints);

                List<string> deletedWaypointIds = GetDeletedWaypointsIds(fromPlayer).Where(waypointId => playerSharedDbWaypoints.Find(sw => sw.Guid == waypointId) != null).ToList();
                mapDB.DeleteWaypoints(deletedWaypointIds);

                return new WaypointSyncResult(newWaypoints.Count, updatedWaypoints.Count, rejectedWaypoints.Count, deletedWaypointIds.Count);
            }
            // TODO log
            return new WaypointSyncResult(0, 0, 0, 0);
        }

        internal void UpdatePlayerWaypoints(IPlayer forPlayer, CartographyMap map)
        {
            throw new NotImplementedException();
        }
    }
}