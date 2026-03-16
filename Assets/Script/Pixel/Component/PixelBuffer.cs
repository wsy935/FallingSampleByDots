using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Pixel
{
    //NativeArray是值类型，修改时需要重新赋值，所以放弃使用二维数组
    public struct PixelData : IBufferElementData
    {
        public PixelType type;
        public int frameIdx;
        public float temperature;
        public float survivalTime;

        public static PixelData NewPixel(PixelType type,in PixelConfig config)
        {            
            return new PixelData
            {
                type = type,
                frameIdx = 0,
                temperature = config.tempConfig.baseTemp,
                survivalTime = 0
            };
        }
    }
}