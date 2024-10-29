using Unity.Entities;
using UnityEngine;

public struct CubeSpawner : IComponentData
{
    public Entity Cube;
}
    
public class CubeSpawnerAuthoring : MonoBehaviour
{
    public GameObject Cube;
    class Baker : Baker<CubeSpawnerAuthoring>
    {
        public override void Bake(CubeSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CubeSpawner
            {
                Cube = GetEntity(authoring.Cube, TransformUsageFlags.Dynamic),
            });
        }
    }
    
}