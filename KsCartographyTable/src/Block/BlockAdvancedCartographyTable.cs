using System;
using System.Collections.Generic;
using Kaisentlaia.CartographyTable.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Kaisentlaia.CartographyTable.Blocks
{
    internal class BlockAdvancedCartographyTable : BlockCartographyTable
    {
        internal Vec3f candleWickPosition = new Vec3f(0.1875f, 1.29f, 0.1875f);
        
        Vec3f[] candleWickPositionsByRot = new Vec3f[4];

        internal void initRotations()
        {
            for (int i = 0; i < 4; i++)
            {
                Matrixf m = new Matrixf();
                m.Translate(0.5f, 0.5f, 0.5f);
                m.RotateYDeg(i * 90);
                m.Translate(-0.5f, -0.5f, -0.5f);

                Vec4f rotated = m.TransformVector(new Vec4f(candleWickPosition.X, candleWickPosition.Y, candleWickPosition.Z, 1));
                candleWickPositionsByRot[i] = new Vec3f(rotated.X, rotated.Y, rotated.Z);
            }
        }

        public override bool DoPartialSelection(IWorldAccessor world, BlockPos pos)
        {
            return true; // Essential for boxes outside 0-1 range
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, pos, byItemStack);
            
            // Place companion block based on orientation
            BlockPos companionPos = GetCompanionPosition(pos);
            Block companionBlock = world.GetBlock(new AssetLocation("kscartographytable:advancedcartographytable-part"));
            world.BlockAccessor.SetBlock(companionBlock.BlockId, companionPos);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            // Remove companion block
            BlockPos companionPos = GetCompanionPosition(pos);
            if (world.BlockAccessor.GetBlock(companionPos).Code.Path == "advancedcartographytable-part")
            {
                world.BlockAccessor.SetBlock(0, companionPos);
            }
            
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        private BlockPos GetCompanionPosition(BlockPos pos)
        {
            string side = Variant["side"];
            return side switch
            {
                "north" => pos.EastCopy(),
                "south" => pos.WestCopy(),
                "east" => pos.SouthCopy(),
                "west" => pos.NorthCopy(),
                _ => pos.EastCopy()
            };
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            initRotations();

            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            // Empty - handled per-box in GetPlacedBlockInteractionHelp
            interactions = ObjectCacheUtil.GetOrCreate(api, "advancedCartographyTableBlockInteractions", () => Array.Empty<WorldInteraction>());
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            int boxIndex = blockSel.SelectionBoxIndex;

            switch (boxIndex)
            {
                case 0: // Table base
                    return HandleTableInteract(world, byPlayer, blockSel);
                    
                case 1: // Ink and quill
                    return HandleInkInteract(world, byPlayer, blockSel);
                    
                case 2: // Book
                    return HandleBookInteract(world, byPlayer, blockSel);            
                default:
                    return base.OnBlockInteractStart(world, byPlayer, blockSel);
            }
        }

        private bool HandleTableInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (!CanInteract(byPlayer)) return true;
            
            BlockEntityCartographyTable beTable = FindBlockEntity(world, blockSel.Position);
            if (beTable == null) return false;
            
            // Purge waypoint groups if enabled in mod configuration
            if (KsCartographyTableModSystem.purgeWpGroups) 
            {
                return beTable.OnPurgeWaypointGroups(world, byPlayer, blockSel);
            }
            
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        private bool HandleInkInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (!CanInteract(byPlayer)) return true;
            
            BlockEntityCartographyTable beTable = FindBlockEntity(world, blockSel.Position);
            if (beTable == null) return false;
            
            return beTable.OnPlayerInteract(world, byPlayer, blockSel);
        }

        private bool HandleBookInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (!CanInteract(byPlayer)) return true;
            
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            
            // Book area requires resin to wipe the map
            if (slot?.Itemstack != null && HasItemInHand(byPlayer, "resin"))
            {
                BlockEntityCartographyTable beTable = FindBlockEntity(world, blockSel.Position);
                if (beTable == null) return false;
                
                return beTable.OnWipeTableMap(world, byPlayer, blockSel);
            }
            
            return false;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            List<WorldInteraction> help = new List<WorldInteraction>();
            int boxIndex = selection.SelectionBoxIndex;
            
            // Map to logical boxes: 0=table, 1=ink, 2=book
            int logicalBox = boxIndex switch
            {
                0 or 2 => 0,  // Table base (indices 0 and 2)
                1 => 1,       // Ink/quill (index 1 only)
                _ => 2        // Book (index 3 only)
            };
            
            switch (logicalBox)
            {
                case 0: // Table base
                    break;
                    
                case 1: // Ink and quill are managed in the CartographyTable class
                    break;
                    
                case 2: // Cartography book
                    help.Add(new WorldInteraction()
                    {
                        ActionLangCode = "kscartographytable:blockhelp-cartography-table-wipe-map",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = GetResinStacks(world)
                    });
                    break;
            }

            return help.ToArray().Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }

        public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
        {
            if (ParticleProperties != null && ParticleProperties.Length > 0)
            {
                string side = Variant["side"];
                int rotIndex = side switch
                {
                    "east" => 1,
                    "south" => 2,
                    "west" => 3,
                    _ => 0
                };
                
                Vec3f wickPos = candleWickPositionsByRot[rotIndex];

                for (int i = 0; i < ParticleProperties.Length; i++)
                {
                    AdvancedParticleProperties bps = ParticleProperties[i];
                    bps.WindAffectednesAtPos = windAffectednessAtPos;
                    bps.basePos.X = pos.X + wickPos.X;
                    bps.basePos.Y = pos.InternalY + wickPos.Y;
                    bps.basePos.Z = pos.Z + wickPos.Z;
                    manager.Spawn(bps);
                }
            }
        }
    }
}