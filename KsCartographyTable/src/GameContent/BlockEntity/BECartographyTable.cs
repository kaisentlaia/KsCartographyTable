using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
    public class BlockEntityCartographyTable : BlockEntity
    {
        protected ILoadedSound ambientSound;
        private ICoreServerAPI CoreServerAPI;
        private ICoreClientAPI CoreClientAPI;
        public EnumAppSide Side;
        public CartographyMap Map;
        public CartographyTableFxController fxController;

        public bool IsAdvanced
        {
            get
            {
                return Block is BlockAdvancedCartographyTable || Block is BlockAdvancedCartographyTablePart;
            }
        }

        public enum EnumCartographyTableCloseSoundTypes
        {
            NothingWritten,
            SomethingWritten,
            Unknown,
            None
        }
        private readonly Dictionary<string, (CartographyAction action, long timestamp)> recentInteractions = new();
        private const long INTERACTION_GRACE_MS = 500;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            Side = Api.Side;
            if (Side == EnumAppSide.Server)
            {
                CoreServerAPI = Api as ICoreServerAPI;
            }
            if (Side == EnumAppSide.Client)
            {
                CoreClientAPI = Api as ICoreClientAPI;
                fxController = new CartographyTableFxController(CoreClientAPI, Pos, IsAdvanced);
                RegisterGameTickListener(Every50ms, 50);
            }
        }
    
        private void Every50ms(float dt)
        {
            fxController?.Tick(dt);
        }

        public void EnsureMap()
        {
            if (Map == null)
            {
                Map = new CartographyMap(Api);
            }
        }

        internal bool OnPonderMap(IPlayer byPlayer)
        {
            EnsureMap();
            if (CoreClientAPI != null)
            {
                KsCartographyTableModSystem.ClientCartographyService.Ponder(byPlayer as IClientPlayer, this);
            }

            return true;
        }

        internal bool OnWipeTableMap(IPlayer byPlayer)
        {
            EnsureMap();
            if (Side == EnumAppSide.Server)
            {
                CoreServerAPI.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} OnWipeTableMap server - Waypoint count {Map.WaypointCount} - Areas count {Map.ExploredAreasIds.Count}");
                KsCartographyTableModSystem.ServerCartographyService.WipeTableMap(Block, byPlayer, this);
            }

            if (Side == EnumAppSide.Client)
            {
                CoreClientAPI.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} OnWipeTableMap client");
                CoreClientAPI.World.Player.TriggerFpAnimation(EnumHandInteract.BlockInteract);
            }

            MarkDirty();

            return true;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (!IsAdvanced && Map != null && Map.WaypointCount > 0)
            {
                dsc.AppendLine(Lang.Get(CartographyTableLangCodes.GUI_TABLE_WAYPOINTS, Map.WaypointCount));
            } else if (Block is BlockAdvancedCartographyTable && Map != null && (Map.WaypointCount > 0 || Map.ExploredAreasIds.Count > 0)) {
                double km2 = Map.ExploredAreasIds.Count * 0.001024;
                dsc.AppendLine(Lang.Get(CartographyTableLangCodes.GUI_TABLE_MAP_WAYPOINTS, Map.WaypointCount, $"{km2:F1}"));
            } else
            {
                dsc.AppendLine(Lang.Get(CartographyTableLangCodes.GUI_TABLE_EMPTY));
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            Map?.Serialize(tree);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            EnsureMap();
            bool wasWiping = Map.IsWiping;
            bool wasWriting = Map.IsWriting;
            bool wasPondering = Map.IsPondering;
            
            Map.Deserialize(tree);

            if (Side == EnumAppSide.Client)
            {
                UpdateFxState(wasWiping, wasPondering, wasWriting);
            }
        }
        
        public void UpdateMapExploredAreasIds(List<FastVec2i> piecesIds)
        {
            EnsureMap();
            Map.ExploredAreasIds = [.. piecesIds.Select(pieceId => pieceId.ToChunkIndex())];
            Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} UpdateMapExploredAreasIds {Map.ExploredAreasIds.Count}");
            MarkDirty();
        }

        internal void UpdateMapWaypointCount(int waypointCount)
        {
            EnsureMap();
            Map.WaypointCount = waypointCount;
            Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} UpdateMapWaypointCount {Map.WaypointCount}");
            MarkDirty();
        }

        internal void SetPalantirWaypointPositions(List<Vec3d> palantirWaypoints)
        {
            EnsureMap();
            Map.PalantirWaypoints = palantirWaypoints;
            Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} UpdatePalantirWaypoints {Map.PalantirWaypoints.Count}");
            MarkDirty();
        }

        internal DateTime GetPlayerLastDownload(IPlayer forPlayer)
        {
            EnsureMap();
            return Map.GetPlayerLastSync(forPlayer);
        }

        internal void SetPlayerSyncToNow(IPlayer player)
        {
            EnsureMap();
            Map.SetPlayerLastSync(player);
            MarkDirty();
        }

        internal bool OnCartographySessionStart(CartographyAction action, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (action == CartographyAction.UploadMap)
            {
                if (Side == EnumAppSide.Client)
                {
                    KsCartographyTableModSystem.ClientCartographyService.StartCartographyUploadSession(action, world, byPlayer, blockSel.Position, Block, this);
                }
                return true;
            }
            if (action == CartographyAction.DownloadMap)
            {
                if (Side == EnumAppSide.Server)
                {
                    KsCartographyTableModSystem.ServerCartographyService.StartCartographyDownloadSession(action, world, byPlayer, Block, blockSel.Position, this);
                }
                
                return true;
            }
            return false;
        }

        internal bool OnCartographySessionStep(CartographyAction action, float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (action == CartographyAction.UploadMap)
            {
                if (Side == EnumAppSide.Client && KsCartographyTableModSystem.ClientCartographyService.HasCartographyUploadSession(byPlayer, Block))
                {
                    KsCartographyTableModSystem.ClientCartographyService.ContinueCartographyUploadSession(byPlayer, secondsUsed, Block);
                }
                // always return true even when the session is complete to keep the interaction going until stopped by the player
                return true;
            }
            if (action == CartographyAction.DownloadMap)
            {
                if (Side == EnumAppSide.Server && KsCartographyTableModSystem.ServerCartographyService.HasCartographyDownloadSession(byPlayer, Block))
                {
                    KsCartographyTableModSystem.ServerCartographyService.ContinueCartographyDownloadSession(byPlayer, secondsUsed, Block, this);
                }
                // always return true even when the session is complete to keep the interaction going until stopped by the player
                return true;
            }
            return false;
        }

        internal void OnCartographySessionStop(CartographyAction action, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (action == CartographyAction.UploadMap)
            {
                if (Side == EnumAppSide.Client)
                {
                    KsCartographyTableModSystem.ClientCartographyService.EndCartographyUploadSession(byPlayer, Block, this);
                }
            }
            if (action == CartographyAction.DownloadMap)
            {
                if (Side == EnumAppSide.Server)
                {
                    KsCartographyTableModSystem.ServerCartographyService.EndCartographyDownloadSession(byPlayer, world.BlockAccessor.GetBlock(blockSel.Position), this);
                }
            }
        }

        private void UpdateFxState(bool wasWiping, bool wasPondering, bool wasWriting)
        {
            EnsureMap();
            fxController?.OnStateChanged(
                wasWiping, Map.IsWiping,
                wasWriting, Map.IsWriting,
                wasPondering, Map.IsPondering,
                Map.HasWrittenData);
        }

        public void SetWriting(bool writing)
        {
            EnsureMap();
            if (Map.IsWriting != writing)
            {
                bool wasWiping = Map.IsWiping;
                bool wasWriting = Map.IsWriting;
                bool wasPondering = Map.IsPondering;
                Map.IsWriting = writing;
                Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} SetWriting {writing} {Side}");

                switch (Side)
                {
                    case EnumAppSide.Server:
                        MarkDirty();
                        break;
                    case EnumAppSide.Client:
                        UpdateFxState(wasWiping, wasPondering, wasWriting);
                        break;
                }
            }
        }

        public void SetWiping(bool wiping)
        {
            EnsureMap();
            if (Map.IsWiping != wiping)
            {
                bool wasWiping = Map.IsWiping;
                bool wasWriting = Map.IsWriting;
                bool wasPondering = Map.IsPondering;
                Map.IsWiping = wiping;
                Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} SetWiping {wiping} {Side}");

                switch (Side)
                {
                    case EnumAppSide.Server:
                        MarkDirty();
                        break;
                    case EnumAppSide.Client:
                        UpdateFxState(wasWiping, wasPondering, wasWriting);
                        break;
                }
            }
        }

        public void SetPondering(bool pondering)
        {
            EnsureMap();
            if (Map.IsPondering != pondering)
            {
                bool wasWiping = Map.IsWiping;
                bool wasWriting = Map.IsWriting;
                bool wasPondering = Map.IsPondering;
                Map.IsPondering = pondering;
                Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} SetPondering {pondering} {Side}");

                switch (Side)
                {
                    case EnumAppSide.Server:
                        MarkDirty();
                        break;
                    case EnumAppSide.Client:
                        UpdateFxState(wasWiping, wasPondering, wasWriting);
                        break;
                }
            }
        }

        public void SetWritten(bool written)
        {
            Map.HasWrittenData = written;
            Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} SetWritten {written} {Side}");

            if (Api.Side == EnumAppSide.Server)
            {
                MarkDirty();
            }
        }

        internal bool IsBusy()
        {
            EnsureMap();
            return Map.IsWiping || Map.IsPondering || Map.IsWriting;
        }

        internal string GetWritingAnimation()
        {
            EnsureMap();
            return Map.IsWriting ? "mapwrite" : null;
        }

        internal void SetIdle(IPlayer byPlayer, BlockCartographyTable blockCartographyTable)
        {
            EnsureMap();
            SetWiping(false);
            SetWriting(false);
            SetPondering(false);
            if (Side == EnumAppSide.Client && KsCartographyTableModSystem.ClientCartographyService.HasCartographyUploadSession(byPlayer, blockCartographyTable))
            {
                KsCartographyTableModSystem.ClientCartographyService.EndCartographyUploadSession(byPlayer, blockCartographyTable, this);
            }
            if (Side == EnumAppSide.Server && KsCartographyTableModSystem.ServerCartographyService.HasCartographyDownloadSession(byPlayer, blockCartographyTable))
            {
                KsCartographyTableModSystem.ServerCartographyService.EndCartographyDownloadSession(byPlayer, blockCartographyTable, this);
            }
        }
        
        public void RegisterInteraction(IPlayer player, CartographyAction action)
        {
            if (!recentInteractions.TryGetValue(player.PlayerUID, out var value))
            {
                Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} Registering interaction {action} for {player.PlayerUID}");
            }
            recentInteractions[player.PlayerUID] = (action, Api.World.ElapsedMilliseconds);
        }

        public CartographyAction? GetRecentInteraction(IPlayer player)
        {
            if (recentInteractions.TryGetValue(player.PlayerUID, out var data))
            {
                if (Api.World.ElapsedMilliseconds - data.timestamp < INTERACTION_GRACE_MS)
                {
                    return data.action;
                }
                else
                {
                    Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} Removing interaction {data.action} for {player.PlayerUID}");
                }
                recentInteractions.Remove(player.PlayerUID);
            }
            else
            {
                Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} No interaction for {player.PlayerUID}");
            }
            return null;
        }

        public bool HasAnotherPlayerInteracting(IPlayer player)
        {
            if (recentInteractions.Keys.Any(uid => uid != player.PlayerUID))
            {
                Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} Another player is interacting, {recentInteractions.Keys}");
            }
            return recentInteractions.Keys.Any(uid => uid != player.PlayerUID);
        }

        public void ClearRecentInteraction(IPlayer player)
        {
            Api.Logger.Debug($"{CartographyTableConstants.MAP_EVENT} Clearing interaction for {player.PlayerUID}");
            recentInteractions.Remove(player.PlayerUID);
        }
    }
}