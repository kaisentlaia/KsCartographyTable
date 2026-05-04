
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
    [ProtoContract]
	public class WaypointSyncResult
    {
        [ProtoMember(1)]
        public int Added;
        [ProtoMember(2)]
		public int Edited;
        [ProtoMember(3)]
		public int Deleted;
        [ProtoMember(4)]
		public int Rejected;
        [ProtoMember(5)]
		public bool Synced;

		public WaypointSyncResult()
        {
        }

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

		private bool EnsureWaypointGuids(IServerPlayer player)
		{
			if (WaypointMapLayer == null) return false;
			bool changed = false;
			foreach (Waypoint w in WaypointMapLayer.Waypoints)
			{
				if (w.OwningPlayerUid != player.PlayerUID) continue;
				if (string.IsNullOrEmpty(w.Guid))
				{
					w.Guid = Guid.NewGuid().ToString();
					CoreServerAPI.Logger.Notification(
						$"[kscartographytable] Assigned missing Guid to waypoint '{w.Title}' for {player.PlayerName}");
					changed = true;
				}
			}
			return changed;
		}

		public void ResendWaypointsToPlayer(IServerPlayer toPlayer)
		{
			Dictionary<int, PlayerGroupMembership> playerGroupMemberships = toPlayer.ServerData.PlayerGroupMemberships;
			List<Waypoint> list = [];
			foreach (Waypoint waypoint in WaypointMapLayer.Waypoints)
			{
				if (toPlayer.PlayerUID == waypoint.OwningPlayerUid)
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
		internal void AddDeletedWaypointId(Waypoint deletedWaypoint, IPlayer byPlayer)
		{
            List<string> deletedWaypoints = GetDeletedWaypointsIds(byPlayer);
            deletedWaypoints.Add(deletedWaypoint.Guid);
            SaveDeletedWaypointsIds(deletedWaypoints, byPlayer);
		}

        private List<string> GetDeletedWaypointsIds(IPlayer byPlayer)
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
		
        private void SaveDeletedWaypointsIds(List<string> deletedWaypointIds, IPlayer byPlayer)
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
                if (EnsureWaypointGuids(fromPlayer))
                {
                    ResendWaypointsToPlayer(fromPlayer);
                }
                DateTime playerLastDownload = blockEntity.Map.GetPlayerLastDownload(fromPlayer);
                List<CartographyWaypoint> playerSharedDbWaypoints = mapDB.GetPlayerSharedWaypoints(fromPlayer);
                List<Waypoint> playerCurrentWaypoints = GetPlayerWaypoints(fromPlayer);

                List<CartographyWaypoint> newWaypoints = [.. playerCurrentWaypoints.Where(w => playerSharedDbWaypoints.Find(sw => sw.Guid == w.Guid) == null).Select(waypoint => new CartographyWaypoint(waypoint))];

                List<CartographyWaypoint> waypointsToCreate = [];
                List<CartographyWaypoint> waypointsToUpdate = [.. playerSharedDbWaypoints
                    .Select(sharedWaypoint =>
                    {
                        var currentWaypoint = playerCurrentWaypoints.Find(current =>
                            current.Guid == sharedWaypoint.Guid &&
                            (current.Color != sharedWaypoint.Color ||
                            current.Title != sharedWaypoint.Title ||
                            current.Icon != sharedWaypoint.Icon ||
                            current.Pinned != sharedWaypoint.Pinned));

                        if (currentWaypoint != null)
                        {
                            sharedWaypoint.Color = currentWaypoint.Color;
                            sharedWaypoint.Title = currentWaypoint.Title;
                            sharedWaypoint.Icon = currentWaypoint.Icon;
                            sharedWaypoint.Pinned = currentWaypoint.Pinned;

                            return sharedWaypoint;
                        }

                        return null;
                    })
                    .Where(w => w != null)];
                newWaypoints.ForEach(waypoint =>
                {
                    CartographyWaypoint matching = mapDB.GetMatchingWaypoint(waypoint);
                    if (matching != null)
                    {
                        waypoint.ParentGuid = matching.Guid;
                        waypoint.LastUpdated = matching.LastUpdated;
                        waypointsToUpdate.Add(waypoint);
                    } 
                    else
                    {
                        waypointsToCreate.Add(waypoint);
                    }
                });
                mapDB.CreateWaypoints(waypointsToCreate);

                List<CartographyWaypoint> rejectedWaypoints = [.. waypointsToUpdate.Where(w => w.LastUpdated < playerLastDownload)];

                List<CartographyWaypoint> updatedWaypoints = [.. waypointsToUpdate.Where(w => w.LastUpdated >= playerLastDownload)];

                mapDB.UpdateWaypoints(updatedWaypoints);

                List<CartographyWaypoint> deletedWaypoints = mapDB.GetWaypointsToDelete(GetDeletedWaypointsIds(fromPlayer));
                mapDB.DeleteWaypoints(deletedWaypoints);

                return new WaypointSyncResult(waypointsToCreate.Count, updatedWaypoints.Count, rejectedWaypoints.Count, deletedWaypoints.Count);
            }
            return new WaypointSyncResult(0, 0, 0, 0);
        }

        internal WaypointSyncResult UpdatePlayerWaypoints(IPlayer forPlayer, BlockPos blockPos, ServerMapDB mapDB)
        {
            BlockEntityCartographyTable blockEntity = (BlockEntityCartographyTable)CoreServerAPI.World.BlockAccessor.GetBlockEntity(blockPos);
            if (blockEntity != null)
            {
                // BUG if player A added a waypoint to the table and player B edited it, when player A updates their map a new waypoint gets created, it should modify the existing one instead
                List<CartographyWaypoint> playerSharedWaypoints = mapDB.GetPlayerSharedWaypoints(forPlayer);
                List<CartographyWaypoint> newWaypointsForPlayer = mapDB.GetNewWaypointsForPlayer(forPlayer);
                List<CartographyWaypoint> updatedWaypointsForPlayer = mapDB.GetUpdatedWaypointsForPlayer(forPlayer, blockEntity.GetPlayerLastDownload(forPlayer));
                List<CartographyWaypoint> deletedWaypointsForPlayer = mapDB.GetDeletedWaypointsForPlayer(forPlayer, blockEntity.GetPlayerLastDownload(forPlayer));

                List<CartographyWaypoint> matchingPlayerWaypoints = [];
                List<CartographyWaypoint> newSharedWaypoints = [];
                List<Waypoint> currentPlayerWaypoints = GetPlayerWaypoints(forPlayer as IServerPlayer);
                newWaypointsForPlayer.ForEach(parentWaypoint =>
                {
                    Waypoint playerIdenticalWaypoint = currentPlayerWaypoints.Find(playerWaypoint => playerWaypoint.Color == parentWaypoint.Color && playerWaypoint.Position == parentWaypoint.Position && playerWaypoint.Icon == parentWaypoint.Icon && playerWaypoint.Title == parentWaypoint.Title);
                    bool playerHasIdenticalWaypoint = playerIdenticalWaypoint != null;
                    bool waypointTrackedInDb = playerIdenticalWaypoint != null && playerSharedWaypoints.Find(sharedWaypoint => sharedWaypoint.Guid == playerIdenticalWaypoint.Guid) != null;
                    if (waypointTrackedInDb)
                    {
					    CoreServerAPI.Logger.Error($"New waypoint already exists in db: {playerIdenticalWaypoint.Guid} {playerIdenticalWaypoint.Title} {playerIdenticalWaypoint.Icon}");
                    }
                    else if (playerHasIdenticalWaypoint)
                    {
					    CoreServerAPI.Logger.Notification($"Tracking existing waypoint: {playerIdenticalWaypoint.Guid} {playerIdenticalWaypoint.Title} {playerIdenticalWaypoint.Icon}");
                        // the waypoint is present in the player's waypoint but doesn't exist on the db yet
                        CartographyWaypoint sharedWaypoint = new(playerIdenticalWaypoint)
                        {
                            LastUpdated = parentWaypoint.LastUpdated,
                            ParentGuid = parentWaypoint.Guid
                        };
                        matchingPlayerWaypoints.Add(sharedWaypoint);
                    }
                    else
                    {
                        Waypoint newWaypoint = new()
                        {
                            Color = parentWaypoint.Color,
                            Position = parentWaypoint.Position,
                            Guid = Guid.NewGuid().ToString(),
                            Icon = parentWaypoint.Icon,
                            OwningPlayerUid = forPlayer.PlayerUID,
                            Title = parentWaypoint.Title
                        };
					    CoreServerAPI.Logger.Notification($"Creating new player waypoint: {newWaypoint.Guid} {newWaypoint.Title} {newWaypoint.Icon}");
                        WaypointMapLayer.Waypoints.Add(newWaypoint);

                        CartographyWaypoint sharedWaypoint = new(newWaypoint)
                        {
                            LastUpdated = parentWaypoint.LastUpdated,
                            ParentGuid = parentWaypoint.Guid
                        };
                        newSharedWaypoints.Add(sharedWaypoint);
                    }
                });

                mapDB.CreateWaypoints(matchingPlayerWaypoints);
                mapDB.CreateWaypoints(newSharedWaypoints);

                updatedWaypointsForPlayer.ForEach(waypoint =>
                {
                    Waypoint updatedWaypoint = WaypointMapLayer.Waypoints.Find(playerWaypoint => playerWaypoint.Guid == waypoint.Guid);
                    updatedWaypoint.Color = waypoint.Color;
                    updatedWaypoint.Title = waypoint.Title;
                    updatedWaypoint.Icon = waypoint.Icon;
                    updatedWaypoint.Pinned = waypoint.Pinned;
                });

                int deletedCount = 0;
                deletedWaypointsForPlayer.ForEach(waypoint =>
                {
                    if (WaypointMapLayer.Waypoints.Find(playerWaypoint => playerWaypoint.Guid == waypoint.Guid) != null)
                    {
                        deletedCount += 1;
                        WaypointMapLayer.Waypoints.RemoveAll(playerWaypoint => playerWaypoint.Guid == waypoint.Guid);
                        AddDeletedWaypointId(waypoint, forPlayer);
                    }
                });

                ResendWaypointsToPlayer(forPlayer as IServerPlayer);
                
                return new WaypointSyncResult(newSharedWaypoints.Count, updatedWaypointsForPlayer.Count, 0, deletedCount);
                
            }
            return new WaypointSyncResult(0, 0, 0, 0);
        }
    }
}