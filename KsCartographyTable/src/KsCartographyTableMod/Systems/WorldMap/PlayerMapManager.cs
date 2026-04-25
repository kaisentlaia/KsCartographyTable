using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
	public class PlayerMapManager
	{
        WorldMapManager WorldMapManager;
		public ICoreClientAPI CoreClientAPI;
        ChunkMapLayer chunkMapLayer;
        public ChunkMapLayer ChunkMapLayer
        {
            get {
                if (chunkMapLayer == null)
                {
                    WorldMapManager = CoreClientAPI.ModLoader.GetModSystem<WorldMapManager>();
                    if (WorldMapManager != null)
                    {
                        chunkMapLayer = WorldMapManager.MapLayers.FirstOrDefault((MapLayer ml) => ml is ChunkMapLayer) as ChunkMapLayer;                    
                    }
                }

                return chunkMapLayer;
            }
        }
		MapDB playerMapDb;
		public MapDB PlayerMapDb
		{
			get
			{
				if (playerMapDb == null)
				{
                    var mapDbField = typeof(ChunkMapLayer).GetField("mapdb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    playerMapDb = mapDbField?.GetValue(ChunkMapLayer) as MapDB;
				}

				return playerMapDb;
			}
		}
		SharedMapDB playerMapDbReader;
		public SharedMapDB PlayerMapDbReader
		{
			get
			{
				if (playerMapDbReader == null)
				{
                    string mapPath = Path.Combine(GamePaths.DataPath, "Maps", CoreClientAPI.World.SavegameIdentifier + ".db");
                    playerMapDbReader = new SharedMapDB(CoreClientAPI);
                    string error = null;
                    playerMapDbReader.OpenOrCreate(mapPath, ref error, false, true, false);
				}

				return playerMapDbReader;
			}
		}
        private string deletedWaypointsFilePath;
		public PlayerMapManager(ICoreClientAPI api) {
			CoreClientAPI = api;
            WorldMapManager = CoreClientAPI.ModLoader.GetModSystem<WorldMapManager>();
		}

        public Dictionary<FastVec2i, MapPieceDB> GetNewMapPieces(CartographyMap map, Block forTable)
        {
            if (forTable is not BlockAdvancedCartographyTable)
            {
                return null;
            }
            List<FastVec2i> playerMapPiecesIds = PlayerMapDbReader.GetAllMapPiecesIds();
            HashSet<ulong> tableMapPiecesIds = [.. map.ExploredAreasIds];
            Dictionary<FastVec2i, MapPieceDB> pieces = [];
            if (tableMapPiecesIds.Count == 0)
            {
                pieces = PlayerMapDbReader.GetAllMapPieces();
            }
            else
            {
                List<FastVec2i> filteredMapPiecesPositions = tableMapPiecesIds.Count > 0 ? [.. playerMapPiecesIds.Where(id => !tableMapPiecesIds.Contains(id.ToChunkIndex()))] : playerMapPiecesIds;
                pieces = PlayerMapDbReader.GetMapPiecesFromPositions(filteredMapPiecesPositions);
            }
            if (pieces.Count == 0)
            {
                return null;
            }
            return pieces;
        }

        public bool SendMapToTable(CartographyMap map, Block forTable, BlockPos blockPos)
        {
            if (forTable is not BlockAdvancedCartographyTable)
            {
                return false;
            }

            Dictionary<FastVec2i, MapPieceDB> pieces = GetNewMapPieces(map, forTable);

            if (pieces == null)
            {
                return false;
            }

            const int maxChunksPerPacket = 100;

            // BUG this kicks out the player if they are playing on a LAN/remote server instead of a local server, if they have a big map
            if (pieces.Count > maxChunksPerPacket)
            {
                var piecesList = pieces.ToList(); // Convert to list for indexed access

                for (int i = 0; i < piecesList.Count; i += maxChunksPerPacket)
                {
                    var chunk = piecesList.Skip(i).Take(maxChunksPerPacket).ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value
                    );

                    bool isFinalBatch = i + maxChunksPerPacket >= piecesList.Count;

                    CoreClientAPI.Network.GetChannel(CartographyTableConstants.CHANNEL_UPLOAD_TO_SERVER).SendPacket(new MapUploadPacket(chunk, forTable, blockPos, isFinalBatch, totalChunksSent: isFinalBatch ? pieces.Count : 0));
                }
                return true;
            }
            else
            {
                CoreClientAPI.Network.GetChannel(CartographyTableConstants.CHANNEL_UPLOAD_TO_SERVER).SendPacket(new MapUploadPacket(pieces, forTable, blockPos, true, totalChunksSent: pieces.Count));
            }
            return false;
        }

        internal void UpdateMap(MapUploadPacket packet)
        {
            PlayerMapDbReader.SetMapPieces(packet.Pieces);

            if (packet.IsFinalBatch)
            {
                CoreClientAPI.Logger.Notification("Finished downloading map pieces from server.");
            }
        }
    }
}