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
        List<CartographyWaypoint> Waypoints;
        WaypointMapLayer WaypointMapLayer;
        public CartographyHelper(ICoreServerAPI ServerAPI, List<CartographyWaypoint> TableWaypoints) {
            CoreServerAPI = ServerAPI;
            Waypoints = TableWaypoints;
            SetWaypointMapLayer();
        }

        public void SetWaypoints(List<CartographyWaypoint> newWaypoints) {
            if (newWaypoints != null) {
                Waypoints = newWaypoints;
            } else {
                Waypoints = new List<CartographyWaypoint>();
            }
        }

        private void SetWaypointMapLayer() {
            if(WaypointMapLayer == null) {                
                var serverWorldMapManager = CoreServerAPI.ModLoader.GetModSystem<WorldMapManager>();
                if (serverWorldMapManager != null) {
                    WaypointMapLayer = serverWorldMapManager.MapLayers.FirstOrDefault((MapLayer ml) => ml is WaypointMapLayer) as WaypointMapLayer;
                }
            }
        }

        public List<CartographyWaypoint> shareWaypoints(IServerPlayer player) {
            SetWaypointMapLayer();
            var userWaypoints = getUserWaypoints(player);
            var sharedWaypoints = Waypoints;  
            
            var onlyOnUserMap = userWaypoints.FindAll(delegate (Waypoint UserWaypoint) {
                return sharedWaypoints.Find(delegate (CartographyWaypoint SharedWaypoint) {
                    return SharedWaypoint.CorrespondsTo(UserWaypoint);
                }) == null;
            });
            var onlyOnSharedMapBySameUser = sharedWaypoints.FindAll(delegate (CartographyWaypoint SharedWaypoint) {
                return SharedWaypoint.OwnedBy(player) && userWaypoints.Find(delegate (Waypoint UserWaypoint) {
                    return SharedWaypoint.CorrespondsTo(UserWaypoint);
                }) == null;
            });
            var onBothMapsWithChanges = userWaypoints.FindAll(delegate (Waypoint UserWaypoint) {
                return sharedWaypoints.Find(delegate (CartographyWaypoint SharedWaypoint) {
                    return SharedWaypoint.CorrespondsTo(UserWaypoint) && !SharedWaypoint.ContentEqualTo(UserWaypoint);
                }) != null;
            });

            onlyOnUserMap.ForEach(UserWaypoint => {
                sharedWaypoints.Add(new CartographyWaypoint(UserWaypoint, player));
            });

            onlyOnSharedMapBySameUser.ForEach(SharedWaypoint => {
                sharedWaypoints.Remove(SharedWaypoint);
            });

            onBothMapsWithChanges.Foreach(UserWaypoint => {
                var toEdit = sharedWaypoints.Find(delegate (CartographyWaypoint SharedWaypoint) {
                    return SharedWaypoint.CorrespondsTo(UserWaypoint);
                });
                if (toEdit != null) {
                    toEdit.Color = UserWaypoint.Color;
                    toEdit.Icon = UserWaypoint.Icon;
                    toEdit.Pinned = UserWaypoint.Pinned;
                    toEdit.Title = UserWaypoint.Title;
                    toEdit.OwningPlayerUid = player.PlayerUID;
                    toEdit.Modified = DateTime.Now;
                    toEdit.ModifiedByPlayerUid = player.PlayerUID;
                }
            });

            // TODO put in lang
            if (onlyOnUserMap.Count > 0 || onBothMapsWithChanges.Count > 0 || onlyOnSharedMapBySameUser.Count > 0) {
                if (onlyOnUserMap.Count > 0) {
                    CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, $"You copied {onlyOnUserMap.Count} new waypoints to the cartography table.", EnumChatType.Notification);
                }
                if (onBothMapsWithChanges.Count > 0) {
                    CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, $"You modified {onBothMapsWithChanges.Count} waypoints on the cartography table.", EnumChatType.Notification);
                }
                if (onlyOnSharedMapBySameUser.Count > 0) {
                    CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, $"You removed {onlyOnSharedMapBySameUser.Count} waypoints from the cartography table.", EnumChatType.Notification);
                }
            } else {
                CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, "The cartography table is already up to date.", EnumChatType.Notification);
            }
            return sharedWaypoints;
        }

        public void updateWaypoints(IServerPlayer player) {
            SetWaypointMapLayer();
            var userWaypoints = getUserWaypoints(player);
            var sharedWaypoints = Waypoints;
            var onlyOnSharedMapByOtherUser = sharedWaypoints.FindAll(delegate (CartographyWaypoint SharedWaypoint) {
                return !SharedWaypoint.CreatedBy(player) && userWaypoints.Find(delegate (Waypoint UserWaypoint) {
                    return SharedWaypoint.CorrespondsTo(UserWaypoint) && SharedWaypoint.ContentEqualTo(UserWaypoint);
                }) == null;
            });
            var onBothMapsWithChanges = userWaypoints.FindAll(delegate (Waypoint UserWaypoint) {
                return sharedWaypoints.Find(delegate (CartographyWaypoint SharedWaypoint) {
                    return SharedWaypoint.CorrespondsTo(UserWaypoint) && !SharedWaypoint.ContentEqualTo(UserWaypoint);
                }) != null;
            });

            onlyOnSharedMapByOtherUser.ForEach(SharedWaypoint => {
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

            onBothMapsWithChanges.Foreach(UserWaypoint => {
                var edited = sharedWaypoints.Find(delegate (CartographyWaypoint SharedWaypoint) {
                    return samePosition(SharedWaypoint, UserWaypoint);
                });
                UserWaypoint.Color = edited.Color;
                UserWaypoint.Icon = edited.Icon;
                UserWaypoint.Pinned = edited.Pinned;
                UserWaypoint.Title = edited.Title;
                UserWaypoint.OwningPlayerUid = player.PlayerUID;
            });

            // TODO put in lang
            if (onlyOnSharedMapByOtherUser.Count > 0 || onBothMapsWithChanges.Count > 0) {
                CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, $"You copied {onlyOnSharedMapByOtherUser.Count} new waypoints and edited {onBothMapsWithChanges.Count} waypoints on your map.", EnumChatType.Notification);
            } else {
                CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, "Your map is already up to date.", EnumChatType.Notification);
            }
        }

        public List<Waypoint> getUserWaypoints(IServerPlayer player) {
            List<Waypoint> waypoints = new List<Waypoint>();
            if(CoreServerAPI != null) {
                var serverWorldMapManager = CoreServerAPI.ModLoader.GetModSystem<WorldMapManager>();
                if (serverWorldMapManager != null) {
                    var WaypointMapLayer = serverWorldMapManager.MapLayers.FirstOrDefault((MapLayer ml) => ml is WaypointMapLayer) as WaypointMapLayer;
                    if (WaypointMapLayer != null) {
                        waypoints = WaypointMapLayer.Waypoints.FindAll(UserWaypoint => UserWaypoint.OwningPlayerUid == player.PlayerUID);
                    }
                }
            }
            return waypoints;
        }

        private bool samePosition(Waypoint firstWaypoint, Waypoint secondWaypoint) {
            return firstWaypoint.Position.Equals(secondWaypoint.Position);
        }

        private bool sameData(Waypoint firstWaypoint, Waypoint secondWaypoint) {
            return firstWaypoint.Icon == secondWaypoint.Icon && firstWaypoint.Color == secondWaypoint.Color && firstWaypoint.Title == secondWaypoint.Title && firstWaypoint.Pinned == secondWaypoint.Pinned;
        }

        private bool isOwner(IServerPlayer player, Waypoint waypoint) {
            return waypoint.OwningPlayerUid == player.PlayerUID;
        }
    }
}