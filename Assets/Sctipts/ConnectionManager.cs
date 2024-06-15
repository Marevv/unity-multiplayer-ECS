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
using Unity.Scenes;
using Unity.VisualScripting;
using UnityEngine.UI;

public class ConnectionManager : MonoBehaviour
{
    
    [SerializeField] private string _listenIP = "127.0.0.1";
    [SerializeField] private string _connectIP = "127.0.0.1";
    [SerializeField] private ushort _port = 7979;

    private const string localhost = "127.0.0.1";
    private const ushort defaultPort = 7979;

    public TMP_InputField ipInputField;
    public TMP_InputField portInputField;
    public Button connectButton;

    private bool _connecting = false;

    private bool _isServer = false;
    private bool _isClient = false;

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
        _isServer = ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.ClientAndServer ||
                    ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Server;
        _isClient = ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.ClientAndServer ||
                    ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Client;
        
        if(_isServer)
            Connect();
    }

    private void SetupConnection()
    {
        _connectIP = ipInputField.text == "" ? localhost : ipInputField.text;
        _port = portInputField.text == "" ? defaultPort : ushort.Parse(portInputField.text);
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

        while ((_isServer && !ClientServerBootstrap.HasServerWorld) || (_isClient && !ClientServerBootstrap.HasClientWorlds))
        {
            yield return null;
        }

        if (_isServer)
        {
            var args = Environment.GetCommandLineArgs();

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "-ip")
                {
                    _listenIP = args[i + 1];
                    i++;
                }
                else if (args[i] == "-port")
                {
                    _port = ushort.Parse(args[i + 1]);
                    i++;
                    break;
                }
            }
            
            using var query =
                ServerWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            query.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(NetworkEndpoint.Parse(_listenIP, _port));
        }

        if (_isClient)
        {
            using var query =
                ClientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            query.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(ClientWorld.EntityManager, NetworkEndpoint.Parse(_listenIP, _port));
        }

        _connecting = false;
    }



}
