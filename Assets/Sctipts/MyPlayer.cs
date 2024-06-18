using UnityEngine;
using Unity.Entities;
using Unity.VisualScripting;


public class MyPlayer : MonoBehaviour
{
    public float speed = 5f;
}

public struct PlayerData : IComponentData
{
    public float speed;
}


class PlayerBaker : Baker<MyPlayer>
{
    public override void Bake(MyPlayer authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new PlayerData()
        {
            speed = authoring.speed
        });
        AddComponent<PlayerInputData>(entity);
    }
}