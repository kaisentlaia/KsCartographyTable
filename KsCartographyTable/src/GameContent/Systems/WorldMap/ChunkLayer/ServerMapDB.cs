
using Vintagestory.GameContent;
using Vintagestory.API.Common;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using System;
using Kaisentlaia.KsCartographyTableMod.API.Common;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
	[ProtoContract]
	public class MapSyncPacket
	{
		[ProtoMember(1)]
		public Dictionary<FastVec2i, MapPieceDB> Pieces { get; set; } = new Dictionary<FastVec2i, MapPieceDB>();

		[ProtoMember(2)]
		public bool IsFinalBatch { get; set; } = false;

		[ProtoMember(3)]
		public string BlockId { get; set; } = "";

		[ProtoMember(4)]
		public BlockPos BlockPos { get; set; } = null;

		[ProtoMember(5)]
		public WaypointSyncResult WaypointSyncResult { get; set; } = null;
		public MapSyncPacket() { }

		public MapSyncPacket(Dictionary<FastVec2i, MapPieceDB> pieces, Block block, BlockPos blockPos, bool isFinalBatch = false, WaypointSyncResult waypointSyncResult = null) 
		{
			Pieces = pieces;
			BlockId = block.Id.ToString();
			BlockPos = blockPos;
			IsFinalBatch = isFinalBatch;
            WaypointSyncResult = waypointSyncResult;
		}
	}

	public class ServerMapDB : MapDB
	{
		SqliteCommand getAllMapPiecesCmd;
		SqliteCommand setPlayerExploredMapPieceCmd;
		SqliteCommand getNewMapPiecesForPlayerCmd;
		SqliteCommand getMapPieceWithPosCmd;
		SqliteCommand createWaypointsCmd;
		SqliteCommand updateWaypointsCmd;
		SqliteCommand getPlayerWaypointsCmd;
		SqliteCommand getMatchingWaypointCmd;
		SqliteCommand setDeletedWaypointsCmd;
		SqliteCommand getNewWaypointsForPlayerCmd;
		SqliteCommand getUpdatedWaypointsForPlayerCmd;
		SqliteCommand getDeletedWaypointsForPlayerCmd;
		SqliteCommand getWaypointsToDeleteCmd;
		ICoreAPI coreApi;
		public ServerMapDB(ICoreAPI coreApi) : base(coreApi.World.Logger)
		{
			this.coreApi = coreApi;
		}

		public override void OnOpened()
		{
			// TODO error if not server
			base.OnOpened();

			getAllMapPiecesCmd = sqliteConn.CreateCommand();
			getAllMapPiecesCmd.CommandText = "SELECT position, data FROM mappiece";
			getAllMapPiecesCmd.Prepare();

			getMapPieceWithPosCmd = sqliteConn.CreateCommand();
			getMapPieceWithPosCmd.CommandText = "SELECT position, data FROM mappiece WHERE position=@pos";
			getMapPieceWithPosCmd.Parameters.Add("@pos", SqliteType.Integer, 1);
			getMapPieceWithPosCmd.Prepare();


			if (coreApi.Side == EnumAppSide.Server)
			{
				setPlayerExploredMapPieceCmd = sqliteConn.CreateCommand();
				setPlayerExploredMapPieceCmd.CommandText = "INSERT OR IGNORE INTO playerchunkmapping (position, playerId) VALUES (@pos, @uid)";
				setPlayerExploredMapPieceCmd.Parameters.Add("@uid", SqliteType.Text);
				setPlayerExploredMapPieceCmd.Parameters.Add("@pos", SqliteType.Integer, 1);
				setPlayerExploredMapPieceCmd.Prepare();

				getNewMapPiecesForPlayerCmd = sqliteConn.CreateCommand();
				getNewMapPiecesForPlayerCmd.CommandText = @"
                    SELECT m.position, m.data 
                    FROM mappiece m 
                    LEFT JOIN playerchunkmapping p ON m.position = p.position AND p.playerId = @uid
                    WHERE p.playerId IS NULL";
				getNewMapPiecesForPlayerCmd.Parameters.Add("@uid", SqliteType.Text);
				getNewMapPiecesForPlayerCmd.Prepare();

				createWaypointsCmd = sqliteConn.CreateCommand();
				createWaypointsCmd.CommandText = "INSERT OR IGNORE INTO sharedwaypoints (guid, parentGuid, owningPlayerUid, position, title, icon, color, pinned, lastUpdated, deleted) VALUES (@guid, @parentGuid, @owningPlayerUid, @position, @title, @icon, @color, @pinned, @lastUpdated, 0)";
				createWaypointsCmd.Parameters.Add("@guid", SqliteType.Text);
				createWaypointsCmd.Parameters.Add("@parentGuid", SqliteType.Text);
				createWaypointsCmd.Parameters.Add("@owningPlayerUid", SqliteType.Text);
				createWaypointsCmd.Parameters.Add("@position", SqliteType.Text);
				createWaypointsCmd.Parameters.Add("@title", SqliteType.Text);
				createWaypointsCmd.Parameters.Add("@icon", SqliteType.Text);
				createWaypointsCmd.Parameters.Add("@color", SqliteType.Integer, 1);
				createWaypointsCmd.Parameters.Add("@pinned", SqliteType.Integer, 1);
				createWaypointsCmd.Parameters.Add("@lastUpdated", SqliteType.Integer, 1);
				createWaypointsCmd.Prepare();

				updateWaypointsCmd = sqliteConn.CreateCommand();
				updateWaypointsCmd.CommandText = "UPDATE sharedwaypoints SET title=@title, icon=@icon, color=@color, pinned=@pinned, lastUpdated=@lastUpdated WHERE guid=@guid OR parentGuid=@guid";
				updateWaypointsCmd.Parameters.Add("@guid", SqliteType.Text);
				updateWaypointsCmd.Parameters.Add("@title", SqliteType.Text);
				updateWaypointsCmd.Parameters.Add("@icon", SqliteType.Text);
				updateWaypointsCmd.Parameters.Add("@color", SqliteType.Integer, 1);
				updateWaypointsCmd.Parameters.Add("@pinned", SqliteType.Integer, 1);
				updateWaypointsCmd.Parameters.Add("@lastUpdated", SqliteType.Integer, 1);
				updateWaypointsCmd.Prepare();

				getPlayerWaypointsCmd = sqliteConn.CreateCommand();
				getPlayerWaypointsCmd.CommandText = "SELECT * FROM sharedwaypoints WHERE owningPlayerUid=@owningPlayerUid AND deleted=0";
				getPlayerWaypointsCmd.Parameters.Add("@owningPlayerUid", SqliteType.Text);
				getPlayerWaypointsCmd.Prepare();

				getMatchingWaypointCmd = sqliteConn.CreateCommand();
				getMatchingWaypointCmd.CommandText = "SELECT * FROM sharedwaypoints WHERE owningPlayerUid!=@owningPlayerUid AND deleted=0 AND parentGuid IS NULL AND title=@title AND position=@position AND icon=@icon AND pinned=@pinned";
				getMatchingWaypointCmd.Parameters.Add("@owningPlayerUid", SqliteType.Text);
				getMatchingWaypointCmd.Parameters.Add("@position", SqliteType.Text);
				getMatchingWaypointCmd.Parameters.Add("@title", SqliteType.Text);
				getMatchingWaypointCmd.Parameters.Add("@icon", SqliteType.Text);
				getMatchingWaypointCmd.Parameters.Add("@color", SqliteType.Integer, 1);
				getMatchingWaypointCmd.Parameters.Add("@pinned", SqliteType.Integer, 1);
				getMatchingWaypointCmd.Prepare();

				setDeletedWaypointsCmd = sqliteConn.CreateCommand();
				setDeletedWaypointsCmd.CommandText = "UPDATE sharedwaypoints SET deleted=1, lastUpdated=@lastUpdated WHERE guid=@guid OR parentGuid=@guid";
				setDeletedWaypointsCmd.Parameters.Add("@guid", SqliteType.Text);
				setDeletedWaypointsCmd.Parameters.Add("@lastUpdated", SqliteType.Integer, 1);
				setDeletedWaypointsCmd.Prepare();

				getNewWaypointsForPlayerCmd = sqliteConn.CreateCommand();
				getNewWaypointsForPlayerCmd.CommandText = @"SELECT * FROM sharedwaypoints sw 
					WHERE parentGuid IS NULL
					AND owningPlayerUid != @owningPlayerUid
					AND deleted = 0
					AND NOT EXISTS (
						SELECT 1 
						FROM sharedwaypoints child
						WHERE child.parentGuid = sw.guid
						AND child.owningPlayerUid = @owningPlayerUid
					)";
				getNewWaypointsForPlayerCmd.Parameters.Add("@owningPlayerUid", SqliteType.Text);
				getNewWaypointsForPlayerCmd.Prepare();

				getUpdatedWaypointsForPlayerCmd = sqliteConn.CreateCommand();
				getUpdatedWaypointsForPlayerCmd.CommandText = "SELECT * FROM sharedwaypoints WHERE owningPlayerUid=@owningPlayerUid AND deleted=0 AND lastUpdated > @lastUpdated";
				getUpdatedWaypointsForPlayerCmd.Parameters.Add("@owningPlayerUid", SqliteType.Text);
				getUpdatedWaypointsForPlayerCmd.Parameters.Add("@lastUpdated", SqliteType.Integer, 1);
				getUpdatedWaypointsForPlayerCmd.Prepare();

				getDeletedWaypointsForPlayerCmd = sqliteConn.CreateCommand();
				getDeletedWaypointsForPlayerCmd.CommandText = "SELECT * FROM sharedwaypoints WHERE owningPlayerUid=@owningPlayerUid AND deleted=1 AND lastUpdated > @lastUpdated";
				getDeletedWaypointsForPlayerCmd.Parameters.Add("@owningPlayerUid", SqliteType.Text);
				getDeletedWaypointsForPlayerCmd.Parameters.Add("@lastUpdated", SqliteType.Integer, 1);
				getDeletedWaypointsForPlayerCmd.Prepare();

				getWaypointsToDeleteCmd = sqliteConn.CreateCommand();
				getWaypointsToDeleteCmd.CommandText = "SELECT * FROM sharedwaypoints WHERE (guid=@guid OR parentGuid=@guid) AND deleted=0";
				getWaypointsToDeleteCmd.Parameters.Add("@guid", SqliteType.Text);
				getWaypointsToDeleteCmd.Prepare();
			}
		}

		protected override void CreateTablesIfNotExists(SqliteConnection sqliteConn)
		{
			// TODO error if not server
			base.CreateTablesIfNotExists(sqliteConn);

			if (coreApi.Side == EnumAppSide.Server)
			{
				using SqliteCommand sqliteCommand3 = sqliteConn.CreateCommand();
				sqliteCommand3.CommandText = "CREATE TABLE IF NOT EXISTS playerchunkmapping (playerId text NOT NULL, position integer NOT NULL, PRIMARY KEY (playerId, position));";
				sqliteCommand3.ExecuteNonQuery();

				using SqliteCommand sqliteCommand4 = sqliteConn.CreateCommand();
				sqliteCommand4.CommandText = "CREATE INDEX IF NOT EXISTS idx_playerchunkmapping ON playerchunkmapping(playerId);";
				sqliteCommand4.ExecuteNonQuery();

				using SqliteCommand sqliteCommand5 = sqliteConn.CreateCommand();
				sqliteCommand5.CommandText = "CREATE TABLE IF NOT EXISTS sharedwaypoints (guid text NOT NULL, parentGuid text, owningPlayerUid text NOT NULL, position text NOT NULL, title text NOT NULL, icon text NOT NULL, color integer NOT NULL, pinned integer NOT NULL, deleted integer NOT NULL, lastUpdated integer NOT NULL, PRIMARY KEY (guid));";
				sqliteCommand5.ExecuteNonQuery();
			}
		}

		private FastVec2i ChunkIdToFastVect2i(ulong chunkId)
		{

			int x = (int)(chunkId & 0x7FFFFFF);           // Lower 27 bits
			int z = (int)((chunkId >> 27) & 0x7FFFFFF);   // Upper 27 bits

			// Sign extend for negative values (27-bit to 32-bit)
			if ((x & 0x4000000) != 0)  // If bit 26 is set (negative)
				x |= unchecked((int)0xF8000000);  // Set upper bits to 1

			if ((z & 0x4000000) != 0)  // If bit 26 is set (negative)
				z |= unchecked((int)0xF8000000);  // Set upper bits to 1

			return new FastVec2i(x, z);
		}

		public Dictionary<FastVec2i, MapPieceDB> GetAllMapPieces()
		{
			var pieces = new Dictionary<FastVec2i, MapPieceDB>();
			using var sqlite_datareader = getAllMapPiecesCmd.ExecuteReader();
			while (sqlite_datareader.Read())
			{
				object data = sqlite_datareader["data"];
				ulong chunkId = Convert.ToUInt64(sqlite_datareader["position"]);
				if (data == null || data == DBNull.Value) continue;

                if (data is not byte[] bytes)
                    continue;

                pieces.Add(ChunkIdToFastVect2i(chunkId), SerializerUtil.Deserialize<MapPieceDB>(bytes));
			}

			return pieces;
		}

		public Dictionary<FastVec2i, MapPieceDB> GetMapPiecesFromPositions(List<FastVec2i> chunkCoords)
		{
			Dictionary<FastVec2i, MapPieceDB> pieces = new Dictionary<FastVec2i, MapPieceDB>();
			for (int i = 0; i < chunkCoords.Count; i++)
			{
				getMapPieceWithPosCmd.Parameters["@pos"].Value = chunkCoords[i].ToChunkIndex();
				using SqliteDataReader sqliteDataReader = getMapPieceWithPosCmd.ExecuteReader();
				while (sqliteDataReader.Read())
				{
					object data = sqliteDataReader["data"];
					ulong chunkId = Convert.ToUInt64(sqliteDataReader["position"]);
                    if (data == null || data == DBNull.Value) continue;

                    if (data is not byte[] bytes)
                        continue;

					pieces.Add(ChunkIdToFastVect2i(chunkId), SerializerUtil.Deserialize<MapPieceDB>(bytes));
				}
			}

			return pieces;
		}

		public List<FastVec2i> GetAllMapPiecesIds()
		{
			var ids = new List<FastVec2i>();
			using var sqlite_datareader = getAllMapPiecesCmd.ExecuteReader();
			while (sqlite_datareader.Read())
			{
				ulong chunkId = Convert.ToUInt64(sqlite_datareader["position"]);

				ids.Add(ChunkIdToFastVect2i(chunkId));
			}

			return ids;
		}

		public int GetMapPieceCount()
		{
			using var cmd = sqliteConn.CreateCommand();
			cmd.CommandText = "SELECT COUNT(*) FROM mappiece";
			return Convert.ToInt32(cmd.ExecuteScalar());
		}

		public void SetMapPiecesForPlayer(Dictionary<FastVec2i, MapPieceDB> pieces, IPlayer player)
		{
			using (SqliteTransaction sqliteTransaction = sqliteConn.BeginTransaction())
			{
				setPlayerExploredMapPieceCmd.Transaction = sqliteTransaction;
				foreach (KeyValuePair<FastVec2i, MapPieceDB> piece in pieces)
				{
					var chunkIndex = piece.Key.ToChunkIndex();
					setPlayerExploredMapPieceCmd.Parameters["@pos"].Value = chunkIndex;
					setPlayerExploredMapPieceCmd.Parameters["@uid"].Value = player.PlayerUID;
					setPlayerExploredMapPieceCmd.ExecuteNonQuery();
				}

				sqliteTransaction.Commit();
			}
		}

		public Dictionary<FastVec2i, MapPieceDB> GetNewMapPiecesForPlayer(IPlayer player)
		{
			Dictionary<FastVec2i, MapPieceDB> pieces = [];

			getNewMapPiecesForPlayerCmd.Parameters["@uid"].Value = player.PlayerUID;
			using (var reader = getNewMapPiecesForPlayerCmd.ExecuteReader())
			{
				while (reader.Read())
				{
					ulong chunkId = Convert.ToUInt64(reader["position"]);
					object data = reader["data"];
					if (data == null) continue;

					pieces[ChunkIdToFastVect2i(chunkId)] = SerializerUtil.Deserialize<MapPieceDB>(data as byte[]);
				}
			}

			return pieces;
		}

		public void CreateWaypoints(List<CartographyWaypoint> waypoints)
		{
            using SqliteTransaction sqliteTransaction = sqliteConn.BeginTransaction();
            createWaypointsCmd.Transaction = sqliteTransaction;
            foreach (CartographyWaypoint waypoint in waypoints)
            {
                KsCartographyTableModSystem.DebugLog(coreApi, $"INSERTING guid={waypoint.Guid}, title={waypoint.Title}, parentGuid={waypoint.ParentGuid ?? "null"}");
                createWaypointsCmd.Parameters["@guid"].Value = waypoint.Guid;
                createWaypointsCmd.Parameters["@parentGuid"].Value = string.IsNullOrEmpty(waypoint.ParentGuid) ? DBNull.Value : waypoint.ParentGuid;
                createWaypointsCmd.Parameters["@owningPlayerUid"].Value = waypoint.OwningPlayerUid;
                createWaypointsCmd.Parameters["@position"].Value = $"{waypoint.Position.X},{waypoint.Position.Y},{waypoint.Position.Z}";
                createWaypointsCmd.Parameters["@title"].Value = waypoint.Title;
                createWaypointsCmd.Parameters["@icon"].Value = waypoint.Icon;
                createWaypointsCmd.Parameters["@color"].Value = waypoint.Color;
                createWaypointsCmd.Parameters["@pinned"].Value = waypoint.Pinned;
                createWaypointsCmd.Parameters["@lastUpdated"].Value = ((DateTimeOffset)waypoint.LastUpdated.ToUniversalTime()).ToUnixTimeMilliseconds();
                createWaypointsCmd.ExecuteNonQuery();
            }

            sqliteTransaction.Commit();
        }

		public void UpdateWaypoints(List<CartographyWaypoint> waypoints)
		{
			using (SqliteTransaction sqliteTransaction = sqliteConn.BeginTransaction())
			{
				updateWaypointsCmd.Transaction = sqliteTransaction;
				foreach (CartographyWaypoint waypoint in waypoints)
				{
            		KsCartographyTableModSystem.DebugLog(coreApi, $"UPDATING guid={waypoint.Guid}, title={waypoint.Title}, parentGuid={waypoint.ParentGuid ?? "null"}");
					updateWaypointsCmd.Parameters["@guid"].Value = string.IsNullOrEmpty(waypoint.ParentGuid) ? waypoint.Guid : waypoint.ParentGuid;
					updateWaypointsCmd.Parameters["@title"].Value = waypoint.Title;
					updateWaypointsCmd.Parameters["@icon"].Value = waypoint.Icon;
					updateWaypointsCmd.Parameters["@color"].Value = waypoint.Color;
					updateWaypointsCmd.Parameters["@pinned"].Value = waypoint.Pinned;
					updateWaypointsCmd.Parameters["@lastUpdated"].Value = ((DateTimeOffset)DateTime.Now.ToUniversalTime()).ToUnixTimeMilliseconds();
					updateWaypointsCmd.ExecuteNonQuery();
				}

				sqliteTransaction.Commit();
			}
		}

		public List<CartographyWaypoint> GetPlayerSharedWaypoints(IPlayer player)
		{
			List<CartographyWaypoint> waypoints = [];

			getPlayerWaypointsCmd.Parameters["@owningPlayerUid"].Value = player.PlayerUID;
			using (var reader = getPlayerWaypointsCmd.ExecuteReader())
			{
				while (reader.Read())
				{
					waypoints.Add(new CartographyWaypoint(
						reader["guid"].ToString(),
						reader["parentGuid"] != DBNull.Value ? reader["parentGuid"].ToString() : null,
						reader["owningPlayerUid"].ToString(),
						reader["title"].ToString(),
						reader["icon"].ToString(),
						reader["position"].ToString(),
						Convert.ToInt64(reader["color"]),
						Convert.ToInt64(reader["pinned"]),
						Convert.ToInt64(reader["deleted"]),
						Convert.ToInt64(reader["lastUpdated"])
					));
				}
			}

			return waypoints;
		}

		public List<CartographyWaypoint> GetNewWaypointsForPlayer(IPlayer player)
		{
			List<CartographyWaypoint> waypoints = [];

			getNewWaypointsForPlayerCmd.Parameters["@owningPlayerUid"].Value = player.PlayerUID;
			using (var reader = getNewWaypointsForPlayerCmd.ExecuteReader())
			{
				while (reader.Read())
				{
					waypoints.Add(new CartographyWaypoint(
						reader["guid"].ToString(),
						reader["parentGuid"] != DBNull.Value ? reader["parentGuid"].ToString() : null,
						reader["owningPlayerUid"].ToString(),
						reader["title"].ToString(),
						reader["icon"].ToString(),
						reader["position"].ToString(),
						Convert.ToInt64(reader["color"]),
						Convert.ToInt64(reader["pinned"]),
						Convert.ToInt64(reader["deleted"]),
						Convert.ToInt64(reader["lastUpdated"])
					));
				}
			}

			return waypoints;
		}

		public List<CartographyWaypoint> GetUpdatedWaypointsForPlayer(IPlayer player, DateTime lastUpdated)
		{
			List<CartographyWaypoint> waypoints = [];

			getUpdatedWaypointsForPlayerCmd.Parameters["@owningPlayerUid"].Value = player.PlayerUID;
			getUpdatedWaypointsForPlayerCmd.Parameters["@lastUpdated"].Value = ((DateTimeOffset)lastUpdated.ToUniversalTime()).ToUnixTimeMilliseconds();
			using (var reader = getUpdatedWaypointsForPlayerCmd.ExecuteReader())
			{
				while (reader.Read())
				{
					waypoints.Add(new CartographyWaypoint(
						reader["guid"].ToString(),
						reader["parentGuid"] != DBNull.Value ? reader["parentGuid"].ToString() : null,
						reader["owningPlayerUid"].ToString(),
						reader["title"].ToString(),
						reader["icon"].ToString(),
						reader["position"].ToString(),
						Convert.ToInt64(reader["color"]),
						Convert.ToInt64(reader["pinned"]),
						Convert.ToInt64(reader["deleted"]),
						Convert.ToInt64(reader["lastUpdated"])
					));
				}
			}

			return waypoints;
		}

		public List<CartographyWaypoint> GetDeletedWaypointsForPlayer(IPlayer player, DateTime lastUpdated)
		{
			List<CartographyWaypoint> waypoints = [];

			getDeletedWaypointsForPlayerCmd.Parameters["@owningPlayerUid"].Value = player.PlayerUID;
			getDeletedWaypointsForPlayerCmd.Parameters["@lastUpdated"].Value = ((DateTimeOffset)lastUpdated.ToUniversalTime()).ToUnixTimeMilliseconds();
			using (var reader = getDeletedWaypointsForPlayerCmd.ExecuteReader())
			{
				while (reader.Read())
				{
					waypoints.Add(new CartographyWaypoint(
						reader["guid"].ToString(),
						reader["parentGuid"] != DBNull.Value ? reader["parentGuid"].ToString() : null,
						reader["owningPlayerUid"].ToString(),
						reader["title"].ToString(),
						reader["icon"].ToString(),
						reader["position"].ToString(),
						Convert.ToInt64(reader["color"]),
						Convert.ToInt64(reader["pinned"]),
						Convert.ToInt64(reader["deleted"]),
						Convert.ToInt64(reader["lastUpdated"])
					));
				}
			}

			return waypoints;
		}

		public CartographyWaypoint GetMatchingWaypoint(CartographyWaypoint waypoint)
		{
			CartographyWaypoint matchingWaypoint = null;

			getMatchingWaypointCmd.Parameters["@owningPlayerUid"].Value = waypoint.OwningPlayerUid;
			getMatchingWaypointCmd.Parameters["@position"].Value = $"{waypoint.Position.X},{waypoint.Position.Y},{waypoint.Position.Z}";
			getMatchingWaypointCmd.Parameters["@title"].Value = waypoint.Title;
			getMatchingWaypointCmd.Parameters["@icon"].Value = waypoint.Icon;
			getMatchingWaypointCmd.Parameters["@pinned"].Value = waypoint.Pinned;
			using (var reader = getMatchingWaypointCmd.ExecuteReader())
			{
				while (reader.Read())
				{
					matchingWaypoint = new CartographyWaypoint(
						reader["guid"].ToString(),
						reader["parentGuid"] != DBNull.Value ? reader["parentGuid"].ToString() : null,
						reader["owningPlayerUid"].ToString(),
						reader["title"].ToString(),
						reader["icon"].ToString(),
						reader["position"].ToString(),
						Convert.ToInt64(reader["color"]),
						Convert.ToInt64(reader["pinned"]),
						Convert.ToInt64(reader["deleted"]),
						Convert.ToInt64(reader["lastUpdated"])
					);
				}
			}

			return matchingWaypoint;
		}

		public void Wipe()
		{
			Purge();

			using SqliteCommand sqliteCommand1 = sqliteConn.CreateCommand();
			sqliteCommand1.CommandText = "delete FROM playerchunkmapping";
			sqliteCommand1.ExecuteNonQuery();

			using SqliteCommand sqliteCommand2 = sqliteConn.CreateCommand();
			sqliteCommand2.CommandText = "delete FROM sharedwaypoints";
			sqliteCommand2.ExecuteNonQuery();
		}

        internal void DeleteWaypoints(List<CartographyWaypoint> deletedWaypoints)
        {
            using SqliteTransaction sqliteTransaction = sqliteConn.BeginTransaction();
            setDeletedWaypointsCmd.Transaction = sqliteTransaction;
            foreach (CartographyWaypoint waypoint in deletedWaypoints)
            {
				setDeletedWaypointsCmd.Parameters["@guid"].Value = string.IsNullOrEmpty(waypoint.ParentGuid) ? waypoint.Guid : waypoint.ParentGuid;
				setDeletedWaypointsCmd.Parameters["@lastUpdated"].Value = ((DateTimeOffset)DateTime.Now.ToUniversalTime()).ToUnixTimeMilliseconds();
                setDeletedWaypointsCmd.ExecuteNonQuery();
            }

            sqliteTransaction.Commit();
        }

        internal int GetSharedWaypointsCount()
        {
			using var cmd = sqliteConn.CreateCommand();
			cmd.CommandText = "SELECT COUNT(*) FROM sharedwaypoints WHERE parentGuid IS NULL AND deleted=0";
			return Convert.ToInt32(cmd.ExecuteScalar());
        }

        internal List<Vec3d> GetPalantirWaypointPositions()
        {
			using var cmd = sqliteConn.CreateCommand();
			cmd.CommandText = "SELECT * FROM sharedwaypoints WHERE parentGuid IS NULL AND icon='palantir-manual' AND deleted=0";
			List<Vec3d> waypointPositions = [];

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var positionParts = reader["position"].ToString().Split(',');
                
                waypointPositions.Add(new Vec3d(
                    double.Parse(positionParts[0]),
                    double.Parse(positionParts[1]),
                    double.Parse(positionParts[2])
                ));
            }
                
			return waypointPositions;
        }

        internal List<CartographyWaypoint> GetWaypointsToDelete(List<string> deletedWaypointIds)
        {
			List<CartographyWaypoint> waypoints = [];

			foreach (string guid in deletedWaypointIds)
			{
				if (!string.IsNullOrEmpty(guid))
				{
					getWaypointsToDeleteCmd.Parameters["@guid"].Value = guid;
					using var reader = getWaypointsToDeleteCmd.ExecuteReader();
					while (reader.Read())
					{
						waypoints.Add(new CartographyWaypoint(
							reader["guid"].ToString(),
							reader["parentGuid"] != DBNull.Value ? reader["parentGuid"].ToString() : null,
							reader["owningPlayerUid"].ToString(),
							reader["title"].ToString(),
							reader["icon"].ToString(),
							reader["position"].ToString(),
							Convert.ToInt64(reader["color"]),
							Convert.ToInt64(reader["pinned"]),
							Convert.ToInt64(reader["deleted"]),
							Convert.ToInt64(reader["lastUpdated"])
						));
					}
				}
            }

			return waypoints;
        }

		private void DisposeAllCmds()
		{			
			getAllMapPiecesCmd?.Dispose();
			setPlayerExploredMapPieceCmd?.Dispose();
			getNewMapPiecesForPlayerCmd?.Dispose();
			getMapPieceWithPosCmd?.Dispose();
			createWaypointsCmd?.Dispose();
			updateWaypointsCmd?.Dispose();
			getPlayerWaypointsCmd?.Dispose();
			getMatchingWaypointCmd?.Dispose();
			setDeletedWaypointsCmd?.Dispose();
			getNewWaypointsForPlayerCmd?.Dispose();
			getUpdatedWaypointsForPlayerCmd?.Dispose();
			getDeletedWaypointsForPlayerCmd?.Dispose();
			getWaypointsToDeleteCmd?.Dispose();
		}

		public override void Close()
		{
			DisposeAllCmds();
			base.Close();
		}
		
		public override void Dispose()
		{			
			DisposeAllCmds();
			base.Dispose();
		}
    }
}