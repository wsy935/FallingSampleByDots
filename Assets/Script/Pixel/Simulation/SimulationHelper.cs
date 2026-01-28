using Unity.Burst;
using Unity.Mathematics;

namespace Pixel
{
    [BurstCompile]
    public struct SimulationHelper
    {
        public SimulationContext ctx;        

        [BurstCompile]
        public int2 GetWorldIdx(int x, int y, int2 chunkPos) => ctx.worldConfig.CoordsToWorldIdx(x, y, chunkPos);

        [BurstCompile]
        public PixelType GetPixelTypeByWorldIdx(int x, int y)
        {            
            int2 chunkPos = ctx.worldConfig.WorldIdxToChunkPos(x, y);
            int2 localPos = ctx.worldConfig.WorldIdxToLocalPos(x, y);
            return GetPixelType(localPos, chunkPos);            
        }

        [BurstCompile]
        public PixelType GetPixelType(int2 localPos,int2 chunkPos)
        {
            if (!ctx.IsInBounds(localPos.x, localPos.y) || !ctx.IsChunkInWorld(chunkPos)) return PixelType.Disable;

            int chunkIdx = ctx.worldConfig.CoordsToChunkIdx(chunkPos.x, chunkPos.y);
            var entity = ctx.chunkEntities[chunkIdx];
            if (ctx.bufferLookup.TryGetBuffer(entity, out var bufferData))
            {
                int idx = ctx.GetIndex(localPos.x,localPos.y);
                return bufferData[idx].type;
            }
            return PixelType.Disable;
        }

        [BurstCompile]
        public void SwapByWorldIdx(int x1, int y1, int x2, int y2)
        {
            if (!IsPixelInWorld(x1, y1) || !IsPixelInWorld(x2, y2)) return;
            
        }

        [BurstCompile]
        public void SetPixelByWorldIdx(int x,int y)
        {
            
        }

        [BurstCompile]
        private bool IsPixelInWorld(int x, int y)
        {
            return x >= 0 && x < ctx.worldConfig.width &&
                y >= 0 && y < ctx.worldConfig.height;
        }

        [BurstCompile]
        private bool CanInteract(int2 localPos,int2 chunkPos)
        {            
            var targetPixel = GetPixelType(localPos, chunkPos);
            if (targetPixel == PixelType.Disable) return false;
            return (ctx.currentPixelConfig.interactionMask & targetPixel) != 0;
        }
        /// <summary>
        /// 
        /// </summary>        
        [BurstCompile]
        public void TryMoveToEdge(int x, int y, int2 chunkPos)
        {
            int dir = ctx.random.NextBool() ? -1 : 1;
            int2 tChunkPos = chunkPos;
            int2 tLocalPos = new(x + dir, y);
            do
            {
                if (!ctx.IsInBounds(tLocalPos.x, tLocalPos.y))
                {
                    tChunkPos = ctx.GetChunk(tLocalPos.x, tLocalPos.y, tChunkPos);
                    tLocalPos = ctx.GetNeighbourLocalPos(tLocalPos.x, tLocalPos.y);
                }
                if (!ctx.IsChunkInWorld(tChunkPos))
                    break;
                if ()
            }
            while (true);
        }
        [BurstCompile]
        public bool CanMove(int2 tLocalPos,int2 tChunkPos)
        {
            if (!CanInteract(tLocalPos, tChunkPos)) return false;
            var downPixelType = GetPixelType(tLocalPos, tChunkPos);            
        }
    }
}