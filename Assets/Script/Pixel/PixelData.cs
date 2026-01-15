using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Flags]
public enum PixelType : int
{
    Empty = 1 << 0,
    Sand = 1 << 1,
    Water = 1 << 2,
    Wall = 1 << 3
}

public partial struct PixelChunk : IComponentData
{
    public int2 size;
    public int2 pos;
    public int borderWidth;
    public bool isDirty;
}

public partial struct PixelBuffer : IBufferElementData
{
    public PixelType type;
}