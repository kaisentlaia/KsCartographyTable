using System;
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
				if (PlayerMapDb == null)
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
				if (PlayerMapDbReader == null)
				{
                    string mapPath = Path.Combine(GamePaths.DataPath, "Maps", CoreClientAPI.World.SavegameIdentifier + ".db");
                    playerMapDbReader = new SharedMapDB(CoreClientAPI);
                    string error = null;
                    playerMapDbReader.OpenOrCreate(mapPath, ref error, false, true, false);
				}

				return playerMapDbReader;
			}
		}
		public PlayerMapManager(ICoreClientAPI api) {
			CoreClientAPI = api;
            WorldMapManager = CoreClientAPI.ModLoader.GetModSystem<WorldMapManager>();
		}

        public bool SendMapToTable(CartographyMap map, Block block, BlockPos blockPos)
        {
            if (block.GetType() != typeof(BlockAdvancedCartographyTable))
            {
                return false;
            }

            if (playerMapDbReader != null)
            {
                List<FastVec2i> playerMapPiecesIds = playerMapDbReader.GetAllMapPiecesIds();
                HashSet<ulong> tableMapPiecesIds = [.. map.ExploredAreasIds];
                Dictionary<FastVec2i, MapPieceDB> pieces = new Dictionary<FastVec2i, MapPieceDB>();
                if (tableMapPiecesIds.Count == 0)
                {
                    pieces = playerMapDbReader.GetAllMapPieces();
                }
                else
                {
                    List<FastVec2i> filteredMapPiecesPositions = tableMapPiecesIds.Count > 0 ? playerMapPiecesIds.Where(id => !tableMapPiecesIds.Contains(id.ToChunkIndex())).ToList() : playerMapPiecesIds;
                    pieces = playerMapDbReader.GetMapPiecesFromPositions(filteredMapPiecesPositions);
                }
                if (pieces.Count == 0)
                {
                    return false;
                }
                
                const int maxChunksPerPacket = 100;

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

                        CoreClientAPI.Network.GetChannel(CartographyTableConstants.UPLOAD_CHANNEL).SendPacket(new MapUploadPacket(chunk, block, blockPos, isFinalBatch, total: isFinalBatch ? pieces.Count : 0));
                    }
                }
                else
                {
                    CoreClientAPI.Network.GetChannel(CartographyTableConstants.UPLOAD_CHANNEL).SendPacket(new MapUploadPacket(pieces, block, blockPos, true, total: pieces.Count));
                }
            }
            else
            {
                CoreClientAPI.Logger.Error("MapDB is null");
            }
            return false;
        }

        internal void UpdateMap(MapUploadPacket packet)
        {
            if (playerMapDb != null)
            {
                playerMapDb.SetMapPieces(packet.Pieces);

                if (packet.IsFinalBatch)
                {
                    CoreClientAPI.Logger.Notification("Finished downloading map pieces from server.");
                }
            }
        }
    }
}