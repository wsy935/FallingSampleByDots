using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Pixel
{    
    public struct WorldConfig :  IComponentData
    {
        public int Width;
        public int Height;        
    }    
}