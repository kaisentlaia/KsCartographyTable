using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace KsCartographyTable.test.Unit;

public class FakeBlockAccessor(BlockEntity fakeBlockEntity) : IBlockAccessor
{
    public int ChunkSize => throw new NotImplementedException();

    public int RegionSize => throw new NotImplementedException();

    public int MapSizeX => throw new NotImplementedException();

    public int MapSizeY => throw new NotImplementedException();

    public int MapSizeZ => throw new NotImplementedException();

    public int RegionMapSizeX => throw new NotImplementedException();

    public int RegionMapSizeY => throw new NotImplementedException();

    public int RegionMapSizeZ => throw new NotImplementedException();

    public bool UpdateSnowAccumMap { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public Vec3i MapSize => throw new NotImplementedException();

    private readonly BlockEntity blockEntity = fakeBlockEntity;

    public void BreakBlock(BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
        throw new NotImplementedException();
    }

    public bool BreakDecor(BlockPos pos, BlockFacing side = null, int? decorIndex = null)
    {
        throw new NotImplementedException();
    }

    public List<BlockUpdate> Commit()
    {
        throw new NotImplementedException();
    }

    public IMiniDimension CreateMiniDimension(Vec3d position)
    {
        throw new NotImplementedException();
    }

    public void DamageBlock(BlockPos pos, BlockFacing facing, float damage)
    {
        throw new NotImplementedException();
    }

    public void ExchangeBlock(int blockId, BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public Block GetBlock(BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public Block GetBlock(BlockPos pos, int layer)
    {
        throw new NotImplementedException();
    }

    public Block GetBlock(int x, int y, int z, int layer)
    {
        throw new NotImplementedException();
    }

    public Block GetBlock(int posX, int posY, int posZ)
    {
        throw new NotImplementedException();
    }

    public Block GetBlock(int blockId)
    {
        throw new NotImplementedException();
    }

    public Block GetBlock(AssetLocation code)
    {
        throw new NotImplementedException();
    }

    public BlockEntity GetBlockEntity(BlockPos position)
    {
        return blockEntity;
    }

    public T GetBlockEntity<T>(BlockPos position) where T : BlockEntity
    {
        return blockEntity as T;
    }

    public int GetBlockId(BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public int GetBlockId(int posX, int posY, int posZ)
    {
        throw new NotImplementedException();
    }

    public Block GetBlockOrNull(int x, int y, int z, int layer = 4)
    {
        throw new NotImplementedException();
    }

    public Block GetBlockRaw(int x, int y, int z, int layer = 0)
    {
        throw new NotImplementedException();
    }

    public IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ)
    {
        throw new NotImplementedException();
    }

    public IWorldChunk GetChunk(long chunkIndex3D)
    {
        throw new NotImplementedException();
    }

    public IWorldChunk GetChunkAtBlockPos(BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public IWorldChunk GetChunkAtBlockPos(int posX, int posY, int posZ)
    {
        throw new NotImplementedException();
    }

    public ClimateCondition GetClimateAt(BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.NowValues, double totalDays = 0)
    {
        throw new NotImplementedException();
    }

    public ClimateCondition GetClimateAt(BlockPos pos, ClimateCondition baseClimate, EnumGetClimateMode mode, double totalDays)
    {
        throw new NotImplementedException();
    }

    public ClimateCondition GetClimateAt(BlockPos pos, int climate)
    {
        throw new NotImplementedException();
    }

    public Block GetDecor(BlockPos pos, int decorIndex)
    {
        throw new NotImplementedException();
    }

    public Block[] GetDecors(BlockPos position)
    {
        throw new NotImplementedException();
    }

    public int GetDistanceToRainFall(BlockPos pos, int horziontalSearchWidth = 4, int verticalSearchWidth = 1)
    {
        throw new NotImplementedException();
    }

    public int GetLightLevel(BlockPos pos, EnumLightLevelType type)
    {
        throw new NotImplementedException();
    }

    public int GetLightLevel(int x, int y, int z, EnumLightLevelType type)
    {
        throw new NotImplementedException();
    }

    public Vec4f GetLightRGBs(int posX, int posY, int posZ)
    {
        throw new NotImplementedException();
    }

    public Vec4f GetLightRGBs(BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public int GetLightRGBsAsInt(int posX, int posY, int posZ)
    {
        throw new NotImplementedException();
    }

    public IMapChunk GetMapChunk(Vec2i chunkPos)
    {
        throw new NotImplementedException();
    }

    public IMapChunk GetMapChunk(int chunkX, int chunkZ)
    {
        throw new NotImplementedException();
    }

    public IMapChunk GetMapChunkAtBlockPos(BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public IMapRegion GetMapRegion(int regionX, int regionZ)
    {
        throw new NotImplementedException();
    }

    public Block GetMostSolidBlock(BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public Block GetMostSolidBlock(int x, int y, int z)
    {
        throw new NotImplementedException();
    }

    public int GetRainMapHeightAt(BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public int GetRainMapHeightAt(int posX, int posZ)
    {
        throw new NotImplementedException();
    }

    public Dictionary<int, Block> GetSubDecors(BlockPos position)
    {
        throw new NotImplementedException();
    }

    public int GetTerrainMapheightAt(BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public Vec3d GetWindSpeedAt(Vec3d pos)
    {
        throw new NotImplementedException();
    }

    public Vec3d GetWindSpeedAt(BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public bool IsNotTraversable(double x, double y, double z)
    {
        throw new NotImplementedException();
    }

    public bool IsNotTraversable(double x, double y, double z, int dim)
    {
        throw new NotImplementedException();
    }

    public bool IsNotTraversable(BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public bool IsSideSolid(int x, int y, int z, BlockFacing facing)
    {
        throw new NotImplementedException();
    }

    public bool IsValidPos(int posX, int posY, int posZ)
    {
        throw new NotImplementedException();
    }

    public bool IsValidPos(BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public void MarkAbsorptionChanged(int oldAbsorption, int newAbsorption, BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public void MarkBlockDirty(BlockPos pos, IPlayer skipPlayer = null)
    {
        throw new NotImplementedException();
    }

    public void MarkBlockDirty(BlockPos pos, Action OnRetesselated)
    {
        throw new NotImplementedException();
    }

    public void MarkBlockEntityDirty(BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public void MarkBlockModified(BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public void MarkChunkDecorsModified(BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public void RedrawNeighbouringChunk(BlockPos pos, BlockFacing side = null)
    {
        throw new NotImplementedException();
    }

    public void RemoveBlockEntity(BlockPos position)
    {
        throw new NotImplementedException();
    }

    public void RemoveBlockLight(byte[] oldLightHsV, BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public void Rollback()
    {
        throw new NotImplementedException();
    }

    public void SearchBlocks(BlockPos minPos, BlockPos maxPos, ActionConsumable<Block, BlockPos> onBlock, Action<int, int, int> onChunkMissing = null)
    {
        throw new NotImplementedException();
    }

    public void SearchFluidBlocks(BlockPos minPos, BlockPos maxPos, ActionConsumable<Block, BlockPos> onBlock, Action<int, int, int> onChunkMissing = null)
    {
        throw new NotImplementedException();
    }

    public void SetBlock(int blockId, BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public void SetBlock(int blockId, BlockPos pos, int layer)
    {
        throw new NotImplementedException();
    }

    public void SetBlock(int blockId, BlockPos pos, ItemStack byItemstack)
    {
        throw new NotImplementedException();
    }

    public bool SetDecor(Block block, BlockPos position, BlockFacing onFace)
    {
        throw new NotImplementedException();
    }

    public bool SetDecor(Block block, BlockPos position, int decorIndex)
    {
        throw new NotImplementedException();
    }

    public void SpawnBlockEntity(string classname, BlockPos position, ItemStack byItemStack = null)
    {
        throw new NotImplementedException();
    }

    public void SpawnBlockEntity(BlockEntity be)
    {
        throw new NotImplementedException();
    }

    public void TriggerNeighbourBlockUpdate(BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public void WalkBlocks(BlockPos minPos, BlockPos maxPos, Action<Block, int, int, int> onBlock, bool centerOrder = false)
    {
        throw new NotImplementedException();
    }

    public void WalkStructures(BlockPos pos, Action<GeneratedStructure> onStructure)
    {
        throw new NotImplementedException();
    }

    public void WalkStructures(BlockPos minpos, BlockPos maxpos, Action<GeneratedStructure> onStructure)
    {
        throw new NotImplementedException();
    }
}