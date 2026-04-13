using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
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
        private int exploredAreasCount = 0;
        public int ExploredAreasCount
        {
            get { return exploredAreasCount; }
            set { exploredAreasCount = value; }
        }

        private List<ulong> exploredAreasIds = new List<ulong>();
        public List<ulong> ExploredAreasIds
        {
            get { return exploredAreasIds; }
            set { exploredAreasIds = value; }
        }


        public CartographyMap(List<Waypoint> InitialWaypoints = null, List<Waypoint> InitialDeletedWaypoints = null, IPlayer player = null) {
            waypoints = new List<CartographyWaypoint>();
            deletedWaypoints = new List<CartographyWaypoint>();
            if (InitialWaypoints != null && player != null)
            {
                InitialWaypoints.ForEach(waypoint =>
                {
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
                Update(waypoint, player);
            } else {
                waypoints.Add(new CartographyWaypoint(waypoint, player));
            }
            if (HasDeleted(waypoint)) {
                Undelete(waypoint);
            }
        }

        public void Update(Waypoint waypoint, IPlayer player) {
            var existing = Find(waypoint);
            if (existing != null) {
                existing.Color = waypoint.Color;
                existing.Icon = waypoint.Icon;
                existing.Pinned = waypoint.Pinned;
                existing.Title = waypoint.Title;
                existing.OwningPlayerUid = player.PlayerUID;
                existing.Modified = DateTime.Now;
                existing.ModifiedByPlayerUid = player.PlayerUID;
            } else {
                waypoints.Add(new CartographyWaypoint(waypoint, player));
            }
            if (deletedWaypoints.Any(dwp => dwp.Guid == waypoint.Guid)) {
                Undelete(waypoint);
            }
        }

        public void Delete(Waypoint waypoint) {
            var toDelete = waypoints.FirstOrDefault(wp => wp.Guid == waypoint.Guid);
            if (toDelete != null) {
                if (!deletedWaypoints.Any(dwp => dwp.Guid == waypoint.Guid)) {
                    deletedWaypoints.Add(toDelete);
                }
                waypoints.Remove(toDelete);
            }
        }

        private void Undelete(Waypoint waypoint) {
            var toUndelete = deletedWaypoints.FirstOrDefault(dwp => dwp.Guid == waypoint.Guid);
            if (toUndelete != null) {
                deletedWaypoints.Remove(toUndelete);
            }
        }

        public CartographyWaypoint Find(Waypoint waypoint) {
            return waypoints.FirstOrDefault(wp => wp.Guid == waypoint.Guid);
        }
        public CartographyWaypoint FindDeleted(Waypoint waypoint) {
            return deletedWaypoints.FirstOrDefault(wp => wp.Guid == waypoint.Guid);
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
            return deletedWaypoints.Any(dwp => dwp.Guid == waypoint.Guid);
        }
    }
}