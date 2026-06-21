using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Kaisentlaia.KsCartographyTableMod.API.Server;
using Kaisentlaia.KsCartographyTableMod.GameContent;
using NSubstitute;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace KsCartographyTable.test.Unit;

[TestFixture("map upload")]
public class ServerCartographyServiceShould(string savegameIdentifier)
{
    private ServerCartographyService serverCartographyService;
    private FakeCoreServerApi fakeCoreServerApi;
    private FakeBlockAccessor fakeBlockAccessor;
    private FakePlayer fakePlayer;
    private BlockAdvancedCartographyTable fakeTable;
    private BlockEntityCartographyTable fakeBlockEntity;

    private List<MapSyncPacket> packets = [];

    private readonly List<ulong> chunkIds = [
        2137551552120,
        2137551552121,
        2137551552122,
        2137551552123,
        2137551552124,
        2137551552125,
        2137551552126,
        2137551552127,
        2137551552128,
        2137551552129
    ];

    [SetUp]
    public void Setup()
    {
        // magic: without this any attempt to use Lang will fail with a System.IO.DirectoryNotFoundException
        string vsPath = Environment.GetEnvironmentVariable("VINTAGE_STORY");
        PropertyInfo assetsPathProp = typeof(GamePaths).GetProperty("AssetsPath");
        assetsPathProp.GetSetMethod(true).Invoke(null, [Path.Combine(vsPath, "assets")]);

        fakePlayer = new FakePlayer(Guid.NewGuid().ToString());
        fakeTable = Substitute.For<BlockAdvancedCartographyTable>();
        fakeBlockEntity = Substitute.For<BlockEntityCartographyTable>();
        fakeBlockEntity.Pos = new BlockPos(0, 0, 128);
        fakeBlockEntity.Block = fakeTable;
        fakeBlockAccessor = new FakeBlockAccessor(fakeBlockEntity);
        fakeCoreServerApi = new FakeCoreServerApi(savegameIdentifier, fakeBlockAccessor);
        fakeBlockEntity.Api = fakeCoreServerApi;
        Dictionary<FastVec2i, MapPieceDB> fakeMapPieces = [];
        Random r = new();
        
        chunkIds
            .Select((chunkId, index) => new { chunkId, index })
            .ToList()
            .ForEach(chunk => {            
                fakeMapPieces.Add(
                    ServerMapDB.ChunkIdToFastVect2i(chunk.chunkId),
                    new MapPieceDB { Pixels = [r.Next(0, 100), r.Next(0, 100), r.Next(0, 100), r.Next(0, 100), r.Next(0, 100)] }
                );
                if (chunk.index % 3 == 0 || chunk.index > 8)
                {
                    packets.Add(new(fakeMapPieces, fakeTable, fakeBlockEntity.Pos, chunk.index > 8, null, false));
                    fakeMapPieces = [];
                }
            });

        serverCartographyService = new ServerCartographyService(fakeCoreServerApi);
        KsCartographyTableModSystem ksCartographyTableModSystem = new(true, true);
        ksCartographyTableModSystem.Start(fakeCoreServerApi);
        ksCartographyTableModSystem.StartServerSide(fakeCoreServerApi);
        Lang.Load(fakeCoreServerApi.Logger, fakeCoreServerApi.Assets);

        SQLitePCL.Batteries.Init();

    }

    [Test]
    public void WriteMapDataToDb()
    {
        packets.ForEach(packet =>
        {
            serverCartographyService.OnMapUploadRequest(fakePlayer, packet);
        });

        Assert.That(fakeBlockEntity.Map?.ExploredAreasIds, Is.Not.Null);
        Assert.That(fakeBlockEntity.Map?.ExploredAreasIds, Has.Count.EqualTo(chunkIds.Count));
        Assert.That(fakeBlockEntity.Map?.ExploredAreasIds, Is.EqualTo(chunkIds));
    }

    [TearDown]
    public void CleanUp()
    {
        serverCartographyService.Dispose();
        string path =  Path.Combine(
            GamePaths.DataPath,
            "ModData",
            fakeCoreServerApi.World.SavegameIdentifier
        );
        Directory.Delete(path, true);
    }
}