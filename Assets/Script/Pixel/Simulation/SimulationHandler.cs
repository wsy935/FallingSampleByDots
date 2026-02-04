using Unity.Burst;
using Unity.Collections;

using Random = Unity.Mathematics.Random;
namespace Pixel
{
    [BurstCompile]
    public struct SimulationHandler
    {
        public PixelConfigLookup pixelConfigLookup;
        public NativeArray<PixelData> buffer;
        public WorldConfig worldConfig;
        public Random random;
        public uint frameIdx;

        [BurstCompile]
        public void HandleMove(int x, int y)
        {
            if (!worldConfig.IsInWorld(x, y)) return;
            
            int idx = worldConfig.CoordsToIdx(x,y);
            var type = buffer[idx].type;
            var config = pixelConfigLookup.GetConfig(type);
            if (config.moveFlag == MoveFlag.Nothing) return;

            //上下移动
            if ((config.moveFlag & MoveFlag.Down) != 0 && TryMove(x, y, x, y - 1))
                return;
            if ((config.moveFlag & MoveFlag.Up) != 0 && TryMove(x, y, x, y + 1, false))
                return;
            int dir = random.NextBool() ? 1 : -1;
            //斜向移动
            if ((config.moveFlag & MoveFlag.DownDiagonal) != 0 && TryMove(x, y, x + dir, y - 1))
                return;
            if ((config.moveFlag & MoveFlag.UpDiagonal) != 0 && TryMove(x, y, x + dir, y + 1,false))
                return;
            //水平移动
            if ((config.moveFlag & MoveFlag.Horizontal) != 0 && TryMove(x, y, x + dir, y))
                return;
        }

        /// <summary>
        /// 尝试移动or交换像素,op控制比较方式true，density大于才交换；false,小于才交换 
        /// </summary>
        [BurstCompile]
        private bool TryMove(int srcX, int srcY, int tarX, int tarY, bool op = true)
        {
            if (!worldConfig.IsInWorld(tarX, tarY)) return false;

            int srcIdx = worldConfig.CoordsToIdx(srcX, srcY);
            var srcConfig = pixelConfigLookup.GetConfig(buffer[srcIdx].type);
            int tarIdx = worldConfig.CoordsToIdx(tarX, tarY);
            if (buffer[tarIdx].frameIdx == frameIdx && buffer[tarIdx].type != PixelType.Empty)
                return false;
            var tarConfig = pixelConfigLookup.GetConfig(buffer[tarIdx].type);

            if (op ? srcConfig.density > tarConfig.density : srcConfig.density < tarConfig.density)
            {
                (buffer[srcIdx], buffer[tarIdx]) = (buffer[tarIdx], buffer[srcIdx]);
                buffer[srcIdx] = new()
                {
                    type = buffer[srcIdx].type,
                    frameIdx = frameIdx
                };
                buffer[tarIdx] = new()
                {
                    type = buffer[tarIdx].type,
                    frameIdx = frameIdx
                };
                return true;
            }
            return false;
        }        
    }
}
