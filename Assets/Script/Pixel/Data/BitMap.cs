using System;
using Unity.Burst;
using Unity.Collections;

namespace Pixel
{
    /// <summary>
    /// 用于追踪当前step中已成功移动的像素，防止被重复处理
    /// 使用位图存储，1bit/pixel，内存占用小
    /// </summary>
    [BurstCompile]
    public struct BitMap : IDisposable
    {
        private NativeArray<uint> bits;
        private int width;
        private int height;

        public BitMap(int width, int height, Allocator allocator)
        {
            this.width = width;
            this.height = height;
            // 每个像素1bit，计算需要的uint数量 (32bit per uint)
            int totalBits = width * height;
            int uintCount = (totalBits + 31) / 32;
            bits = new NativeArray<uint>(uintCount, allocator);
        }

        /// <summary>
        /// 清空所有标记
        /// </summary>
        
        public void Clear()
        {
            for (int i = 0; i < bits.Length; i++)
            {
                bits[i] = 0;
            }
        }

        /// <summary>
        /// 检查是否已被标记
        /// </summary>
        
        public bool IsMark(int x, int y)
        {
            int idx = y * width + x;
            int bitIdx = idx >> 5;      // /32
            int bitOffset = idx & 31;   // %32
            return (bits[bitIdx] & (1u << bitOffset)) != 0;
        }

        /// <summary>
        /// 标记
        /// </summary>       
        
        public void Mark(int x, int y)
        {
            int idx = y * width + x;
            int bitIdx = idx >> 5;
            int bitOffset = idx & 31;
            bits[bitIdx] |= (1u << bitOffset);
        }

        public void Dispose()
        {
            if (bits.IsCreated)
                bits.Dispose();
        }
    }
}
