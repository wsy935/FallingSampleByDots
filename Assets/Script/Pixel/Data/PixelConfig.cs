using System;
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
        public Color32 color;
    }    
}
