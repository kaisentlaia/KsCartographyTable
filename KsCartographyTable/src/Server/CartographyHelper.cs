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
        private CartographyMap map;
        public CartographyMap Map {
            get { return map; }
            set {                
                if (value != null) {
                    map = value;
                } else {
                    map = new CartographyMap();
                }
            }
        }
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

        public CartographyMap updateTableMap(IServerPlayer player) {
            SetWaypointMapLayer();
            var playerWaypoints = getPlayerWaypoints(player);
            var playerDeletedWaypoints = GetPlayerDeletedWaypoints(player);
            var playerMap = new CartographyMap(playerWaypoints, playerDeletedWaypoints, player);
            var sharedWaypoints = map.Waypoints;
            var deletedWaypoints = map.DeletedWaypoints;

            var toAdd = playerWaypoints.FindAll(PlayerWaypoint => !map.Contains(PlayerWaypoint));
            var toUpdate = playerWaypoints.FindAll(PlayerWaypoint => map.Contains(PlayerWaypoint) && !map.Contains(PlayerWaypoint, true));
            var toDelete = playerDeletedWaypoints.FindAll(PlayerWaypoint => map.Contains(PlayerWaypoint));
            
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
            return map;
        }

        public void updatePlayerMap(IServerPlayer player) {
            SetWaypointMapLayer();

            var playerWaypoints = getPlayerWaypoints(player);
            var worldDeletedWaypoints = GetPlayerDeletedWaypoints();
            var playerMap = new CartographyMap(playerWaypoints, worldDeletedWaypoints, player);
            var sharedWaypoints = map.Waypoints;
            var deletedWaypoints = map.DeletedWaypoints;

            var onlyOnTableMapToAdd = sharedWaypoints.FindAll(SharedWaypoint => !playerMap.Contains(SharedWaypoint));
            var onBothMaps = sharedWaypoints.FindAll(SharedWaypoint => playerMap.Contains(SharedWaypoint));
            var onBothMapsToEdit = sharedWaypoints.FindAll(SharedWaypoint => playerMap.Contains(SharedWaypoint) && !playerMap.Contains(SharedWaypoint, true));
            var onlyOnPlayerMapToDelete = playerWaypoints.FindAll(PlayerWaypoint => !map.Contains(PlayerWaypoint) && map.HasDeleted(PlayerWaypoint));

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

            onBothMapsToEdit.Foreach(SharedWaypoint => {
                var PlayerWaypoint = WaypointMapLayer.Waypoints.Find(waypoint => SharedWaypoint.CorrespondsTo(waypoint));
                if (PlayerWaypoint != null) {
                    PlayerWaypoint.Color = SharedWaypoint.Color;
                    PlayerWaypoint.Icon = SharedWaypoint.Icon;
                    PlayerWaypoint.Pinned = SharedWaypoint.Pinned;
                    PlayerWaypoint.Title = SharedWaypoint.Title;
                    PlayerWaypoint.OwningPlayerUid = player.PlayerUID;
                }
            });

            onlyOnPlayerMapToDelete.Foreach(PlayerWaypoint => {
                var toDelete = WaypointMapLayer.Waypoints.Find(waypoint => waypoint.Position.Equals(PlayerWaypoint.Position));
                WaypointMapLayer.Waypoints.Remove(toDelete);
            });

            if (onlyOnTableMapToAdd.Count > 0 || onBothMapsToEdit.Count > 0 || onlyOnPlayerMapToDelete.Count > 0) {
                if (onlyOnTableMapToAdd.Count > 0) {
                    CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get("kscartographytable:message-new-user-waypoints", onlyOnTableMapToAdd.Count, onBothMapsToEdit.Count), EnumChatType.Notification);
                }
                if (onBothMapsToEdit.Count > 0) {
                    CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get("kscartographytable:message-edited-user-waypoints", onBothMapsToEdit.Count, onBothMapsToEdit.Count), EnumChatType.Notification);
                }
                if (onlyOnPlayerMapToDelete.Count > 0) {
                    CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get("kscartographytable:message-deleted-user-waypoints", onlyOnPlayerMapToDelete.Count), EnumChatType.Notification);
                }
                player.Entity.World.PlaySoundAt(new AssetLocation("game:sounds/effect/writing"),player);
                ResendWaypoints(player);
            } else {
                CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get("kscartographytable:message-user-map-up-to-date"), EnumChatType.Notification);
            }
        }

        public List<Waypoint> getPlayerWaypoints(IServerPlayer player) {
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

        private List<Waypoint> GetPlayerDeletedWaypoints(IPlayer player = null) {
            byte[] data = CoreServerAPI.WorldManager.SaveGame.GetData("deletedWaypoints");
            var deletedWaypoints = data == null ? new List<Waypoint>() : SerializerUtil.Deserialize<List<Waypoint>>(data);
            if(player != null) {
                deletedWaypoints = deletedWaypoints.FindAll(waypoint => waypoint.OwningPlayerUid == player.PlayerUID);
            }
            return deletedWaypoints;
        }

        public void MarkDeleted(IServerPlayer player, int index) {
            SetWaypointMapLayer();
            Waypoint[] array = WaypointMapLayer.Waypoints.Where((Waypoint p) => p.OwningPlayerUid == player.PlayerUID).ToArray();
            if (array.Length > 0 && index >=0 && index < array.Length) {
                Waypoint waypoint = array[index];
                var deletedWaypoints = GetPlayerDeletedWaypoints();
                if (deletedWaypoints.Find(dwp => dwp.Guid == waypoint.Guid) == null) {
                    deletedWaypoints.Add(waypoint);
                }
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