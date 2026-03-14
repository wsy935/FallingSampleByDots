using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using System;
using Unity.Jobs;
using UnityEngine;

namespace Pixel
{
    [BurstCompile]
    public partial struct PretreatmentSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WorldConfig>();
            state.RequireForUpdate<PixelConfigLookup>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new PretreatmentJob
            {
                pixelConfigLookup = SystemAPI.GetSingleton<PixelConfigLookup>(),
                buffer = SystemAPI.GetSingletonBuffer<PixelData>(),
                worldConfig = SystemAPI.GetSingleton<WorldConfig>(),
                chunks = SystemAPI.GetSingletonBuffer<Chunk>(),
                deltaTime = Time.deltaTime
            };

            state.Dependency = job.Schedule(job.chunks.Length, 2, state.Dependency);
            state.CompleteDependency();
        }
    }

    [BurstCompile]
    public struct PretreatmentJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public DynamicBuffer<PixelData> buffer;
        [ReadOnly] public WorldConfig worldConfig;
        [ReadOnly] public PixelConfigLookup pixelConfigLookup;
        [ReadOnly] public DynamicBuffer<Chunk> chunks;
        [ReadOnly] public float deltaTime;

        [BurstCompile]
        public void Execute(int index)
        {
            var chunk = chunks[index];

            int chunkSize = worldConfig.chunkSize;
            for (int i = 0; i < chunkSize; i++)
            {
                for (int j = 0; j < chunkSize; j++)
                {
                    int2 pos = worldConfig.GetCoordsByChunk(chunk.pos, j, i);

                    // 检查位置是否在世界范围内
                    if (!worldConfig.IsInWorld(pos.x, pos.y))
                        continue;
                    int idx = worldConfig.CoordsToIdx(pos.x,pos.y);
                    if (buffer[idx].type == PixelType.Empty) continue;
                    UpdateSurvivalTime(idx);
                    UpdateTemperature(idx,pos.x, pos.y);
                }
            }
        }

        public void UpdateSurvivalTime(int idx)
        {            
            var pixel = buffer[idx];            
            // 每帧增加存活时间
            pixel.survivalTime += deltaTime;
            buffer[idx] = pixel;
        }

        /// <summary>
        /// 更新指定位置的温度
        /// </summary>
        public void UpdateTemperature(int idx,int x, int y)
        {
            var pixel = buffer[idx];
            var config = pixelConfigLookup.GetConfig(pixel.type);
            if (config.matType == MaterialType.Gas)
                return;
            float totalDeltaT = 0;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0)
                        continue;
                    int checkX = x + j;
                    int checkY = y + i;
                    if (!worldConfig.IsInWorld(checkX, checkY))
                        continue;

                    int tarIdx = worldConfig.CoordsToIdx(checkX, checkY);
                    var tarPixel = buffer[tarIdx];                    
                    var tarPixelConfig = pixelConfigLookup.GetConfig(tarPixel.type);
                    if(tarPixelConfig.matType != MaterialType.Solid && tarPixelConfig.matType != MaterialType.Liquid)
                        continue;
                    float k_eff = (config.tempConfig.rate + tarPixelConfig.tempConfig.rate) * 0.5f;
                    totalDeltaT += k_eff * (tarPixel.temperature - pixel.temperature);
                }
            }
            pixel.temperature += totalDeltaT * deltaTime;
            buffer[idx] = pixel;
        }
    }
}