using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace KsCartographyTable.test.Unit;

public class FakePlayer : IPlayer
{
    public IPlayerRole Role { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public PlayerGroupMembership[] Groups => throw new NotImplementedException();

    public List<Entitlement> Entitlements => throw new NotImplementedException();

    public BlockSelection CurrentBlockSelection => throw new NotImplementedException();

    public EntitySelection CurrentEntitySelection => throw new NotImplementedException();

    public string PlayerName => throw new NotImplementedException();

    private string playerUID;
    public string PlayerUID { get { return playerUID; } }

    public int ClientId => throw new NotImplementedException();

    public EntityPlayer Entity => throw new NotImplementedException();

    public IWorldPlayerData WorldData => throw new NotImplementedException();

    public IPlayerInventoryManager InventoryManager => throw new NotImplementedException();

    public string[] Privileges => throw new NotImplementedException();

    public bool ImmersiveFpMode => throw new NotImplementedException();

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
}