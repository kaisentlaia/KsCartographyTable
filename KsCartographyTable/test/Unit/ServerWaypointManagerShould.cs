using System;
using Kaisentlaia.KsCartographyTableMod.GameContent;
using System.Collections.Generic;
using Vintagestory.GameContent;
using Vintagestory.API.Common;
using System.IO;
using System.Linq;

namespace KsCartographyTable.test.Unit;

public class ServeWaypointManagerShould
{
    private ServerWaypointManager serverWaypointManager;
    private FakeCoreServerApi fakeCoreServerApi;
    private FakePlayer fakePlayer1;
    private FakePlayer fakePlayer2;
    private Waypoint fakeWaypoint1;
    private Waypoint fakeWaypoint2;
    
    [SetUp]
    public void Setup()
    {
        fakePlayer1 = new FakePlayer(Guid.NewGuid().ToString());
        fakePlayer2 = new FakePlayer(@"invalid+characters/in\guid");
        fakeCoreServerApi = new FakeCoreServerApi();
        serverWaypointManager = new ServerWaypointManager(fakeCoreServerApi);
        fakeWaypoint1 = new()
        {
            Color = 1,
            Position = new Vintagestory.API.MathTools.Vec3d(),
            Guid = Guid.NewGuid().ToString(),
            Icon = "star",
            OwningPlayerUid = fakePlayer1.PlayerUID,
            Title = "test waypoint"
        };
        fakeWaypoint2 = new()
        {
            Color = 1,
            Position = new Vintagestory.API.MathTools.Vec3d(),
            Guid = Guid.NewGuid().ToString(),
            Icon = "star",
            OwningPlayerUid = fakePlayer2.PlayerUID,
            Title = "test waypoint"
        };
    }

    [Test]
    public void ReturnEmptyListIfFileDoesntExist()
    {
        List<string> ids = serverWaypointManager.GetDeletedWaypointsIds(fakePlayer1);
        Assert.That(ids, Is.Empty);

        List<string> ids2 = serverWaypointManager.GetDeletedWaypointsIds(fakePlayer2);
        Assert.That(ids2, Is.Empty);
    }

    [Test]
    public void SaveDeletedWaypointIdsOnFile()
    {
        
        serverWaypointManager.AddDeletedWaypointId(fakeWaypoint1, fakePlayer1);
        List<string> ids = serverWaypointManager.GetDeletedWaypointsIds(fakePlayer1);
        Assert.That(ids, Is.Not.Empty);
        Assert.That(ids, Does.Contain(fakeWaypoint1.Guid));
        Assert.That(ids, Has.Count.EqualTo(1));
    }

    [Test]
    public void ReadDeletedWaypointIdsFromFileAfterSaving()
    {
        serverWaypointManager.AddDeletedWaypointId(fakeWaypoint1, fakePlayer1);

        List<string> ids = serverWaypointManager.GetDeletedWaypointsIds(fakePlayer1);
        Assert.That(ids, Is.Not.Empty);
        Assert.That(ids, Does.Contain(fakeWaypoint1.Guid));
        Assert.That(ids, Has.Count.EqualTo(1));
    }

    [Test]
    public void HandleInvalidCharactersInPlayerUID()
    {
        serverWaypointManager.AddDeletedWaypointId(fakeWaypoint2, fakePlayer2);

        List<string> ids = serverWaypointManager.GetDeletedWaypointsIds(fakePlayer2);
        Assert.That(ids, Is.Not.Empty);
        Assert.That(ids, Does.Contain(fakeWaypoint2.Guid));
        Assert.That(ids, Has.Count.EqualTo(1));
    }

    [Test]
    public void ReadWaypointIdsFromUnescapedFilenameIfPresent()
    {
        List<string> testIds = [Guid.NewGuid().ToString(), Guid.NewGuid().ToString()];
        string filePath = Path.Combine(serverWaypointManager.modDataPath, fakePlayer1.PlayerUID + ".json");
        try
        {
            string json = JsonUtil.ToString(testIds.ToList());
            File.WriteAllText(filePath, json);
        }
        catch
        {
            Assert.Fail();
        }

        List<string> ids = serverWaypointManager.GetDeletedWaypointsIds(fakePlayer1);

        Assert.That(ids, Is.Not.Empty);
        Assert.That(ids, Does.Contain(testIds[0]));
        Assert.That(ids, Does.Contain(testIds[1]));
        Assert.That(ids, Has.Count.EqualTo(2));        
    }

    [Test]
    public void AddWaypointIdsToUnescapedFilenameIfPresent()
    {
        List<string> testIds = [Guid.NewGuid().ToString(), Guid.NewGuid().ToString()];
        string filePath = Path.Combine(serverWaypointManager.modDataPath, fakePlayer1.PlayerUID + ".json");
        try
        {
            string json = JsonUtil.ToString(testIds.ToList());
            File.WriteAllText(filePath, json);
        }
        catch
        {
            Assert.Fail();
        }

        serverWaypointManager.AddDeletedWaypointId(fakeWaypoint1, fakePlayer1);
        List<string> ids = serverWaypointManager.GetDeletedWaypointsIds(fakePlayer1);

        Assert.That(ids, Is.Not.Empty);
        Assert.That(ids, Does.Contain(testIds[0]));
        Assert.That(ids, Does.Contain(testIds[1]));
        Assert.That(ids, Does.Contain(fakeWaypoint1.Guid));
        Assert.That(ids, Has.Count.EqualTo(3));        
    }

    [TearDown]
    public void CleanUp()
    {
        Directory.Delete(serverWaypointManager.modDataPath, true);
    }
}