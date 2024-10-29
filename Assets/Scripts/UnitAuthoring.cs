using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class UnitAuthoring : MonoBehaviour
{
    public int Health;
    public int Damage;
    public float MoveSpeed;
    public float AttackRange;
    public float AttackInterval;

    class UnitBaker : Baker<UnitAuthoring>
    {
        public override void Bake(UnitAuthoring authoring)
        {
            // var config = SystemAPI.GetSingleton<Config>();
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            TeamType team = default(TeamType);
            if (authoring.transform.parent != null)
            {
                team = (authoring.transform.parent.name == "Team_0") ? TeamType.Red : TeamType.Blue;
            }

            AddComponent(entity, new Unit
            {
                Health = authoring.Health,
                Damage = authoring.Damage,
                MoveSpeed = authoring.MoveSpeed,
                AttackRange = authoring.AttackRange,
                AttackInterval = authoring.AttackInterval,
            });
            AddComponent(entity, new Team{Value = team});
            AddComponent(entity, new NewUnitTag());

            //Debug.Log(color);
            //AddComponent(entity, new URPMaterialPropertyBaseColor
            //{
            //    //Value = new float4 { x = 1, y = 0, z = 0, w = 1 },
            //    Value = KYS.colorToFloat4(color),
            //});
        }
    }
}

public partial struct InitializeUnitSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (team, entity) in SystemAPI
                     .Query<RefRO<Team>>().WithAny<NewUnitTag>()
                     .WithEntityAccess())
        {
            var color = team.ValueRO.Value switch
            {
                TeamType.Red => config.Team_0_Color,
                TeamType.Blue => config.Team_1_Color,
                _ => new Color()
            };

            ecb.SetComponent(entity, new URPMaterialPropertyBaseColor
            {
                Value = new float4(color.r, color.g, color.b, color.a)
            });
            
            ecb.RemoveComponent<NewUnitTag>(entity);
        }
        
        ecb.Playback(state.EntityManager);
    }
}

public struct NewUnitTag : IComponentData
{
}

public enum TeamType : byte
{
    Uninitialized,
    Red,
    Blue,
}

public struct Team : IComponentData
{
    public TeamType Value;
}

public struct Unit : IComponentData
{
    public Entity Target;

    public int Health;
    public int Damage;
    public float MoveSpeed;
    public float AttackRange;

    public float AttackInterval;

    public float LastAttackTime;
    public bool IsFirstAttack;
}

// [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
// public partial struct UnitSystem : ISystem
// {
//     void OnCreate(ref SystemState state)
//     {
//     }
//
//     void OnDestroy(ref SystemState state)
//     {
//     }
//
//     public void OnUpdate(ref SystemState state)
//     {
//         float deltaTime = SystemAPI.Time.DeltaTime;
//
//         foreach (var (transform, team, unit, velocity) in SystemAPI
//                      .Query<RefRW<LocalTransform>, RefRW<Team>, RefRW<Unit>, RefRW<PhysicsVelocity>>())
//         {
//             if (!SystemAPI.Exists(unit.ValueRO.Target))
//             {
//                 Entity closestEntity = new Entity { };
//                 float minDistanceSquared = float.MaxValue;
//                 foreach (var (targetTransform, targetTeam, targetUnit, targetEntity) in SystemAPI
//                              .Query<RefRW<LocalTransform>, RefRO<Team>, RefRO<Unit>>().WithEntityAccess())
//                 {
//                     var targetDistanceSquared =
//                         math.distancesq(targetTransform.ValueRO.Position, transform.ValueRO.Position);
//                     if (team.ValueRO.Value != targetTeam.ValueRO.Value && targetDistanceSquared < minDistanceSquared)
//                     {
//                         closestEntity = targetEntity;
//                         minDistanceSquared = targetDistanceSquared;
//                     }
//                 }
//
//                 unit.ValueRW.Target = closestEntity;
//             }
//
//             float3 newVelocity = new float3 { };
//             if (SystemAPI.Exists(unit.ValueRO.Target))
//             {
//                 var targetTransform = SystemAPI.GetComponent<LocalTransform>(unit.ValueRO.Target);
//
//                 if (math.distance(targetTransform.Position, transform.ValueRO.Position) > unit.ValueRO.AttackRange)
//                 {
//                     var moveDirection = math.normalize(targetTransform.Position - transform.ValueRO.Position);
//
//
//                     newVelocity = moveDirection * unit.ValueRO.MoveSpeed;
//                     //velocity.ValueRW.Linear.z = newVelocity.z;
//
//                     unit.ValueRW.IsFirstAttack = true;
//                 }
//                 else
//                 {
//                     if (unit.ValueRO.IsFirstAttack)
//                     {
//                         unit.ValueRW.LastAttackTime = Time.time;
//                         unit.ValueRW.IsFirstAttack = false;
//                     }
//
//                     if (unit.ValueRO.LastAttackTime + unit.ValueRO.AttackInterval <= Time.time)
//                     {
//                         SystemAPI.GetComponentRW<Unit>(unit.ValueRO.Target).ValueRW.Health -= unit.ValueRO.Damage;
//                         unit.ValueRW.LastAttackTime = Time.time;
//                     }
//                 }
//             }
//
//             velocity.ValueRW.Linear = newVelocity;
//
//             //velocity.ValueRW.Linear.y = 0f;
//             //velocity.ValueRW.Angular = float3.zero;
//
//             //transform.ValueRW.Position.y = 0;
//             //transform.ValueRW.Rotation = Quaternion.identity;
//         }
//
//         EntityCommandBuffer ecbKill = new EntityCommandBuffer(Allocator.Temp);
//
//         foreach (var (unit, entity) in SystemAPI.Query<RefRO<Unit>>().WithEntityAccess())
//         {
//             if (unit.ValueRO.Health <= 0)
//             {
//                 ecbKill.DestroyEntity(entity);
//             }
//         }
//
//         ecbKill.Playback(state.EntityManager);
//         ecbKill.Dispose();
//     }
// }