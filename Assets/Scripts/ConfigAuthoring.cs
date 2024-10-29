using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class ConfigAuthoring : MonoBehaviour
{
    public Color Team_0_Color;
    public Color Team_1_Color;

    public float MinZoom = 1;

    class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Config
            {
                Team_0_Color = authoring.Team_0_Color,
                Team_1_Color = authoring.Team_1_Color,
                MinZoom = authoring.MinZoom,
            });
        }
    }
}

public struct Config : IComponentData
{
    public Color Team_0_Color;
    public Color Team_1_Color;
    public float MinZoom;
}
