using Vintagestory.GameContent;
using Vintagestory.API.Common;
using System;

namespace Kaisentlaia.CartographyTable.GameContent
{
    public class CartographyWaypoint : Waypoint {
        public string CreatedByPlayerUid;
        public string ModifiedByPlayerUid;
        public string SharedTitle;
        public DateTime? Created;
        public DateTime? Modified;

        public CartographyWaypoint (Waypoint waypoint, IPlayer player)
        {
            Created = DateTime.Now;
            if (player != null) {
                CreatedByPlayerUid = player.PlayerUID;
            }
            if (waypoint != null) {
                Color = waypoint.Color;
                Guid = waypoint.Guid;
                Icon = waypoint.Icon;
                Title = waypoint.Title;
                SharedTitle = $"{waypoint.Title} | Created by {player.PlayerName}";
                Text = waypoint.Text;
                ShowInWorld = waypoint.ShowInWorld;
                Pinned = waypoint.Pinned;
                Temporary = waypoint.Temporary;
                OwningPlayerGroupId = waypoint.OwningPlayerGroupId;
                OwningPlayerUid = waypoint.OwningPlayerUid;
                Position = waypoint.Position;
            }
        }

        public bool CorrespondsTo(Waypoint waypoint) {
            return Position.Equals(waypoint.Position);
        }

        public bool CreatedBy(IPlayer player) {
            return CreatedByPlayerUid == player.PlayerUID;
        }

        public bool OwnedBy(IPlayer player) {
            return OwningPlayerUid == player.PlayerUID;
        }

        public bool ModifiedBy(IPlayer player) {
            return ModifiedByPlayerUid == player.PlayerUID;
        }


        public bool ContentEqualTo(Waypoint waypoint) {
            return Icon == waypoint.Icon && Color == waypoint.Color && Title == waypoint.Title && Pinned == waypoint.Pinned;
        }
    }
}