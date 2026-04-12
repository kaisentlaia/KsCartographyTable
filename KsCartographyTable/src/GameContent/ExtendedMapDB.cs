
using Vintagestory.GameContent;
using Vintagestory.API.Common;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

[ProtoContract]
public class ExtendedMapPieceDB
{
      [ProtoMember(1)]
      public ulong ChunkIndex { get; set; }
      [ProtoMember(2)]
      public byte[] Data { get; set; }
}

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

class ExtendedMapDB : MapDB
{
      SqliteCommand getAllMapPiecesCmd;
      public ExtendedMapDB(ILogger api) : base(api)
      {

      }

      public override void OnOpened()
      {
            base.OnOpened();

            getAllMapPiecesCmd = sqliteConn.CreateCommand();
            getAllMapPiecesCmd.CommandText = "SELECT position, data FROM mappiece";
            getAllMapPiecesCmd.Prepare();
      }

      public Dictionary<FastVec2i, MapPieceDB> GetAllMapPieces()
      {
            var pieces = new Dictionary<FastVec2i, MapPieceDB>();
            using var sqlite_datareader = getAllMapPiecesCmd.ExecuteReader();
            while (sqlite_datareader.Read())
            {
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
      
      public int GetMapPieceCount()
      {
            using var cmd = sqliteConn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM mappiece";
            return System.Convert.ToInt32(cmd.ExecuteScalar());
      }
}