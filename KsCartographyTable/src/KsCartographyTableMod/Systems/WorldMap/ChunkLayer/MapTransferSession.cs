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
        public CartographyMapData CartographyMapData { get; }
        
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
            CartographyMapData cartographyMapData)
        {
            Player = player;
            BlockCartographyTable = blockCartographyTable;
            Action = action;
            World = world;
            CartographyMapData = cartographyMapData;
            TotalChunksSent = 0;
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
            if (CartographyMapData.IsEmpty())
            {
                return false;
            }

            if (CartographyMapData.HasWaypointData())
            {
                bool isFinal = !CartographyMapData.HasChunkData();
                MapUploadPacket packet = new MapUploadPacket(
                    [],
                    BlockSel.Block,
                    BlockSel.Position,
                    CartographyMapData.NewWaypoints,
                    CartographyMapData.EditedWaypoints,
                    CartographyMapData.DeletedWaypoints,
                    isFinal,
                    isFinal ? CartographyMapData.NewWaypoints.Count() : 0,
                    isFinal ? CartographyMapData.EditedWaypoints.Count() : 0,
                    isFinal ? CartographyMapData.DeletedWaypoints.Count() : 0
                );
                SendPacket(packet);
            }


            var pieces = CartographyMapData.MapPieces;
            // Split into batches
            remainingBatches = new Queue<Dictionary<FastVec2i, MapPieceDB>>();

            var piecesList = pieces.ToList();

            for (int i = 0; i < piecesList.Count; i += BATCH_SIZE)
            {
                var batch = piecesList.Skip(i).Take(BATCH_SIZE).ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                );
                remainingBatches.Enqueue(batch);
            }

            return CartographyMapData.HasWaypointData() ? true : SendNextBatch();
        }

        public bool SendNextBatch(bool forceLast = false)
        {
            if (remainingBatches == null || remainingBatches.Count == 0)
            {
                return false;
            }

            var batch = remainingBatches.Dequeue();
            bool isFinal = remainingBatches.Count == 0 || forceLast;
            TotalChunksSent += batch.Count;

            // Send via network
            MapUploadPacket packet = new MapUploadPacket(
                batch,
                BlockSel.Block,
                BlockSel.Position,
                [],
                [],
                [],
                isFinal,
                isFinal ? CartographyMapData.NewWaypoints.Count() : 0,
                isFinal ? CartographyMapData.EditedWaypoints.Count() : 0,
                isFinal ? CartographyMapData.DeletedWaypoints.Count() : 0,
                isFinal ? TotalChunksSent : 0
            );
            SendPacket(packet);

            return remainingBatches.Count > 0;
        }

        public void Dispose()
        {
            if (remainingBatches?.Count > 0)
            {
                SendNextBatch(true);
            }
            remainingBatches?.Clear();
            // TODO destroy PlayerCartographyMap?
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