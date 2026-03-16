using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

using Random = Unity.Mathematics.Random;
namespace Pixel
{
    public struct ReactionHandler
    {
        public PixelConfigLookup pixelConfigLookup;
        public DynamicBuffer<Chunk> chunks;
        public DynamicBuffer<PixelData> buffer;
        public WorldConfig worldConfig;
        public Random random;
        public int frameCount;

        /// <summary>
        /// 检查周围是否有特定类型的相邻像素
        /// </summary>
        private bool TryTansformAdjacent(int x, int y, in ReactionRule reactionRule)
        {                        
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {                    
                    int checkX = x + j;
                    int checkY = y + i;

                    if ((checkX == x && checkY == y) || !worldConfig.IsInWorld(checkX, checkY))
                        continue;

                    int idx = worldConfig.CoordsToIdx(checkX, checkY);                    
                    if (buffer[idx].type == reactionRule.targetType)
                    {
                        int sourceIdx = worldConfig.CoordsToIdx(x, y);
                        buffer[idx] = PixelData.NewPixel(reactionRule.targetProduct, pixelConfigLookup.GetConfig(reactionRule.targetProduct));
                        buffer[sourceIdx] = PixelData.NewPixel(reactionRule.selfProduct, pixelConfigLookup.GetConfig(reactionRule.selfProduct));
                        
                        int chunkIdx = worldConfig.GetChunkIdxByWorld(checkX, checkY);
                        int sourceChunkIdx = worldConfig.GetChunkIdxByWorld(x, y);
                        
                        var chunk = chunks[chunkIdx];
                        chunk.forceDiryFrame = frameCount;
                        chunks[chunkIdx] = chunk;
                        
                        var sourceChunk = chunks[sourceChunkIdx];
                        sourceChunk.forceDiryFrame = frameCount;
                        chunks[sourceChunkIdx] = sourceChunk;
                        return true;
                    }                        
                }
            }
            return false;
        }

        /// <summary>
        /// 尝试在上方及左右两侧生成像素
        /// </summary>
        public void SpawnAbove(int x, int y, PixelType productType)
        {
            var productConfig = pixelConfigLookup.GetConfig(productType);
            for (int i = 0; i <= 1; i++)
            {
                int spawnY = y + i;
                for (int j = -1; j <= 1; j++)
                {
                    int spawnX = x + j;
                    if (!worldConfig.IsInWorld(spawnX, spawnY) || (spawnX == x && spawnY == y))
                        continue;
                    int spawnIdx = worldConfig.CoordsToIdx(spawnX, spawnY);

                    if (buffer[spawnIdx].type != PixelType.Empty)
                        continue;
                    // 生成新像素
                    buffer[spawnIdx] = PixelData.NewPixel(productType, productConfig);

                    //生成像素的区块设置为脏
                    int chunkIdx = worldConfig.GetChunkIdxByWorld(spawnX, spawnY);
                    var chunk = chunks[chunkIdx];
                    chunk.forceDiryFrame = frameCount;
                    chunks[chunkIdx] = chunk;
                }
            }
        }

        /// <summary>
        /// 将当前像素转换为产物
        /// </summary>
        public void TransformSelfToProduct(int x, int y, PixelType productType)
        {
            int idx = worldConfig.CoordsToIdx(x, y);
            buffer[idx] = PixelData.NewPixel(productType, pixelConfigLookup.GetConfig(productType));

            int chunkIdx = worldConfig.GetChunkIdxByWorld(x, y);
            var chunk = chunks[chunkIdx];
            chunk.forceDiryFrame = frameCount;
            chunks[chunkIdx] = chunk;
        }

        public void ProcessReaction(int x, int y)
        {
            int idx = worldConfig.CoordsToIdx(x, y);
            var pixel = buffer[idx];

            var config = pixelConfigLookup.GetConfig(pixel.type);
            // 遍历所有反应规则
            if (config.reactionRuleCount <= 0)
                return;

            for (int i = 0; i < config.reactionRuleCount; i++)
            {
                int reactionIdx = config.reactionRuleOffset + i;
                var rule = pixelConfigLookup.GetReactionRule(reactionIdx);
                float threshold = random.NextFloat(rule.min, rule.max);
                bool triggerReaction = rule.type switch
                {                    
                    ReactionType.ContactWith => TryTansformAdjacent(x, y, rule),
                    ReactionType.LifetimeExpired => pixel.survivalTime >= threshold,
                    _ => false
                };

                if (triggerReaction)
                {
                    // 执行反应效果
                    switch (rule.effect)
                    {
                        case ReactionEffect.SpawnAbove:
                            SpawnAbove(x, y, rule.product);
                            break;

                        case ReactionEffect.SpawnSelf:
                            TransformSelfToProduct(x, y, rule.product);
                            break;                        
                    }
                }
            }
        }
    }
}