using System;
using System.Collections.Generic;
using System.Linq;
using Kaisentlaia.KsCartographyTableMod.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
    public class CartographyMap {
        private List<ulong> exploredAreasIds = new List<ulong>();
        public List<ulong> ExploredAreasIds
        {
            get { return exploredAreasIds; }
            set { exploredAreasIds = value; }
        }
        private int waypointCount = 0;
        public int WaypointCount
        {
            get { return waypointCount; }
            set { waypointCount = value; }
        }
        private Dictionary<string, int> lastPlayerDownloads = [];
        public Dictionary<string, int> LastPlayerDownloads
        {
            get { return lastPlayerDownloads; }
            set { lastPlayerDownloads = value; }
        }
        ICoreAPI api;

        public CartographyMap(ICoreAPI api)
        {
            this.api = api;
        }

        public void Serialize(ITreeAttribute tree)
        {
            try
            {
                tree.SetString("WaypointCount", JsonUtil.ToString(WaypointCount));
            }
            catch (Exception ex)
            {
                api.Logger.Error("Failed to serialize waypoint count: {0}", ex);
                tree.SetString("WaypointCount", JsonUtil.ToString(0));
            }
            try
            {
                tree.SetString("LastPlayerDownloads", JsonUtil.ToString(LastPlayerDownloads));
            }
            catch (Exception ex)
            {
                api.Logger.Error("Failed to serialize last player updates: {0}", ex);
                tree.SetString("LastPlayerDownloads", JsonUtil.ToString(new Dictionary<string, int>()));
            }
            try
            {
                tree.SetString("ExploredAreasIds", JsonUtil.ToString(ExploredAreasIds));
            }
            catch (Exception ex)
            {
                api.Logger.Error("Failed to serialize explored areas ids: {0}", ex);
                tree.SetString("ExploredAreasIds", JsonUtil.ToString(new List<FastVec2i>()));
            }
        }

        public void Deserialize(ITreeAttribute tree)
        {
            try
            {
                if (tree.HasAttribute("WaypointCount"))
                {
                    var waypointCount = tree.GetString("WaypointCount");
                    WaypointCount = waypointCount != null
                        ? JsonUtil.FromString<int>(waypointCount)
                        : 0;
                }
                else
                {
                    WaypointCount = 0;
                }
            }
            catch (Exception ex)
            {
                api.Logger.Error("Failed to deserialize waypoint count: {0}", ex);
                WaypointCount = 0;
            }
            try
            {
                if (tree.HasAttribute("LastPlayerDownloads"))
                {
                    var lastPlayerDownloads = tree.GetString("LastPlayerDownloads");
                    LastPlayerDownloads = lastPlayerDownloads != null
                        ? JsonUtil.FromString<Dictionary<string, int>>(lastPlayerDownloads)
                        : [];
                }
                else
                {
                    LastPlayerDownloads = [];
                }
            }
            catch (Exception ex)
            {
                api.Logger.Error("Failed to deserialize last player updates: {0}", ex);
                    LastPlayerDownloads = [];
            }
            try
            {
                if (tree.HasAttribute("ExploredAreasIds"))
                {
                    var exploredAreasIdsStr = tree.GetString("ExploredAreasIds");
                    ExploredAreasIds = exploredAreasIdsStr != null
                        ? JsonUtil.FromString<List<ulong>>(exploredAreasIdsStr)
                        : [];
                }
                else
                {
                    ExploredAreasIds = [];
                }
            }
            catch (Exception ex)
            {                
                api.Logger.Error("Failed to deserialize explored areas ids: {0}", ex);
                ExploredAreasIds = [];
            }
        }

        internal DateTime getPlayerLastDownload(IServerPlayer fromPlayer)
        {
            return DateTimeOffset
                .FromUnixTimeMilliseconds(LastPlayerDownloads[fromPlayer.PlayerUID])
                .LocalDateTime;
        }
    }
}