
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		private List<Waypoint> GetPlayerWaypoints(IPlayer player)
		{
			List<Waypoint> waypoints = [];
            if (player == null)
            {
                CoreServerAPI?.Logger.Error($"{CartographyTableConstants.MAP_EVENT} GetPlayerWaypoints for null player!");
			    return waypoints;
            }
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
					KsCartographyTableModSystem.DebugLog(CoreServerAPI, $"Assigned missing Guid to waypoint '{w.Title}' for {player.PlayerName}");
					changed = true;
				}
			}
			return changed;
		}

        public void ResendWaypointsToAllPlayers()
        {
            CoreServerAPI.World.AllOnlinePlayers.Foreach(player =>
            {
                WorldMapManager.SendMapDataToClient(WaypointMapLayer, player as IServerPlayer, SerializerUtil.Serialize(GetPlayerWaypoints(player)));
            });
        }

		public void ResendWaypointsToPlayer(IServerPlayer toPlayer)
		{
			List<Waypoint> list = [];
            if (toPlayer == null)
            {
                CoreServerAPI.Logger.Error($"{CartographyTableConstants.MAP_EVENT} Resending waypoints to null player!");
                return;
            }
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
		public TextCommandResult ClearAllWaypoints(bool dryRun, IServerPlayer forPlayer, bool mapOnly)
		{
            string additionalAction = !mapOnly ? $" and mark{(dryRun?"":"ed")} them to be deleted on the cartography table at the next transcription" : "";
            // TODO localization
            if (forPlayer != null)
            {
                List<Waypoint> waypointsToWipe = GetPlayerWaypoints(forPlayer);
                
                int waypointsCount = waypointsToWipe.Count;
                if (!dryRun)
                {
                    waypointsToWipe.ForEach(waypoint =>
                    {
                        if (!mapOnly)
                        {
                            AddDeletedWaypointId(waypoint, forPlayer);
                        }
                        WaypointMapLayer.Waypoints.Remove(waypoint);
                    });
                    ResendWaypointsToPlayer(forPlayer);
                    CoreServerAPI.SendMessage(forPlayer, GlobalConstants.GeneralChatGroup, $"Deleted {waypointsCount} waypoints from your map{additionalAction}.", EnumChatType.Notification);
                    return TextCommandResult.Success();
                }
                CoreServerAPI.SendMessage(forPlayer, GlobalConstants.GeneralChatGroup, $"Will delete {waypointsCount} waypoints from your map{additionalAction}. Run '.kct waypoints wipe {(mapOnly ? "maponly" : "mapandtable")} confirm' to confirm.", EnumChatType.Notification);
                return TextCommandResult.Success();

            }
            else
            {
                int waypointsCount = WaypointMapLayer.Waypoints.Count;
                if (!dryRun)
                {
                    if (!mapOnly)
                    {
                        WaypointMapLayer.Waypoints.ForEach(waypoint =>
                        {
                            AddDeletedWaypointId(waypoint, CoreServerAPI.World.PlayerByUid(waypoint.OwningPlayerUid));
                        });
                    }
                    WaypointMapLayer.Waypoints.Clear();
                    ResendWaypointsToAllPlayers();
                    return TextCommandResult.Success($"Deleted {waypointsCount} waypoints from all players' maps{additionalAction}.");
                }
                return TextCommandResult.Success($"Will delete {waypointsCount} waypoints from all players' maps{additionalAction}. Run '/kct waypoints wipe {(mapOnly ? "maponly" : "mapandtable")} confirm' to confirm.");
            }
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
                DateTime playerLastDownload = blockEntity.Map.GetPlayerLastSync(fromPlayer);
                List<CartographyWaypoint> playerSharedDbWaypoints = mapDB.GetPlayerSharedWaypoints(fromPlayer);
                List<Waypoint> playerCurrentWaypoints = GetPlayerWaypoints(fromPlayer);

                List<CartographyWaypoint> newWaypoints = [.. playerCurrentWaypoints.Where(w => playerSharedDbWaypoints.Find(sw => sw.Guid == w.Guid) == null).Select(waypoint => new CartographyWaypoint(waypoint))];

                List<CartographyWaypoint> waypointsToCreate = [];
                List<CartographyWaypoint> existingWaypointsToTrack = [];
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
					    KsCartographyTableModSystem.DebugLog(CoreServerAPI, $"Found a matching waypoint in db, creating with parentGuid: {matching.Guid} {matching.Title} {matching.Icon} {matching.Position}");
                        waypoint.ParentGuid = matching.Guid;
                        waypoint.LastUpdated = matching.LastUpdated;
                        existingWaypointsToTrack.Add(waypoint);
                    } 
                    else
                    {
					    KsCartographyTableModSystem.DebugLog(CoreServerAPI, $"No matching waypoint in db, creating with parent null: {waypoint.Guid} {waypoint.Title} {waypoint.Icon} {waypoint.Position}");
                        waypointsToCreate.Add(waypoint);
                    }
                });
                mapDB.CreateWaypoints(waypointsToCreate);
                mapDB.CreateWaypoints(existingWaypointsToTrack);

                List<CartographyWaypoint> rejectedWaypoints = [.. waypointsToUpdate.Where(w => w.LastUpdated > playerLastDownload)];

                rejectedWaypoints.ForEach(rejectedWaypoint =>
                {
                    KsCartographyTableModSystem.DebugLog(CoreServerAPI, $"Rejected waypoint: {rejectedWaypoint.Guid} {rejectedWaypoint.Title} {rejectedWaypoint.Icon} {rejectedWaypoint.Position} last updated {rejectedWaypoint.LastUpdated} vs player's last download {playerLastDownload}");
                });

                List<CartographyWaypoint> updatedWaypoints = [.. waypointsToUpdate.Where(w => w.LastUpdated <= playerLastDownload)];

                updatedWaypoints.ForEach(updatedWaypoint =>
                {
                    KsCartographyTableModSystem.DebugLog(CoreServerAPI, $"Updated waypoint: {updatedWaypoint.Guid} {updatedWaypoint.Title} {updatedWaypoint.Icon} {updatedWaypoint.Position} last updated {updatedWaypoint.LastUpdated} vs player's last download {playerLastDownload}");
                });

                mapDB.UpdateWaypoints(updatedWaypoints);

                List<CartographyWaypoint> deletedWaypoints = mapDB.GetWaypointsToDelete(GetDeletedWaypointsIds(fromPlayer));                
                mapDB.DeleteWaypoints(deletedWaypoints);

                return new WaypointSyncResult(waypointsToCreate.Count, updatedWaypoints.Count, rejectedWaypoints.Count, deletedWaypoints.Count);
            }
            return new WaypointSyncResult(0, 0, 0, 0);
        }

        internal WaypointSyncResult UpdatePlayerWaypoints(IPlayer forPlayer, BlockEntityCartographyTable blockEntity, ServerMapDB mapDB)
        {
            if (blockEntity != null)
            {
                List<CartographyWaypoint> playerSharedWaypoints = mapDB.GetPlayerSharedWaypoints(forPlayer);
                List<CartographyWaypoint> newWaypointsForPlayer = mapDB.GetNewWaypointsForPlayer(forPlayer);
                List<CartographyWaypoint> updatedWaypointsForPlayer = mapDB.GetUpdatedWaypointsForPlayer(forPlayer, blockEntity.GetPlayerLastDownload(forPlayer));
                List<CartographyWaypoint> deletedWaypointsForPlayer = mapDB.GetDeletedWaypointsForPlayer(forPlayer, blockEntity.GetPlayerLastDownload(forPlayer));

                List<CartographyWaypoint> matchingPlayerWaypoints = [];
                List<CartographyWaypoint> newSharedWaypoints = [];
                List<Waypoint> currentPlayerWaypoints = GetPlayerWaypoints(forPlayer);
                newWaypointsForPlayer.ForEach(parentWaypoint =>
                {
                    Waypoint playerIdenticalWaypoint = currentPlayerWaypoints.Find(playerWaypoint => playerWaypoint.Position == parentWaypoint.Position && playerWaypoint.Icon == parentWaypoint.Icon && playerWaypoint.Title == parentWaypoint.Title);
                    bool playerHasIdenticalWaypoint = playerIdenticalWaypoint != null;
                    bool waypointTrackedInDb = playerIdenticalWaypoint != null && playerSharedWaypoints.Find(sharedWaypoint => sharedWaypoint.Guid == playerIdenticalWaypoint.Guid) != null;
                    if (waypointTrackedInDb)
                    {
					    KsCartographyTableModSystem.DebugLog(CoreServerAPI, $"New waypoint already exists in db: {parentWaypoint.Guid} {parentWaypoint.Title} {parentWaypoint.Icon}");
                    }
                    else if (playerHasIdenticalWaypoint)
                    {
					    KsCartographyTableModSystem.DebugLog(CoreServerAPI, $"Tracking existing waypoint: {playerIdenticalWaypoint.Guid} {playerIdenticalWaypoint.Title} {playerIdenticalWaypoint.Icon}");
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
					    KsCartographyTableModSystem.DebugLog(CoreServerAPI, $"Creating new player waypoint: {newWaypoint.Guid} {newWaypoint.Title} {newWaypoint.Icon}");
                        WaypointMapLayer.Waypoints.Add(newWaypoint);

                        CartographyWaypoint sharedWaypoint = new(newWaypoint)
                        {
                            LastUpdated = parentWaypoint.LastUpdated,
                            ParentGuid = parentWaypoint.Guid
                        };
                        newSharedWaypoints.Add(sharedWaypoint);
                    }
                });

                int createdCount = newSharedWaypoints.Count;

                playerSharedWaypoints.ForEach(sharedWaypoint =>
                {
                    Waypoint existing = currentPlayerWaypoints.Find(playerWaypoint => playerWaypoint.Guid == sharedWaypoint.Guid);
                    if (existing == null)
                    {
					    KsCartographyTableModSystem.DebugLog(CoreServerAPI, $"Restoring player waypoint: {sharedWaypoint.Guid} {sharedWaypoint.Title} {sharedWaypoint.Icon}");
                        Waypoint newWaypoint = new()
                        {
                            Color = sharedWaypoint.Color,
                            Position = sharedWaypoint.Position,
                            Guid = sharedWaypoint.Guid,
                            Icon = sharedWaypoint.Icon,
                            OwningPlayerUid = sharedWaypoint.OwningPlayerUid,
                            Title = sharedWaypoint.Title
                        };
                        WaypointMapLayer.Waypoints.Add(newWaypoint);
                        createdCount++;
                    }
                });

                mapDB.CreateWaypoints(matchingPlayerWaypoints);
                mapDB.CreateWaypoints(newSharedWaypoints);

                int updatedCount = 0;
                updatedWaypointsForPlayer.ForEach(waypoint =>
                {
                    KsCartographyTableModSystem.DebugLog(CoreServerAPI, $"Updated waypoint to use: {waypoint.Guid} {waypoint.Title} {waypoint.Icon} {waypoint.Color} pinned {waypoint.Pinned}");
                    Waypoint playerWaypointToUpdate = WaypointMapLayer.Waypoints.Find(playerWaypoint => playerWaypoint.Guid == waypoint.Guid);
                    if (playerWaypointToUpdate.Color != waypoint.Color || playerWaypointToUpdate.Title != waypoint.Title || playerWaypointToUpdate.Icon != waypoint.Icon || playerWaypointToUpdate.Pinned != waypoint.Pinned)
                    {
                        KsCartographyTableModSystem.DebugLog(CoreServerAPI, $"Player waypoint to update: {playerWaypointToUpdate.Guid} {playerWaypointToUpdate.Title} {playerWaypointToUpdate.Icon} {playerWaypointToUpdate.Color} pinned {playerWaypointToUpdate.Pinned}");
                        playerWaypointToUpdate.Color = waypoint.Color;
                        playerWaypointToUpdate.Title = waypoint.Title;
                        playerWaypointToUpdate.Icon = waypoint.Icon;
                        playerWaypointToUpdate.Pinned = waypoint.Pinned;
                        updatedCount += 1;
                        
                        KsCartographyTableModSystem.DebugLog(CoreServerAPI, $"Player waypoint updated to: {playerWaypointToUpdate.Guid} {playerWaypointToUpdate.Title} {playerWaypointToUpdate.Icon} {playerWaypointToUpdate.Color} pinned {playerWaypointToUpdate.Pinned}");
                    }
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
                
                return new WaypointSyncResult(createdCount, updatedCount, 0, deletedCount);
                
            }
            return new WaypointSyncResult(0, 0, 0, 0);
        }
    }
}