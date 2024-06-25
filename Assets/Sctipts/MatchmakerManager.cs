using System;
using System.Collections.Generic;
using Unity.Networking.Transport;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class MatchmakerManager : MonoBehaviour
{
    public static MatchmakerManager Instance { get; private set; }

    public const string DEFAULT_QUEUE = "default-queue";

    private CreateTicketResponse _createTicketResponse;
    
    private float _pollTickTimer;
    private float _pollTickTimerMax = 1.1f;



    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (_createTicketResponse != null)
        {
            _pollTickTimer -= Time.deltaTime;
            if (_pollTickTimer <= 0f)
            {
                _pollTickTimer = _pollTickTimerMax;

                PollMatchmakingTicket();
            }
        }
    }

    
    public async void FindMatch()
    {
        Debug.Log("FindMatch");
        
        
        _createTicketResponse = await MatchmakerService.Instance.CreateTicketAsync(
            new List<Player>
            {
                new Player(AuthenticationService.Instance.PlayerId,
                    new MatchmakingPlayerData
                    {
                        Skill = 100
                    })
            }, new CreateTicketOptions { QueueName = DEFAULT_QUEUE });
    }

    [Serializable]
    public class MatchmakingPlayerData
    {
        public int Skill;
    }
    
    private async void PollMatchmakingTicket()
    {
        Debug.Log("PollMatchmakerTicker");

        TicketStatusResponse ticketStatusResponse =
            await MatchmakerService.Instance.GetTicketAsync(_createTicketResponse.Id);

        if (ticketStatusResponse == null)
        {
            Debug.Log("Null means no updates on this ticker, keep waiting");
            return;
        }

        if (ticketStatusResponse.Type == typeof(MultiplayAssignment))
        {
            MultiplayAssignment multiplayAssignment = ticketStatusResponse.Value as MultiplayAssignment;

            Debug.Log("MultiplayAssignment.Status: " + multiplayAssignment.Status);

            switch (multiplayAssignment.Status)
            {
                case MultiplayAssignment.StatusOptions.Found:
                    _createTicketResponse = null;

                    Debug.Log(multiplayAssignment.Ip + " " + multiplayAssignment.Port);
                    string ipAddress = multiplayAssignment.Ip;
                    ushort port = (ushort)multiplayAssignment.Port;
                    ConnectionManager.Instance.Connect(ipAddress + ":" + port);
                    break;
                case MultiplayAssignment.StatusOptions.InProgress:
                    break;
                case MultiplayAssignment.StatusOptions.Failed:
                    _createTicketResponse = null;
                    Debug.Log("Failed to create Multiplay server!");
                    break;
                case MultiplayAssignment.StatusOptions.Timeout:
                    _createTicketResponse = null;
                    Debug.Log("Multiplay Timeout!");
                    break;
            }

            
        }
    }
}