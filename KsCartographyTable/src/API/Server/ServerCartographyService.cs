using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Kaisentlaia.KsCartographyTableMod.GameContent;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Kaisentlaia.KsCartographyTableMod.API.Server
{
	public class ServerCartographyService
	{
		readonly ICoreServerAPI CoreServerAPI;
		private readonly ServerWaypointManager tableWaypointManager;
		private readonly TableMapManager tableMapManager;
		WorldMapManager WorldMapManager;
		WaypointMapLayer waypointMapLayer;
        private Dictionary<string, MapTransferSession> activeSessions = [];

        private Dictionary<string, int> uploadedChunks = [];

		Dictionary<string, ServerMapDB> tableDBConnections = new Dictionary<string, ServerMapDB>();
        
        private ServerMapDB GetBlockMapDB(string blockId) {
            if (!tableDBConnections.ContainsKey(blockId)) {
                string mapFolderPath = Path.Combine(GamePaths.DataPath, "ModData", CoreServerAPI.World.SavegameIdentifier, CartographyTableConstants.MOD_ID);
                GamePaths.EnsurePathExists(mapFolderPath);
                string mapPath = Path.Combine(mapFolderPath, blockId + ".db");
                CoreServerAPI.Logger.Notification("Initializing map database at " + mapPath);
				tableDBConnections.Add(blockId, new ServerMapDB(CoreServerAPI));
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
		public WaypointMapLayer WaypointMapLayer
		{
			get
			{
				if (waypointMapLayer == null)
				{
					WorldMapManager = CoreServerAPI.ModLoader.GetModSystem<WorldMapManager>();
					if (WorldMapManager != null)
					{
						waypointMapLayer = WorldMapManager.MapLayers.FirstOrDefault((MapLayer ml) => ml is WaypointMapLayer) as WaypointMapLayer;
					}
				}

				return waypointMapLayer;
			}
		}

		public ServerCartographyService(ICoreServerAPI ServerAPI)
		{
			CoreServerAPI = ServerAPI;

			tableMapManager = new TableMapManager(CoreServerAPI);
			tableWaypointManager = new ServerWaypointManager(CoreServerAPI);

			RegisterChannels();
		}

		public void RegisterChannels()
		{
			CoreServerAPI.Network.RegisterChannel(CartographyTableConstants.CHANNEL_UPLOAD_TO_SERVER)
				.RegisterMessageType<MapSyncPacket>()
				.SetMessageHandler<MapSyncPacket>(OnMapUploadRequest);

			CoreServerAPI.Network.RegisterChannel(CartographyTableConstants.CHANNEL_DOWNLOAD_TO_CLIENT)
				.RegisterMessageType<MapSyncPacket>();
		}

        private void OnMapUploadRequest(IServerPlayer fromPlayer, MapSyncPacket packet)
		{
            if (!uploadedChunks.ContainsKey(fromPlayer.PlayerUID))
            {
                uploadedChunks[fromPlayer.PlayerUID] = 0;
            }
            uploadedChunks[fromPlayer.PlayerUID] += packet.Pieces.Count;

            Block table = CoreServerAPI.World.BlockAccessor.GetBlock(packet.BlockPos);
            BlockEntityCartographyTable beCartographyTable = (BlockEntityCartographyTable) CoreServerAPI.World.BlockAccessor.GetBlockEntity(packet.BlockPos);
            ServerMapDB mapDB = GetBlockMapDB(packet.BlockId);
            if (table is BlockAdvancedCartographyTable)
            {
                tableMapManager.UpdateMap(fromPlayer, packet, mapDB);
            }

            if (table is BlockAdvancedCartographyTable)
            {   
                beCartographyTable.UpdateMapExploredAreasIds(mapDB.GetAllMapPiecesIds());
            }

            if (!packet.IsFinalBatch)
            {
                return;
            }

            double km2 = uploadedChunks.TryGetValue(fromPlayer.PlayerUID, out var chunkCount) ? chunkCount * 0.001024 : 0;
            uploadedChunks[fromPlayer.PlayerUID] = 0;
            WaypointSyncResult waypointResult = tableWaypointManager.UpdateTableWaypoints(fromPlayer, packet.BlockPos, mapDB);
            
            if (km2 == 0 && table is BlockAdvancedCartographyTable)
            {
                CoreServerAPI.SendMessage(fromPlayer, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.TABLE_MAP_UP_TO_DATE), EnumChatType.Notification);
            }  
            if (!waypointResult.Synced)
            {
                CoreServerAPI.SendMessage(fromPlayer, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.TABLE_WAYPOINTS_UP_TO_DATE), EnumChatType.Notification);
            }

            // BUG UpdateFinalSoundType gets called too late, once the sound has already stopped. We will probably need to send a packet instead and stop the interaction sound client side when such packet is received
            if (km2 == 0 && !waypointResult.Synced)
            {

                beCartographyTable.UpdateFinalSoundType(BlockEntityCartographyTable.EnumCartographyTableCloseSoundTypes.NothingWritten);                    
                beCartographyTable.MarkDirty();
                return;
            }
            beCartographyTable.UpdateFinalSoundType(BlockEntityCartographyTable.EnumCartographyTableCloseSoundTypes.SomethingWritten);                    
            beCartographyTable.MarkDirty();
            if (km2 > 0 && table is BlockAdvancedCartographyTable)
            {
                CoreServerAPI.SendMessage(fromPlayer, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.TABLE_MAP_UPDATED, $"{km2:F1}"), EnumChatType.Notification);
            }
            if (waypointResult.Synced)
            {
                if (waypointResult.Added > 0)
                {
                    CoreServerAPI.SendMessage(fromPlayer, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.TABLE_WAYPOINTS_ADDED, waypointResult.Added), EnumChatType.Notification);
                }
                if (waypointResult.Edited > 0)
                {
                    CoreServerAPI.SendMessage(fromPlayer, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.TABLE_WAYPOINTS_EDITED, waypointResult.Edited), EnumChatType.Notification);
                }
                if (waypointResult.Deleted > 0)
                {
                    CoreServerAPI.SendMessage(fromPlayer, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.TABLE_WAYPOINTS_DELETED, waypointResult.Deleted), EnumChatType.Notification);
                }
                if (waypointResult.Rejected > 0)
                {
                    CoreServerAPI.SendMessage(fromPlayer, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.PLAYER_WAYPOINTS_REJECTED, waypointResult.Rejected), EnumChatType.Notification);
                }
            }
            beCartographyTable.UpdateMapWaypointCount(mapDB.GetSharedWaypointsCount());         
		}

		public void WipeTableMap(Block block, IPlayer byPlayer, BlockPos blockPos)
		{
            bool hadData = false;
            ServerMapDB mapDB = GetBlockMapDB(block.Id.ToString());

            if (mapDB != null)
            {
                hadData = mapDB.GetMapPieceCount() > 0 || mapDB.GetSharedWaypointsCount() > 0;
                if (hadData)
                {
                    mapDB.Wipe();
                    BlockEntityCartographyTable blockEntity = (BlockEntityCartographyTable)CoreServerAPI.World.BlockAccessor.GetBlockEntity(blockPos);
                    blockEntity.UpdateMapWaypointCount(0);
                    blockEntity.UpdateMapExploredAreasIds([]);
                }
            }
			if (hadData)
			{
				CoreServerAPI.SendMessage(byPlayer, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.TABLE_MAP_WIPED), EnumChatType.Notification);

                // TODO change with a scraping sound
				byPlayer.Entity.World.PlaySoundAt(new AssetLocation("game:sounds/effect/writing"), byPlayer);
			}
			else
			{
                CoreServerAPI.SendMessage(null, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.TABLE_MAP_ALREADY_EMPTY), EnumChatType.Notification);
			}
		}

        public void CleanupMapData(Block block)
        {
            ServerMapDB mapDB = GetBlockMapDB(block.Id.ToString());

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

		public void MarkWaypointDeleted(IServerPlayer fromPlayer, int index)
		{
			var playerWaypoints = WaypointMapLayer.Waypoints
				.Where(p => p.OwningPlayerUid == fromPlayer.PlayerUID)
				.ToList();

			if (index < 0 || index >= playerWaypoints.Count)
			{
				return;
			}

			Waypoint deletedWaypoint = playerWaypoints[index];
			tableWaypointManager.AddDeletedWaypointId(deletedWaypoint, fromPlayer);
		}

        public void WipeWaypoints()
		{
			tableWaypointManager.ClearAllWaypoints();
		}

        public void Dispose()
        {
            tableDBConnections.Values.ToList().ForEach(connection =>
            {
                connection.Close();
                connection.Dispose();
            });
        }

        internal bool StartCartographyDownloadSession(CartographyAction action, IWorldAccessor world, IPlayer forPlayer, BlockSelection blockSel)
        {
            blockSel.Block = world.BlockAccessor.GetBlock(blockSel.Position);            
            string sessionId = blockSel.Block.Id.ToString() + forPlayer.PlayerUID;
            if (activeSessions.Get(sessionId) != null)
            {
                return false;
            }

            Dictionary<FastVec2i, MapPieceDB> newMapPiecesForPlayer = [];
            ServerMapDB mapDB = GetBlockMapDB(blockSel.Block.Id.ToString());
            if (blockSel.Block is BlockAdvancedCartographyTable)
            {
                newMapPiecesForPlayer = mapDB.GetNewMapPiecesForPlayer(forPlayer);
            }
            WaypointSyncResult waypointSyncResult = tableWaypointManager.UpdatePlayerWaypoints(forPlayer, blockSel.Position, mapDB);
            MapTransferSession session = new(forPlayer, blockSel, action, world, newMapPiecesForPlayer, CoreServerAPI, waypointSyncResult, mapDB);
            activeSessions.Add(sessionId, session);
            session.SendFirstBatch();
            return true;
        }

        internal bool ContinueCartographyDownloadSession(IPlayer byPlayer, float secondsUsed, Block block, BlockEntityCartographyTable beCartographyTable)
        {
            string sessionId = block.Id.ToString() + byPlayer.PlayerUID;

            if (!activeSessions.ContainsKey(sessionId))
            {
                return false; // No session, end interaction
            }

            MapTransferSession session = activeSessions.Get(sessionId);

            if (session.IsComplete)
            {
                beCartographyTable.UpdateFinalSoundType(session.HasSentData() ? BlockEntityCartographyTable.EnumCartographyTableCloseSoundTypes.NothingWritten : BlockEntityCartographyTable.EnumCartographyTableCloseSoundTypes.SomethingWritten);
                return true; // Keep interaction alive, player still holding button
            }

            session.TrySendNextBatch(secondsUsed);
            return true; // Always return true while player holds button
        }

        internal void EndCartographyDownloadSession(IWorldAccessor world, IPlayer byPlayer, Block block)
        {
            string sessionId = block.Id.ToString() + byPlayer.PlayerUID;

            if (activeSessions.ContainsKey(sessionId))
            {
                MapTransferSession session = activeSessions.Get(sessionId);
                session.Dispose();
                activeSessions.Remove(sessionId);
            }
        }

        internal void ResendWaypointsToPlayer(IServerPlayer toPlayer)
        {
            tableWaypointManager.ResendWaypointsToPlayer(toPlayer);
        }
    }
}