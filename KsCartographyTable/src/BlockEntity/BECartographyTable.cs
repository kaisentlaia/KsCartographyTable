/*
* The namespace the class will be in. This is essentially the folder the script is found in.
* If you need to use the BlockCartographyTable class in any other script, you will have to add 'using VSTutorial.Blocks' to that script.
*/
using System;
using System.Collections.Generic;
using System.Text;
using Kaisentlaia.CartographyTable.GameContent;
using Kaisentlaia.CartographyTable.Utilities;
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
        public CartographyHelper CartographyHelper;
        private List<CartographyWaypoint> Waypoints;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            if(Api.Side == EnumAppSide.Server) {
                CoreServerAPI = Api as ICoreServerAPI;
                CartographyHelper = new CartographyHelper(CoreServerAPI, Waypoints);
            }
            if(Api.Side == EnumAppSide.Client) {
                CoreClientAPI = Api as ICoreClientAPI;
            }
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            Waypoints = new List<CartographyWaypoint>();
            if (CartographyHelper != null) {
                CartographyHelper.SetWaypoints(Waypoints);
            }
        }

        internal bool OnPurgeWaypointGroups(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (CoreServerAPI != null && KsCartographyTableModSystem.purgeWpGroups) {
                CartographyHelper.PurgeWaypointGroups(byPlayer);
            }
            return true;
        }

        internal bool OnPlayerInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (CoreServerAPI != null) {
                if (!byPlayer.Entity.Controls.Sprint) {
                    CartographyHelper.SetWaypoints(Waypoints);
                    Waypoints = CartographyHelper.shareWaypoints(byPlayer as IServerPlayer);
                    MarkDirty();
                } else if (byPlayer.Entity.Controls.Sprint) {
                    CartographyHelper.updateWaypoints(byPlayer as IServerPlayer);
                }
            }
            if (CoreClientAPI != null) {
                CoreClientAPI.World.Player.TriggerFpAnimation(EnumHandInteract.BlockInteract);
            }

            return true;
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc) {
            if (Waypoints != null && Waypoints.Count > 0) {
                dsc.AppendLine(Lang.Get("kscartographytable:gui-waypoint-count", Waypoints.Count));
            } else {
                dsc.AppendLine(Lang.Get("kscartographytable:gui-empty-map"));
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            // bytes would be better, but they cause exceptions
            if (Waypoints != null) {
                try {
                    tree.SetString("Waypoints", JsonUtil.ToString(Waypoints));
                } catch (Exception ex) {
                    Api.Logger.Error(ex.StackTrace);
                    tree.SetString("Waypoints", JsonUtil.ToString(new List<CartographyWaypoint>()));
                }
            } else {
                tree.SetString("Waypoints", JsonUtil.ToString(new List<CartographyWaypoint>()));
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            // bytes would be better, but they cause exceptions
            try {
                if(tree.HasAttribute("Waypoints")) {
                    var savedWaypoints = tree.GetString("Waypoints");
                    if (savedWaypoints != null) {
                        Waypoints = JsonUtil.FromString<List<CartographyWaypoint>>(savedWaypoints); 
                    } else {
                        Waypoints = new List<CartographyWaypoint>();
                    }
                } else {
                    Waypoints = new List<CartographyWaypoint>();
                }
            } catch (Exception ex) {
                Api.Logger.Error(ex.StackTrace);
                Waypoints = new List<CartographyWaypoint>();
            }
        }
    }
}