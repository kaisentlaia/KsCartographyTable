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
                beCartographyTable.Map.ExploredAreasIds = [.. mapDB.GetAllMapPiecesIds().Select(v => v.ToChunkIndex())];
                beCartographyTable.MarkDirty();
            }

            if (packet.IsFinalBatch)
            {         
                if (!uploadedChunks.ContainsKey(fromPlayer.PlayerUID))
                {
                    uploadedChunks[fromPlayer.PlayerUID] = 0;
                }           
                double km2 = uploadedChunks[fromPlayer.PlayerUID] * 0.001024;
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
                if (km2 == 0 && !waypointResult.Synced)
                {
                    return;
                }
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
                }
                if (beCartographyTable.Map.LastPlayerDownloads.ContainsKey(fromPlayer.PlayerUID))
                {
                    beCartographyTable.Map.LastPlayerDownloads[fromPlayer.PlayerUID] = ((DateTimeOffset)DateTime.Now.ToUniversalTime()).ToUnixTimeMilliseconds();
                } else
                {
                    beCartographyTable.Map.LastPlayerDownloads.Add(fromPlayer.PlayerUID, ((DateTimeOffset)DateTime.Now.ToUniversalTime()).ToUnixTimeMilliseconds());
                }
                beCartographyTable.Map.WaypointCount = mapDB.GetSharedWaypointsCount();
                beCartographyTable.MarkDirty();
            }            
		}

		public void WipeTableMap(CartographyMap map, Block block, IPlayer byPlayer, BlockPos blockPos)
		{
            bool mapWiped = false;
			if (block is BlockAdvancedCartographyTable)
			{
				ServerMapDB mapDB = GetBlockMapDB(block.Id.ToString());

				if (mapDB != null)
				{
					mapDB.Wipe();
					BlockEntityCartographyTable blockEntity = (BlockEntityCartographyTable)CoreServerAPI.World.BlockAccessor.GetBlockEntity(blockPos);
                    blockEntity.UpdateMapExploredAreasIds(new List<FastVec2i>());     
				}
			}
			if (mapWiped)
			{
				CoreServerAPI.SendMessage(byPlayer, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.TABLE_MAP_WIPED), EnumChatType.Notification);

				byPlayer.Entity.World.PlaySoundAt(new AssetLocation("game:sounds/effect/writing"), byPlayer);
			}
			else
			{
                CoreServerAPI.SendMessage(null, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.TABLE_MAP_ALREADY_EMPTY), EnumChatType.Notification);
			}
		}

        public void CleanupMapData(Block block, BlockPos pos)
        {
			if (block is BlockAdvancedCartographyTable)
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
            // TODO close all sessions
            tableDBConnections.Values.ToList().ForEach(connection =>
            {
                connection.Close();
                connection.Dispose();
            });
        }

        internal bool StartCartographyDownloadSession(CartographyAction action, CartographyMap map, IWorldAccessor world, IPlayer forPlayer, BlockSelection blockSel)
        {
            string sessionId = blockSel.Block.Id.ToString() + forPlayer.PlayerUID;
            if (activeSessions.Get(sessionId) != null)
            {
                return false;
            }
            tableWaypointManager.UpdatePlayerWaypoints(forPlayer, map);

            Dictionary<FastVec2i, MapPieceDB> newMapPiecesForPlayer = [];
            if (blockSel.Block is BlockAdvancedCartographyTable)
            {
                ServerMapDB mapDB = GetBlockMapDB(blockSel.Block.Id.ToString());
                newMapPiecesForPlayer = mapDB.GetNewMapPiecesForPlayer(forPlayer);
            }
            MapTransferSession session = new MapTransferSession(forPlayer, blockSel, action, world, newMapPiecesForPlayer, CoreServerAPI);
            activeSessions.Add(sessionId, session);
            session.SendFirstBatch();
            return true;
        }

        internal bool ContinueCartographyDownloadSession(CartographyMap map, float secondsUsed, IWorldAccessor world, IPlayer byPlayer, Block block)
        {
            throw new NotImplementedException();
        }

        internal void EndCartographyDownloadSession(CartographyMap map, float secondsUsed, IWorldAccessor world, IPlayer byPlayer, Block block)
        {
            throw new NotImplementedException();
        }

        internal void ResendWaypointsToPlayer(IServerPlayer toPlayer)
        {
            tableWaypointManager.ResendWaypointsToPlayer(toPlayer);
        }
    }
}