using System;
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
		private readonly PlayerWaypointManager playerWaypointManager;
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
            playerWaypointManager = new PlayerWaypointManager(CoreClientAPI);

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

        public void UpdateTableMap(CartographyMap map, Block block, BlockPos blockPos)
        {
            if (!playerMapManager.SendMapToTable(map, block, blockPos))
            {
                if (block is BlockAdvancedCartographyTable)
                {
                    CoreClientAPI.ShowChatMessage(Lang.Get(CartographyTableLangCodes.TABLE_MAP_UP_TO_DATE));   
                }
            }
        }

        public void Ponder(CartographyMap map, IClientPlayer byPlayer)
        {
            PalantirTravelPacket palantirTravel = new PalantirTravelPacket(
                map.GetPalantirWaypoints(),
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
            List<CartographyWaypoint> newWaypoints = playerWaypointManager.GetNewWaypoints(map);
            List<CartographyWaypoint> editedWaypoints = playerWaypointManager.GetEditedWaypoints(map);
            List<CartographyWaypoint> deletedWaypoints = playerWaypointManager.GetDeletedWaypoints(map);
            CartographyMapData playerCartographyMap;
            if (block is BlockAdvancedCartographyTable)
            {
                Dictionary<FastVec2i, MapPieceDB> mapPieces = playerMapManager.GetNewMapPieces(map, block);
                playerCartographyMap = new CartographyMapData(
                    mapPieces,
                    newWaypoints,
                    editedWaypoints,
                    deletedWaypoints
                );
            }
            else
            {
                playerCartographyMap = new CartographyMapData(
                    newWaypoints,
                    editedWaypoints,
                    deletedWaypoints
                );
            }
            if (playerCartographyMap.IsEmpty())
            {
                // TODO send table up to date message
                return false;
            }
            MapTransferSession session = new MapTransferSession(byPlayer, block, action, world, playerCartographyMap);
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