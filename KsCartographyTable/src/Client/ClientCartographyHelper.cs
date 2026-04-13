using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kaisentlaia.CartographyTable.Blocks;
using Kaisentlaia.CartographyTable.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Kaisentlaia.CartographyTable.Client
{
    public class ClientCartographyHelper
    {
        private ExtendedMapDB mapDBclientReader;

        private MapDB mapDBclient;
        ICoreClientAPI CoreClientAPI;
        WorldMapManager WorldMapManager;
        ChunkMapLayer ChunkMapLayer;

        public ClientCartographyHelper(ICoreClientAPI api)
        {
            CoreClientAPI = api;

            CoreClientAPI.Network.RegisterChannel("cartographytablechannel" + EnumCartographyMapChannels.CHANNEL_UPLOAD)
                .RegisterMessageType<MapUploadPacket>();

            CoreClientAPI.Network.RegisterChannel("cartographytablechannel" + EnumCartographyMapChannels.CHANNEL_DOWNLOAD)
                .RegisterMessageType<MapUploadPacket>()
                .SetMessageHandler<MapUploadPacket>(OnMapDownloadRequest);
        }

        private void SetChunkMapLayer()
        {
            if (ChunkMapLayer == null)
            {
                WorldMapManager = CoreClientAPI.ModLoader.GetModSystem<WorldMapManager>();
                if (WorldMapManager != null)
                {
                    ChunkMapLayer = WorldMapManager.MapLayers.FirstOrDefault((MapLayer ml) => ml is ChunkMapLayer) as ChunkMapLayer;                    
                }
            }
        }
        
        private MapDB GetGameMapDB()
        {
            SetChunkMapLayer();            
            if (ChunkMapLayer == null) return null;
            
            // Access private field via reflection
            var field = typeof(ChunkMapLayer).GetField("mapdb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(ChunkMapLayer) as MapDB;
        }

        public void OnMapDownloadRequest(MapUploadPacket packet)
        {
            if (mapDBclient == null)
            {
                mapDBclient = GetGameMapDB();
            }

            if (mapDBclient != null)
            {
                mapDBclient.SetMapPieces(packet.Pieces);

                if (packet.IsFinalBatch)
                {
                    CoreClientAPI.Logger.Notification("Finished downloading map pieces from server.");
                }
            }
        }

        public void UpdateTableMap(CartographyMap map, IClientPlayer player, Block block, BlockPos blockPos)
        {
            if (block.GetType() != typeof(BlockAdvancedCartographyTable))
            {
                return;
            }
            if (mapDBclientReader == null)
            {
                string mapPath = Path.Combine(GamePaths.DataPath, "Maps", CoreClientAPI.World.SavegameIdentifier + ".db");
                mapDBclientReader = new ExtendedMapDB(CoreClientAPI.World.Logger);
                string error = null;
                mapDBclientReader.OpenOrCreate(mapPath, ref error, false, true, false);

                // Check if connection failed
                if (error != null)
                {
                    CoreClientAPI.Logger.Error("Failed to open map database: {0}", error);
                    mapDBclientReader = null;
                }
            }

            if (mapDBclientReader != null)
            {
                List<FastVec2i> playerMapPiecesIds = mapDBclientReader.GetAllMapPiecesIds();
                HashSet<ulong> tableMapPiecesIds = [.. map.ExploredAreasIds];
                List<FastVec2i> filteredMapPiecesPositions = tableMapPiecesIds.Count > 0 ? playerMapPiecesIds.Where(id => !tableMapPiecesIds.Contains(id.ToChunkIndex())).ToList() : playerMapPiecesIds;
                if (filteredMapPiecesPositions.Count == 0 && tableMapPiecesIds.Count > 0)
                {
                    CoreClientAPI.Logger.Notification("Nothing to upload");
                    CoreClientAPI.ShowChatMessage(Lang.Get("kscartographytable:message-table-map-up-to-date"));
                    return;
                }
                Dictionary<FastVec2i, MapPieceDB> pieces = tableMapPiecesIds.Count == 0 ? mapDBclientReader.GetAllMapPieces() : mapDBclientReader.GetMapPiecesFromPositions(filteredMapPiecesPositions);
                
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

                        CoreClientAPI.Network.GetChannel("cartographytablechannel" + EnumCartographyMapChannels.CHANNEL_UPLOAD).SendPacket(new MapUploadPacket(chunk, block, blockPos, isFinalBatch: i + maxChunksPerPacket >= piecesList.Count));
                    }
                }
                else
                {
                    CoreClientAPI.Network.GetChannel("cartographytablechannel" + EnumCartographyMapChannels.CHANNEL_UPLOAD).SendPacket(new MapUploadPacket(pieces, block, blockPos, true));
                }
            }
            else
            {
                CoreClientAPI.Logger.Error("MapDB is null");
            }
        }
    }
}