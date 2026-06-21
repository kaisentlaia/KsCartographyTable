using System;
using System.Collections.Generic;
using NSubstitute;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace KsCartographyTable.test.Unit;

public class FakeCoreServerApi : ICoreServerAPI
{
    public IServerEventAPI Event => Substitute.For<IServerEventAPI>();

    public IWorldManagerAPI WorldManager => Substitute.For<IWorldManagerAPI>();

    public IServerAPI Server => Substitute.For<IServerAPI>();

    public IPermissionManager Permissions => Substitute.For<IPermissionManager>();

    public IGroupManager Groups => Substitute.For<IGroupManager>();

    public IPlayerDataManager PlayerData => Substitute.For<IPlayerDataManager>();

    public IServerNetworkAPI Network => Substitute.For<IServerNetworkAPI>();
    private readonly IServerWorldAccessor world;

    public IServerWorldAccessor World { get { return world; } }

    public ILogger Logger => Substitute.For<ILogger>();
    public string[] CmdlArguments => throw new NotImplementedException();

    public IChatCommandApi ChatCommands => Substitute.For<IChatCommandApi>();

    public EnumAppSide Side { get { return EnumAppSide.Server; } }

    public IClassRegistryAPI ClassRegistry => Substitute.For<IClassRegistryAPI>();

    public IAssetManager Assets { get { return new FakeAssetManager(); } }

    public IModLoader ModLoader => Substitute.For<IModLoader>();

    public ITagRegistry<TagSet> CollectibleTagRegistry => throw new NotImplementedException();

    public ITagRegistry<TagSetFast> EntityTagRegistry => throw new NotImplementedException();

    public Dictionary<string, object> ObjectCache => throw new NotImplementedException();

    public string DataBasePath => Substitute.For<string>();

    IEventAPI ICoreAPI.Event => Event;

    IWorldAccessor ICoreAPI.World => World;

    INetworkAPI ICoreAPI.Network => Network;

    public FakeCoreServerApi(string savegameIdentifier, IBlockAccessor blockAccessor = null)
    {
        if (blockAccessor == null)
        {
            blockAccessor = Substitute.For<IBlockAccessor>();
        }
        world = new FakeServerWorldAccessor(savegameIdentifier, blockAccessor);
    }

    public void BroadcastMessageToAllGroups(string message, EnumChatType chatType, string data = null)
    {
        return;
    }

    public string GetOrCreateDataPath(string foldername)
    {
        return @"C:\Users\Kaisentlaia\AppData\Roaming\VintagestoryData";
    }

    public void HandleCommand(IServerPlayer player, string message)
    {
        return;
    }

    public void InjectConsole(string message)
    {
        return;
    }

    public T LoadModConfig<T>(string filename)
    {
        throw new NotImplementedException();
    }

    public JsonObject LoadModConfig(string filename)
    {
        throw new NotImplementedException();
    }

    public void RegisterBlock(Block block)
    {
        return;
    }

    public void RegisterBlockBehaviorClass(string className, Type blockBehaviorType)
    {
        return;
    }

    public void RegisterBlockClass(string className, Type blockType)
    {
        return;
    }

    public void RegisterBlockEntityBehaviorClass(string className, Type blockEntityBehaviorType)
    {
        return;
    }

    public void RegisterBlockEntityClass(string className, Type blockentityType)
    {
        return;
    }

    public void RegisterCollectibleBehaviorClass(string className, Type blockBehaviorType)
    {
        return;
    }

    public void RegisterColorMap(ColorMap map)
    {
        return;
    }

    public bool RegisterCommand(ServerChatCommand chatcommand)
    {
        throw new NotImplementedException();
    }

    public bool RegisterCommand(string command, string descriptionMsg, string syntaxMsg, ServerChatCommandDelegate handler, string requiredPrivilege = null)
    {
        throw new NotImplementedException();
    }

    public void RegisterCraftingRecipe(GridRecipe recipe)
    {
        return;
    }

    public void RegisterCropBehavior(string className, Type type)
    {
        return;
    }

    public void RegisterEntity(string className, Type entity)
    {
        return;
    }

    public void RegisterEntityBehaviorClass(string className, Type entityBehavior)
    {
        return;
    }

    public void RegisterEntityClass(string entityClassName, EntityProperties config)
    {
        return;
    }

    public void RegisterItem(Item item)
    {
        return;
    }

    public void RegisterItemClass(string className, Type itemType)
    {
        return;
    }

    public void RegisterMountable(string className, GetMountableDelegate mountableInstancer)
    {
        return;
    }

    public T RegisterRecipeRegistry<T>(string recipeRegistryCode) where T : RecipeRegistryBase
    {
        throw new NotImplementedException();
    }

    public void RegisterTreeGenerator(AssetLocation generatorCode, ITreeGenerator gen)
    {
        return;
    }

    public void RegisterTreeGenerator(AssetLocation generatorCode, GrowTreeDelegate genhandler)
    {
        return;
    }

    public void SendIngameDiscovery(IServerPlayer player, string discoveryCode, string text = null, params object[] langparams)
    {
        return;
    }

    public void SendIngameError(IServerPlayer player, string errorCode, string text = null, params object[] langparams)
    {
        return;
    }

    public void SendMessage(IPlayer player, int groupId, string message, EnumChatType chatType, string data = null)
    {
        return;
    }

    public void SendMessageToGroup(int groupid, string message, EnumChatType chatType, string data = null)
    {
        return;
    }

    public void StoreModConfig<T>(T jsonSerializeableData, string filename)
    {
        return;
    }

    public void StoreModConfig(JsonObject jobj, string filename)
    {
        return;
    }

    public void TriggerOnAssetsFirstLoaded()
    {
        return;
    }
}