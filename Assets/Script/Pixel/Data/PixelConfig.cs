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
        public Color32 color;
    }    
}
