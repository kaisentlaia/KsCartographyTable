using System;
using System.Collections.Generic;
using System.Linq;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
    public class MapTransferSession
    {
        public IPlayer Player { get; }
        public BlockSelection BlockSel { get; }
        public CartographyAction Action { get; }
        public IWorldAccessor World { get; }
        public ICoreAPI Api { get; }
        public WaypointSyncResult WaypointSyncResult { get; }
        public Dictionary<FastVec2i, MapPieceDB> MapPieces { get; private set; }
        public ServerMapDB MapDB { get; private set; }
        public int SentChunkCount { get; private set; }
        
        public bool IsComplete { get; set; }
        
        private Queue<Dictionary<FastVec2i, MapPieceDB>> remainingBatches;
        
        private const int BATCH_SIZE = 25;
        private const double SEND_EVERY_SECONDS = 0.2;
        private string channel;
        private double lastSendTime;

        public MapTransferSession(
            IPlayer player,
            BlockSelection blockSel,
            CartographyAction action,
            IWorldAccessor world,
            Dictionary<FastVec2i, MapPieceDB> mapPieces,
            ICoreAPI api,
            WaypointSyncResult waypointSyncResult = null, // added only for download sessions
            ServerMapDB mapDB = null // added only for download sessions
        )
        {
            Player = player;
            BlockSel = blockSel;
            Action = action;
            World = world;
            Api = api;
            MapPieces = mapPieces;
            channel = action == CartographyAction.DownloadMap ? CartographyTableConstants.CHANNEL_DOWNLOAD_TO_CLIENT : CartographyTableConstants.CHANNEL_UPLOAD_TO_SERVER;
            WaypointSyncResult = waypointSyncResult;
            MapDB = mapDB;
            SentChunkCount = 0;
        }

        private void SendPacket(MapSyncPacket packet)
        {
            if (Api is ICoreClientAPI clientApi)
            {
                var channel = clientApi.Network.GetChannel(this.channel);
                if (channel == null) {                    
                    clientApi.SendChatMessage($"Channel {CartographyTableConstants.CHANNEL_UPLOAD_TO_SERVER} not found on CLIENT");
                    Api.Logger.Error($"Channel {this.channel} not found on CLIENT");
                    return;
                }
                channel.SendPacket(packet);
            }
            else if (Api is ICoreServerAPI serverApi)
            {
                var channel = serverApi.Network.GetChannel(this.channel);
                if (channel == null) {
                    Api.Logger.Error($"Channel {this.channel} not found on SERVER");
                    return;
                }
                channel.SendPacket(packet, [Player as IServerPlayer]);
                MapDB?.SetMapPiecesForPlayer(packet.Pieces, Player);
            }
            SentChunkCount += packet.Pieces.Count;
        }

        public bool SendFirstBatch()
        {
            remainingBatches = new Queue<Dictionary<FastVec2i, MapPieceDB>>();

            var piecesList = MapPieces.ToList();

            for (int i = 0; i < piecesList.Count; i += BATCH_SIZE)
            {
                var batch = piecesList.Skip(i).Take(BATCH_SIZE).ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                );
                remainingBatches.Enqueue(batch);
            }

            return SendNextBatch();
        }
        public bool TrySendNextBatch(double currentSeconds)
        {
            // Only send if 1/4 second has passed since last send
            if (currentSeconds - lastSendTime < SEND_EVERY_SECONDS)
            {
                return true; // Still alive, just not sending yet
            }
            
            lastSendTime = currentSeconds;
            return SendNextBatch();
        }

        public bool SendNextBatch()
        {
            if (remainingBatches.Count == 0)
            {
                return SendFinalBatch();
            }
            var batch = remainingBatches.Dequeue();

            MapSyncPacket packet = new MapSyncPacket(
                batch,
                BlockSel.Block,
                BlockSel.Position
            );
            SendPacket(packet);

            if (remainingBatches.Count == 0)
            {
                return SendFinalBatch();
            }
            return true;
        }

        public bool SendFinalBatch()
        {
            if (IsComplete) return false;
            
            MapSyncPacket packet;
            if (remainingBatches.Count > 0)
            {
                var batch = remainingBatches.Dequeue();
                packet = new MapSyncPacket(batch, BlockSel.Block, BlockSel.Position, true, WaypointSyncResult);
            }
            else
            {
                packet = new MapSyncPacket([], BlockSel.Block, BlockSel.Position, true, WaypointSyncResult);
            }
            SendPacket(packet);
            IsComplete = true;
            return false;
        }

        public void Dispose()
        {
            if (remainingBatches?.Count > 0)
            {
                SendFinalBatch();
            }
            remainingBatches?.Clear();
        }

        internal bool HasSentData()
        {
            if (WaypointSyncResult != null)
            {
                return SentChunkCount > 0 || WaypointSyncResult.Synced;
            }
            return SentChunkCount > 0;
        }
    }

    public enum CartographyAction
    {
        None,
        UploadMap,
        DownloadMap,
        WipeTable,
        PonderMap
    }
}