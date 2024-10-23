using System;
using System.ComponentModel;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

class PlayerAuthoring : MonoBehaviour
{
    [SerializeField]
    public float MoveSpeed;

    class PlayerBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Player
            {
                MoveSpeed = authoring.MoveSpeed,
                Resources = 0,
            });
        }
    }
}

public struct Player : IComponentData
{
    public float MoveSpeed;
    public int Resources;
}


public partial struct PlayerSystem : ISystem
{
    void OnCreate(ref SystemState state) {
    }
    void OnDestroy(ref SystemState state) {
    }
    
    public void OnUpdate(ref SystemState state)
    {

        var moveInput = InputSystem.actions.FindAction("Move").ReadValue<Vector2>();
        float deltaTime = SystemAPI.Time.DeltaTime;

        var zoom = InputSystem.actions.FindAction("Zoom").ReadValue<float>();
        zoom = Math.Max(-1, Math.Min(1, zoom));
        if (zoom != 0)
        {
            Debug.Log(zoom);
        }

        foreach (var (transform, player) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Player>>())
        {
            transform.ValueRW.Position = transform.ValueRO.Position + new float3 { x = moveInput.x, y = 0, z = moveInput.y } * player.ValueRO.MoveSpeed * deltaTime;

            CameraSingleton.OffsetPos += Vector3.up * zoom;
            var cameraTransform = CameraSingleton.Instance.transform;
            //var camera = GameObject.FindGameObjectWithTag("MainCamera").transform;
            var pos = transform.ValueRO.Position;
            cameraTransform.position = new Vector3(pos.x, pos.y, pos.z);
            cameraTransform.position += CameraSingleton.OffsetPos;
        }
    }
}
