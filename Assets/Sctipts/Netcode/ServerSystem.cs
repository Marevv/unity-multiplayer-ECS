using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

public struct ServerMessageRpcCommand : IRpcCommand
{
    public FixedString64Bytes message;
}

public struct InitializedClient : IComponentData
{
    
}


[WorldSystemFilter((WorldSystemFilterFlags.ServerSimulation))]
public partial class ServerSystem : SystemBase
{

    private ComponentLookup<NetworkId> _clients;

    protected override void OnCreate()
    {
        _clients = GetComponentLookup<NetworkId>(true);
    }

    protected override void OnUpdate()
    {

        _clients.Update(this);
        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ClientMessageRpcCommand>>().WithEntityAccess())
        {
            Debug.Log(command.ValueRO.message + " from client index " + request.ValueRO.SourceConnection.Index + " Version " + request.ValueRO.SourceConnection.Version);
            commandBuffer.DestroyEntity(entity);
        }
        
        foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<SpawnUnityRpcCommand>>().WithEntityAccess())
        {
            if (SystemAPI.TryGetSingleton(out PrefabsData prefabs) && prefabs.unit != null)
            {
                Entity unit = commandBuffer.Instantiate(prefabs.unit);
                commandBuffer.SetComponent(unit, new LocalTransform()
                {
                    Position = new float3(UnityEngine.Random.Range(-10f, 10f), 0, UnityEngine.Random.Range(-10f, 10f)),
                    Rotation = quaternion.identity,
                    Scale = 1f
                });

                var networkId = _clients[request.ValueRO.SourceConnection];
                commandBuffer.SetComponent(unit, new GhostOwner()
                {
                    NetworkId = networkId.Value
                });
                
                commandBuffer.AppendToBuffer(request.ValueRO.SourceConnection, new LinkedEntityGroup()
                {
                    Value = unit
                });
            }
            
            commandBuffer.DestroyEntity(entity);
        }
        //
        // EntityQuery prefabsDataSingletonQuery = new EntityQueryBuilder(Allocator.Temp).WithAllRW<PrefabsData>().Build(EntityManager);
        // if (!prefabsDataSingletonQuery.HasSingleton<PrefabsData>())
        // {
        //     Entity prefabsDataEntity = commandBuffer.CreateEntity();
        //     commandBuffer.AddComponent<PrefabsData>(prefabsDataEntity);
        // }
        
        foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<InitializedClient>().WithEntityAccess())
        {
            commandBuffer.AddComponent<InitializedClient>(entity);
            if (SystemAPI.TryGetSingleton(out PrefabsData prefabs) && prefabs.player != null)
            {
                Entity player = commandBuffer.Instantiate(prefabs.player);
                commandBuffer.SetComponent(player, new LocalTransform()
                {
                    Position = new float3(UnityEngine.Random.Range(-10f, 10f), 0, UnityEngine.Random.Range(-10f, 10f)),
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
                commandBuffer.SetComponent(player,new GhostOwner()
                {
                    NetworkId = id.ValueRO.Value
                });
                commandBuffer.AppendToBuffer(entity, new LinkedEntityGroup()
                {
                    Value = player
                });
            }
        }
        
        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }

    public void SendMessageRpc(string text, World world, Entity target = default)
    {
        if (world == null || world.IsCreated == false)
            return;

        var entity = world.EntityManager.CreateEntity(typeof(SendRpcCommandRequest), typeof(ServerMessageRpcCommand));
        world.EntityManager.SetComponentData(entity, new ServerMessageRpcCommand()
        {
            message = text
        });
        if (target != Entity.Null)
        {
            world.EntityManager.SetComponentData(entity, new SendRpcCommandRequest()
            {
                TargetConnection = target
            });
        }
    }
}
