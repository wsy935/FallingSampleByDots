using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Pixel
{
    /// <summary>
    /// 存储当前Chunk模拟所需使用的上下文,以及相关方法
    /// </summary>
    [BurstCompile]
    public struct SimulationContext
    {
        public DynamicBuffer<PixelBuffer> buffer;
        public ChunkConfig chunkConfig;
        public PixelConfig currentPixelConfig;
        public Random random;

        [BurstCompile]
        public int GetIndex(int x, int y) => x + y * chunkConfig.RealEdge;

        [BurstCompile]
        public PixelBuffer GetPixel(int x, int y)
        {
            return buffer[GetIndex(x, y)];
        }

        [BurstCompile]
        public void SetPixel(int x, int y, PixelBuffer pixel)
        {
            buffer[GetIndex(x, y)] = pixel;
        }

        [BurstCompile]
        public bool TryMoveOrSwap(int x1, int y1, int x2, int y2)
        {
            if (!IsInBounds(x1, y1) || !IsInBounds(x2, y2)) return false;
            if (!CanInteract(x2, y2)) return false;
            Swap(x1, y1, x2, y2);
            return true;
        }

        [BurstCompile]
        public void Swap(int x1, int y1, int x2, int y2)
        {
            int idx1 = GetIndex(x1, y1);
            int idx2 = GetIndex(x2, y2);
            (buffer[idx1], buffer[idx2]) = (buffer[idx2], buffer[idx1]);
        }

        /// <summary>
        /// 禁止移动到Border
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        [BurstCompile]
        public bool IsInBounds(int x, int y)
        {
            return x >= chunkConfig.border && x < chunkConfig.edge + chunkConfig.border &&
                   y >= chunkConfig.border && y < chunkConfig.edge + chunkConfig.border;
        }

        [BurstCompile]
        public bool CanInteract(int x, int y)
        {
            if (!IsInBounds(x, y)) return false;
            var targetPixel = GetPixel(x, y);
            return (currentPixelConfig.interactionMask & targetPixel.type) != 0;
        }
    }

}