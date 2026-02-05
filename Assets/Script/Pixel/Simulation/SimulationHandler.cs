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

        [BurstCompile]
        public void HandleMove(int x, int y)
        {
            if (!worldConfig.IsInWorld(x, y)) return;

            int idx = worldConfig.CoordsToIdx(x, y);
            var config = pixelConfigLookup.GetConfig(buffer[idx].type);
            if (config.moveFlag == MoveFlag.Nothing) return;

            //上下移动            
            if ((config.moveFlag & MoveFlag.Down) != 0 && VerticalMoveWithSpeed(x, y, config, MoveFlag.Down))
                return;
            if ((config.moveFlag & MoveFlag.Up) != 0 && VerticalMoveWithSpeed(x, y, config, MoveFlag.Up))
                return;

            //斜向移动
            int offset = random.NextBool() ? 1 : -1;
            if ((config.moveFlag & MoveFlag.DownDiagonal) != 0 && TryMove(x, y, x + offset, y - 1))
                return;
            //DiagonalMoveWithSpeed( x, y, config, offset, MoveFlag.DownDiagonal)
            if ((config.moveFlag & MoveFlag.UpDiagonal) != 0 && TryMove(x, y, x + offset, y + 1))
                return;

            //水平移动
            if ((config.moveFlag & MoveFlag.Horizontal) != 0 && HorizontalMoveWithSpeed(x, y, config, offset))
                return;                    
        }

        [BurstCompile]
        private bool VerticalMoveWithSpeed(int x, int y, in PixelConfig srcConfig, MoveFlag moveFlag)
        {
            bool hasMove = false;
            int speed = srcConfig.speed.y;
            bool isDown = moveFlag == MoveFlag.Down;
            int tarX = x, tarY;
            for (int i = 0; i < speed; i++)
            {
                tarY = isDown ? y - 1 : y + 1;
                if (!TryMove(x, y, tarX, tarY))
                    break;

                y = tarY;
                hasMove = true;
            }
            return hasMove;
        }

        [BurstCompile]
        private bool DiagonalMoveWithSpeed(int x, int y, in PixelConfig srcConfig, int offset, MoveFlag moveFlag)
        {
            int speed = (srcConfig.speed.x + srcConfig.speed.y) >> 1;
            bool isDown = moveFlag == MoveFlag.DownDiagonal;
            int tarOffset = isDown ? -1 : 1;
            int baseOffset = isDown ? -2 : 2;
            bool hasMove = false;
            for (int i = 0; i < speed; i++)
            {
                int2 target = new(x + offset, y + tarOffset);
                int2 basePos = new(x + offset, y + baseOffset);
                PixelData basePixel = GetPixelData(basePos.x, basePos.y);

                if (!TryMove(x, y, target.x, target.y))
                    break;

                if (basePixel.type == PixelType.Empty || basePixel.type == PixelType.Disable)
                    break;

                var baseConfig = pixelConfigLookup.GetConfig(basePixel.type);
                bool densityCheck = isDown
                    ? srcConfig.density <= baseConfig.density
                    : srcConfig.density >= baseConfig.density;
                if (!densityCheck)
                    break;

                x = target.x;
                y = target.y;
                hasMove = true;
            }
            return hasMove;
        }

        [BurstCompile]
        private bool HorizontalMoveWithSpeed(int x, int y, in PixelConfig srcConfig, int offset)
        {
            bool hasMove = false;
            for (int i = 0; i < srcConfig.speed.x; i++)
            {
                (int tarX, int tarY) = (x + offset, y);

                PixelData basePixel = GetPixelData(tarX, tarY - 1);
                bool canMove = basePixel.type != PixelType.Disable && basePixel.type != PixelType.Empty;
                if (canMove)
                {
                    var basePixelConfig = pixelConfigLookup.GetConfig(basePixel.type);
                    if (basePixelConfig.density > srcConfig.density && TryMove(x, y, tarX, tarY))
                    {
                        (x, y) = (tarX, tarY);
                        hasMove = true;
                    }
                    else
                        break;
                }
                else
                    break;
            }
            return hasMove;
        }

        /// <summary>
        /// 尝试移动像素,固体或液体密度大于目标才交换，气体密度小于才交换，且气体只能与空or气体发生交换
        /// </summary>
        [BurstCompile]
        private bool TryMove(int srcX, int srcY, int tarX, int tarY)
        {
            if (!worldConfig.IsInWorld(tarX, tarY)) return false;

            int srcIdx = worldConfig.CoordsToIdx(srcX, srcY);
            int tarIdx = worldConfig.CoordsToIdx(tarX, tarY);
            PixelData srcPixelData = buffer[srcIdx],
                tarPixelData = buffer[tarIdx];
            if (tarPixelData.frameIdx == frameIdx && tarPixelData.type != PixelType.Empty)
                return false;

            var srcConfig = pixelConfigLookup.GetConfig(srcPixelData.type);
            var tarConfig = pixelConfigLookup.GetConfig(tarPixelData.type);

            bool canMove = srcConfig.matType == MaterialType.Gas
                ? (tarConfig.matType == MaterialType.Gas && srcConfig.density < tarConfig.density) || tarPixelData.type == PixelType.Empty
                : srcConfig.density > tarConfig.density;

            if (!canMove)
                return false;

            tarPixelData.frameIdx = frameIdx;
            srcPixelData.frameIdx = frameIdx;
            (buffer[srcIdx], buffer[tarIdx]) = (tarPixelData, srcPixelData);

            return true;
        }
    }
}
