using System;
using System.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using System.Net;
using Unity.Burst;
using Unity.Collections;
using Unity.Networking.Transport;
using TMPro;
using Unity.Networking.Transport.Relay;
using Unity.Scenes;
using Unity.Services.Relay;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum UserType
{
    Server,
    Client,
    Host
}

public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance { get; set; }

    [SerializeField] private string _connectIP = "127.0.0.1";
    [SerializeField] private ushort _port = 7979;
    private string _listenIP = "0.0.0.0";

    public TMP_InputField ipInputField;
    public TMP_InputField portInputField;
    public Button connectButton;

    private bool _connecting = false;


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
    //
    // private void OnEnable()
    // {
    //     connectButton.onClick.AddListener(SetupConnection);
    // }
    //
    // private void OnDisable()
    // {
    //     connectButton.onClick.RemoveListener(SetupConnection);
    // }

    private void Start()
    {
        if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Server)
        {
            Debug.Log("Initializing Server");
            StartCoroutine(InitializeDedicatedServer());
        }
    }

    public void Connect()
    {
        if (_connecting) return;
        _connecting = true;
        
        Debug.Log("Connecting");
        StartCoroutine(ConnectToDedicatedServer());
        UiManager.Instance.InGameUI();
    }

    public void Connect(string ipAddress, ushort port)
    {
        Debug.Log($"Before IP: {ipAddress} and port: {port}");
        _connectIP = ipAddress;
        _port = port;
        Debug.Log($"After IP: {_connectIP} and port: {_port}");
        Connect();
    }

    public NetworkEndpoint GetNetworkEndpoint()
    {
        if (UiManager.Instance.Address == "" || UiManager.Instance.Port == "")
        {
            Debug.Log("IP or Port not provided");
            return NetworkEndpoint.AnyIpv4;
        }

        _connectIP = UiManager.Instance.Address;
        _port = ushort.Parse(UiManager.Instance.Port);

        return NetworkEndpoint.Parse(_connectIP, _port);
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


    private IEnumerator InitializeDedicatedServer()
    {
        var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        if (World.DefaultGameObjectInjectionWorld == null)
            World.DefaultGameObjectInjectionWorld = server;

        DisposeDefaultWorld();

        SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Additive);
        
        while (!ClientServerBootstrap.HasServerWorld)
        {
            yield return null;
        }

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

        Debug.Log($"Listenning on IP: {_listenIP} and port: {_port}");

        using var query =
            ServerWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
        query.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(NetworkEndpoint.Parse(_listenIP, _port));


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

        Debug.Log($"Connecting on IP: {_connectIP} and port: {_port}");
        using var query =
            ClientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
        query.GetSingletonRW<NetworkStreamDriver>().ValueRW
            .Connect(ClientWorld.EntityManager, NetworkEndpoint.Parse(_connectIP, _port));


        _connecting = false;
    }
}