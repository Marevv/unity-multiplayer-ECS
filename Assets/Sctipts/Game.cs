using System;
using Unity.Entities;
using Unity.NetCode;

public class GameBootStrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        AutoConnectPort = 7979;
        return base.Initialize(defaultWorldName);
    }
}
