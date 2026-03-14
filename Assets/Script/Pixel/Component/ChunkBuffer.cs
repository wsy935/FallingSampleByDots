using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Pixel
{
    public struct Chunk : IBufferElementData
    {
        public int2 pos;
        public bool isBlack;
        //检测垂直落体时的变化
        public bool isPhase1Change;
        //检测斜向和水平运动时的变化
        public bool isPhase2Change;        
        public int forceDiryFrame;
        //上一帧或者当前帧被标记则强制为脏
        public readonly bool IsDirty(int frameCount) => (frameCount - forceDiryFrame <= 1) || MoveChange;
        public readonly bool MoveChange => isPhase2Change || isPhase1Change;        
    }
}