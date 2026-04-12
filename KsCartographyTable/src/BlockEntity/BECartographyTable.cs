/*
* The namespace the class will be in. This is essentially the folder the script is found in.
* If you need to use the BlockCartographyTable class in any other script, you will have to add 'using VSTutorial.Blocks' to that script.
*/
using System;
using System.Collections.Generic;
using System.Text;
using Kaisentlaia.CartographyTable.Blocks;
using Kaisentlaia.CartographyTable.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Kaisentlaia.CartographyTable.BlockEntities
{
    /*
    * The class definition. Here, you define BlockCartographyTable as a child of Block, which
    * means you can 'override' many of the functions within the general Block class. 
    */
    public class BlockEntityCartographyTable : BlockEntity
    {
        private ICoreServerAPI CoreServerAPI;
        private ICoreClientAPI CoreClientAPI;
        private CartographyMap Map;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            if (Api.Side == EnumAppSide.Server)
            {
                CoreServerAPI = Api as ICoreServerAPI;
            }
            if (Api.Side == EnumAppSide.Client)
            {
                CoreClientAPI = Api as ICoreClientAPI;
            }
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            if (Map == null)
            {
                Map = new CartographyMap();
            }
        }

        internal bool OnPurgeWaypointGroups(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (CoreServerAPI != null && KsCartographyTableModSystem.purgeWpGroups)
            {
                KsCartographyTableModSystem.ServerCartographyHelper.PurgeWaypointGroups(byPlayer);
            }
            return true;
        }

        internal bool OnWipeTableMap(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (CoreServerAPI != null)
            {
                if (Map != null && Map.Waypoints.Count > 0)
                {
                    int waypointCount = Map.Waypoints.Count;
                    KsCartographyTableModSystem.ServerCartographyHelper.WipeTableMap(Map);
                    Map = new CartographyMap();
                    MarkDirty();

                    CoreServerAPI.SendMessage(byPlayer, GlobalConstants.GeneralChatGroup,
                        Lang.Get("kscartographytable:message-table-map-wiped", waypointCount),
                        EnumChatType.Notification);

                    byPlayer.Entity.World.PlaySoundAt(new AssetLocation("game:sounds/effect/writing"), byPlayer);
                }
                else
                {
                    CoreServerAPI.SendMessage(byPlayer, GlobalConstants.GeneralChatGroup,
                        Lang.Get("kscartographytable:message-table-map-already-empty"),
                        EnumChatType.Notification);
                }
            }

            if (CoreClientAPI != null)
            {
                CoreClientAPI.World.Player.TriggerFpAnimation(EnumHandInteract.BlockInteract);
            }

            return true;
        }

        internal bool OnPlayerInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (CoreServerAPI != null)
            {
                if (!byPlayer.Entity.Controls.Sprint)
                {
                    KsCartographyTableModSystem.ServerCartographyHelper.UpdateTableMap(Map, byPlayer as IServerPlayer);
                    MarkDirty();
                }
                else if (byPlayer.Entity.Controls.Sprint)
                {
                    KsCartographyTableModSystem.ServerCartographyHelper.UpdatePlayerMap(Map, byPlayer as IServerPlayer, Block, blockSel.Position);
                }
            }
            if (CoreClientAPI != null)
            {
                CoreClientAPI.World.Player.TriggerFpAnimation(EnumHandInteract.BlockInteract);
                if (!byPlayer.Entity.Controls.Sprint)
                {
                    KsCartographyTableModSystem.ClientCartographyHelper.UpdateTableMap(Map, byPlayer as IClientPlayer, Block, blockSel.Position);
                }
            }

            return true;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (Block.GetType() != typeof(BlockAdvancedCartographyTable) && Map != null && Map.Waypoints.Count > 0)
            {
                dsc.AppendLine(Lang.Get("kscartographytable:gui-waypoint-count", Map.Waypoints.Count));
            } else if (Block.GetType() == typeof(BlockAdvancedCartographyTable) && Map != null && (Map.Waypoints.Count > 0 || Map.ExploredAreasCount > 0)) {
                dsc.AppendLine(Lang.Get("kscartographytable:gui-waypoint-chunks-count", Map.Waypoints.Count, Map.ExploredAreasCount));
            } else
            {
                dsc.AppendLine(Lang.Get("kscartographytable:gui-empty-map"));
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (Map != null)
            {
                try
                {
                    tree.SetString("Waypoints", JsonUtil.ToString(Map.Waypoints));
                }
                catch (Exception ex)
                {
                    Api.Logger.Error("Failed to serialize waypoints: {0}", ex);
                    tree.SetString("Waypoints", JsonUtil.ToString(new List<CartographyWaypoint>()));
                }
                try
                {
                    tree.SetString("DeletedWaypoints", JsonUtil.ToString(Map.DeletedWaypoints));
                }
                catch (Exception ex)
                {
                    Api.Logger.Error("Failed to serialize deleted waypoints: {0}", ex);
                    tree.SetString("DeletedWaypoints", JsonUtil.ToString(new List<CartographyWaypoint>()));
                }
                try
                {
                    tree.SetString("ExploredAreasCount", JsonUtil.ToString(Map.ExploredAreasCount));
                }
                catch (Exception ex)
                {
                    Api.Logger.Error("Failed to serialize explored areas count: {0}", ex);
                    tree.SetString("ExploredAreasCount", JsonUtil.ToString(0));
                }
            }
            else
            {
                tree.SetString("Waypoints", JsonUtil.ToString(new List<CartographyWaypoint>()));
                tree.SetString("DeletedWaypoints", JsonUtil.ToString(new List<CartographyWaypoint>()));
                tree.SetString("ExploredAreasCount", JsonUtil.ToString(0));
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            if (Map == null)
            {
                Map = new CartographyMap();
            }
            try
            {
                if (tree.HasAttribute("Waypoints"))
                {
                    var savedWaypoints = tree.GetString("Waypoints");
                    Map.Waypoints = savedWaypoints != null
                        ? JsonUtil.FromString<List<CartographyWaypoint>>(savedWaypoints)
                        : new List<CartographyWaypoint>();
                }
                else
                {
                    Map.Waypoints = new List<CartographyWaypoint>();
                }
            }
            catch (Exception ex)
            {
                Api.Logger.Error("Failed to deserialize waypoints: {0}", ex);
                Map.Waypoints = new List<CartographyWaypoint>();
            }
            try
            {
                if (tree.HasAttribute("DeletedWaypoints"))
                {
                    var deletedWaypoints = tree.GetString("DeletedWaypoints");
                    Map.DeletedWaypoints = deletedWaypoints != null
                        ? JsonUtil.FromString<List<CartographyWaypoint>>(deletedWaypoints)
                        : new List<CartographyWaypoint>();
                }
                else
                {
                    Map.DeletedWaypoints = new List<CartographyWaypoint>();
                }
            }
            catch (Exception ex)
            {
                Api.Logger.Error("Failed to deserialize deleted waypoints: {0}", ex);
                Map.DeletedWaypoints = new List<CartographyWaypoint>();
            }
            try
            {
                if (tree.HasAttribute("ExploredAreasCount"))
                {
                    var exploredAreasCountStr = tree.GetString("ExploredAreasCount");
                    Map.ExploredAreasCount = exploredAreasCountStr != null
                        ? JsonUtil.FromString<int>(exploredAreasCountStr)
                        : 0;
                }
                else
                {
                    Map.ExploredAreasCount = 0;
                }
            }
            catch (Exception ex)
            {
                Api.Logger.Error("Failed to deserialize explored areas count: {0}", ex);
                Map.ExploredAreasCount = 0;
            }
        }
        public void UpdateMapExploredAreasCount(int count) {
            if (Map != null)
            {
                Map.ExploredAreasCount = count;
                MarkDirty();
            }
        }
    }
}