using System.Collections.Generic;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
    public class CartographyMapData
    {
        private Dictionary<FastVec2i, MapPieceDB> mapPieces;
        private List<CartographyWaypoint> newWaypoints;
        private List<CartographyWaypoint> editedWaypoints;
        private List<CartographyWaypoint> deletedWaypoints;

        public Dictionary<FastVec2i, MapPieceDB> MapPieces { get; private set; }
        public List<CartographyWaypoint> NewWaypoints { get; private set; }
        public List<CartographyWaypoint> EditedWaypoints { get; private set; }
        public List<CartographyWaypoint> DeletedWaypoints { get; private set; }

        private EnumCartographyTableTypes type;

        public CartographyMapData(List<CartographyWaypoint> newWaypoints, List<CartographyWaypoint> editedWaypoints, List<CartographyWaypoint> deletedWaypoints)
        {
            NewWaypoints = newWaypoints;
            EditedWaypoints = editedWaypoints;
            DeletedWaypoints = deletedWaypoints;
            MapPieces = [];
            type = EnumCartographyTableTypes.Simple;
        }

        public CartographyMapData(Dictionary<FastVec2i, MapPieceDB> mapPieces, List<CartographyWaypoint> newWaypoints, List<CartographyWaypoint> editedWaypoints, List<CartographyWaypoint> deletedWaypoints)
        {
            MapPieces = mapPieces;
            NewWaypoints = newWaypoints;
            EditedWaypoints = editedWaypoints;
            DeletedWaypoints = deletedWaypoints;
            type = EnumCartographyTableTypes.Advanced;
        }

        public bool IsEmpty()
        {
            if (type == EnumCartographyTableTypes.Simple)
            {
                return !HasWaypointData();
            }
            return !HasWaypointData() && !HasChunkData();
        }

        public bool HasChunkData()
        {
            return mapPieces.Count > 0;
        }

        public bool HasWaypointData()
        {
            return newWaypoints.Count > 0 || editedWaypoints.Count >= 0 || deletedWaypoints.Count >= 0;
        }
    }
}