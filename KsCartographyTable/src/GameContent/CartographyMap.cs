using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Kaisentlaia.CartographyTable.GameContent
{
    public class CartographyMap {
        private List<CartographyWaypoint> waypoints;
        public List<CartographyWaypoint> Waypoints
        {
            get { return waypoints; }
            set {                
                if (value != null) {
                    waypoints = value;
                } else {
                    waypoints = new List<CartographyWaypoint>();
                }
            }
        }
        private List<CartographyWaypoint> deletedWaypoints;
        public List<CartographyWaypoint> DeletedWaypoints
        {
            get { return deletedWaypoints; }
            set {                
                if (value != null) {
                    deletedWaypoints = value;
                } else {
                    deletedWaypoints = new List<CartographyWaypoint>();
                }
            }
        }

        public CartographyMap(List<Waypoint> InitialWaypoints = null, List<Waypoint> InitialDeletedWaypoints = null, IPlayer player = null) {
            Waypoints = null;
            DeletedWaypoints = null;
            if (InitialWaypoints != null && player != null) {
                InitialWaypoints.ForEach(waypoint => {
                    waypoints.Add(new CartographyWaypoint(waypoint, player));
                });
            }
            if (InitialDeletedWaypoints != null && player != null) {
                InitialDeletedWaypoints.ForEach(waypoint => {
                    deletedWaypoints.Add(new CartographyWaypoint(waypoint, player));
                });
            }
        }

        public void Create(Waypoint waypoint, IPlayer player) {
            if(Contains(waypoint)) {
                // failsafe, already exists, avoid duplication
                Update(waypoint, player);
            } else {
                waypoints.Add(new CartographyWaypoint(waypoint, player));
            }
            if (HasDeleted(waypoint)) {
                Console.WriteLine("warning - added waypoint present in deleted waypoints");
                Undelete(waypoint);
            }
        }

        public void Update(Waypoint waypoint, IPlayer player) {
            if(Contains(waypoint)) {
                var toEdit = Find(waypoint);
                if (toEdit != null) {
                    toEdit.Color = waypoint.Color;
                    toEdit.Icon = waypoint.Icon;
                    toEdit.Pinned = waypoint.Pinned;
                    toEdit.Title = waypoint.Title;
                    toEdit.OwningPlayerUid = player.PlayerUID;
                    toEdit.Modified = DateTime.Now;
                    toEdit.ModifiedByPlayerUid = player.PlayerUID;
                } else {
                    // failsafe, didn't exist
                    Console.WriteLine("warning - no waypoint to edit");
                    Create(waypoint, player);
                }
            } else {
                // failsafe, didn't exist
                waypoints.Add(new CartographyWaypoint(waypoint, player));
                Create(waypoint, player);
            }
            if (deletedWaypoints.Find(dwp => dwp.CorrespondsTo(waypoint)) != null) {
                Console.WriteLine("warning - created waypoint present in deleted waypoints");
                Undelete(waypoint);
            }
        }

        public void Delete(Waypoint waypoint) {
            var toDelete = Find(waypoint);
            if (toDelete != null) {
                if(!HasDeleted(waypoint)) {
                    deletedWaypoints.Add(toDelete);
                }
                if(Contains(waypoint)) {
                    waypoints.Remove(toDelete);
                }
            }
        }

        private void Undelete(Waypoint waypoint) {
            var toUndelete = FindDeleted(waypoint);
            if(toUndelete != null) {
                deletedWaypoints.Remove(toUndelete);
            }

        }

        public CartographyWaypoint Find(Waypoint waypoint) {
            return waypoints.Find(wp => wp.CorrespondsTo(waypoint));
        }
        public CartographyWaypoint FindDeleted(Waypoint waypoint) {
            return deletedWaypoints.Find(wp => wp.CorrespondsTo(waypoint));
        }

        public bool Contains(Waypoint waypoint, bool sameContent = false) {
            var foundWaypoint = Find(waypoint);
            if (foundWaypoint != null && sameContent) {                
                return foundWaypoint.ContentEqualTo(waypoint);
            }
            return foundWaypoint != null;
        }

        public bool HasEdits(Waypoint waypoint) {
            var MapWaypoint = Find(waypoint);
            if (MapWaypoint != null) {
                var different = !MapWaypoint.ContentEqualTo(waypoint);
                var userEdited = MapWaypoint.ModifiedByPlayerUid != null;
                return different || userEdited;
            }
            return false;
        }

        public bool HasDeleted(Waypoint waypoint) {
            return FindDeleted(waypoint) != null;
        }
    }
}