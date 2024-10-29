using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

public class PrefabsAuthoring : MonoBehaviour
{
    [SerializeField] GameObject Unit;
    [SerializeField] GameObject Player;

    public class PrefabsBaker : Baker<PrefabsAuthoring>
    {
        public override void Bake(PrefabsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Prefabs
            {
                Player = GetEntity(authoring.Player, TransformUsageFlags.Dynamic),
                UnitPrefab = GetEntity(authoring.Unit, TransformUsageFlags.Dynamic)
            });
        }
    }

}

public struct Prefabs : IComponentData
{
    public Entity Player;
    public Entity UnitPrefab;
}