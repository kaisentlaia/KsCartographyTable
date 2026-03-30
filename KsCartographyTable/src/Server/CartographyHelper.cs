using System;
using System.Collections.Generic;
using System.Linq;
using Kaisentlaia.CartographyTable.GameContent;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Kaisentlaia.CartographyTable.Utilities
{
    public class CartographyHelper {
        ICoreServerAPI CoreServerAPI;
        WorldMapManager WorldMapManager;
        WaypointMapLayer WaypointMapLayer;

        public CartographyHelper(ICoreServerAPI ServerAPI) {
            CoreServerAPI = ServerAPI;
            SetWaypointMapLayer();
        }

        private void SetWaypointMapLayer() {
            if(WaypointMapLayer == null) {                
                WorldMapManager = CoreServerAPI.ModLoader.GetModSystem<WorldMapManager>();
                if (WorldMapManager != null) {
                    WaypointMapLayer = WorldMapManager.MapLayers.FirstOrDefault((MapLayer ml) => ml is WaypointMapLayer) as WaypointMapLayer;
                }
            }
        }

        public void UpdateTableMap(CartographyMap map, IServerPlayer player) {
            SetWaypointMapLayer();
            var playerWaypoints = GetPlayerWaypoints(player);
            var playerDeletedWaypoints = GetPlayerDeletedWaypoints(player);

            var tableGuids = map.Waypoints.Select(wp => wp.Guid).ToHashSet();
            
            var toAdd = playerWaypoints.Where(PlayerWaypoint => !tableGuids.Contains(PlayerWaypoint.Guid)).ToList();
            var toUpdate = playerWaypoints.Where(PlayerWaypoint => {
                var tableWaypoint = map.Waypoints.FirstOrDefault(wp => wp.Guid == PlayerWaypoint.Guid);
                if (tableWaypoint == null) return false;
                return tableWaypoint.Icon != PlayerWaypoint.Icon ||
                       tableWaypoint.Color != PlayerWaypoint.Color ||
                       tableWaypoint.Title != PlayerWaypoint.Title ||
                       tableWaypoint.Pinned != PlayerWaypoint.Pinned;
            }).ToList();
            var toDelete = playerDeletedWaypoints.Where(PlayerWaypoint => tableGuids.Contains(PlayerWaypoint.Guid)).ToList();
            
            toAdd.ForEach(PlayerWaypoint => {
                map.Create(PlayerWaypoint, player);
            });

            toUpdate.ForEach(PlayerWaypoint => {
                map.Update(PlayerWaypoint, player);
            });

            toDelete.ForEach(waypoint => {
                map.Delete(waypoint);
                ClearDeletedWaypoint(waypoint);
            });

            if (toAdd.Count > 0 || toUpdate.Count > 0 || toDelete.Count > 0) {
                if (toAdd.Count > 0) {
                    CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get("kscartographytable:message-new-waypoints-count", toAdd.Count), EnumChatType.Notification);
                }
                if (toUpdate.Count > 0) {
                    CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get("kscartographytable:message-edited-waypoints-count", toUpdate.Count), EnumChatType.Notification);
                }
                if (toDelete.Count > 0) {
                    CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get("kscartographytable:message-deleted-waypoints-count", toDelete.Count), EnumChatType.Notification);
                }
                player.Entity.World.PlaySoundAt(new AssetLocation("game:sounds/effect/writing"),player);
            } else {
                CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get("kscartographytable:message-table-map-up-to-date"), EnumChatType.Notification);
            }
        }

        public void UpdatePlayerMap(CartographyMap map, IServerPlayer player) {
            SetWaypointMapLayer();

            var playerWaypoints = GetPlayerWaypoints(player);
            var tableDeletedWaypoints = map.DeletedWaypoints;

            var playerGuids = playerWaypoints.Select(pw => pw.Guid).ToHashSet();
            var tableGuids = map.Waypoints.Select(wp => wp.Guid).ToHashSet();
            var deletedGuids = tableDeletedWaypoints.Select(dw => dw.Guid).ToHashSet();

            var onlyOnTableMapToAdd = map.Waypoints.Where(SharedWaypoint => 
                !playerGuids.Contains(SharedWaypoint.Guid)).ToList();

            var onBothMapsToEdit = map.Waypoints.Where(SharedWaypoint => {
                var existingWaypoint = playerWaypoints.FirstOrDefault(pw => pw.Guid == SharedWaypoint.Guid);
                return existingWaypoint != null && (
                    existingWaypoint.Color != SharedWaypoint.Color ||
                    existingWaypoint.Icon != SharedWaypoint.Icon ||
                    existingWaypoint.Pinned != SharedWaypoint.Pinned ||
                    existingWaypoint.Title != SharedWaypoint.Title
                );
            }).ToList();

            var onlyOnPlayerMapToDelete = playerWaypoints.Where(PlayerWaypoint => {
                bool notOnTable = !tableGuids.Contains(PlayerWaypoint.Guid);
                bool markedDeletedOnTable = deletedGuids.Contains(PlayerWaypoint.Guid);
                return notOnTable && markedDeletedOnTable;
            }).ToList();

            onlyOnTableMapToAdd.ForEach(SharedWaypoint => {
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

            onBothMapsToEdit.ForEach(SharedWaypoint => {
                var PlayerWaypoint = playerWaypoints.FirstOrDefault(pw => pw.Guid == SharedWaypoint.Guid);
                if (PlayerWaypoint != null) {
                    PlayerWaypoint.Color = SharedWaypoint.Color;
                    PlayerWaypoint.Icon = SharedWaypoint.Icon;
                    PlayerWaypoint.Pinned = SharedWaypoint.Pinned;
                    PlayerWaypoint.Title = SharedWaypoint.Title;
                    PlayerWaypoint.OwningPlayerUid = player.PlayerUID;
                }
            });

            bool anyDeleted = false;
            onlyOnPlayerMapToDelete.ForEach(PlayerWaypoint => {
                var toDelete = WaypointMapLayer.Waypoints.FirstOrDefault(wp => wp.Guid == PlayerWaypoint.Guid);
                if (toDelete != null) {
                    WaypointMapLayer.Waypoints.Remove(toDelete);
                    anyDeleted = true;
                }
            });

            if (onlyOnTableMapToAdd.Count > 0 || onBothMapsToEdit.Count > 0 || anyDeleted) {
                if (onlyOnTableMapToAdd.Count > 0) {
                    CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get("kscartographytable:message-new-user-waypoints", onlyOnTableMapToAdd.Count), EnumChatType.Notification);
                }
                if (onBothMapsToEdit.Count > 0) {
                    CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get("kscartographytable:message-edited-user-waypoints", onBothMapsToEdit.Count), EnumChatType.Notification);
                }
                if (anyDeleted) {
                    CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get("kscartographytable:message-deleted-user-waypoints", onlyOnPlayerMapToDelete.Count), EnumChatType.Notification);
                }
                player.Entity.World.PlaySoundAt(new AssetLocation("game:sounds/effect/writing"),player);
                ResendWaypoints(player);
            } else {
                CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get("kscartographytable:message-user-map-up-to-date"), EnumChatType.Notification);
            }
        }

        public List<Waypoint> GetPlayerWaypoints(IServerPlayer player) {
            List<Waypoint> waypoints = new List<Waypoint>();
            if (WaypointMapLayer != null) {
                waypoints = WaypointMapLayer.Waypoints.FindAll(PlayerWaypoint => PlayerWaypoint.OwningPlayerUid == player.PlayerUID);
            }
            return waypoints;
        }

        public void PurgeWaypointGroups(IPlayer player) {
            if (KsCartographyTableModSystem.purgeWpGroups) {
                SetWaypointMapLayer();
                var allWaypointsWithGroupId = WaypointMapLayer.Waypoints.FindAll(PlayerWaypoint => PlayerWaypoint.OwningPlayerGroupId != -1);
                if (allWaypointsWithGroupId.Count > 0) {
                    allWaypointsWithGroupId.Foreach(wp => {
                        wp.OwningPlayerGroupId = -1;
                    });
                    CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get($"Groups removed from {allWaypointsWithGroupId.Count} waypoints"), EnumChatType.Notification);
                }
            }
        }

        public void WipeTableMap(CartographyMap map) {
            if (map != null) {
                map.Waypoints.Clear();
                map.DeletedWaypoints.Clear();
                CoreServerAPI.SendMessage(null, GlobalConstants.GeneralChatGroup, Lang.Get("kscartographytable:message-table-map-wiped", 0), EnumChatType.Notification);
            } else {
                CoreServerAPI.SendMessage(null, GlobalConstants.GeneralChatGroup, Lang.Get("kscartographytable:message-table-map-already-empty"), EnumChatType.Notification);
            }
        }

        private List<Waypoint> GetPlayerDeletedWaypoints(IPlayer player = null) {
            byte[] data = CoreServerAPI.WorldManager.SaveGame.GetData("deletedWaypoints");
            var deletedWaypoints = data == null ? new List<Waypoint>() : SerializerUtil.Deserialize<List<Waypoint>>(data);
            if (player != null) {
                return deletedWaypoints.Where(waypoint => waypoint.OwningPlayerUid == player.PlayerUID).ToList();
            }
            return deletedWaypoints;
        }

        public void MarkDeleted(IServerPlayer player, int index) {
            SetWaypointMapLayer();
            var playerWaypoints = WaypointMapLayer.Waypoints
                .Where(p => p.OwningPlayerUid == player.PlayerUID)
                .ToList();
            
            if (index < 0 || index >= playerWaypoints.Count) {
                return;
            }
            
            Waypoint waypoint = playerWaypoints[index];
            var deletedWaypoints = GetPlayerDeletedWaypoints();
            if (!deletedWaypoints.Any(dwp => dwp.Guid == waypoint.Guid)) {
                deletedWaypoints.Add(waypoint);
                CoreServerAPI.WorldManager.SaveGame.StoreData("deletedWaypoints", SerializerUtil.Serialize(deletedWaypoints));
            }
        }

        public void ClearDeletedWaypoint(Waypoint waypoint) {
            SetWaypointMapLayer();
            var deletedWaypoints = GetPlayerDeletedWaypoints();
            var toClear = deletedWaypoints.FindAll(dwp => dwp.Guid == waypoint.Guid);
            toClear.ForEach(wp => deletedWaypoints.Remove(wp));
            CoreServerAPI.WorldManager.SaveGame.StoreData("deletedWaypoints", SerializerUtil.Serialize(deletedWaypoints));
        }

        public void ClearAllDeletedWaypoints() {
            CoreServerAPI.WorldManager.SaveGame.StoreData("deletedWaypoints", SerializerUtil.Serialize(new List<Waypoint>()));
        }

        public void WipeWaypoints() {
            SetWaypointMapLayer();
            WaypointMapLayer.Waypoints.Clear();
            WaypointMapLayer.ownWaypoints.Clear();
        }

        public void ResendWaypoints(IServerPlayer toPlayer)
        {
            SetWaypointMapLayer();
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
    }
}