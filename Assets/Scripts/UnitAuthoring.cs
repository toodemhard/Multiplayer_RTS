using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class UnitAuthoring : MonoBehaviour
{
    public int Health;
    public int Damage;
    public float MoveSpeed;
    public float AttackRange;
    class UnitBaker : Baker<UnitAuthoring>
    {
        public override void Bake(UnitAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var team = (authoring.transform.parent.name == "Team_0") ? 0 : 1;

            AddComponent(entity, new Unit
            {
                Team = team,
                Health = authoring.Health,
                Damage = authoring.Damage,
                MoveSpeed = authoring.MoveSpeed,
                AttackRange = authoring.AttackRange,
            });

            float4 color = new float4 { x = 0, y = 0, z = 0, w = 1 };
            if (team == 0)
            {
                color = new float4 { x = 1, y = 1, z = 0, w = 1.0f };
            }
            else if (team == 1)
            {
                color = new float4 { x = 0, y = 0, z = 1, w = 1.0f };
            }

            AddComponent(entity, new URPMaterialPropertyBaseColor
            {
                Value = color,
            });
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
}

//public partial struct SetColors : ISystem
//{
//    void OnCreate(ref SystemState state)
//    {
//        state.RequireForUpdate<Unit>();
//    }

//    void OnUpdate(ref SystemState state)
//    {
//        state.Enabled = false;

//        foreach (var (unit, entity) in SystemAPI.Query<RefRO<Unit>>().WithEntityAccess())
//        {
//            if (unit.ValueRO.Team == 0)
//            {
//                SystemAPI.AddComponent<MaterialColor>(entity, new MaterialColor
//                {
//                    Value = new float4 { x = 1, y = 0, z = 0, w = 1.0f },
//                });
//            }
//        }
//    }
//}

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

        foreach (var (transform, unit) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<Unit>>())
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

            if (SystemAPI.Exists(unit.ValueRO.Target))
            {
                var targetTransform = SystemAPI.GetComponent<LocalTransform>(unit.ValueRO.Target);
                var moveDirection = math.normalize(targetTransform.Position - transform.ValueRO.Position);
                transform.ValueRW.Position += moveDirection * unit.ValueRO.MoveSpeed * deltaTime;
            }
        }
    }
}
