using Unity.Entities;
using UnityEngine;


public struct PrefabsData : IComponentData
{
    public Entity unit;
    public Entity player;
}

public class PrefabsAuthoring : MonoBehaviour
{
    public GameObject unit;
    public GameObject player;

    public class Baker : Baker<PrefabsAuthoring>
    {
        public override void Bake(PrefabsAuthoring authoring)
        {
            Entity unitPrefab = default;
            Entity playerPrefab = default;
            if (authoring.unit != null)
            {
                unitPrefab = GetEntity(authoring.unit, TransformUsageFlags.Dynamic);
            }

            if (authoring.player != null)
            {
                playerPrefab = GetEntity(authoring.player, TransformUsageFlags.Dynamic);
            }

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PrefabsData()
            {
                unit = unitPrefab,
                player = playerPrefab
            });
        }
    }
}