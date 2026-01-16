using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Pixel
{
    public static class Util
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CoordToIndex(int x, int y, int2 size)
        {
            if (x < 0 && x >= size.x || y < 0 && y >= size.y)
            {
                throw new IndexOutOfRangeException();
            }
            return y * size.x + x;            
        }
    }
}       