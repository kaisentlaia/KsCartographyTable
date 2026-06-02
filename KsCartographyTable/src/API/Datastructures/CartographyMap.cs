using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

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

        public bool Empty
        {
            get { return ExploredAreasIds.Count < 1 && waypointCount < 1; }
        }
        private Dictionary<string, long> lastPlayerSyncs = [];
        public Dictionary<string, long> LastPlayerSyncs
        {
            get { return lastPlayerSyncs; }
            set { lastPlayerSyncs = value; }
        }
        private bool isWiping = false;
        public bool IsWiping
        {
            get { return isWiping; }
            set { isWiping = value; }
        }
        private bool isWriting = false;
        public bool IsWriting
        {
            get { return isWriting; }
            set { isWriting = value; }
        }
        private bool hasWrittenData = false;
        public bool HasWrittenData
        {
            get { return hasWrittenData; }
            set { hasWrittenData = value; }
        }
        private bool isPondering = false;
        public bool IsPondering
        {
            get { return isPondering; }
            set { isPondering = value; }
        }
        private List<Vec3d> palantirWaypoints = new List<Vec3d>();
        public List<Vec3d> PalantirWaypoints
        {
            get { return palantirWaypoints; }
            set { palantirWaypoints = value; }
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
                tree.SetString("LastPlayerSyncs", JsonUtil.ToString(LastPlayerSyncs));
            }
            catch (Exception ex)
            {
                api.Logger.Error("Failed to serialize last player updates: {0}", ex);
                tree.SetString("LastPlayerSyncs", JsonUtil.ToString(new Dictionary<string, int>()));
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
            try
            {
                tree.SetString("IsWiping", JsonUtil.ToString(IsWiping));
            }
            catch (Exception ex)
            {
                api.Logger.Error("Failed to serialize is wiping: {0}", ex);
                tree.SetString("IsWiping", JsonUtil.ToString(false));
            }
            try
            {
                tree.SetString("IsWriting", JsonUtil.ToString(IsWriting));
            }
            catch (Exception ex)
            {
                api.Logger.Error("Failed to serialize is writing: {0}", ex);
                tree.SetString("IsWriting", JsonUtil.ToString(false));
            }
            try
            {
                tree.SetString("HasWrittenData", JsonUtil.ToString(HasWrittenData));
            }
            catch (Exception ex)
            {
                api.Logger.Error("Failed to serialize has written data: {0}", ex);
                tree.SetString("HasWrittenData", JsonUtil.ToString(false));
            }
            try
            {
                tree.SetString("IsPondering", JsonUtil.ToString(IsPondering));
            }
            catch (Exception ex)
            {
                api.Logger.Error("Failed to serialize is pondering: {0}", ex);
                tree.SetString("IsPondering", JsonUtil.ToString(false));
            }
            try
            {
                tree.SetString("PalantirWaypoints", JsonUtil.ToString(PalantirWaypoints));
            }
            catch (Exception ex)
            {
                api.Logger.Error("Failed to serialize palantir waypoints: {0}", ex);
                tree.SetString("PalantirWaypoints", JsonUtil.ToString(new List<Vec3d>()));
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
                if (tree.HasAttribute("LastPlayerSyncs"))
                {
                    var lastPlayerSyncs = tree.GetString("LastPlayerSyncs");
                    LastPlayerSyncs = lastPlayerSyncs != null
                        ? JsonUtil.FromString<Dictionary<string, long>>(lastPlayerSyncs)
                        : [];
                }
                else
                {
                    LastPlayerSyncs = [];
                }
            }
            catch (Exception ex)
            {
                api.Logger.Error("Failed to deserialize last player updates: {0}", ex);
                LastPlayerSyncs = [];
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
            
            try
            {
                if (tree.HasAttribute("IsWiping"))
                {
                    var isWipingStr = tree.GetString("IsWiping");
                    IsWiping = isWipingStr != null
                        ? JsonUtil.FromString<bool>(isWipingStr)
                        : false;
                }
                else
                {
                    IsWiping = false;
                }
            }
            catch (Exception ex)
            {                
                api.Logger.Error("Failed to deserialize is wiping: {0}", ex);
                IsWiping = false;
            }

            try
            {
                if (tree.HasAttribute("IsWriting"))
                {
                    var isWritingStr = tree.GetString("IsWriting");
                    IsWriting = isWritingStr != null
                        ? JsonUtil.FromString<bool>(isWritingStr)
                        : false;
                }
                else
                {
                    IsWriting = false;
                }
            }
            catch (Exception ex)
            {                
                api.Logger.Error("Failed to deserialize is writing: {0}", ex);
                IsWriting = false;
            }

            try
            {
                if (tree.HasAttribute("HasWrittenData"))
                {
                    var hasWrittenDataStr = tree.GetString("HasWrittenData");
                    HasWrittenData = hasWrittenDataStr != null
                        ? JsonUtil.FromString<bool>(hasWrittenDataStr)
                        : false;
                }
                else
                {
                    HasWrittenData = false;
                }
            }
            catch (Exception ex)
            {                
                api.Logger.Error("Failed to deserialize has written data: {0}", ex);
                HasWrittenData = false;
            }

            try
            {
                if (tree.HasAttribute("IsPondering"))
                {
                    var isPonderingStr = tree.GetString("IsPondering");
                    IsPondering = isPonderingStr != null
                        ? JsonUtil.FromString<bool>(isPonderingStr)
                        : false;
                }
                else
                {
                    IsPondering = false;
                }
            }
            catch (Exception ex)
            {                
                api.Logger.Error("Failed to deserialize has is pondering: {0}", ex);
                IsPondering = false;
            }
            
            try
            {
                if (tree.HasAttribute("PalantirWaypoints"))
                {
                    var palantirWaypointsStr = tree.GetString("PalantirWaypoints");
                    PalantirWaypoints = palantirWaypointsStr != null
                        ? JsonUtil.FromString<List<Vec3d>>(palantirWaypointsStr)
                        : [];
                }
                else
                {
                    PalantirWaypoints = [];
                }
            }
            catch (Exception ex)
            {                
                api.Logger.Error("Failed to deserialize palantir waypoints: {0}", ex);
                PalantirWaypoints = [];
            }
        }

        internal DateTime GetPlayerLastSync(IPlayer fromPlayer)
        {
            if (!LastPlayerSyncs.TryGetValue(fromPlayer.PlayerUID, out long lastPlayerSyncMillis))
            {
                return DateTime.Now.AddYears(-1);
            }
            return DateTimeOffset
                .FromUnixTimeMilliseconds(lastPlayerSyncMillis)
                .LocalDateTime;
        }

        internal void SetPlayerLastSync(IPlayer forPlayer)
        {
            long now = ((DateTimeOffset)DateTime.Now.ToUniversalTime()).ToUnixTimeMilliseconds();
            if (LastPlayerSyncs.TryAdd(forPlayer.PlayerUID, now))
            {
                return;
            }
            LastPlayerSyncs[forPlayer.PlayerUID] = now;
        }
    }
}