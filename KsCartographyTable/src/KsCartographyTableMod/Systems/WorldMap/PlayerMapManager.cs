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
		ServerMapDB playerMapDbReader;
		public ServerMapDB PlayerMapDbReader
		{
			get
			{
				if (playerMapDbReader == null)
				{
                    string mapPath = Path.Combine(GamePaths.DataPath, "Maps", CoreClientAPI.World.SavegameIdentifier + ".db");
                    playerMapDbReader = new ServerMapDB(CoreClientAPI);
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

        public Dictionary<FastVec2i, MapPieceDB> GetNewMapPieces(CartographyMap map, Block forTable)
        {
            if (forTable is not BlockAdvancedCartographyTable)
            {
                return [];
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
            return pieces;
        }

        internal void UpdateMap(MapSyncPacket packet)
        {
            PlayerMapDbReader.SetMapPieces(packet.Pieces);

            if (packet.IsFinalBatch)
            {
                CoreClientAPI.Logger.Notification("Finished downloading map pieces from server.");
            }
        }
    }
}