using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

class KYS
{

public static float4 colorToFloat4(Color color)
{
    return new float4 { x = color.r, y = color.g, z = color.b, w = color.a };
}

}

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
            var config = FindObjectOfType<Config>().GetComponent<Config>();
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            int team = -1;
            if (authoring.transform.parent != null) {
                team = (authoring.transform.parent.name == "Team_0") ? 0 : 1;
            }

            AddComponent(entity, new Unit
            {
                Team = team,
                Health = authoring.Health,
                Damage = authoring.Damage,
                MoveSpeed = authoring.MoveSpeed,
                AttackRange = authoring.AttackRange,
                AttackInterval = authoring.AttackInterval,
            });

            Color color = new Color();
            if (team == 0)
            {
                color = config.Team_0_Color;
            }
            else if (team == 1)
            {
                color = config.Team_1_Color;
            }

            //Debug.Log(color);
            //AddComponent(entity, new URPMaterialPropertyBaseColor
            //{
            //    //Value = new float4 { x = 1, y = 0, z = 0, w = 1 },
            //    Value = KYS.colorToFloat4(color),
            //});
        }
    }
}

public struct Unit : IComponentData
{
    public Entity Target;

    public int Team;

    public int Health;
    public int Damage;
    public float MoveSpeed;
    public float AttackRange;

    public float AttackInterval;

    public float LastAttackTime;
    public bool IsFirstAttack;
}

public partial struct SetColors : ISystem
{
    void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Unit>();
    }

    void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;
        var config = GameObject.FindObjectOfType<Config>().GetComponent<Config>();

        //var config = FindObjectOfType<Config>().GetComponent<Config>();

        foreach (var (unit, entity) in SystemAPI.Query<RefRO<Unit>>().WithEntityAccess())
        {
            Color color = new Color();
            if (unit.ValueRO.Team == 0)
            {
                color = config.Team_0_Color;
            } else if (unit.ValueRO.Team == 1)
            {
                color = config.Team_1_Color;
            }
                //state.EntityManager.AddComponent<URPMaterialPropertyBaseColor>(entity);
            state.EntityManager.SetComponentData<URPMaterialPropertyBaseColor>(entity, new URPMaterialPropertyBaseColor {
                Value = new float4 { x = color.r, y = color.g, z = color.b, w = 1 },
            });
        }
    }
}

public partial struct UnitSystem : ISystem
{
    void OnCreate(ref SystemState state)
    {

    }
    void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {

        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (transform, unit, velocity) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<Unit>, RefRW<PhysicsVelocity>>())
        {
            if (!SystemAPI.Exists(unit.ValueRO.Target))
            {
                Entity closestEntity = new Entity { };
                float minDistanceSquared = float.MaxValue;
                foreach (var (targetTransform, targetUnit, targetEntity) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Unit>>().WithEntityAccess())
                {
                    var targetDistanceSquared = math.distancesq(targetTransform.ValueRO.Position, transform.ValueRO.Position);
                    if (targetUnit.ValueRO.Team != unit.ValueRO.Team && targetDistanceSquared < minDistanceSquared)
                    {
                        closestEntity = targetEntity;
                        minDistanceSquared = targetDistanceSquared;
                    }

                }

                unit.ValueRW.Target = closestEntity;
            }

            float3 newVelocity = new float3 { };
            if (SystemAPI.Exists(unit.ValueRO.Target))
            {
                var targetTransform = SystemAPI.GetComponent<LocalTransform>(unit.ValueRO.Target);

                if (math.distance(targetTransform.Position, transform.ValueRO.Position) > unit.ValueRO.AttackRange)
                {
                    var moveDirection = math.normalize(targetTransform.Position - transform.ValueRO.Position);


                    newVelocity = moveDirection * unit.ValueRO.MoveSpeed;
                    //velocity.ValueRW.Linear.z = newVelocity.z;

                    unit.ValueRW.IsFirstAttack = true;
                } else 
                {
                    if (unit.ValueRO.IsFirstAttack)
                    {
                        unit.ValueRW.LastAttackTime = Time.time;
                        unit.ValueRW.IsFirstAttack = false;
                    }

                    if (unit.ValueRO.LastAttackTime + unit.ValueRO.AttackInterval <= Time.time)
                    {
                        SystemAPI.GetComponentRW<Unit>(unit.ValueRO.Target).ValueRW.Health -= unit.ValueRO.Damage;
                        unit.ValueRW.LastAttackTime = Time.time;
                    }
                }

            }
            velocity.ValueRW.Linear = newVelocity;

            //velocity.ValueRW.Linear.y = 0f;
            //velocity.ValueRW.Angular = float3.zero;

            //transform.ValueRW.Position.y = 0;
            //transform.ValueRW.Rotation = Quaternion.identity;
        }

        EntityCommandBuffer ecbKill = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (unit, entity) in SystemAPI.Query<RefRO<Unit>>().WithEntityAccess())
        {
            if (unit.ValueRO.Health <= 0)
            {
                ecbKill.DestroyEntity(entity);
            }
        }

        ecbKill.Playback(state.EntityManager);
        ecbKill.Dispose();
    }
}
