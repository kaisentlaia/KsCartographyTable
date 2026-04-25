using System;
using System.Collections.Generic;
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
		private readonly TableWaypointManager tableWaypointManager;
		private readonly TableMapManager tableMapManager;
		WorldMapManager WorldMapManager;
		WaypointMapLayer waypointMapLayer;
        private Dictionary<string, MapTransferSession> activeSessions = [];
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
			tableWaypointManager = new TableWaypointManager(CoreServerAPI);

			RegisterChannels();
		}

		public void RegisterChannels()
		{
			CoreServerAPI.Network.RegisterChannel(CartographyTableConstants.CHANNEL_UPLOAD_TO_SERVER)
				.RegisterMessageType<MapUploadPacket>()
				.SetMessageHandler<MapUploadPacket>(OnMapUploadRequest);

			CoreServerAPI.Network.RegisterChannel(CartographyTableConstants.CHANNEL_DOWNLOAD_TO_CLIENT)
				.RegisterMessageType<MapUploadPacket>();
		}

        private void OnMapUploadRequest(IServerPlayer fromPlayer, MapUploadPacket packet)
		{
			double km2updated = tableMapManager.UpdateMap(fromPlayer, packet);
			if (km2updated > 0)
			{
				CoreServerAPI.SendMessage(fromPlayer, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.TABLE_MAP_UPDATED, $"{km2updated:F1}"), EnumChatType.Notification);
			}
		}

		public void UpdateTableMap(CartographyMap map, IServerPlayer player)
		{
			WaypointsUpdated result = tableWaypointManager.Update(map, player);
			if (result.Updated)
			{
				if (result.Added > 0)
				{
					CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.TABLE_WAYPOINTS_ADDED, result.Added), EnumChatType.Notification);
				}
				if (result.Edited > 0)
				{
					CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.TABLE_WAYPOINTS_EDITED, result.Edited), EnumChatType.Notification);
				}
				if (result.Deleted > 0)
				{
					CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.TABLE_WAYPOINTS_DELETED, result.Deleted), EnumChatType.Notification);
				}
				player.Entity.World.PlaySoundAt(new AssetLocation("game:sounds/effect/writing"), player);
			}
			else
			{
				CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.TABLE_WAYPOINTS_UP_TO_DATE), EnumChatType.Notification);
			}
		}

		public void UpdatePlayerMap(CartographyMap map, IServerPlayer player, Block block, BlockPos blockPos)
		{
			double updatedExploredMap = tableMapManager.SendMapToPlayer(player, block, blockPos);
			WaypointsUpdated updatedWaypoints = tableWaypointManager.SendWaypointsToPlayer(map, player);

			if (updatedWaypoints.Updated)
			{
				string waypointsMessage = string.Empty;
				if (updatedWaypoints.Added > 0)
				{
					waypointsMessage = Lang.Get(CartographyTableLangCodes.PLAYER_WAYPOINTS_ADDED, updatedWaypoints.Added);
				}
				if (updatedWaypoints.Edited > 0)
				{
					waypointsMessage = Lang.Get(CartographyTableLangCodes.PLAYER_WAYPOINTS_EDITED, updatedWaypoints.Edited);
				}
				if (updatedWaypoints.Deleted > 0)
				{
					waypointsMessage = Lang.Get(CartographyTableLangCodes.PLAYER_WAYPOINTS_DELETED, updatedWaypoints.Deleted);
				}
				CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, waypointsMessage, EnumChatType.Notification);
			}
			else
			{
				CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.PLAYER_WAYPOINTS_UP_TO_DATE), EnumChatType.Notification);
			}

			if (block is BlockAdvancedCartographyTable)
			{
				if (updatedExploredMap > 0)
				{
					CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.PLAYER_MAP_UPDATED, $"{updatedExploredMap:F1}"), EnumChatType.Notification);
				}
				else
				{
					CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.PLAYER_MAP_UP_TO_DATE), EnumChatType.Notification);
				}
			}

			if (updatedExploredMap > 0 || updatedWaypoints.Updated)
			{
				player.Entity.World.PlaySoundAt(new AssetLocation("game:sounds/effect/writing"), player);
			}
		}

		public void PurgeWaypointGroups(IPlayer player)
		{
			if (KsCartographyTableModSystem.purgeWpGroups)
			{
				var allWaypointsWithGroupId = tableWaypointManager.GetWaypointsWithGroupId();
				if (allWaypointsWithGroupId.Count > 0)
				{
					allWaypointsWithGroupId.Foreach(wp =>
					{
						wp.OwningPlayerGroupId = -1;
					});
					CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, Lang.Get($"Groups removed from {allWaypointsWithGroupId.Count} waypoints"), EnumChatType.Notification);
				}
			}
		}

		public void WipeTableMap(CartographyMap map, Block block, IPlayer byPlayer, BlockPos blockPos)
		{
			int waypointsWiped = tableWaypointManager.Wipe(map);
			bool mapWiped = tableMapManager.Wipe(block, blockPos);
			if (waypointsWiped > 0 || mapWiped)
			{
				CoreServerAPI.SendMessage(byPlayer, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.TABLE_MAP_WIPED, waypointsWiped), EnumChatType.Notification);

				byPlayer.Entity.World.PlaySoundAt(new AssetLocation("game:sounds/effect/writing"), byPlayer);
			}
			else
			{
				CoreServerAPI.SendMessage(null, GlobalConstants.GeneralChatGroup, Lang.Get(CartographyTableLangCodes.TABLE_MAP_ALREADY_EMPTY), EnumChatType.Notification);
			}
		}

		public void MarkDeleted(IServerPlayer fromPlayer, int index)
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

		public void CleanupMapData(Block block, BlockPos pos)
		{
			tableMapManager.CleanupMapData(block, pos);
		}

		public void Dispose()
		{
			tableMapManager.Dispose();
		}

        internal bool StartCartographyDownloadSession(CartographyAction action, CartographyMap map, IWorldAccessor world, IPlayer forPlayer, Block block)
        {
            string sessionId = block.Id.ToString() + forPlayer.PlayerUID;
            if (activeSessions.Get(sessionId) != null)
            {
                return false;
            }
            List<CartographyWaypoint> newWaypoints = tableWaypointManager.GetNewWaypoints(forPlayer, map);
            List<CartographyWaypoint> editedWaypoints = tableWaypointManager.GetEditedWaypoints(forPlayer, map);
            List<CartographyWaypoint> deletedWaypoints = tableWaypointManager.GetDeletedWaypoints(forPlayer, map);
            CartographyMapData playerCartographyMap;
            if (block is BlockAdvancedCartographyTable)
            {
                Dictionary<FastVec2i, MapPieceDB> mapPieces = tableMapManager.GetNewMapPieces(forPlayer, block);
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
            MapTransferSession session = new MapTransferSession(forPlayer, block, action, world, playerCartographyMap);
            activeSessions.Add(sessionId, session);
            return session.SendFirstBatch();
        }

        internal bool continueCartographyDownloadSession(CartographyMap map, float secondsUsed, IWorldAccessor world, IPlayer byPlayer, Block block)
        {
            throw new NotImplementedException();
        }

        internal void endCartographyDownloadSession(CartographyMap map, float secondsUsed, IWorldAccessor world, IPlayer byPlayer, Block block)
        {
            throw new NotImplementedException();
        }
    }
}