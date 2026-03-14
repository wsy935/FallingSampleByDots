using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Pixel
{
    [Serializable]
    public struct PixelConfig
    {
        public PixelType type;
        public MaterialType matType;
        public bool isStatic;
        public float density;
        [Tooltip("模拟时的速度,x为水平方向,y为垂直方向")]
        public int2 speed;

        [Header("渲染")]
        public Color32 color;
        [Tooltip("温度配置")]
        public TempreatureConfig tempConfig;
        [NonSerialized]
        public int reactionRuleOffset,
            reactionRuleCount;
    }    

    [Serializable]
    public struct TempreatureConfig
    {
        public float baseTemp;
        public float maxTemp;
        public float rate;

        public static TempreatureConfig Default = new()
        {
            baseTemp = 23,
            rate = 0.5f,
            maxTemp = 1
        };

        public readonly bool IsSet => rate != 0 || baseTemp != 0 || maxTemp != 0;
    }
}
