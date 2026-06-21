using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NSubstitute;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace KsCartographyTable.test.Unit;

public class FakeServerWorldAccessor(string fakeSavegameIdentifier, IBlockAccessor fakeBlockAccessor) : IServerWorldAccessor
{
    public ConcurrentDictionary<long, Entity> LoadedEntities => throw new NotImplementedException();

    public Vintagestory.API.Datastructures.OrderedDictionary<AssetLocation, ITreeGenerator> TreeGenerators => throw new NotImplementedException();

    public Dictionary<string, string> RemappedEntities => throw new NotImplementedException();

    public string WorldName => throw new NotImplementedException();

    public ITreeAttribute Config => Substitute.For<ITreeAttribute>();

    public EntityPos DefaultSpawnPosition => Substitute.For<EntityPos>();

    public FrameProfilerUtil FrameProfiler => Substitute.For<FrameProfilerUtil>();

    public ICoreAPI Api => Substitute.For<ICoreAPI>();

    public IChunkProvider ChunkProvider => Substitute.For<IChunkProvider>();

    public ILandClaimAPI Claims => Substitute.For<ILandClaimAPI>();

    public long[] LoadedChunkIndices => Substitute.For<long[]>();

    public long[] LoadedMapChunkIndices => Substitute.For<long[]>();

    public float[] BlockLightLevels => Substitute.For<float[]>();

    public float[] SunLightLevels => Substitute.For<float[]>();

    public int SeaLevel => throw new NotImplementedException();

    public int Seed => throw new NotImplementedException();

    private readonly string savegameIdentifier = fakeSavegameIdentifier;
    public string SavegameIdentifier { get { return savegameIdentifier; } }

    public int SunBrightness => throw new NotImplementedException();

    public bool EntityDebugMode => throw new NotImplementedException();

    public IAssetManager AssetManager => Substitute.For<IAssetManager>();

    public ILogger Logger => Substitute.For<ILogger>();

    public EnumAppSide Side => throw new NotImplementedException();

    private readonly IBlockAccessor blockAccessor = fakeBlockAccessor;
    public IBlockAccessor BlockAccessor { get { return blockAccessor; } }

    public IBulkBlockAccessor BulkBlockAccessor => Substitute.For<IBulkBlockAccessor>();

    public IClassRegistryAPI ClassRegistry => Substitute.For<IClassRegistryAPI>();

    public IGameCalendar Calendar => Substitute.For<IGameCalendar>();

    public CollisionTester CollisionTester => Substitute.For<CollisionTester>();

    public Random Rand => Substitute.For<Random>();

    public long ElapsedMilliseconds => throw new NotImplementedException();

    public List<CollectibleObject> Collectibles => Substitute.For<List<CollectibleObject>>();

    public IList<Block> Blocks => Substitute.For<IList<Block>>();

    public IList<Item> Items => Substitute.For<IList<Item>>();

    public List<EntityProperties> EntityTypes => Substitute.For<List<EntityProperties>>();

    public List<string> EntityTypeCodes => Substitute.For<List<string>>();

    public List<GridRecipe> GridRecipes => Substitute.For<List<GridRecipe>>();

    public int DefaultEntityTrackingRange => throw new NotImplementedException();

    public IPlayer[] AllOnlinePlayers => Substitute.For<IPlayer[]>();

    public IPlayer[] AllPlayers => Substitute.For<IPlayer[]>();

    public AABBIntersectionTest InteresectionTester => Substitute.For<AABBIntersectionTest>();

    public System.Collections.Generic.OrderedDictionary<IRecipeIngredientBase, List<IRecipeBase>> FastSearchRecipesByIngredient => throw new NotImplementedException();

    public void CreateExplosion(BlockPos pos, EnumBlastType blastType, double destructionRadius, double injureRadius, float blockDropChanceMultiplier = 1, string ignitedByPlayerUid = null)
    {
        throw new NotImplementedException();
    }

    public void DespawnEntity(Entity entity, EntityDespawnData reason)
    {
        throw new NotImplementedException();
    }

    public Block GetBlock(int blockId)
    {
        throw new NotImplementedException();
    }

    public Block GetBlock(AssetLocation blockCode)
    {
        throw new NotImplementedException();
    }

    public IBlockAccessor GetBlockAccessor(bool synchronize, bool relight, bool strict, bool debug = false)
    {
        throw new NotImplementedException();
    }

    public IBulkBlockAccessor GetBlockAccessorBulkMinimalUpdate(bool synchronize, bool debug = false)
    {
        throw new NotImplementedException();
    }

    public IBulkBlockAccessor GetBlockAccessorBulkUpdate(bool synchronize, bool relight, bool debug = false)
    {
        throw new NotImplementedException();
    }

    public IBulkBlockAccessor GetBlockAccessorMapChunkLoading(bool synchronize, bool debug = false)
    {
        throw new NotImplementedException();
    }

    public IBlockAccessorPrefetch GetBlockAccessorPrefetch(bool synchronize, bool relight)
    {
        throw new NotImplementedException();
    }

    public IBlockAccessorRevertable GetBlockAccessorRevertable(bool synchronize, bool relight, bool debug = false)
    {
        throw new NotImplementedException();
    }

    public ICachingBlockAccessor GetCachingBlockAccessor(bool synchronize, bool relight)
    {
        throw new NotImplementedException();
    }

    public Entity[] GetEntitiesAround(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null)
    {
        throw new NotImplementedException();
    }

    public Entity[] GetEntitiesInsideCuboid(BlockPos startPos, BlockPos endPos, ActionConsumable<Entity> matches = null)
    {
        throw new NotImplementedException();
    }

    public Entity GetEntityById(long entityId)
    {
        throw new NotImplementedException();
    }

    public EntityProperties GetEntityType(AssetLocation entityCode)
    {
        throw new NotImplementedException();
    }

    public Entity[] GetIntersectingEntities(BlockPos basePos, Cuboidf[] collisionBoxes, ActionConsumable<Entity> matches = null)
    {
        throw new NotImplementedException();
    }

    public Item GetItem(int itemId)
    {
        throw new NotImplementedException();
    }

    public Item GetItem(AssetLocation itemCode)
    {
        throw new NotImplementedException();
    }

    public IBlockAccessor GetLockFreeBlockAccessor()
    {
        throw new NotImplementedException();
    }

    public Entity GetNearestEntity(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null)
    {
        throw new NotImplementedException();
    }

    public IPlayer[] GetPlayersAround(Vec3d position, float horRange, float vertRange, ActionConsumable<IPlayer> matches = null)
    {
        throw new NotImplementedException();
    }

    public RecipeRegistryBase GetRecipeRegistry(string code)
    {
        throw new NotImplementedException();
    }

    public void HighlightBlocks(IPlayer player, int highlightSlotId, List<BlockPos> blocks, List<int> colors, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary, float scale = 1)
    {
        throw new NotImplementedException();
    }

    public void HighlightBlocks(IPlayer player, int highlightSlotId, List<BlockPos> blocks, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary)
    {
        throw new NotImplementedException();
    }

    public bool IsFullyLoadedChunk(BlockPos pos)
    {
        throw new NotImplementedException();
    }

    public bool LoadEntity(Entity entity, long fromChunkIndex3d)
    {
        throw new NotImplementedException();
    }

    public IPlayer NearestPlayer(double x, double y, double z)
    {
        throw new NotImplementedException();
    }

    public IPlayer PlayerByUid(string playerUid)
    {
        throw new NotImplementedException();
    }

    public bool PlayerHasPrivilege(int clientid, string privilege)
    {
        throw new NotImplementedException();
    }

    public int PlaySoundAt(SoundAttributes sound, double x, double y, double z, int dimension, IPlayer dualCallByPlayer = null, float volumeMultiplier = 1)
    {
        throw new NotImplementedException();
    }

    public void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32, float volume = 1)
    {
        throw new NotImplementedException();
    }

    public int PlaySoundAt(SoundAttributes sound, BlockPos pos, double yOffsetFromCenter, IPlayer dualCallByPlayer = null, float volumeMultiplier = 1)
    {
        throw new NotImplementedException();
    }

    public void PlaySoundAt(AssetLocation location, BlockPos pos, double yOffsetFromCenter, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32, float volume = 1)
    {
        throw new NotImplementedException();
    }

    public int PlaySoundAt(SoundAttributes sound, Entity atEntity, IPlayer dualCallByPlayer = null, float volumeMultiplier = 1)
    {
        throw new NotImplementedException();
    }

    public void PlaySoundAt(AssetLocation location, Entity atEntity, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32, float volume = 1)
    {
        throw new NotImplementedException();
    }

    public void PlaySoundAt(AssetLocation location, Entity atEntity, IPlayer dualCallByPlayer, float pitch, float range = 32, float volume = 1)
    {
        throw new NotImplementedException();
    }

    public void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer, float pitch, float range = 32, float volume = 1)
    {
        throw new NotImplementedException();
    }

    public void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer, EnumSoundType soundType, float pitch, float range = 32, float volume = 1)
    {
        throw new NotImplementedException();
    }

    public int PlaySoundAt(SoundAttributes sound, IPlayer atPlayer, IPlayer dualCallByPlayer = null, float volumeMultiplier = 1)
    {
        throw new NotImplementedException();
    }

    public void PlaySoundAt(AssetLocation location, IPlayer atPlayer, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32, float volume = 1)
    {
        throw new NotImplementedException();
    }

    public int PlaySoundFor(SoundAttributes sound, IPlayer forPlayer, float volumeMultiplier = 1)
    {
        throw new NotImplementedException();
    }

    public void PlaySoundFor(AssetLocation location, IPlayer forPlayer, bool randomizePitch = true, float range = 32, float volume = 1)
    {
        throw new NotImplementedException();
    }

    public void PlaySoundFor(AssetLocation location, IPlayer forPlayer, float pitch, float range = 32, float volume = 1)
    {
        throw new NotImplementedException();
    }

    public void RayTraceForSelection(Vec3d fromPos, Vec3d toPos, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
    {
        throw new NotImplementedException();
    }

    public void RayTraceForSelection(IWorldIntersectionSupplier supplier, Vec3d fromPos, Vec3d toPos, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
    {
        throw new NotImplementedException();
    }

    public void RayTraceForSelection(Vec3d fromPos, float pitch, float yaw, float range, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
    {
        throw new NotImplementedException();
    }

    public void RayTraceForSelection(Ray ray, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter filter = null, EntityFilter efilter = null)
    {
        throw new NotImplementedException();
    }

    public long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay)
    {
        throw new NotImplementedException();
    }

    public long RegisterCallback(Action<IWorldAccessor, BlockPos, float> OnTimePassed, BlockPos pos, int millisecondDelay)
    {
        throw new NotImplementedException();
    }

    public long RegisterCallbackUnique(Action<IWorldAccessor, BlockPos, float> OnGameTick, BlockPos pos, int millisecondInterval)
    {
        throw new NotImplementedException();
    }

    public long RegisterGameTickListener(Action<float> onGameTick, int millisecondInterval, int initialDelayOffsetMs = 0)
    {
        throw new NotImplementedException();
    }

    public Block[] SearchBlocks(AssetLocation wildcard)
    {
        throw new NotImplementedException();
    }

    public Item[] SearchItems(AssetLocation wildcard)
    {
        throw new NotImplementedException();
    }

    public void SpawnCubeParticles(BlockPos blockPos, Vec3d pos, float radius, int quantity, float scale = 1, IPlayer dualCallByPlayer = null, Vec3f velocity = null)
    {
        throw new NotImplementedException();
    }

    public void SpawnCubeParticles(Vec3d pos, ItemStack item, float radius, int quantity, float scale = 1, IPlayer dualCallByPlayer = null, Vec3f velocity = null)
    {
        throw new NotImplementedException();
    }

    public void SpawnEntity(Entity entity)
    {
        throw new NotImplementedException();
    }

    public Entity SpawnItemEntity(ItemStack itemstack, Vec3d position, Vec3d velocity = null)
    {
        throw new NotImplementedException();
    }

    public Entity SpawnItemEntity(ItemStack itemstack, BlockPos pos, Vec3d velocity = null)
    {
        throw new NotImplementedException();
    }

    public void SpawnParticles(float quantity, int color, Vec3d minPos, Vec3d maxPos, Vec3f minVelocity, Vec3f maxVelocity, float lifeLength, float gravityEffect, float scale = 1, EnumParticleModel model = EnumParticleModel.Quad, IPlayer dualCallByPlayer = null)
    {
        throw new NotImplementedException();
    }

    public void SpawnParticles(IParticlePropertiesProvider particlePropertiesProvider, IPlayer dualCallByPlayer = null)
    {
        throw new NotImplementedException();
    }

    public void SpawnPriorityEntity(Entity entity)
    {
        throw new NotImplementedException();
    }

    public void UnregisterCallback(long listenerId)
    {
        throw new NotImplementedException();
    }

    public void UnregisterGameTickListener(long listenerId)
    {
        throw new NotImplementedException();
    }

    public void UpdateEntityChunk(Entity entity, long newChunkIndex3d)
    {
        throw new NotImplementedException();
    }
}