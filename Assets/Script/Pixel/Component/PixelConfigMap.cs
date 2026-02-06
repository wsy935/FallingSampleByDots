using System;
using Unity.Collections;
using Unity.Entities;

namespace Pixel
{        
    public struct PixelConfigLookup : IComponentData,IDisposable
    {
        private NativeArray<PixelConfig> configs;

        public PixelConfigLookup(Allocator allocator)
        {
            int size = Enum.GetValues(typeof(PixelType)).Length;
            configs = new(size, allocator);
        }

        public void AddConfig(PixelType type, PixelConfig config)
        {            
            int key = (int)type;            
            configs[key] = config;
        }
                
        public PixelConfig GetConfig(PixelType type)
        {
            int key = (int)type;
            return configs[key];
        }

        public void Dispose()
        {
            configs.Dispose();
        }
    }
}