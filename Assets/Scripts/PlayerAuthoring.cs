using System;
using System.ComponentModel;
using PlasticPipe.PlasticProtocol.Messages;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

class PlayerAuthoring : MonoBehaviour
{
    [SerializeField]
    public float MoveSpeed;
    public int Team;

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
        }
    }
}


public struct Player : IComponentData
{
    public float MoveSpeed;
    public int Resources;
    public Entity MeleeUnitPrefab;
    public int Team;
}


[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct PlayerSystem : ISystem
{
    void OnCreate(ref SystemState state) {
    }
    void OnDestroy(ref SystemState state) {
    }

    public void OnUpdate(ref SystemState state)
    {
        var config = GameObject.FindObjectOfType<Config>().GetComponent<Config>();
        var moveInput = InputSystem.actions.FindAction("Move").ReadValue<Vector2>();
        float deltaTime = SystemAPI.Time.DeltaTime;

        var zoom = InputSystem.actions.FindAction("Zoom").ReadValue<float>();
        zoom = System.Math.Max(-1, System.Math.Min(1, zoom));

        foreach (var (transform, player) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Player>>())
        {
            transform.ValueRW.Position = transform.ValueRO.Position + new float3 { x = moveInput.x, y = 0, z = moveInput.y } * player.ValueRO.MoveSpeed * deltaTime;

            CameraSingleton.OffsetPos += Vector3.up * zoom;
            CameraSingleton.OffsetPos.y = System.Math.Max(config.MinZoom, CameraSingleton.OffsetPos.y);
            var cameraTransform = CameraSingleton.Instance.transform;

            var pos = transform.ValueRO.Position;
            cameraTransform.position = new Vector3(pos.x, pos.y, pos.z);
            cameraTransform.position += CameraSingleton.OffsetPos;

            if (Input.GetMouseButtonDown(0)) {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

                Debug.Log(ray);

                var input = new RaycastInput
                {
                    Start = ray.origin,
                    End = ray.direction * 1000,
                    Filter = new CollisionFilter
                    {
                        BelongsTo = 1,
                        CollidesWith = ~0u,
                        GroupIndex = 0,
                    }
                };

                var hit = new Unity.Physics.RaycastHit();
                if (collisionWorld.CastRay(input, out hit))
                {
                    Debug.Log("hit!");
                    var instance = state.EntityManager.Instantiate(player.ValueRO.MeleeUnitPrefab);
                    state.EntityManager.SetComponentData(instance, new LocalTransform {
                        Position = hit.Position,
                        Rotation = quaternion.identity,
                        Scale = SystemAPI.GetComponent<LocalTransform>(player.ValueRO.MeleeUnitPrefab).Scale,
                    });
                    var unit = state.EntityManager.GetComponentData<Unit>(instance);
                    unit.Team = player.ValueRO.Team;
                    state.EntityManager.SetComponentData(instance, unit);

                    //var config = GameObject.FindObjectOfType<Config>().GetComponent<Config>();

                    //var config = FindObjectOfType<Config>().GetComponent<Config>();

                    Color color = new Color();
                    if (unit.Team == 0)
                    {
                        color = config.Team_0_Color;
                    }
                    else if (unit.Team == 1)
                    {
                        color = config.Team_1_Color;
                    }
                    //state.EntityManager.AddComponent<URPMaterialPropertyBaseColor>(entity);
                    state.EntityManager.SetComponentData<URPMaterialPropertyBaseColor>(instance, new URPMaterialPropertyBaseColor
                    {
                        Value = new float4 { x = color.r, y = color.g, z = color.b, w = 1 },
                    });
                }
            }
        }
    }
}
