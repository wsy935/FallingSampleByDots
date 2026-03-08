using System;
using Unity.Mathematics;

namespace Pixel
{
    public enum PixelType : byte
    {
        // 不可用
        Disable = 0,
        Empty = 1,
        Sand = 2,
        Water = 3,
        Wall = 4
    }

    public enum MaterialType
    {
        Nothing = 0,
        Solid = 1,
        Liquid = 2,
        Gas = 3
    }
}