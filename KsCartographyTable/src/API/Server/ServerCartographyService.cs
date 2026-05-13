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
                CoreServerAPI.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} Initializing map database at {mapPath}");
				tableDBConnections.Add(blockId, new ServerMapDB(CoreServerAPI));
                string error = null;
                tableDBConnections.Get(blockId).OpenOrCreate(mapPath, ref error, true, true, false);

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

            BlockEntityCartographyTable beCartographyTable = (BlockEntityCartographyTable) CoreServerAPI.World.BlockAccessor.GetBlockEntity(packet.BlockPos);
            ServerMapDB mapDB = GetBlockMapDB(packet.BlockId);
            beCartographyTable.SetWriting(true);
            if (beCartographyTable.IsAdvanced)
            {
                tableMapManager.UpdateMap(fromPlayer, packet, mapDB);
            }

            if (!packet.IsFinalBatch)
            {
                return;
            }

            beCartographyTable.SetPlayerSyncToNow(fromPlayer);

            double km2 = uploadedChunks.TryGetValue(fromPlayer.PlayerUID, out var chunkCount) ? chunkCount * 0.001024 : 0;
            uploadedChunks[fromPlayer.PlayerUID] = 0;
            WaypointSyncResult waypointResult = tableWaypointManager.UpdateTableWaypoints(fromPlayer, packet.BlockPos, mapDB);
            
            if (km2 == 0 && beCartographyTable.IsAdvanced)
            {
                KsCartographyTableModSystem.ShowChatMessage(CoreServerAPI, fromPlayer, CartographyTableLangCodes.TABLE_MAP_UP_TO_DATE);
            }  
            if (!waypointResult.Synced)
            {
                if (waypointResult.Rejected > 0)
                {
                    KsCartographyTableModSystem.ShowChatMessage(CoreServerAPI, fromPlayer, CartographyTableLangCodes.PLAYER_WAYPOINTS_REJECTED, waypointResult.Rejected.ToString());
                    CoreServerAPI.SendIngameError(fromPlayer, "mapfailure", Lang.Get(CartographyTableLangCodes.FAILURE_UPDATE_FIRST));
                }
                else
                {
                    KsCartographyTableModSystem.ShowChatMessage(CoreServerAPI, fromPlayer, CartographyTableLangCodes.TABLE_WAYPOINTS_UP_TO_DATE);                    
                }
            }

            if (km2 == 0 && !waypointResult.Synced)
            {
                CoreServerAPI.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} Setting written to false and writing to false");
                beCartographyTable.SetWritten(false);
                beCartographyTable.SetWriting(false);
                return;
            }
            if (km2 > 0 && beCartographyTable.IsAdvanced)
            {
                CoreServerAPI.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} Setting written to true");
                beCartographyTable.SetWritten(true);
                KsCartographyTableModSystem.ShowChatMessage(CoreServerAPI, fromPlayer, CartographyTableLangCodes.TABLE_MAP_UPDATED, $"{km2:F1}");
            }
            if (waypointResult.Synced)
            {
                CoreServerAPI.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} Setting written to true");
                beCartographyTable.SetWritten(true);
                if (waypointResult.Added > 0)
                {
                    KsCartographyTableModSystem.ShowChatMessage(CoreServerAPI, fromPlayer, CartographyTableLangCodes.TABLE_WAYPOINTS_ADDED, waypointResult.Added.ToString());
                }
                if (waypointResult.Edited > 0)
                {
                    KsCartographyTableModSystem.ShowChatMessage(CoreServerAPI, fromPlayer, CartographyTableLangCodes.TABLE_WAYPOINTS_EDITED, waypointResult.Edited.ToString());
                }
                if (waypointResult.Deleted > 0)
                {
                    KsCartographyTableModSystem.ShowChatMessage(CoreServerAPI, fromPlayer, CartographyTableLangCodes.TABLE_WAYPOINTS_DELETED, waypointResult.Deleted.ToString());
                }
            }
            CoreServerAPI.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} Setting writing to false");
            beCartographyTable.SetWriting(false);
            beCartographyTable.UpdateMapWaypointCount(mapDB.GetSharedWaypointsCount());
            beCartographyTable.SetPalantirWaypointPositions(mapDB.GetPalantirWaypointPositions());
		}

		public void WipeTableMap(Block block, IPlayer byPlayer, BlockEntityCartographyTable blockEntity)
		{
            bool hadData = false;
            ServerMapDB mapDB = GetBlockMapDB(block.Id.ToString());
            CoreServerAPI.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} WipeTableMap blockId {block.Id} has mapDb {mapDB != null}");

            if (mapDB != null)
            {
                hadData = mapDB.GetMapPieceCount() > 0 || mapDB.GetSharedWaypointsCount() > 0;
                if (hadData)
                {
                    CoreServerAPI.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} map had data, wiping");
                    mapDB.Wipe();
                }
            }
			if (hadData)
			{
                KsCartographyTableModSystem.ShowChatMessage(CoreServerAPI, byPlayer, CartographyTableLangCodes.TABLE_MAP_WIPED);
			}
            blockEntity.UpdateMapWaypointCount(0);
            blockEntity.UpdateMapExploredAreasIds([]);
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
            try
            {
                var connections = tableDBConnections.Values.ToList();
                tableDBConnections.Clear();
                foreach (var connection in connections)
                {
                    try
                    {
                        connection?.Close();
                        connection?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        CoreServerAPI.Logger.Error("Error disposing map database: {0}", ex);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                tableDBConnections.Clear();
                CoreServerAPI.Logger.Warning("Map database connections were modified during disposal; some SQLite connections may not have been closed cleanly.");
            }
        }

        internal bool HasCartographyDownloadSession(IPlayer byPlayer, Block block)
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

        internal bool StartCartographyDownloadSession(CartographyAction action, IWorldAccessor world, IPlayer forPlayer, Block block, BlockPos blockPos, BlockEntityCartographyTable beCartographyTable)
        {
            string sessionId = block.Id.ToString() + forPlayer.PlayerUID;
            if (activeSessions.Get(sessionId) != null)
            {
                return false;
            }
            beCartographyTable.SetWriting(true);
            KsCartographyTableModSystem.ShowChatMessage(CoreServerAPI, forPlayer, CartographyTableLangCodes.SESSION_STARTED);

            Dictionary<FastVec2i, MapPieceDB> newMapPiecesForPlayer = [];
            ServerMapDB mapDB = GetBlockMapDB(block.Id.ToString());
            if (beCartographyTable.IsAdvanced)
            {
                newMapPiecesForPlayer = mapDB.GetNewMapPiecesForPlayer(forPlayer);
            }
            WaypointSyncResult waypointSyncResult = tableWaypointManager.UpdatePlayerWaypoints(forPlayer, beCartographyTable, mapDB);
            MapTransferSession session = new(forPlayer, block, blockPos, action, world, newMapPiecesForPlayer, CoreServerAPI, waypointSyncResult, mapDB);
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
                beCartographyTable.SetWritten(session.HasSentData());
                beCartographyTable.SetWriting(false);
                return true; // Keep interaction alive, player still holding button
            }

            session.TrySendNextBatch(secondsUsed);
            return true; // Always return true while player holds button
        }

        internal void EndCartographyDownloadSession(IPlayer byPlayer, Block block, BlockEntityCartographyTable beCartographyTable)
        {
            string sessionId = block.Id.ToString() + byPlayer.PlayerUID;

            if (activeSessions.ContainsKey(sessionId))
            {
                MapTransferSession session = activeSessions.Get(sessionId);
                beCartographyTable.SetWriting(false);
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