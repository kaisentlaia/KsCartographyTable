using System;
using System.Collections.Generic;
using NSubstitute;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace KsCartographyTable.test.Unit;

public class FakePlayer : IServerPlayer
{
    public IPlayerRole Role { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public PlayerGroupMembership[] Groups => Substitute.For<PlayerGroupMembership[]>();

    public List<Entitlement> Entitlements => Substitute.For<List<Entitlement>>();

    public BlockSelection CurrentBlockSelection => Substitute.For<BlockSelection>();

    public EntitySelection CurrentEntitySelection => Substitute.For<EntitySelection>();

    public string PlayerName => Substitute.For<string>();

    private string playerUID;

    public event OnEntityAction InWorldAction;

    public string PlayerUID { get { return playerUID; } }

    public int ClientId { get { return 12345; } }

    public EntityPlayer Entity => Substitute.For<EntityPlayer>();

    public IWorldPlayerData WorldData => Substitute.For<IWorldPlayerData>();

    public IPlayerInventoryManager InventoryManager => Substitute.For<IPlayerInventoryManager>();

    public string[] Privileges => Substitute.For<string[]>();

    public bool ImmersiveFpMode { get { return false; } }

    public int ItemCollectMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public int CurrentChunkSentRadius { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public EnumClientState ConnectionState => throw new NotImplementedException();

    public string IpAddress => throw new NotImplementedException();

    public string LanguageCode => throw new NotImplementedException();

    public float Ping => throw new NotImplementedException();

    public IServerPlayerData ServerData => throw new NotImplementedException();

    public FakePlayer(string guid)
    {
        playerUID = guid;
    }

    public PlayerGroupMembership GetGroup(int groupId)
    {
        throw new NotImplementedException();
    }

    public PlayerGroupMembership[] GetGroups()
    {
        throw new NotImplementedException();
    }

    public bool HasPrivilege(string privilegeCode)
    {
        throw new NotImplementedException();
    }

    public void BroadcastPlayerData(bool sendInventory = false)
    {
        throw new NotImplementedException();
    }

    public void Disconnect()
    {
        throw new NotImplementedException();
    }

    public void Disconnect(string message)
    {
        throw new NotImplementedException();
    }

    public void SendIngameError(string code, string message = null, params object[] langparams)
    {
        throw new NotImplementedException();
    }

    public void SendMessage(int groupId, string message, EnumChatType chatType, string data = null)
    {
        throw new NotImplementedException();
    }

    public void SendLocalisedMessage(int groupId, string message, params object[] args)
    {
        throw new NotImplementedException();
    }

    public void SetRole(string roleCode)
    {
        throw new NotImplementedException();
    }

    public void SetSpawnPosition(PlayerSpawnPos pos)
    {
        throw new NotImplementedException();
    }

    public void ClearSpawnPosition()
    {
        throw new NotImplementedException();
    }

    public FuzzyEntityPos GetSpawnPosition(bool consumeSpawnUse)
    {
        throw new NotImplementedException();
    }

    public void SetModData<T>(string key, T data)
    {
        throw new NotImplementedException();
    }

    public T GetModData<T>(string key, T defaultValue = default)
    {
        throw new NotImplementedException();
    }

    public void SetModdata(string key, byte[] data)
    {
        throw new NotImplementedException();
    }

    public void RemoveModdata(string key)
    {
        throw new NotImplementedException();
    }

    public byte[] GetModdata(string key)
    {
        throw new NotImplementedException();
    }
}