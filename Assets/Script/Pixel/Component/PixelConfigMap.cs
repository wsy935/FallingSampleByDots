using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Pixel
{        
    public struct PixelConfigLookup : IComponentData,IDisposable
    {
        private NativeArray<PixelConfig> configs;

        public PixelConfigLookup(int size,Allocator allocator)
        {
            configs = new(size, allocator);
        }

        public void AddConfig(PixelType type, PixelConfig config)
        {
            //-1 偏移Nothing
            int key = (int)type-1;
            if (key >= configs.Length)
            {
                throw new IndexOutOfRangeException($"[PixelConfigLookup] not compatible Type in configs with {type}");
            }
            configs[key] = config;
        }
                
        public PixelConfig GetConfig(PixelType type)
        {
            int key = (int)type-1;
            return configs[key];
        }

        public void Dispose()
        {
            configs.Dispose();
        }
    }
}