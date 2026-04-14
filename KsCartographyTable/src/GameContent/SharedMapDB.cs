
using Vintagestory.GameContent;
using Vintagestory.API.Common;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

[ProtoContract]
public class MapUploadPacket
{
      [ProtoMember(1)]
      public Dictionary<FastVec2i, MapPieceDB> Pieces { get; set; } = new Dictionary<FastVec2i, MapPieceDB>();

      [ProtoMember(2)]
      public bool IsFinalBatch { get; set; } = true;

      [ProtoMember(3)]
      public string BlockId { get; set; } = "";

      [ProtoMember(4)]
      public BlockPos BlockPos { get; set; } = null;

      public MapUploadPacket() { }

      public MapUploadPacket(Dictionary<FastVec2i, MapPieceDB> pieces, Block block, BlockPos blockPos, bool isFinalBatch = true)
      {
            Pieces = pieces;
            BlockId = block.Id.ToString();
            BlockPos = blockPos;
            IsFinalBatch = isFinalBatch;
      }
}

class SharedMapDB : MapDB
{
      SqliteCommand getAllMapPiecesCmd;
      SqliteCommand setPlayerMapPieceCmd;
      SqliteCommand getMapPieceCmd;
      ICoreAPI coreApi;
      public SharedMapDB(ICoreAPI coreApi) : base(coreApi.World.Logger) {
            this.coreApi = coreApi;
      }

      public override void OnOpened() {
            base.OnOpened();

            getAllMapPiecesCmd = sqliteConn.CreateCommand();
            getAllMapPiecesCmd.CommandText = "SELECT position, data FROM mappiece";
            getAllMapPiecesCmd.Prepare();

            getMapPieceCmd = sqliteConn.CreateCommand();
            getMapPieceCmd.CommandText = "SELECT position, data FROM mappiece WHERE position=@pos";
            getMapPieceCmd.Parameters.Add("@pos", SqliteType.Integer, 1);
            getMapPieceCmd.Prepare();
            

            if (coreApi.Side == EnumAppSide.Server) {
                  setPlayerMapPieceCmd = sqliteConn.CreateCommand();
                  setPlayerMapPieceCmd.CommandText = "INSERT OR IGNORE INTO playerchunkmapping (position, playerId) VALUES (@pos, @uid)";
                  setPlayerMapPieceCmd.Parameters.Add("@uid", SqliteType.Text, 1);
                  setPlayerMapPieceCmd.Parameters.Add("@pos", SqliteType.Integer, 1);
                  setPlayerMapPieceCmd.Prepare();
            }
      }

      protected override void CreateTablesIfNotExists(SqliteConnection sqliteConn) {
            base.CreateTablesIfNotExists(sqliteConn);

            if (coreApi.Side == EnumAppSide.Server) {
                  using SqliteCommand sqliteCommand3 = sqliteConn.CreateCommand();
                  sqliteCommand3.CommandText = "CREATE TABLE IF NOT EXISTS playerchunkmapping (playerId text NOT NULL, position integer NOT NULL, PRIMARY KEY (playerId, position));";
                  sqliteCommand3.ExecuteNonQuery();

                  using SqliteCommand sqliteCommand4 = sqliteConn.CreateCommand();
                  sqliteCommand4.CommandText = "CREATE INDEX IF NOT EXISTS idx_playerchunkmapping ON playerchunkmapping(playerId);";
                  sqliteCommand4.ExecuteNonQuery();
            }
      }

      public Dictionary<FastVec2i, MapPieceDB> GetAllMapPieces() {
            var pieces = new Dictionary<FastVec2i, MapPieceDB>();
            using var sqlite_datareader = getAllMapPiecesCmd.ExecuteReader();
            while (sqlite_datareader.Read()) {
                  object data = sqlite_datareader["data"];
                  ulong pos = System.Convert.ToUInt64(sqlite_datareader["position"]);
                  if (data == null) return null;

                  int x = (int)(pos & 0x7FFFFFF);           // Lower 27 bits
                  int z = (int)((pos >> 27) & 0x7FFFFFF);   // Upper 27 bits

                  // Sign extend for negative values (27-bit to 32-bit)
                  if ((x & 0x4000000) != 0)  // If bit 26 is set (negative)
                        x |= unchecked((int)0xF8000000);  // Set upper bits to 1

                  if ((z & 0x4000000) != 0)  // If bit 26 is set (negative)
                        z |= unchecked((int)0xF8000000);  // Set upper bits to 1

                  pieces.Add(new FastVec2i(x, z), SerializerUtil.Deserialize<MapPieceDB>(data as byte[]));
            }

            return pieces;
      }

      public Dictionary<FastVec2i, MapPieceDB> GetMapPiecesFromPositions(List<FastVec2i> chunkCoords) {
            Dictionary<FastVec2i, MapPieceDB> pieces = new Dictionary<FastVec2i, MapPieceDB>();
            for (int i = 0; i < chunkCoords.Count; i++) {
                  getMapPieceCmd.Parameters["@pos"].Value = chunkCoords[i].ToChunkIndex();
                  using SqliteDataReader sqliteDataReader = getMapPieceCmd.ExecuteReader();
                  while (sqliteDataReader.Read()) {
                        object data = sqliteDataReader["data"];
                        ulong pos = System.Convert.ToUInt64(sqliteDataReader["position"]);
                        if (data == null)
                        {
                              return null;
                        }

                        int x = (int)(pos & 0x7FFFFFF);           // Lower 27 bits
                        int z = (int)((pos >> 27) & 0x7FFFFFF);   // Upper 27 bits

                        // Sign extend for negative values (27-bit to 32-bit)
                        if ((x & 0x4000000) != 0)  // If bit 26 is set (negative)
                              x |= unchecked((int)0xF8000000);  // Set upper bits to 1

                        if ((z & 0x4000000) != 0)  // If bit 26 is set (negative)
                              z |= unchecked((int)0xF8000000);  // Set upper bits to 1

                        pieces.Add(new FastVec2i(x, z), SerializerUtil.Deserialize<MapPieceDB>(data as byte[]));
                  }
            }

            return pieces;
      }

      public List<FastVec2i> GetAllMapPiecesIds() {
            var ids = new List<FastVec2i>();
            using var sqlite_datareader = getAllMapPiecesCmd.ExecuteReader();
            while (sqlite_datareader.Read()) {
                  ulong pos = System.Convert.ToUInt64(sqlite_datareader["position"]);
                  
                  int x = (int)(pos & 0x7FFFFFF);           // Lower 27 bits
                  int z = (int)((pos >> 27) & 0x7FFFFFF);   // Upper 27 bits

                  // Sign extend for negative values (27-bit to 32-bit)
                  if ((x & 0x4000000) != 0)  // If bit 26 is set (negative)
                        x |= unchecked((int)0xF8000000);  // Set upper bits to 1

                  if ((z & 0x4000000) != 0)  // If bit 26 is set (negative)
                        z |= unchecked((int)0xF8000000);  // Set upper bits to 

                  ids.Add(new FastVec2i(x, z));
            }

            return ids;
      }
      
      public int GetMapPieceCount() {
            using var cmd = sqliteConn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM mappiece";
            return System.Convert.ToInt32(cmd.ExecuteScalar());
      }

      public void SetMapPiecesForPlayer(Dictionary<FastVec2i, MapPieceDB> pieces, IPlayer player) {
            using SqliteTransaction sqliteTransaction = sqliteConn.BeginTransaction();
            setPlayerMapPieceCmd.Transaction = sqliteTransaction;
            foreach (KeyValuePair<FastVec2i, MapPieceDB> piece in pieces) {
                  setPlayerMapPieceCmd.Parameters["@pos"].Value = piece.Key.ToChunkIndex();
                  setPlayerMapPieceCmd.Parameters["@uid"].Value = player.PlayerUID;
                  setPlayerMapPieceCmd.ExecuteNonQuery();
            }

            sqliteTransaction.Commit();
      }

      public Dictionary<FastVec2i, MapPieceDB> GetNewMapPiecesForPlayer(IPlayer player)
      {
            var pieces = new Dictionary<FastVec2i, MapPieceDB>();
            
            using (var cmd = sqliteConn.CreateCommand())
            {
                  cmd.CommandText = @"
                        SELECT m.position, m.data 
                        FROM mappiece m 
                        LEFT JOIN playerchunkmapping p ON m.position = p.position AND p.playerId = @uid
                        WHERE p.playerId IS NULL";
                  cmd.Parameters.AddWithValue("@uid", player.PlayerUID);
                  
                  using (var reader = cmd.ExecuteReader())
                  {
                        while (reader.Read())
                        {
                              ulong pos = System.Convert.ToUInt64(reader["position"]);
                              object data = reader["data"];
                              if (data == null) continue;

                              // Decode position from chunk index
                              int x = (int)(pos & 0x7FFFFFF);
                              int z = (int)((pos >> 27) & 0x7FFFFFF);

                              // Sign extend for negative values
                              if ((x & 0x4000000) != 0) 
                                    x |= unchecked((int)0xF8000000);
                              if ((z & 0x4000000) != 0) 
                                    z |= unchecked((int)0xF8000000);

                              var coord = new FastVec2i(x, z);
                              pieces[coord] = SerializerUtil.Deserialize<MapPieceDB>(data as byte[]);
                        }
                  }
            }
            
            return pieces;
      }
}