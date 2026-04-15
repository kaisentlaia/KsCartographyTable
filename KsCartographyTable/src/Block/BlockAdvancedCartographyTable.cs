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
        internal Vec3f candleWickPosition = new Vec3f(0.1875f, 1.2031f, 0.1875f);
        
        // Rotation variants for the 4 horizontal orientations
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

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            initRotations();

            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            interactions = ObjectCacheUtil.GetOrCreate(api, "advancedCartographyTableBlockInteractions", () =>
                {

                    List<ItemStack> resinStackList = new List<ItemStack>();
                    var resin = api.World.Collectibles.Find(obj => obj.FirstCodePart() == "resin");
                    if (resin != null)
                    {
                        List<ItemStack> stacks = resin.GetHandBookStacks(capi);
                        if (stacks != null) resinStackList.AddRange(stacks);
                    }

                    return new WorldInteraction[]
                    {
                        new WorldInteraction()
                        {
                            ActionLangCode = "kscartographytable:blockhelp-cartography-table-share-map",
                            HotKeyCode = null,
                            MouseButton = EnumMouseButton.Right
                        },
                        new WorldInteraction()
                        {
                            ActionLangCode = "kscartographytable:blockhelp-cartography-table-update-map",
                            HotKeyCode = "sprint",
                            MouseButton = EnumMouseButton.Right
                        },
                        new WorldInteraction()
                        {
                            ActionLangCode = "kscartographytable:blockhelp-cartography-table-wipe-map",
                            HotKeyCode = null,
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = resinStackList.ToArray()
                        }
                    };
                }
            );
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            // Prevent multiple rapid interactions
            if (!CanInteract(byPlayer))
            {
                return true; // Block the interaction but return true to prevent other handlers
            }

            BlockEntityCartographyTable BlockEntityCartographyTable = FindBlockEntity(world, blockSel.Position);

            if (BlockEntityCartographyTable != null) {
                ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

                // Wipe table map with resin
                if (slot?.Itemstack != null && slot.Itemstack.Collectible.FirstCodePart() == "resin") 
                {
                    return BlockEntityCartographyTable.OnWipeTableMap(world, byPlayer, blockSel);
                }
                // Update cartography table or player map
                else
                {      
                    return BlockEntityCartographyTable.OnPlayerInteract(world, byPlayer, blockSel);
                }
            }
            
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
        {
            if (ParticleProperties != null && ParticleProperties.Length > 0)
            {
                // Determine rotation from block variant (north=0, east=1, south=2, west=3)
                string side = Variant["side"];
                int rotIndex = side switch
                {
                    "east" => 1,
                    "south" => 2,
                    "west" => 3,
                    _ => 0 // north
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