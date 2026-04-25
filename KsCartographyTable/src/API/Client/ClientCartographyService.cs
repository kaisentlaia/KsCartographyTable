using System.Collections.Generic;
using System.Linq;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Kaisentlaia.KsCartographyTableMod.GameContent;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Kaisentlaia.KsCartographyTableMod.API.Client
{
    [ProtoContract]
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

    public class ClientCartographyService
    {
        ICoreClientAPI CoreClientAPI;
        WorldMapManager WorldMapManager;
        private Dictionary<string, MapTransferSession> activeSessions = [];
		private readonly ClientWaypointManager playerWaypointManager;
		private readonly PlayerMapManager playerMapManager;
        ChunkMapLayer chunkMapLayer;
        public ChunkMapLayer ChunkMapLayer
        {
            get
            {
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

        public ClientCartographyService(ICoreClientAPI api)
        {
            CoreClientAPI = api;

            playerMapManager = new PlayerMapManager(CoreClientAPI);
            playerWaypointManager = new ClientWaypointManager(CoreClientAPI);

            RegisterChannels();
        }

        public void RegisterChannels()
        {
            CoreClientAPI.Network.RegisterChannel(CartographyTableConstants.CHANNEL_UPLOAD_TO_SERVER)
                .RegisterMessageType<MapUploadPacket>();

            CoreClientAPI.Network.RegisterChannel(CartographyTableConstants.CHANNEL_DOWNLOAD_TO_CLIENT)
                .RegisterMessageType<MapUploadPacket>()
                .SetMessageHandler<MapUploadPacket>(OnMapDownloadRequest);

            if (KsCartographyTableModSystem.ModCompatibilityManager.IsPalantirEnabled)
            {
                CoreClientAPI.Network.RegisterChannel(CartographyTableConstants.CHANNEL_SEND_TO_PALANTIR)
                    .RegisterMessageType<PalantirTravelPacket>();
            }
        }

        public void OnMapDownloadRequest(MapUploadPacket packet)
        {
            playerMapManager.UpdateMap(packet);
        }

        public void Ponder(CartographyMap map, IClientPlayer byPlayer)
        {
            PalantirTravelPacket palantirTravel = new PalantirTravelPacket(
                playerWaypointManager.GetPalantirWaypoints(),
                new CoordsPacket(byPlayer.Entity.Pos.X, byPlayer.Entity.Pos.Y, byPlayer.Entity.Pos.Z)
            );
            CoreClientAPI.Network.GetChannel(CartographyTableConstants.CHANNEL_SEND_TO_PALANTIR).SendPacket(palantirTravel);
        }

        internal bool StartCartographyUploadSession(CartographyAction action, CartographyMap map, IWorldAccessor world, IPlayer byPlayer, Block block)
        {
            string sessionId = block.Id.ToString() + byPlayer.PlayerUID;
            if (activeSessions.Get(sessionId) != null)
            {
                return false;
            }
            MapTransferSession session = new MapTransferSession(byPlayer, block, action, world, playerMapManager.GetNewMapPieces(map, block));
            activeSessions.Add(sessionId, session);
            return session.SendFirstBatch();
        }

        internal bool ContinueCartographyUploadSession(IPlayer byPlayer, Block block)
        {
            string sessionId = block.Id.ToString() + byPlayer.PlayerUID;
            MapTransferSession session = activeSessions.Get(sessionId);

            if (session == null)
            {
                return false;
            }

            return session.SendNextBatch();
        }

        internal void EndCartographyUploadSession(IPlayer byPlayer, Block block)
        {
            string sessionId = block.Id.ToString() + byPlayer.PlayerUID;
            MapTransferSession session = activeSessions.Get(sessionId);

            if (session != null)
            {
                session.Dispose();
            }
        }
    }
}