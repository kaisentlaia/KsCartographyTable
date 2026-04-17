using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Kaisentlaia.KsCartographyTableMod.GameContent;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Kaisentlaia.KsCartographyTableMod.API.Client
{
    public class CoordsPacket
    {
        [ProtoMember(1)]
        public double X { get; set; }

        [ProtoMember(2)]
        public double Y { get; set; }

        [ProtoMember(3)]
        public double Z { get; set; }

        public CoordsPacket() { }

        public CoordsPacket(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    [ProtoContract]
    public class PalantirTravelPacket
    {
        [ProtoMember(1)]
        public List<CoordsPacket> Waypoints { get; set; } = new();

        [ProtoMember(2)]
        public CoordsPacket PlayerStartingPos { get; set; } = new();

        public PalantirTravelPacket() { }
        public PalantirTravelPacket(List<CoordsPacket> waypoints, CoordsPacket playerStartingPos)
        {
            Waypoints = waypoints;
            PlayerStartingPos = playerStartingPos;
        }
    }

    public class ClientCartographyHelper
    {
        private SharedMapDB mapDBclientReader;

        private MapDB mapDBclient;
        ICoreClientAPI CoreClientAPI;
        WorldMapManager WorldMapManager;
        ChunkMapLayer ChunkMapLayer;

        public ClientCartographyHelper(ICoreClientAPI api)
        {
            CoreClientAPI = api;

            CoreClientAPI.Network.RegisterChannel(CartographyTableConstants.UPLOAD_CHANNEL)
                .RegisterMessageType<MapUploadPacket>();

            CoreClientAPI.Network.RegisterChannel(CartographyTableConstants.DOWNLOAD_CHANNEL)
                .RegisterMessageType<MapUploadPacket>()
                .SetMessageHandler<MapUploadPacket>(OnMapDownloadRequest);

            if (CoreClientAPI.ModLoader.IsModEnabled(CartographyTableConstants.PALANTIR_MOD_ID))
            {
                CoreClientAPI.Network.RegisterChannel(CartographyTableConstants.PALANTIR_CHANNEL)
                    .RegisterMessageType<PalantirTravelPacket>();
            }

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
                mapDBclientReader = new SharedMapDB(CoreClientAPI);
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
                Dictionary<FastVec2i, MapPieceDB> pieces = new Dictionary<FastVec2i, MapPieceDB>();
                if (tableMapPiecesIds.Count == 0)
                {
                    pieces = mapDBclientReader.GetAllMapPieces();
                }
                else
                {
                    List<FastVec2i> filteredMapPiecesPositions = tableMapPiecesIds.Count > 0 ? playerMapPiecesIds.Where(id => !tableMapPiecesIds.Contains(id.ToChunkIndex())).ToList() : playerMapPiecesIds;
                    pieces = mapDBclientReader.GetMapPiecesFromPositions(filteredMapPiecesPositions);
                }
                if (pieces.Count == 0)
                {
                    CoreClientAPI.ShowChatMessage(Lang.Get(CartographyTableLangCodes.TABLE_MAP_UP_TO_DATE));
                    return;
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
        }

        public void Ponder(CartographyMap map, IClientPlayer byPlayer)
        {
            List<CoordsPacket> palantirWaypoints = map.GetPalantirWaypoints();
            PalantirTravelPacket palantirTravel = new PalantirTravelPacket(palantirWaypoints, new CoordsPacket(byPlayer.Entity.Pos.X, byPlayer.Entity.Pos.Y, byPlayer.Entity.Pos.Z));
            CoreClientAPI.Network.GetChannel(CartographyTableConstants.PALANTIR_CHANNEL).SendPacket(palantirTravel);
        }
    }
}