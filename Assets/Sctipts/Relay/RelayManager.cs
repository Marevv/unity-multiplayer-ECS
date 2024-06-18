using System;
using System.Threading.Tasks;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }
    
    private void Awake() {
        Instance = this;
    }


    public async Task<string> CreateRelay()
    {
        try
        {

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            
            Debug.Log("RelayStarted: " + joinCode);
            return joinCode;
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return null;
        }
    }

    public async void JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");

            Debug.Log("RelayJoined: " + joinCode);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }
}
