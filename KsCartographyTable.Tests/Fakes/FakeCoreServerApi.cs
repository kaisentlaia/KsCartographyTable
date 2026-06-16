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

    public IAssetManager Assets => Substitute.For<IAssetManager>();

    public IModLoader ModLoader => Substitute.For<IModLoader>();

    public ITagRegistry<TagSet> CollectibleTagRegistry => throw new NotImplementedException();

    public ITagRegistry<TagSetFast> EntityTagRegistry => throw new NotImplementedException();

    public Dictionary<string, object> ObjectCache => throw new NotImplementedException();

    public string DataBasePath => Substitute.For<string>();

    IEventAPI ICoreAPI.Event => Event;

    IWorldAccessor ICoreAPI.World => World;

    INetworkAPI ICoreAPI.Network => Network;

    public FakeCoreServerApi(string savegameIdentifier)
    {
        world = new FakeServerWorldAccessor(savegameIdentifier);
    }

    public void BroadcastMessageToAllGroups(string message, EnumChatType chatType, string data = null)
    {
        throw new NotImplementedException();
    }

    public string GetOrCreateDataPath(string foldername)
    {
        throw new NotImplementedException();
    }

    public void HandleCommand(IServerPlayer player, string message)
    {
        throw new NotImplementedException();
    }

    public void InjectConsole(string message)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public void RegisterBlockBehaviorClass(string className, Type blockBehaviorType)
    {
        throw new NotImplementedException();
    }

    public void RegisterBlockClass(string className, Type blockType)
    {
        throw new NotImplementedException();
    }

    public void RegisterBlockEntityBehaviorClass(string className, Type blockEntityBehaviorType)
    {
        throw new NotImplementedException();
    }

    public void RegisterBlockEntityClass(string className, Type blockentityType)
    {
        throw new NotImplementedException();
    }

    public void RegisterCollectibleBehaviorClass(string className, Type blockBehaviorType)
    {
        throw new NotImplementedException();
    }

    public void RegisterColorMap(ColorMap map)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public void RegisterCropBehavior(string className, Type type)
    {
        throw new NotImplementedException();
    }

    public void RegisterEntity(string className, Type entity)
    {
        throw new NotImplementedException();
    }

    public void RegisterEntityBehaviorClass(string className, Type entityBehavior)
    {
        throw new NotImplementedException();
    }

    public void RegisterEntityClass(string entityClassName, EntityProperties config)
    {
        throw new NotImplementedException();
    }

    public void RegisterItem(Item item)
    {
        throw new NotImplementedException();
    }

    public void RegisterItemClass(string className, Type itemType)
    {
        throw new NotImplementedException();
    }

    public void RegisterMountable(string className, GetMountableDelegate mountableInstancer)
    {
        throw new NotImplementedException();
    }

    public T RegisterRecipeRegistry<T>(string recipeRegistryCode) where T : RecipeRegistryBase
    {
        throw new NotImplementedException();
    }

    public void RegisterTreeGenerator(AssetLocation generatorCode, ITreeGenerator gen)
    {
        throw new NotImplementedException();
    }

    public void RegisterTreeGenerator(AssetLocation generatorCode, GrowTreeDelegate genhandler)
    {
        throw new NotImplementedException();
    }

    public void SendIngameDiscovery(IServerPlayer player, string discoveryCode, string text = null, params object[] langparams)
    {
        throw new NotImplementedException();
    }

    public void SendIngameError(IServerPlayer player, string errorCode, string text = null, params object[] langparams)
    {
        throw new NotImplementedException();
    }

    public void SendMessage(IPlayer player, int groupId, string message, EnumChatType chatType, string data = null)
    {
        throw new NotImplementedException();
    }

    public void SendMessageToGroup(int groupid, string message, EnumChatType chatType, string data = null)
    {
        throw new NotImplementedException();
    }

    public void StoreModConfig<T>(T jsonSerializeableData, string filename)
    {
        throw new NotImplementedException();
    }

    public void StoreModConfig(JsonObject jobj, string filename)
    {
        throw new NotImplementedException();
    }

    public void TriggerOnAssetsFirstLoaded()
    {
        throw new NotImplementedException();
    }
}