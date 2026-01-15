using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class PixelSOAuthoring : MonoBehaviour
{
    public PixelSet pixelSet;
    public class PixelSOBaker : Baker<PixelSOAuthoring>
    {
        public override void Bake(PixelSOAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            var pixelSOs = authoring.pixelSet.pixels;
            var map = new PixelConfigMap()
            {
                configs = new(pixelSOs.Length, Allocator.Persistent)
            };
            foreach(var e in pixelSOs)
            {
                var config = new PixelConfig()
                {
                    type = e.type,
                    interactionMask = e.interactionMask
                };
                map.configs.Add((int)e.type, config);
            }
            AddComponent(entity, map);
        }
    }
}

public struct PixelConfig
{
    public static PixelConfig Empty => new();
    public PixelType type;
    public PixelType interactionMask;
}

public struct PixelConfigMap : IComponentData
{
    public NativeHashMap<int, PixelConfig> configs;
    public PixelConfig GetConfig(PixelType type)
    {
        if (configs.TryGetValue((int)type, out var config))
            return config;
        else
        {
            Debug.LogError("no compatible type in configMap getConig");
            return PixelConfig.Empty;
        }        
    }
}
