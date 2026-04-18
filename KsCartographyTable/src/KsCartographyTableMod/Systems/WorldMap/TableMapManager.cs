using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
	public class TableMapManager
	{
		public ICoreServerAPI CoreServerAPI;
		public TableMapManager(ICoreServerAPI api) {
			CoreServerAPI = api;
		}

		Dictionary<string, SharedMapDB> tableDBConnections = new Dictionary<string, SharedMapDB>();
        
        private SharedMapDB GetBlockMapDB(string blockId) {
            if (tableDBConnections.Get(blockId) == null) {
                string mapFolderPath = Path.Combine(GamePaths.DataPath, "ModData", CoreServerAPI.World.SavegameIdentifier, CartographyTableConstants.MOD_ID);
                GamePaths.EnsurePathExists(mapFolderPath);
                string mapPath = Path.Combine(mapFolderPath, blockId + ".db");
                CoreServerAPI.Logger.Notification("Initializing map database at " + mapPath);
				tableDBConnections.Add(blockId, new SharedMapDB(CoreServerAPI));
                string error = null;
                tableDBConnections.Get(blockId).OpenOrCreate(mapPath, ref error, true, true, false);

                // Check if connection failed
                if (error != null) {
                    CoreServerAPI.Logger.Error("Failed to open map database: {0}", error);
                    return null;
                }
            }

			return tableDBConnections.Get(blockId);
        }

		public double UpdateMap(IServerPlayer fromPlayer, MapUploadPacket packet)
        {
            SharedMapDB mapDB = GetBlockMapDB(packet.BlockId);

            if (mapDB != null)
            {
                mapDB.SetMapPieces(packet.Pieces);
                mapDB.SetMapPiecesForPlayer(packet.Pieces, fromPlayer);
			
				if (packet.IsFinalBatch)
				{
					BlockEntityCartographyTable blockEntity = (BlockEntityCartographyTable)CoreServerAPI.World.BlockAccessor.GetBlockEntity(packet.BlockPos);
					if (blockEntity != null)
					{
						blockEntity.UpdateMapExploredAreasIds(GetBlockMapDB(packet.BlockId).GetAllMapPiecesIds());
					}
					if (packet.IsFinalBatch && packet.Total > 0)
					{                    
						double km2 = packet.Total * 0.001024;
						return km2;
					}
				}
            }
			return 0;
        }

		public double SendMapToPlayer(IServerPlayer player, Block block, BlockPos blockPos)
        {
            if (block.GetType() != typeof(BlockAdvancedCartographyTable))
            {
                return 0;
            }
            
            SharedMapDB mapDB = GetBlockMapDB(block.Id.ToString());

            if (mapDB != null)
            {
                Dictionary<FastVec2i, MapPieceDB> pieces = mapDB.GetNewMapPiecesForPlayer(player);

                if (pieces.Count == 0)
                {
                    return 0;
                }
                const int maxChunksPerPacket = 100;

                if (pieces.Count > maxChunksPerPacket)
                {
                    var piecesList = pieces.ToList(); // Convert to list for indexed access

                    for (int i = 0; i < piecesList.Count; i += maxChunksPerPacket)
                    {
                        var chunk = piecesList.Skip(i).Take(maxChunksPerPacket).ToDictionary(
                            kvp => kvp.Key,
                            kvp => MapColorOverlay.ApplyColorOverlay(kvp.Value)
                        );

                        CoreServerAPI.Network.GetChannel(CartographyTableConstants.DOWNLOAD_CHANNEL).SendPacket(new MapUploadPacket(chunk, block, blockPos, isFinalBatch: i + maxChunksPerPacket >= piecesList.Count), player);
                    }
                }
                else
                {
                    CoreServerAPI.Network.GetChannel(CartographyTableConstants.DOWNLOAD_CHANNEL).SendPacket(new MapUploadPacket(pieces, block, blockPos, true), player);
                }
                mapDB.SetMapPiecesForPlayer(pieces, player);
                double km2 = pieces.Count * 0.001024;
                return km2;
            }
            else
            {
                CoreServerAPI.Logger.Error("SharedMapDB is null");
                return 0;
            }
        }
		public bool Wipe(Block block, BlockPos blockPos)
		{
			if (block.GetType() == typeof(BlockAdvancedCartographyTable))
			{
				SharedMapDB mapDB = GetBlockMapDB(block.Id.ToString());

				if (mapDB != null)
				{
					mapDB.Wipe();
					BlockEntityCartographyTable blockEntity = (BlockEntityCartographyTable)CoreServerAPI.World.BlockAccessor.GetBlockEntity(blockPos);
                    blockEntity.UpdateMapExploredAreasIds(new List<FastVec2i>());
					return true;       
				}
			}
			return false;
		}

        public void CleanupMapData(Block block, BlockPos pos)
        {
			if (block.GetType() == typeof(BlockAdvancedCartographyTable))
			{
				SharedMapDB mapDB = GetBlockMapDB(block.Id.ToString());

                if (mapDB != null)
                {
                    mapDB?.Dispose();
                    // Delete the .db file from disk
                    string mapPath = Path.Combine(GamePaths.DataPath, "ModData",
                        CoreServerAPI.World.SavegameIdentifier,
                        CartographyTableConstants.MOD_ID,
                        block.Id + ".db");

                    if (File.Exists(mapPath))
                    {
                        File.Delete(mapPath);
                    }

                    tableDBConnections.Remove(block.Id.ToString());
				}
			}
        }

        public void Dispose()
        {
            tableDBConnections.Values.ToList().ForEach(connection =>
            {
                connection.Close();
                connection.Dispose();
            });
        }
	}
}