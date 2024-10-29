using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

public struct CubeInput : IInputComponentData
{
    public int Horizontal;
    public int Vertical;
}

public class CubeInputAuthoring : MonoBehaviour
{
    class Baker : Baker<CubeInputAuthoring>
    {
        public override void Bake(CubeInputAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<CubeInput>(entity);
        }
    }
}


[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial struct SampleCubeInput : ISystem
{
    public void OnCreate(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var playerInput in SystemAPI.Query<RefRW<CubeInput>>().WithAll<GhostOwnerIsLocal>())
        {
            playerInput.ValueRW = default;
            if (Input.GetKey("left"))
            {
                playerInput.ValueRW.Horizontal -= 1;
            }

            if (Input.GetKey("right"))
            {
                playerInput.ValueRW.Horizontal += 1;
            }

            if (Input.GetKey("down"))
            {
                playerInput.ValueRW.Vertical -= 1;
            }

            if (Input.GetKey("up"))
            {
                playerInput.ValueRW.Vertical += 1;
            }
        }
    }
}

public partial struct CubeMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var speed = SystemAPI.Time.DeltaTime * 4;

        foreach (var (input, transform) in SystemAPI.Query<RefRO<CubeInput>, RefRW<LocalTransform>>()
                     .WithAll<Simulate>())
        {
            var moveInput = new float2(input.ValueRO.Horizontal, input.ValueRO.Vertical);
            moveInput = math.normalizesafe(moveInput) * speed;
            transform.ValueRW.Position += new float3(moveInput.x, 0, moveInput.y);
        }
    }
}