using Vintagestory.GameContent;
using System;
using Vintagestory.API.MathTools;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
    public class CartographyWaypoint : Waypoint
    {
        public string ParentGuid;
        public DateTime LastUpdated;
        public bool Deleted;
        public CartographyWaypoint(Waypoint waypoint)
        {
            Color = waypoint.Color;
            Guid = waypoint.Guid;
            Icon = waypoint.Icon;
            Title = waypoint.Title;
            Pinned = waypoint.Pinned;
            OwningPlayerUid = waypoint.OwningPlayerUid;
            Position = waypoint.Position;
            LastUpdated = DateTime.Now; // TODO now or utcNow?
            Deleted = false;
        }
        public CartographyWaypoint(string guid, string parentGuid, string owningPlayerUid, string title, string icon, string position, long color, long pinned, long deleted, long lastUpdated)
        {
            var positionParts = position.Split(',');
            Color = Convert.ToInt32(color);
            Guid = guid;
            ParentGuid = parentGuid;
            Icon = icon;
            Title = title;
            Pinned = pinned == 1;
            OwningPlayerUid = owningPlayerUid;
            Position = new Vec3d(double.Parse(positionParts[0]), double.Parse(positionParts[1]), double.Parse(positionParts[2]));
            LastUpdated = DateTimeOffset.FromUnixTimeMilliseconds(lastUpdated).LocalDateTime;
            Deleted = deleted == 1;
        }
    }
}