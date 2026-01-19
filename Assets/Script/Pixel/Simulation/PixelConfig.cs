using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Pixel
{
    public struct PixelConfig
    {
        public static PixelConfig Empty => new();
        public PixelType type;
        public PixelType interactionMask;
        public FunctionPointer<SimulationHandler> handler;

        public PixelConfig(PixelType type, PixelType interactionMask, FunctionPointer<SimulationHandler> handler)
        {
            this.type = type;
            this.interactionMask = interactionMask;
            this.handler = handler;
        }
    }

    public struct PixelConfigMap : IDisposable
    {
        private readonly NativeHashMap<int, PixelConfig> configs;
        public bool isCreated;
        
        public PixelConfigMap(int length)
        {
            configs = new(length, Allocator.Persistent);
            isCreated = true;
        }

        public void AddConfig(PixelType type, PixelConfig config)
        {
            int key = (int)type;
            if (configs.ContainsKey(key))
            {
                Debug.LogError($"there alerady has a key {type} in Addconfig");
            }
            else
            {
                configs.Add(key, config);
            }
        }

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

        public void Dispose()
        {
            if (configs.IsCreated)
            {
                isCreated = false;
                configs.Dispose();
            }                
        }
    }
}