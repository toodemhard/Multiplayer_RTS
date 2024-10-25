// using Unity.Entities;
// using UnityEngine;
// public class UnitPrefabsAuthoring : MonoBehaviour {
//     public GameObject MeleeUnitPrefab;

//     class Baker : Baker<UnitPrefabsAuthoring> {
//         public override void Bake(UnitPrefabsAuthoring authoring) {
//             var entity = GetEntity(TransformUsageFlags.None);
//             AddComponent(entity, new UnitPrefabs {
//                 MeleeUnitPrefab = GetEntity(authoring.MeleeUnitPrefab, TransformUsageFlags.Dynamic)
//             });
//         }
//     }

// }

// public struct UnitPrefabs : IComponentData {
//     public Entity MeleeUnitPrefab;
// }