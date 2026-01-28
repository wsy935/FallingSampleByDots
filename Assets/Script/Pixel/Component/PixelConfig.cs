using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Pixel
{    
    public struct PixelConfig
    {
        public PixelType type;
        public MaterialType matType;
        public PixelType interactionMask;
        public Color32 color;
        public FunctionPointer<SimulationHandler> handler;

        public PixelConfig(PixelType type, PixelType interactionMask,MaterialType materialType,Color32 color,FunctionPointer<SimulationHandler> handler)
        {
            this.type = type;
            this.matType = materialType;
            this.color = color;
            this.interactionMask = interactionMask;
            this.handler = handler;
        }
    }

    [BurstCompile]
    public struct PixelConfigMap : IComponentData,IDisposable
    {
        private NativeArray<PixelConfig> configs;

        public PixelConfigMap(int size)
        {
            configs = new(size, Allocator.Persistent);
        }

        [BurstCompile]
        public void AddConfig(PixelType type, PixelConfig config)
        {
            int key = GetKey(type);
            configs[key] = config;
        }

        [BurstCompile]
        private int GetKey(PixelType pixelType)
        {
            // -1 以偏移掉Disable
            return math.tzcnt((int)pixelType) - 1;
        }
        
        [BurstCompile]
        public PixelConfig GetConfig(PixelType type)
        {
            int key = GetKey(type);
            return configs[key];
        }

        public void Dispose()
        {
            configs.Dispose();
        }
    }
}
