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
        public Block BlockCartographyTable { get; }
        public BlockEntityCartographyTable BlockEntity { get; }
        public BlockSelection BlockSel { get; }
        public CartographyAction Action { get; }
        public IWorldAccessor World { get; }
        public Dictionary<FastVec2i, MapPieceDB> MapPieces { get; private set; }
        
        public int TotalChunksSent { get; private set; }
        public bool IsComplete { get; set; }
        
        private Queue<Dictionary<FastVec2i, MapPieceDB>> remainingBatches;
        
        private const int BATCH_SIZE = 25;
        private string channel;

        public MapTransferSession(
            IPlayer player,
            Block blockCartographyTable,
            CartographyAction action,
            IWorldAccessor world,
            Dictionary<FastVec2i, MapPieceDB> mapPieces)
        {
            Player = player;
            BlockCartographyTable = blockCartographyTable;
            Action = action;
            World = world;
            TotalChunksSent = 0;
            MapPieces = mapPieces;
            channel = action == CartographyAction.DownloadMap ? CartographyTableConstants.CHANNEL_DOWNLOAD_TO_CLIENT : CartographyTableConstants.CHANNEL_UPLOAD_TO_SERVER;
        }

        private void SendPacket(MapUploadPacket packet)
        {
            if (World.Api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI api = World.Api as ICoreClientAPI;
                api?.Network.GetChannel(channel).SendPacket(packet);
            }
            else if (World.Api.Side == EnumAppSide.Server)
            {
                ICoreServerAPI api = World.Api as ICoreServerAPI;
                api?.Network.GetChannel(channel).SendPacket(packet);
            }
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

        public bool SendNextBatch()
        {
            if (remainingBatches.Count == 0)
            {
                return SendFinalBatch();
            }
            var batch = remainingBatches.Dequeue();
            TotalChunksSent += batch.Count;

            MapUploadPacket packet = new MapUploadPacket(
                batch,
                BlockSel.Block,
                BlockSel.Position
            );
            SendPacket(packet);

            return remainingBatches.Count > 0;
        }

        public bool SendFinalBatch()
        {
            var batch = remainingBatches.Dequeue();
            TotalChunksSent += batch.Count;

            MapUploadPacket packet = new MapUploadPacket(
                batch,
                BlockSel.Block,
                BlockSel.Position,
                true
            );
            SendPacket(packet);

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