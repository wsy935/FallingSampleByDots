using System;

namespace Pixel
{    
    public enum PixelType
    {
        // 不可用
        Disable = 1 << 0,
        Empty = 1 << 1,
        Sand = 1 << 2,
        Water = 1 << 3,
        Wall = 1 << 4,        
    }

    public enum MaterialType
    {
        Nothing = 0,
        Solid = 1,
        Liquid = 2,
        Gas = 3
    }

    [Flags]
    public enum MoveFlag : byte
    {
        None = 1 << 0,
        //下移
        Down = 1 << 1,
        //上浮
        Up = 1 << 2,
        //水平扩散
        Horizontal = 1 << 3,
        //对角线移动
        Diagonal = 1 << 4,
        DownDiagonal = Down & Diagonal,
        UpDiagonal = Up & Diagonal
    }

    [Flags]
    public enum InteractionFlag : byte
    {
        None = 1 << 0,
        Hot = 1 << 1,
        Cold = 1 << 2,
    }
}