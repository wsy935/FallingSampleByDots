using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Pixel
{    
    public struct PixelConfigMap : IComponentData,IDisposable
    {
        private NativeArray<PixelConfig> configs;

        public PixelConfigMap(int size)
        {
            configs = new(size, Allocator.Persistent);
        }
 
        public void AddConfig(PixelType type, PixelConfig config)
        {
            int key = GetKey(type);
            configs[key] = config;
        }

        private int GetKey(PixelType pixelType)
        {
            // -1 以偏移掉Disable
            return math.tzcnt((int)pixelType) - 1;
        }
        
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