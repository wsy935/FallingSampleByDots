using System;
using Unity.Mathematics;
using UnityEngine;

namespace Pixel
{        
    [Serializable]
    public struct PixelConfig
    {
        public PixelType type;
        public MaterialType matType;
        public MoveFlag moveFlag;
        public InteractionFlag interactionFlag;
        public float density;
        [Tooltip("模拟时的速度,x为水平方向,y为垂直方向")]
        public int2 speed;
        
        [Header("渲染")]
        public Color32 color;

        [Header("噪声 - 低频(结构)")]
        [Range(0, 1)] public float noiseLowIntensity;

        [Header("噪声 - 中频(纹理)")]
        [Range(0, 1)] public float noiseMidIntensity;

        [Header("噪声 - 高频(脏污)")]
        [Range(0, 1)] public float noiseHighIntensity;
    }    
}
