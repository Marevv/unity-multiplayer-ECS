using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    public RelayServerData RelayServerData;
    public RelayServerData RelayClientData;

    [SerializeField] private int maxConnections = 3;
    [SerializeField] private string connectionType = "dtls";
    
    private void Awake() {
        Instance = this;
    }

    public async Task<string> SetupHost()
    {
        try
        {

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

            var endpoint = allocation.ServerEndpoints.FirstOrDefault(endpoint => endpoint.ConnectionType == connectionType); 
            
            if (endpoint == null)
            {
                throw new InvalidOperationException($"endpoint for connectionType {connectionType} not found");
            }

            var serverEndpoint = NetworkEndpoint.Parse(endpoint.Host, (ushort)endpoint.Port);
            
            // UTP uses pointers instead of managed arrays for performance reasons, so we use these helper functions to convert them
            var allocationIdBytes = RelayAllocationId.FromByteArray(allocation.AllocationIdBytes);
            var connectionData = RelayConnectionData.FromByteArray(allocation.ConnectionData);
            var key = RelayHMACKey.FromByteArray(allocation.Key);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            RelayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationIdBytes, ref connectionData, ref connectionData, ref key, connectionType == "dtls");
            
            Debug.Log("RelayStarted: " + joinCode);
            return joinCode;
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return null;
        }
    }

    public async Task SetupClient(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            
            var endpoint = joinAllocation.ServerEndpoints.FirstOrDefault(endpoint => endpoint.ConnectionType == connectionType); 
            if (endpoint == null)
            {
                throw new Exception($"endpoint for connectionType {connectionType} not found");
            }
            
            var serverEndpoint = NetworkEndpoint.Parse(endpoint.Host, (ushort)endpoint.Port);
            
            // UTP uses pointers instead of managed arrays for performance reasons, so we use these helper functions to convert them
            var allocationIdBytes = RelayAllocationId.FromByteArray(joinAllocation.AllocationIdBytes);
            var connectionData = RelayConnectionData.FromByteArray(joinAllocation.ConnectionData);
            var hostConnectionData = RelayConnectionData.FromByteArray(joinAllocation.HostConnectionData);
            var key = RelayHMACKey.FromByteArray(joinAllocation.Key);
            
            RelayClientData = new RelayServerData(ref serverEndpoint, 0, ref allocationIdBytes, ref connectionData,
                ref hostConnectionData, ref key, connectionType == "dtls");

            Debug.Log("RelayJoined: " + joinCode);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }
    
    
}
