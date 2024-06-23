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

    private bool _isServer = false;
    private bool _isClient = false;
    private bool _isHost = false;


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


    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        connectButton.onClick.AddListener(SetupConnection);
    }

    private void OnDisable()
    {
        connectButton.onClick.RemoveListener(SetupConnection);
    }

    private void Start()
    {
        _isServer = ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Server;
        _isClient = ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Client;
        _isHost = ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.ClientAndServer;


        // if (_isServer)
        // {
        //     Debug.Log("Connecting Server");
        //     Connect();
        // }
    }

    public void UseRelay()
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

        if (_isHost)
        {
            Debug.Log("Relay Host");

            var relayClientData = RelayManager.Instance.RelayClientData;
            var relayServerData = RelayManager.Instance.RelayServerData;

            var oldConstructor = NetworkStreamReceiveSystem.DriverConstructor;
            
            Debug.Log("ServerData " + RelayManager.Instance.RelayServerData.Endpoint.Port);
            Debug.Log("ClientData " + RelayManager.Instance.RelayClientData.Endpoint.Port);

            NetworkStreamReceiveSystem.DriverConstructor = new RelayDriverConstructor(relayServerData, relayClientData);
            var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
            var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
            NetworkStreamReceiveSystem.DriverConstructor = oldConstructor;

            if (World.DefaultGameObjectInjectionWorld == null)
                World.DefaultGameObjectInjectionWorld = server;

            SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Additive);
            
            Debug.Log("Worlds Created");
            var networkStreamEntity =
                server.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestListen>());
            server.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestListen");
            server.EntityManager.SetComponentData(networkStreamEntity,
                new NetworkStreamRequestListen { Endpoint = NetworkEndpoint.AnyIpv4 });
            Debug.Log("Setup for Host-Server");

            networkStreamEntity =
                client.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
            client.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestConnect");
            
            client.EntityManager.SetComponentData(networkStreamEntity,
                new NetworkStreamRequestConnect { Endpoint = relayClientData.Endpoint });
            Debug.Log("Setup for Host-Client");
        }

        if (_isClient)
        {
            Debug.Log("Relay Client");
            var relayClientData = RelayManager.Instance.RelayClientData;

            var oldConstructor = NetworkStreamReceiveSystem.DriverConstructor;
            NetworkStreamReceiveSystem.DriverConstructor =
                new RelayDriverConstructor(new RelayServerData(), relayClientData);
            var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
            NetworkStreamReceiveSystem.DriverConstructor = oldConstructor;

            if (World.DefaultGameObjectInjectionWorld == null)
                World.DefaultGameObjectInjectionWorld = client;
            
            SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Additive);
            
            var networkStreamEntity =
                client.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
            client.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestConnect");
            
            client.EntityManager.SetComponentData(networkStreamEntity,
                new NetworkStreamRequestConnect { Endpoint = relayClientData.Endpoint });
        }
    }

    private void SetupConnection()
    {
        if (ipInputField.text == "" || portInputField.text == "")
        {
            Debug.Log("IP or Port not provided");
            return;
        }

        _connectIP = ipInputField.text;
        _port = ushort.Parse(portInputField.text);
        Connect();
    }

    public void Connect()
    {
        if (_connecting) return;
        _connecting = true;


        StartCoroutine(IniTializeConnection());
    }

    private IEnumerator IniTializeConnection()
    {
        while ((_isServer && !ClientServerBootstrap.HasServerWorld) ||
               (_isClient && !ClientServerBootstrap.HasClientWorlds))
        {
            yield return null;
        }

        if (_isServer)
        {
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
        }

        if (_isClient)
        {
            Debug.Log($"Connecting on IP: {_connectIP} and port: {_port}");
            using var query =
                ClientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            query.GetSingletonRW<NetworkStreamDriver>().ValueRW
                .Connect(ClientWorld.EntityManager, NetworkEndpoint.Parse(_connectIP, _port));
        }

        _connecting = false;
    }
}