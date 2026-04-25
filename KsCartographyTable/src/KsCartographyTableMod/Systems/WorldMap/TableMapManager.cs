using System;
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

		public void UpdateMap(IServerPlayer fromPlayer, MapUploadPacket packet, ServerMapDB mapDB)
        {
            if (mapDB != null)
            {
                mapDB.SetMapPieces(packet.Pieces);
                mapDB.SetMapPiecesForPlayer(packet.Pieces, fromPlayer);
			
				if (packet.IsFinalBatch)
				{
					BlockEntityCartographyTable blockEntity = (BlockEntityCartographyTable)CoreServerAPI.World.BlockAccessor.GetBlockEntity(packet.BlockPos);
					if (blockEntity != null)
					{
						blockEntity.UpdateMapExploredAreasIds(mapDB.GetAllMapPiecesIds());
					}
				}
            }
        }

        internal Dictionary<FastVec2i, MapPieceDB> GetNewMapPieces(IPlayer forPlayer, Block block)
        {
            throw new NotImplementedException();
        }
    }
}