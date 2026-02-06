using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
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

        #region 基础工具方法

        [BurstCompile]
        private PixelData GetPixelData(int x, int y)
        {
            int idx = worldConfig.CoordsToIdx(x, y);
            if (idx >= 0 && idx < buffer.Length)
            {
                return buffer[idx];
            }
            else
            {
                return new() { type = PixelType.Disable };
            }
        }

        /// <summary>
        /// 纯检测：判断给定配置的源像素是否可以移动到 tar 位置（用于仅需验证不执行移动的场景）
        /// </summary>
        [BurstCompile]
        private bool CanMoveTo(in PixelConfig srcConfig, int tarX, int tarY)
        {
            if (!worldConfig.IsInWorld(tarX, tarY)) return false;

            int tarIdx = worldConfig.CoordsToIdx(tarX, tarY);
            PixelData tarPixelData = buffer[tarIdx];

            if (tarPixelData.frameIdx == frameIdx && tarPixelData.type != PixelType.Empty)
                return false;

            var tarConfig = pixelConfigLookup.GetConfig(tarPixelData.type);

            bool canMove = srcConfig.matType == MaterialType.Gas
                ? (tarConfig.matType == MaterialType.Gas && srcConfig.density < tarConfig.density) || tarPixelData.type == PixelType.Empty
                : srcConfig.density > tarConfig.density;

            return canMove;
        }

        /// <summary>
        /// 尝试移动：合并可移动性检测与执行交换，消除重复的索引计算和缓冲区读取。
        /// 返回 true 表示移动成功，false 表示无法移动。
        /// </summary>
        [BurstCompile]
        private bool TryMove(in PixelConfig srcConfig, int srcX, int srcY, int tarX, int tarY)
        {
            if (!worldConfig.IsInWorld(tarX, tarY)) return false;

            int tarIdx = worldConfig.CoordsToIdx(tarX, tarY);
            PixelData tarPixelData = buffer[tarIdx];

            if (tarPixelData.frameIdx == frameIdx && tarPixelData.type != PixelType.Empty)
                return false;

            var tarConfig = pixelConfigLookup.GetConfig(tarPixelData.type);

            bool canMove = srcConfig.matType == MaterialType.Gas
                ? (tarConfig.matType == MaterialType.Gas && srcConfig.density < tarConfig.density) || tarPixelData.type == PixelType.Empty
                : srcConfig.density > tarConfig.density;

            if (!canMove) return false;

            int srcIdx = worldConfig.CoordsToIdx(srcX, srcY);
            PixelData srcPixelData = buffer[srcIdx];

            tarPixelData.frameIdx = frameIdx;
            srcPixelData.frameIdx = frameIdx;
            (buffer[srcIdx], buffer[tarIdx]) = (tarPixelData, srcPixelData);

            return true;
        }

        #endregion

        #region 逐步移动方法

        /// <summary>
        /// 垂直逐步移动：从 (x,y) 出发，每一步与途中粒子交换，返回最终位置。
        /// </summary>
        [BurstCompile]
        private int2 MoveVertical(int x, int y, in PixelConfig srcConfig, bool isDown)
        {
            int speed = srcConfig.speed.y;
            int yDir = isDown ? -1 : 1;
            int curY = y;

            for (int i = 0; i < speed; i++)
            {
                int nextY = curY + yDir;
                if (!TryMove(in srcConfig, x, curY, x, nextY))
                    break;
                curY = nextY;
            }

            return new int2(x, curY);
        }

        /// <summary>
        /// 对角逐步移动：每步 x 移动 1 格，y 移动距离递增（1,2,3...），模拟加速。
        /// 每步先检查水平基础点可达性（仅验证，不移动），然后逐格交换。
        /// 第一格是斜向移动（x+offset, y+yDir），后续格是纯垂直移动。
        /// </summary>
        [BurstCompile]
        private int2 MoveDiagonal(int x, int y, in PixelConfig srcConfig, int offset, bool isDown)
        {
            int speed = (srcConfig.speed.x + srcConfig.speed.y) >> 1;
            int yDir = isDown ? -1 : 1;
            int curX = x, curY = y;

            for (int step = 0; step < speed; step++)
            {
                // 每步 y 轴移动距离递增
                int yDistance = step + 1;
                int targetX = curX + offset;

                // 检查水平基础点 (targetX, curY) 是否可达
                if (!CanMoveTo(in srcConfig, targetX, curY))
                    break;
                
                // 在偏移列上纯垂直逐步移动
                bool stepComplete = true;
                for (int j = 0; j < yDistance; j++)
                {
                    int nextY = curY + yDir;
                    if (!TryMove(in srcConfig, curX, curY, targetX, nextY))
                    {
                        stepComplete = false;
                        break;
                    }
                    curY = nextY;
                }

                // 如果垂直部分未完成，停止整个斜向移动
                if (!stepComplete)
                    break;     

                curX = targetX;                         
            }

            return new int2(curX, curY);
        }

        /// <summary>
        /// 水平逐步移动：每一步检查支撑并与途中粒子交换，返回最终位置。
        /// </summary>
        [BurstCompile]
        private int2 MoveHorizontal(int x, int y, in PixelConfig srcConfig, int offset)
        {
            int curX = x;
            for (int i = 0; i < srcConfig.speed.x; i++)
            {
                int nextX = curX + offset;

                // 检查目标位置下方是否有支撑
                PixelData basePixel = GetPixelData(nextX, y - 1);
                if (basePixel.type == PixelType.Disable || basePixel.type == PixelType.Empty)
                    break;

                var basePixelConfig = pixelConfigLookup.GetConfig(basePixel.type);
                if (basePixelConfig.density < srcConfig.density)
                    break;

                // 尝试移动到目标位置
                if (!TryMove(in srcConfig, curX, y, nextX, y))
                    break;

                curX = nextX;
            }

            return new int2(curX, y);
        }

        #endregion

        #region 主移动逻辑

        /// <summary>
        /// 返回移动后的位置
        /// </summary>
        [BurstCompile]
        public int2 HandleMove(int x, int y, in PixelConfig config)
        {
            int2 origin = new(x, y);
            int2 target;

            // 垂直移动（优先级最高）
            if ((config.moveFlag & MoveFlag.Down) == MoveFlag.Down)
            {
                target = MoveVertical(x, y, config, isDown: true);
                if (math.any(target != origin))
                    return target;
            }
            if ((config.moveFlag & MoveFlag.Up) == MoveFlag.Down)
            {
                target = MoveVertical(x, y, config, isDown: false);
                if (math.any(target != origin))
                    return target;
            }

            // 对角移动
            int offset = random.NextBool() ? 1 : -1;
            if ((config.moveFlag & MoveFlag.DownDiagonal) == MoveFlag.DownDiagonal)
            {
                target = MoveDiagonal(x, y, config, offset, isDown: true);
                if (math.any(target != origin))
                    return target;
            }
            if ((config.moveFlag & MoveFlag.UpDiagonal) == MoveFlag.UpDiagonal)
            {
                target = MoveDiagonal(x, y, config, offset, isDown: false);
                if (math.any(target != origin))
                    return target;
            }

            // 水平移动（优先级最低）
            if ((config.moveFlag & MoveFlag.Horizontal) == MoveFlag.Horizontal)
            {
                target = MoveHorizontal(x, y, config, offset);
                if (math.any(target != origin))
                    return target;
            }

            return origin;
        }

        #endregion
    }
}
