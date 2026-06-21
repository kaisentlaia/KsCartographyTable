
using System.Collections.Generic;
using NSubstitute;
using Vintagestory.API.Common;

namespace KsCartographyTable.test.Unit;

public class FakeAssetManager : IAssetManager
{
    public Dictionary<AssetLocation, IAsset> AllAssets { get { return []; } }

    public List<IAssetOrigin> Origins { get { return []; } }
    public void Add(AssetLocation path, IAsset asset)
    {
        return;
    }

    public void AddModOrigin(string domain, string fullPath)
    {
        return;
    }

    public void AddModOrigin(string domain, string fullPath, string pathForReservedCharsCheck)
    {
        return;
    }

    public void AddPathOrigin(string domain, string fullPath)
    {
        return;
    }

    public bool Exists(AssetLocation location)
    {
        return false;
    }

    public IAsset Get(AssetLocation Location)
    {
       return Substitute.For<IAsset>();
    }

    public T Get<T>(AssetLocation location)
    {
        throw new System.NotImplementedException();
    }

    public List<AssetLocation> GetLocations(string pathBegins, string domain = null)
    {
        return [];
    }

    public List<IAsset> GetMany(string pathBegins, string domain = null, bool loadAsset = true)
    {
        return [];
    }

    public Dictionary<AssetLocation, T> GetMany<T>(ILogger logger, string pathBegins, string domain = null)
    {
        return [];
    }

    public List<IAsset> GetManyInCategory(string categoryCode, string pathBegins, string domain = null, bool loadAsset = true)
    {
        return [];
    }

    public int Reload(AssetLocation baseLocation)
    {
        return 1;
    }

    public int Reload(AssetCategory category)
    {
        return 1;
    }

    public void RemoveModOrigin(string domain, string fullpath)
    {
        return;
    }

    public IAsset TryGet(AssetLocation Location, bool loadAsset = true)
    {
       return Substitute.For<IAsset>();
    }
}