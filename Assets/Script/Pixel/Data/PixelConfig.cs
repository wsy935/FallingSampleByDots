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
        public bool isStatic;
        public float density;
        [Tooltip("模拟时的速度,x为水平方向,y为垂直方向")]
        public int2 speed;

        [Header("渲染")]
        public Color32 color;
    }
}
