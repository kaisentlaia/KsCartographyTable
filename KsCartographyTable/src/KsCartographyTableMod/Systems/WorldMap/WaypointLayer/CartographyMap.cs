using System;
using System.Collections.Generic;
using System.Linq;
using Kaisentlaia.KsCartographyTableMod.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
    public class CartographyMap {
        private List<CartographyWaypoint> waypoints;
        public List<CartographyWaypoint> Waypoints
        {
            get
            {
                if (waypoints == null)
                {
                    waypoints = new List<CartographyWaypoint>();
                }
                return waypoints;
            }
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
            get
            {
                if (deletedWaypoints == null)
                {
                    deletedWaypoints = new List<CartographyWaypoint>();
                }
                return deletedWaypoints;
            }
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
        ICoreAPI api;

        public CartographyMap(ICoreAPI api)
        {
            this.api = api;
        }

        public CartographyMap(List<Waypoint> initialWaypoints = null, List<Waypoint> initialDeletedWaypoints = null, IPlayer player = null)
        {
            waypoints = initialWaypoints?.Select(w => new CartographyWaypoint(w, player)).ToList()
                ?? new List<CartographyWaypoint>();

            deletedWaypoints = initialDeletedWaypoints?.Select(w => new CartographyWaypoint(w, player)).ToList()
                ?? new List<CartographyWaypoint>();
        }

        public void CreateOrUpdate(Waypoint waypoint, IPlayer player) {
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

        public bool Contains(Waypoint waypoint) {
            var foundWaypoint = Find(waypoint);
            return foundWaypoint != null;
        }

        public bool ContentEquals(Waypoint waypoint) {
            var foundWaypoint = Find(waypoint);
            if (foundWaypoint != null) {                
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

        public List<CoordsPacket> GetPalantirWaypoints()
        {
            return Waypoints.Where(waypoint => waypoint.Icon == "palantir-manual").Select(waypoint => new CoordsPacket(waypoint.Position.X, waypoint.Position.Y, waypoint.Position.Z)).ToList();
        }

        public void Serialize(ITreeAttribute tree)
        {
            try
            {
                tree.SetString("Waypoints", JsonUtil.ToString(Waypoints));
            }
            catch (Exception ex)
            {                
                api.Logger.Error("Failed to serialize waypoints: {0}", ex);
                tree.SetString("Waypoints", JsonUtil.ToString(new List<CartographyWaypoint>()));
            }
            try
            {
                tree.SetString("DeletedWaypoints", JsonUtil.ToString(DeletedWaypoints));
            }
            catch (Exception ex)
            {                
                api.Logger.Error("Failed to serialize deleted waypoints: {0}", ex);
                tree.SetString("DeletedWaypoints", JsonUtil.ToString(new List<CartographyWaypoint>()));
            }
            try
            {
                tree.SetString("ExploredAreasIds", JsonUtil.ToString(ExploredAreasIds));
            }
            catch (Exception ex)
            {                
                api.Logger.Error("Failed to serialize explored areas ids: {0}", ex);
                tree.SetString("ExploredAreasIds", JsonUtil.ToString(new List<FastVec2i>()));
            }
        }

        public void Deserialize(ITreeAttribute tree)
        {
            try
            {
                if (tree.HasAttribute("Waypoints"))
                {
                    var savedWaypoints = tree.GetString("Waypoints");
                    Waypoints = savedWaypoints != null
                        ? JsonUtil.FromString<List<CartographyWaypoint>>(savedWaypoints)
                        : new List<CartographyWaypoint>();
                }
                else
                {
                    Waypoints = new List<CartographyWaypoint>();
                }
            }
            catch (Exception ex)
            {
                api.Logger.Error("Failed to deserialize waypoints: {0}", ex);
                Waypoints = new List<CartographyWaypoint>();
            }
            try
            {
                if (tree.HasAttribute("DeletedWaypoints"))
                {
                    var deletedWaypoints = tree.GetString("DeletedWaypoints");
                    DeletedWaypoints = deletedWaypoints != null
                        ? JsonUtil.FromString<List<CartographyWaypoint>>(deletedWaypoints)
                        : new List<CartographyWaypoint>();
                }
                else
                {
                    DeletedWaypoints = new List<CartographyWaypoint>();
                }
            }
            catch (Exception ex)
            {
                api.Logger.Error("Failed to deserialize deleted waypoints: {0}", ex);
                DeletedWaypoints = new List<CartographyWaypoint>();
            }
            try
            {
                if (tree.HasAttribute("ExploredAreasIds"))
                {
                    var exploredAreasIdsStr = tree.GetString("ExploredAreasIds");
                    ExploredAreasIds = exploredAreasIdsStr != null
                        ? JsonUtil.FromString<List<ulong>>(exploredAreasIdsStr)
                        : new List<ulong>();
                }
                else
                {
                    ExploredAreasIds = new List<ulong>();
                }
            }
            catch (Exception ex)
            {                
                api.Logger.Error("Failed to deserialize explored areas ids: {0}", ex);
                ExploredAreasIds = new List<ulong>();
            }
        }
    }
}