using System;
using System.Net;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class ConnectionBootStrap : ClientServerBootstrap
{
    
    public override bool Initialize(string defaultWorldName)
    {
        AutoConnectPort = 0;
        CreateLocalWorld(defaultWorldName);
        return true;
    }
}
