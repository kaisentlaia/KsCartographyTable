using Kaisentlaia.KsCartographyTableMod.API.Common;
using Vintagestory.API.Server;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
	public class TableMapManager
	{
		public ICoreServerAPI CoreServerAPI;
		public TableMapManager(ICoreServerAPI api) {
			CoreServerAPI = api;
		}

		public void UpdateMap(IServerPlayer fromPlayer, MapSyncPacket packet, ServerMapDB mapDB)
        {
			KsCartographyTableModSystem.DebugLog(CoreServerAPI, $"UpdateMap with {packet.Pieces.Count} pieces, mapdb exists {mapDB != null}");
            if (mapDB != null)
            {
                mapDB.SetMapPieces(packet.Pieces);
                mapDB.SetMapPiecesForPlayer(packet.Pieces, fromPlayer);
				
				BlockEntityCartographyTable blockEntity = (BlockEntityCartographyTable)CoreServerAPI.World.BlockAccessor.GetBlockEntity(packet.BlockPos);
				blockEntity?.UpdateMapExploredAreasIds(mapDB.GetAllMapPiecesIds());
            }
        }
    }
}