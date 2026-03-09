using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Pixel
{
    public struct Chunk : IBufferElementData
    {
        public int2 pos;
        public bool isVerticalChange;
        public bool isDiagonalChange;
        public bool isHorizontalChange;
        public int forceDiryFrame;
        //上一帧或者当前帧被标记为脏
        public bool ForceDiry(int frameCount) => frameCount - forceDiryFrame <= 1;
        public bool IsDirty(int frameCount) => ForceDiry(frameCount) || MoveChange;
        public bool MoveChange => isDiagonalChange || isHorizontalChange || isVerticalChange;
        public bool isBlack;
    }
}