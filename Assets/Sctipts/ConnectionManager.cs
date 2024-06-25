using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Multiplay;
using UnityEngine.SceneManagement;

public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance { get; private set; }

    [SerializeField] private ushort _port = 7979;
    [SerializeField] private bool useMultiplay = false;

    private bool _connecting = false;

    private NetworkEndpoint _serverAddress;

    public bool isClient = false;
    public bool isServer = false;
    
    public const int MAX_PLAYER_AMOUNT = 4;

    public static World ServerWorld
    {
        get
        {
            foreach (var world in World.All)
            {
                if (world.Flags == WorldFlags.GameServer)
                {
                    return world;
                }
            }

            return null;
        }
    }

    public static World ClientWorld
    {
        get
        {
            foreach (var world in World.All)
            {
                if (world.Flags == WorldFlags.GameClient)
                {
                    return world;
                }
            }

            return null;
        }
    }

    public bool UseRelay { get; set; }


    private void Awake()
    {
        Instance = this;
        
    }

    private void Start()
    {
        if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Server)
        {
            Camera.main.enabled = false;
            Debug.Log("Initializing Server");
            InitializeServer();
        }
    }


    public void Connect()
    {
        Connect(UiManager.Instance.GetNetworkEndpoint().Address);
    }

    public void Connect(string address)
    {
        if (_connecting) return;
        _connecting = true;

        Debug.Log("Connecting");
        Debug.Log($"Before server address: {address}");
        _serverAddress = NetworkEndpoint.Parse(address.Split(':')[0], ushort.Parse(address.Split(':')[1]));
        Debug.Log($"Server address: {_serverAddress}");
        StartCoroutine(ConnectToDedicatedServer());
        UiManager.Instance.InGameUI();
    }


    public void StartRelay()
    {
        var relayClientData = RelayManager.Instance.RelayClientData;
        var relayServerData = RelayManager.Instance.RelayServerData;

        var oldConstructor = NetworkStreamReceiveSystem.DriverConstructor;

        NetworkStreamReceiveSystem.DriverConstructor = new RelayDriverConstructor(relayServerData, relayClientData);
        var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        NetworkStreamReceiveSystem.DriverConstructor = oldConstructor;

        if (World.DefaultGameObjectInjectionWorld == null)
            World.DefaultGameObjectInjectionWorld = server;

        DisposeDefaultWorld();


        SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Additive);

        var networkStreamEntity =
            server.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestListen>());
        server.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestListen");
        server.EntityManager.SetComponentData(networkStreamEntity,
            new NetworkStreamRequestListen { Endpoint = NetworkEndpoint.AnyIpv4 });

        networkStreamEntity =
            client.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
        client.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestConnect");

        client.EntityManager.SetComponentData(networkStreamEntity,
            new NetworkStreamRequestConnect { Endpoint = relayClientData.Endpoint });
    }


    public void JoinRelay()
    {
        var relayClientData = RelayManager.Instance.RelayClientData;

        var oldConstructor = NetworkStreamReceiveSystem.DriverConstructor;
        NetworkStreamReceiveSystem.DriverConstructor =
            new RelayDriverConstructor(new RelayServerData(), relayClientData);
        var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        NetworkStreamReceiveSystem.DriverConstructor = oldConstructor;

        if (World.DefaultGameObjectInjectionWorld == null)
            World.DefaultGameObjectInjectionWorld = client;

        DisposeDefaultWorld();

        SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Additive);

        var networkStreamEntity =
            client.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
        client.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestConnect");

        client.EntityManager.SetComponentData(networkStreamEntity,
            new NetworkStreamRequestConnect { Endpoint = relayClientData.Endpoint });
    }

    public async void JoinRelayWithCode()
    {
        if (UiManager.Instance.Address == string.Empty) return;

        await RelayManager.Instance.SetupClient(UiManager.Instance.Address);
        JoinRelay();
        UiManager.Instance.InGameUI();
    }

    public void DisposeDefaultWorld()
    {
        foreach (var world in World.All)
        {
            if (world.Flags == WorldFlags.Game)
            {
                //OldFrontendWorldName = world.Name;
                world.Dispose();
                break;
            }
        }
    }


    private async void InitializeServer()
    {
        var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        if (World.DefaultGameObjectInjectionWorld == null)
            World.DefaultGameObjectInjectionWorld = server;

        DisposeDefaultWorld();
        
        while (!ClientServerBootstrap.HasServerWorld)
        {
            return;
        }

        if (useMultiplay)
        {
            Debug.Log("Using Multiplay");
            await MultiplayManager.Instance.InitializeMultiplay();
        }
        else
        {
            Debug.Log("Not using Multiplay");
            var args = Environment.GetCommandLineArgs();

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "-port")
                {
                    _port = ushort.Parse(args[i + 1]);
                    Debug.Log($"Got this port from args: {_port}");
                    break;
                }
            }


            _serverAddress = NetworkEndpoint.AnyIpv4.WithPort(_port);
            StartServer();
        }
    }

    public async void StartServer()
    {
        if (useMultiplay)
            _serverAddress = NetworkEndpoint.AnyIpv4.WithPort(MultiplayService.Instance.ServerConfig.Port);
        
        Debug.Log($"Starting Server on Address: {_serverAddress}. Using Multiplay: {useMultiplay}");
        using var query =
            ServerWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
        query.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(_serverAddress);
        
        isServer = true;
        
        await SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Additive);
        
        _connecting = false;
    }

    private IEnumerator ConnectToDedicatedServer()
    {
        var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");

        if (World.DefaultGameObjectInjectionWorld == null)
            World.DefaultGameObjectInjectionWorld = client;

        DisposeDefaultWorld();

        SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Additive);

        while (!ClientServerBootstrap.HasClientWorlds)
        {
            yield return null;
        }

        Debug.Log($"Connecting to: {_serverAddress}");
        using var query =
            ClientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
        query.GetSingletonRW<NetworkStreamDriver>().ValueRW
            .Connect(ClientWorld.EntityManager, _serverAddress);
        isClient = true;

        _connecting = false;
        
        
    }

    public bool HasAvailablePlayerSlots()
    {
        if (isServer)
        {
            return ServerWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkStreamConnection>())
                .CalculateEntityCount() < MAX_PLAYER_AMOUNT;
        }
        else if (isClient)
        {
            return ClientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkStreamConnection>())
                .CalculateEntityCount() < MAX_PLAYER_AMOUNT;
        }

        return false;
    }

}