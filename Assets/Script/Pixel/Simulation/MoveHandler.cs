using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;
namespace Pixel
{
    [BurstCompile]
    public struct MoveHandler
    {
        public PixelConfigLookup pixelConfigLookup;
        public DynamicBuffer<PixelData> buffer;
        public DynamicBuffer<Chunk> chunks;
        public WorldConfig worldConfig;
        public int frameCount;
        public Random random;


        /// <summary>
        /// 纯检测：判断源像素是否可以移动到 tar 位置
        /// </summary>
        private bool CanMoveTo(in PixelConfig srcConfig, int tarX, int tarY)
        {
            if (!worldConfig.IsInWorld(tarX, tarY)) return false;

            int tarIdx = worldConfig.CoordsToIdx(tarX, tarY);
            PixelData tarPixel = buffer[tarIdx];

            if (tarPixel.type == PixelType.Empty)
                return true;
            
            var tarConfig = pixelConfigLookup.GetConfig(tarPixel.type);
            if (tarConfig.isStatic)
                return false;
            bool canMove = srcConfig.matType switch
            {
                MaterialType.Gas => srcConfig.density < tarConfig.density,
                MaterialType.Solid => tarConfig.matType != MaterialType.Solid &&
                                tarConfig.density < srcConfig.density,
                MaterialType.Liquid => tarConfig.density < srcConfig.density,
                _ => false
            };

            return canMove;
        }

        /// <summary>
        /// 尝试移动：合并可移动性检测与执行交换，消除重复的索引计算和缓冲区读取。
        /// 返回 true 表示移动成功，false 表示无法移动。
        /// </summary>
        private bool TryMove(in PixelConfig srcConfig, int srcX, int srcY, int tarX, int tarY)
        {
            bool canMove = CanMoveTo(srcConfig, tarX, tarY);
            if (!canMove) return false;

            int srcIdx = worldConfig.CoordsToIdx(srcX, srcY);
            int tarIdx = worldConfig.CoordsToIdx(tarX, tarY);
            PixelData srcPixel = buffer[srcIdx];
            PixelData tarPixel = buffer[tarIdx];

            srcPixel.frameIdx = frameCount;
            tarPixel.frameIdx = frameCount;

            buffer[srcIdx] = tarPixel;
            buffer[tarIdx] = srcPixel;

            return true;
        }

        /// <summary>
        /// 垂直逐步移动：从 (x,y) 出发，每一步与途中粒子交换，返回最终位置。
        /// </summary>
        private int2 MoveVertical_Impl(int x, int y, in PixelConfig srcConfig)
        {
            int speed = srcConfig.speed.y;
            int yDir = srcConfig.matType != MaterialType.Gas ? -1 : 1;
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
        /// 对角逐步移动：每步 x 移动 1 格，y 移动1格，模拟加速。
        /// 每步先检查水平基础点可达性（仅验证，不移动），然后逐格交换。
        /// 第一格是斜向移动（x+offset, y+yDir），后续格是纯垂直移动。
        /// </summary>
        private int2 MoveDiagonal_Impl(int x, int y, in PixelConfig srcConfig, int offset)
        {
            int speed = (srcConfig.speed.x + srcConfig.speed.y) >> 1;
            int yDir = srcConfig.matType != MaterialType.Gas ? -1 : 1;
            int curX = x, curY = y;

            for (int step = 0; step < speed; step++)
            {
                int nextX = curX + offset;
                int nextY = curY + yDir;

                //检查当前位置的基础点是否有支撑
                if (CanMoveTo(srcConfig, curX, nextY))
                    break;

                // 检查水平基础点 (targetX, curY) 是否可达
                if (!CanMoveTo(in srcConfig, nextX, curY))
                    break;

                if (!TryMove(in srcConfig, curX, curY, nextX, nextY))
                    break;

                curY = nextY;
                curX = nextX;
            }

            return new int2(curX, curY);
        }

        /// <summary>
        /// 水平逐步移动：每一步检查支撑并与途中粒子交换，返回最终位置。
        /// </summary>

        private int2 MoveHorizontal_Impl(int x, int y, in PixelConfig srcConfig, int offset)
        {
            int curX = x;
            int yDir = srcConfig.matType == MaterialType.Gas ? 1 : -1;
            for (int i = 0; i < srcConfig.speed.x; i++)
            {
                int nextX = curX + offset;

                // 尝试移动到目标位置
                if (!TryMove(in srcConfig, curX, y, nextX, y))
                    break;
                // 在移动到新位置后，检查是否可以垂直移动
                else if (TryMove(srcConfig, nextX, y, nextX, y + yDir))
                {
                    y = y + yDir;
                    break;
                }
                curX = nextX;
            }

            return new int2(curX, y);
        }

        public int2 MoveVertical(int x, int y, in PixelConfig config)
        {
            int2 origin = new(x, y);
            int2 target;

            target = MoveVertical_Impl(x, y, config);
            if (math.any(target != origin))
                return target;

            return origin;
        }

        public int2 MoveDiagonal(int x, int y, in PixelConfig config)
        {
            int2 origin = new(x, y);
            int2 target;
            int offset = random.NextBool() ? 1 : -1;

            target = MoveDiagonal_Impl(x, y, config, offset);
            if (math.any(target != origin))
                return target;
            else
            {
                target = MoveDiagonal_Impl(x, y, config, -offset);
                if (math.any(target != origin))
                    return target;
            }

            return origin;
        }

        //设置了水平速度的可以水平移动
        public int2 MoveHorizontal(int x, int y, in PixelConfig config)
        {
            int2 origin = new(x, y);
            int2 target;
            int offset = random.NextBool() ? 1 : -1;
            target = MoveHorizontal_Impl(x, y, config, offset);
            if (math.any(target != origin))
                return target;

            return origin;
        }
    }
}
