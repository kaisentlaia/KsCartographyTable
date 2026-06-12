using System;
using System.Collections.Generic;
using System.Linq;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Kaisentlaia.KsCartographyTableMod.API.Server;
using Kaisentlaia.KsCartographyTableMod.GameContent;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
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

    public class ClientCartographyService : IDisposable
    {
        ICoreClientAPI CoreClientAPI;
        WorldMapManager WorldMapManager;
        private Dictionary<string, MapTransferSession> activeSessions = [];
		private readonly PlayerMapManager playerMapManager;
        ChunkMapLayer chunkMapLayer;
        private Dictionary<string, int> downloadedChunks = [];
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

            RegisterChannels();
        }

        public void RegisterChannels()
        {
            CoreClientAPI.Network.RegisterChannel(CartographyTableConstants.CHANNEL_UPLOAD_TO_SERVER)
                .RegisterMessageType<MapSyncPacket>();

            CoreClientAPI.Network.RegisterChannel(CartographyTableConstants.CHANNEL_DOWNLOAD_TO_CLIENT)
                .RegisterMessageType<MapSyncPacket>()
                .SetMessageHandler<MapSyncPacket>(OnMapDownloadRequest);

            if (KsCartographyTableModSystem.ModCompatibilityManager.IsPalantirEnabled)
            {
                CoreClientAPI.Network.RegisterChannel(CartographyTableConstants.CHANNEL_SEND_TO_PALANTIR)
                    .RegisterMessageType<PalantirTravelPacket>();
            }

            CoreClientAPI.Network.RegisterChannel(CartographyTableConstants.CHANNEL_COMMANDS)
                .RegisterMessageType<KctCommandPacket>();
        }

        public void WipeWaypoints(bool dryRun)
        {
            CoreClientAPI.Network.GetChannel(CartographyTableConstants.CHANNEL_COMMANDS).SendPacket(new KctCommandPacket(KctCommand.wipe, dryRun));
        }

        public void OnMapDownloadRequest(MapSyncPacket packet)
        {
            IClientPlayer currentPlayer = CoreClientAPI.World.Player;
            playerMapManager.UpdateMap(packet);
            if (!downloadedChunks.ContainsKey(currentPlayer.PlayerUID))
            {
                downloadedChunks[currentPlayer.PlayerUID] = 0;
            }
            downloadedChunks[currentPlayer.PlayerUID] += packet.Pieces.Count;

            if (!packet.IsFinalBatch) return;

            FinalizeDownload(packet, currentPlayer);
        }

        private void FinalizeDownload(MapSyncPacket packet, IClientPlayer currentPlayer)
        {
            BlockEntityCartographyTable blockEntity = (BlockEntityCartographyTable) CoreClientAPI.World.BlockAccessor.GetBlockEntity(packet.BlockPos);

            if (blockEntity == null)
            {
                CoreClientAPI.Logger.Error($"{CartographyTableConstants.MAP_EVENT} Cannot finalize download for null blockentity!");
                return;
            }
       
            double km2 = downloadedChunks.TryGetValue(currentPlayer.PlayerUID, out var chunkCount) ? chunkCount * 0.001024 : 0;
            downloadedChunks[currentPlayer.PlayerUID] = 0;
            bool mapUpdated = km2 > 0;
            bool waypointsUpdated = packet.WaypointSyncResult.Synced;
            if (!mapUpdated && blockEntity.IsAdvanced)
            {                
                KsCartographyTableModSystem.ShowChatMessage(CoreClientAPI, currentPlayer, CartographyTableLangCodes.PLAYER_MAP_UP_TO_DATE);
            }  
            if (!waypointsUpdated)
            {
                KsCartographyTableModSystem.ShowChatMessage(CoreClientAPI, currentPlayer, CartographyTableLangCodes.PLAYER_WAYPOINTS_UP_TO_DATE);
            }
            blockEntity.SetWriting(false);
            blockEntity.SetPlayerSyncToNow(currentPlayer);
            if (!mapUpdated && !waypointsUpdated)
            {
                return;
            }
            if (mapUpdated && blockEntity.IsAdvanced)
            {
                KsCartographyTableModSystem.ShowChatMessage(CoreClientAPI, currentPlayer, CartographyTableLangCodes.PLAYER_MAP_UPDATED, $"{km2:F1}");
            }
            if (waypointsUpdated)
            {
                if (packet.WaypointSyncResult.Added > 0)
                {
                    KsCartographyTableModSystem.ShowChatMessage(CoreClientAPI, currentPlayer, CartographyTableLangCodes.PLAYER_WAYPOINTS_ADDED, packet.WaypointSyncResult.Added.ToString());
                }
                if (packet.WaypointSyncResult.Edited > 0)
                {
                    KsCartographyTableModSystem.ShowChatMessage(CoreClientAPI, currentPlayer, CartographyTableLangCodes.PLAYER_WAYPOINTS_EDITED, packet.WaypointSyncResult.Edited.ToString());
                }
                if (packet.WaypointSyncResult.Deleted > 0)
                {
                    KsCartographyTableModSystem.ShowChatMessage(CoreClientAPI, currentPlayer, CartographyTableLangCodes.PLAYER_WAYPOINTS_DELETED, packet.WaypointSyncResult.Deleted.ToString());
                }
            }
        }

        public void Ponder(IClientPlayer byPlayer, BlockEntityCartographyTable blockEntity)
        {
            PalantirTravelPacket palantirTravel = new PalantirTravelPacket(
                [.. blockEntity.Map.PalantirWaypoints.Select(waypoint =>
                {
                    return new CoordsPacket(waypoint.X, waypoint.Y, waypoint.Z);
                })],
                new CoordsPacket(byPlayer.Entity.Pos.X, byPlayer.Entity.Pos.Y, byPlayer.Entity.Pos.Z)
            );
            CoreClientAPI.Network.GetChannel(CartographyTableConstants.CHANNEL_SEND_TO_PALANTIR).SendPacket(palantirTravel);
        }

        internal bool HasCartographyUploadSession(IPlayer byPlayer, Block block)
        {
            if (block == null)
            {
                return false;
            }
            string sessionId = block.Id.ToString() + byPlayer.PlayerUID;
            if (activeSessions.ContainsKey(sessionId))
            {
                return true;
            }
            return false;
        }

        internal bool StartCartographyUploadSession(CartographyAction action, IWorldAccessor world, IPlayer byPlayer, BlockPos blockPos, Block block, BlockEntityCartographyTable blockEntity)
        {
            string sessionId = block.Id.ToString() + byPlayer.PlayerUID;
            if (activeSessions.ContainsKey(sessionId))
            {
                KsCartographyTableModSystem.DebugLog(CoreClientAPI, $"session already exists");
                return false;
            }
            KsCartographyTableModSystem.ShowChatMessage(CoreClientAPI, byPlayer, CartographyTableLangCodes.SESSION_STARTED);
            KsCartographyTableModSystem.DebugLog(CoreClientAPI, $"starting new session");
            MapTransferSession session = new(byPlayer, block, blockPos, action, world, playerMapManager.GetNewMapPieces(blockEntity), CoreClientAPI);
            activeSessions.Add(sessionId, session);
            blockEntity.SetWriting(true);
            session.SendFirstBatch();
            return true; 
        }

        internal bool ContinueCartographyUploadSession(IPlayer byPlayer, float secondsUsed, Block block)
        {
            string sessionId = block.Id.ToString() + byPlayer.PlayerUID;

            if (!activeSessions.ContainsKey(sessionId))
            {
                return false; // No session, end interaction
            }

            MapTransferSession session = activeSessions.Get(sessionId);

            if (session.IsComplete)
            {                                          
                return true; // Keep interaction alive, player still holding button
            }

            session.TrySendNextBatch(secondsUsed);
            return true; // Always return true while player holds button
        }


        internal void EndCartographyUploadSession(IPlayer byPlayer, Block block,  BlockEntityCartographyTable blockEntity)
        {
            string sessionId = block.Id.ToString() + byPlayer.PlayerUID;

            if (activeSessions.ContainsKey(sessionId))
            {
                MapTransferSession session = activeSessions.Get(sessionId);
                session.Dispose();
                activeSessions.Remove(sessionId);
            }
            blockEntity.SetWriting(false);
            blockEntity.ClearRecentInteraction(byPlayer);
        }

        public void Dispose()
        {
            activeSessions.Values.Foreach((session) => session.Dispose());
            playerMapManager.Dispose();
        }
    }
}