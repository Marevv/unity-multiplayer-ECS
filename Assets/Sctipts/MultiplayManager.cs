using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Networking.Transport;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine;

public class MultiplayManager : MonoBehaviour
{
    public static MultiplayManager Instance { get; private set; }


    private static IServerQueryHandler serverQueryHandler;
    private float autoAllocateTimer = 9999999f;
    private bool alreadyAutoAllocated;
    private string backfillTicketId;
    private string backfullTicketId;
    private float acceptBackfillTicketsTimer;
    private float acceptBackfillTicketsTimerMax = 1.1f;
    private PayloadAllocation _payloadAllocation;

// Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        autoAllocateTimer -= Time.deltaTime;
        if (autoAllocateTimer <= 0f) {
            autoAllocateTimer = 999f;
            MultiplayEventCallbacks_Allocate(null);
        }
        if (serverQueryHandler != null)
        {
            if (ConnectionManager.Instance.isServer)
            {
                //serverQueryHandler.CurrentPlayers = (ushort)
            }

            serverQueryHandler.UpdateServerCheck();
        }

        if (backfillTicketId != null)
        {
            acceptBackfillTicketsTimer -= Time.deltaTime;
            if (acceptBackfillTicketsTimer <= 0f)
            {
                acceptBackfillTicketsTimer = acceptBackfillTicketsTimerMax;
                HandleBackfillTickets();
            }
        }
    }


    public async Task InitializeMultiplay()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {

            await UnityServices.InitializeAsync();
            
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            Debug.Log("Initializing Multiplay Callbacks");

            MultiplayEventCallbacks multiplayEventCallbacks = new MultiplayEventCallbacks();
            multiplayEventCallbacks.Allocate += MultiplayEventCallbacks_Allocate;
            multiplayEventCallbacks.Deallocate += MultiplayEventCallbacks_Deallocate;
            multiplayEventCallbacks.Error += MultiplayEventCallbacks_Error;
            multiplayEventCallbacks.SubscriptionStateChanged += MultiplayEventCallbacks_SubscriptionStateChanged;
            

            IServerEvents serverEvents =
                await MultiplayService.Instance.SubscribeToServerEventsAsync(multiplayEventCallbacks);


            serverQueryHandler =
                await MultiplayService.Instance.StartServerQueryHandlerAsync(5, "MyServerName", "MutliplayerEntities",
                    "1.0", "Default");

            var serverConfig = MultiplayService.Instance.ServerConfig;
            Debug.Log("Server Config: " + serverConfig.AllocationId);
            if (serverConfig.AllocationId != "")
            {
                //Already Allocated
                MultiplayEventCallbacks_Allocate(new MultiplayAllocation("", serverConfig.ServerId,
                    serverConfig.AllocationId));
            }
        }
        else
        {
            var serverConfig = MultiplayService.Instance.ServerConfig;
            
            Debug.Log("Server Config: " + serverConfig.AllocationId);
            if (serverConfig.AllocationId != "")
            {
                //Already Allocated
                MultiplayEventCallbacks_Allocate(new MultiplayAllocation("", serverConfig.ServerId,
                    serverConfig.AllocationId));
            }
        }
    }

    private void MultiplayEventCallbacks_SubscriptionStateChanged(MultiplayServerSubscriptionState obj)
    {
        Debug.Log("MultiplayEventCallbacks_SubscriptionStateChanged");
        Debug.Log(obj);
    }

    private void MultiplayEventCallbacks_Error(MultiplayError obj)
    {
        Debug.Log("MultiplayEventCallbacks_Error");
        Debug.Log(obj.Reason);
    }

    private void MultiplayEventCallbacks_Deallocate(MultiplayDeallocation obj)
    {
        Debug.Log("MultiplayEventCallbacks_Deallocate");
    }

    private void MultiplayEventCallbacks_Allocate(MultiplayAllocation obj)
    {
        Debug.Log("MultiplayEventCallbacks_Allocate");

        if (alreadyAutoAllocated)
        {
            Debug.Log("Already auto allocated!");
            return;
        }

        alreadyAutoAllocated = true;

        var serverConfig = MultiplayService.Instance.ServerConfig;
        Debug.Log($"ServerID: {serverConfig.ServerId}");
        Debug.Log($"AllocationID: {serverConfig.AllocationId}");
        Debug.Log($"Port: {serverConfig.Port}");
        Debug.Log($"QueryPort: {serverConfig.QueryPort}");
        Debug.Log($"LogDirectory: {serverConfig.ServerLogDirectory}");

        SetupBackfillTickets();

        ConnectionManager.Instance.StartServer();

        MultiplayServerReady();
    }

    public async void MultiplayServerReady()
    {
        Debug.Log("MultiplayserverReadyForPlayers");
        await MultiplayService.Instance.ReadyServerForPlayersAsync();
    }


    private async void SetupBackfillTickets()
    {
        Debug.Log("SetupBackfillTickets");

        _payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<PayloadAllocation>();

        backfillTicketId = _payloadAllocation.BackfillTicketId;
        Debug.Log("backfillTickedId: " + backfillTicketId);

        acceptBackfillTicketsTimer = acceptBackfillTicketsTimerMax;
    }

    private async void HandleUpdateBackfillTickets()
    {
        if (backfillTicketId != null && _payloadAllocation != null && ConnectionManager.Instance.HasAvailablePlayerSlots())
        {
            Debug.Log("HandleUpdateBackfillTickets");
            string payload = await MultiplayService.Instance.GetPayloadAllocationAsPlainText();
    
            try {
                await MatchmakerService.Instance.UpdateBackfillTicketAsync(_payloadAllocation.BackfillTicketId,
                    new BackfillTicket(backfillTicketId, properties: new BackfillTicketProperties(_payloadAllocation.MatchProperties))
                );
            } catch (MatchmakerServiceException e) {
                Debug.Log("Error: " + e);
            }
            
        }
    }

    private async void HandleBackfillTickets()
    {
        if (ConnectionManager.Instance.HasAvailablePlayerSlots())
        {
            BackfillTicket backfillTicket =
                await MatchmakerService.Instance.ApproveBackfillTicketAsync(backfillTicketId);
            backfillTicketId = backfillTicket.Id;

        }
    }

    [Serializable]
    public class PayloadAllocation
    {
        public Unity.Services.Matchmaker.Models.MatchProperties MatchProperties;
        public string GeneratorName;
        public string QueueName;
        public string PoolName;
        public string EnvironmentId;
        public string BackfillTicketId;
        public string MatchId;
        public string PoolId;
    }
}