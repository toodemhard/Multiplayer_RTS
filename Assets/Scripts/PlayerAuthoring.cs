using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

class PlayerAuthoring : MonoBehaviour
{
    [SerializeField] public float MoveSpeed;
    public TeamType Team;

    public GameObject MeleeUnitPrefab;

    class PlayerBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Player
            {
                MoveSpeed = authoring.MoveSpeed,
                Resources = 0,
                MeleeUnitPrefab = GetEntity(authoring.MeleeUnitPrefab, TransformUsageFlags.Dynamic),
                Team = authoring.Team,
            });
            AddComponent(entity, new PlayerInput());
        }
    }
}


public struct Player : IComponentData
{
    public float MoveSpeed;
    public int Resources;
    public Entity MeleeUnitPrefab;
    public TeamType Team;
}

public struct PlayerInput : IInputComponentData
{
    public float2 MoveInput;

    public InputEvent ZoomEvent;
    public float ZoomValue;

    public InputEvent SpawnUnitEvent;
    public float3 SpawnPosition;
}

// public struct SpawnInput : IInputComponentData
// {
//     public InputEvent SpawnUnit;
//     public float3 SpawnPosition;
// }

public class KYS
{
    public static Vector3 floatToVec(float3 f)
    {
        return new Vector3(f.x, f.y, f.z);
    }
}

[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial struct PlayerSampleInputs : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var playerInput in SystemAPI.Query<RefRW<PlayerInput>>().WithAll<GhostOwnerIsLocal>())
        {
            var moveInput = InputSystem.actions.FindAction("Move").ReadValue<Vector2>();
            var zoom = InputSystem.actions.FindAction("Zoom").ReadValue<float>();

            var newPlayerInput = new PlayerInput
            {
                MoveInput = moveInput,
            };

            if (zoom != 0)
            {
                newPlayerInput.ZoomEvent.Set();
                newPlayerInput.ZoomValue = zoom;
            }

            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log("asdlj");
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

                var input = new RaycastInput
                {
                    Start = ray.origin,
                    End = ray.direction * 1000,
                    Filter = new CollisionFilter
                    {
                        BelongsTo = ~0u,                        
                        CollidesWith = ~0u,                        
                        // BelongsTo = 1 << 1,
                        // CollidesWith = 1 << 0,
                    }
                };
                
                Debug.DrawRay(input.Start, input.End, Color.red, 1);


                if (collisionWorld.CastRay(input, out var hit))
                {
                    Debug.Log("hit");
                    newPlayerInput.SpawnUnitEvent.Set();
                    newPlayerInput.SpawnPosition = hit.Position;
                }
            }

            playerInput.ValueRW = newPlayerInput;
        }
    }
}

// [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct PlayerMoveSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        // var config = GameObject.FindFirstObjectByType<Config>().GetComponent<Config>();
        float deltaTime = SystemAPI.Time.DeltaTime;


        foreach (var (transform, input, player) in SystemAPI
                     .Query<RefRW<LocalTransform>, RefRO<PlayerInput>, RefRO<Player>>().WithAll<Simulate>())
        {
            if (input.ValueRO.ZoomEvent.IsSet)
            {
                var zoom = System.Math.Max(-1, System.Math.Min(1, input.ValueRO.ZoomValue));
                transform.ValueRW.Position.y += 1 * zoom;
            }

            transform.ValueRW.Position = transform.ValueRO.Position +
                                         new float3
                                         {
                                             x = input.ValueRO.MoveInput.x, y = 0, z = input.ValueRO.MoveInput.y
                                         } *
                                         player.ValueRO.MoveSpeed * deltaTime;

            // CameraSingleton.OffsetPos.y = System.Math.Max(config.MinZoom, CameraSingleton.OffsetPos.y);
            var pos = transform.ValueRO.Position;

            var cameraTransform = Camera.main.transform;
            cameraTransform.position = new Vector3(pos.x, pos.y, pos.z);
            // cameraTransform.position += KYS.floatToVec(cameraOffset.OffsetPos);
        }
    }
}

// [UpdateInGroup(typeof(GhostInputSystemGroup))]
// public partial struct GatherInputs : ISystem
// {
//     public void OnCreate(ref SystemState state)
//     {
//         state.RequireForUpdate<PhysicsWorldSingleton>();
//     }
//
//     public void OnUpdate(ref SystemState state)
//     {
//         if (Input.GetMouseButtonDown(0))
//         {
//             var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
//             var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
//
//             Debug.Log(ray);
//
//             var input = new RaycastInput
//             {
//                 Start = ray.origin,
//                 End = ray.direction * 1000,
//                 Filter = new CollisionFilter
//                 {
//                     BelongsTo = 1 << 1,
//                     CollidesWith = 1,
//                     GroupIndex = 0,
//                 }
//             };
//
//             var hit = new Unity.Physics.RaycastHit();
//             var camera = Camera.main;
//
//             var newSpawnInput = new SpawnInput();
//             if (collisionWorld.CastRay(input, out hit))
//             {
//                 newSpawnInput.SpawnUnit.Set();
//                 newSpawnInput.SpawnPosition = hit.Position;
//             }
//
//             foreach (var spawnInput in SystemAPI.Query<RefRW<SpawnInput>>())
//             {
//                 spawnInput.ValueRW = newSpawnInput;
//             }
//         }
//     }
// }


// [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
// [UpdateBefore(typeof(TransformSystemGroup))]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct SpawnUnitSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate<Prefabs>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (input, player) in SystemAPI
                     .Query<RefRO<PlayerInput>, RefRO<Player>>().WithAll<Simulate>())
        {
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!networkTime.IsFirstTimeFullyPredictingTick)
            {
                return;
            }

            if (input.ValueRO.SpawnUnitEvent.IsSet)
            {

                Debug.Log("spawn");
                var prefab = SystemAPI.GetSingleton<Prefabs>().UnitPrefab;
                var instance = ecb.Instantiate(prefab);

                state.EntityManager.SetComponentData(instance, new LocalTransform()
                {
                    Position = input.ValueRO.SpawnPosition,
                    Rotation = quaternion.identity,
                    Scale = state.EntityManager.GetComponentData<LocalTransform>(prefab).Scale,
                });

                state.EntityManager.SetComponentData(instance, new Team
                {
                    Value = player.ValueRO.Team,
                });

            }
        }
        // foreach (var input in SystemAPI.Query<RefRO<SpawnInput>>())
        // {
        //     if (input.ValueRO.SpawnUnit.IsSet)
        //     {
        //         Debug.Log("spawn");
        //         //     var instance = state.entitymanager.instantiate(player.valuero.meleeunitprefab);
        //         //     state.entitymanager.setcomponentdata(instance, new localtransform
        //         //     {
        //         //         position = hit.position,
        //         //         rotation = quaternion.identity,
        //         //         scale = systemapi.getcomponent<localtransform>(player.valuero.meleeunitprefab).scale,
        //         //     });
        //         //     var unit = state.entitymanager.getcomponentdata<unit>(instance);
        //         //     unit.team = player.valuero.team;
        //         //     state.entitymanager.setcomponentdata(instance, unit);
        //         //
        //         //
        //         //     //var config = gameobject.findobjectoftype<config>().getcomponent<config>();
        //         //
        //         //     //var config = findobjectoftype<config>().getcomponent<config>();
        //         //
        //         //     color color = new color();
        //         //     if (unit.team == 0)
        //         //     {
        //         //         color = config.team_0_color;
        //         //     }
        //         //     else if (unit.team == 1)
        //         //     {
        //         //         color = config.team_1_color;
        //         //     }
        //         //
        //         //     //state.entitymanager.addcomponent<urpmaterialpropertybasecolor>(entity);
        //         //     state.entitymanager.setcomponentdata<urpmaterialpropertybasecolor>(instance,
        //         //         new urpmaterialpropertybasecolor
        //         //         {
        //         //             value = new float4 { x = color.r, y = color.g, z = color.b, w = 1 },
        //         //         });
        //     }
        // }
    }
}