using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

using Random = Unity.Mathematics.Random;
using Unity.Jobs;
namespace Pixel
{
    [BurstCompile]
    public partial struct SimulationSystem : ISystem, ISystemStartStop
    {
        private PixelConfigMap pixelConfigMap;        
        private NativeArray<Entity> chunkEntities;
        private uint frameIdx;

        private WorldConfig worldConfig;
        private bool isInit;
        
        public void OnCreate(ref SystemState state)
        {
            isInit = false;
            frameIdx = 0;            
        }

        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            if (isInit) return;

            worldConfig = SystemAPI.GetSingleton<WorldConfig>();
            pixelConfigMap = SystemAPI.GetSingleton<PixelConfigMap>();

            // 按照chunk位置填充数组
            chunkEntities = new NativeArray<Entity>(worldConfig.chunkCnt.x * worldConfig.chunkCnt.y, Allocator.Persistent);
            foreach (var (chunk, entity) in SystemAPI.Query<RefRO<PixelChunk>>().WithEntityAccess())
            {
                int idx = worldConfig.ChunkPosToIdx(chunk.ValueRO.pos);
                chunkEntities[idx] = entity;
            }

            isInit = true;
        }

        public void OnStopRunning(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
            if (chunkEntities.IsCreated)
                chunkEntities.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {        
            var updateChunkJob = new UpdateChunkJob()
            {
                pixelConfigMap = pixelConfigMap,
                worldConfig = worldConfig,
                chunkEntities = chunkEntities,
                frameIdx = frameIdx,
                bufferLookup = SystemAPI.GetBufferLookup<PixelBuffer>(false),
                chunkLookup = SystemAPI.GetComponentLookup<PixelChunk>(false)
            };
            state.Dependency = updateChunkJob.ScheduleParallel(state.Dependency);
            // state.Dependency = updateChunkJob.ScheduleParallel(whiteChunkQuery, state.Dependency);            

            frameIdx = frameIdx == uint.MaxValue ? 0 : frameIdx + 1;
        }
        /// <summary>
        /// 更新Chunk：通过黑白交替调度保证并发安全
        /// </summary>
        [BurstCompile]
        private partial struct UpdateChunkJob : IJobEntity
        {
            [ReadOnly] public PixelConfigMap pixelConfigMap;
            [ReadOnly] public WorldConfig worldConfig;
            [ReadOnly] public NativeArray<Entity> chunkEntities;
            [ReadOnly] public uint frameIdx;

            [NativeDisableContainerSafetyRestriction] public BufferLookup<PixelBuffer> bufferLookup;
            [NativeDisableContainerSafetyRestriction] public ComponentLookup<PixelChunk> chunkLookup;

            [BurstCompile]
            public void Execute(ref PixelChunk chunk, DynamicBuffer<PixelBuffer> buffer)
            {
                // if (!chunk.isDirty) return;
                SimulationContext context = new()
                {
                    buffer = buffer,
                    worldConfig = worldConfig,
                    pixelConfigMap = pixelConfigMap,
                    frameIdx = frameIdx,
                    random = new Random(math.hash(chunk.pos) + frameIdx),
                    curChunk = chunk,
                    chunkEntities = chunkEntities,
                    chunkLookup = chunkLookup,
                    bufferLookup = bufferLookup,
                };

                bool hasChange = false;
                for (int y = 0; y < worldConfig.chunkEdge; y++)
                {
                    bool direction = context.random.NextBool();
                    if (direction)
                    {
                        for (int x = 0; x < worldConfig.chunkEdge; x++)
                        {
                            HandlePixel(x, y, ref context);
                            if (!hasChange && buffer[context.GetIndex(x, y)].lastFrame == frameIdx)
                                hasChange = true;
                        }
                    }
                    else
                    {
                        for (int x = worldConfig.chunkEdge - 1; x >= 0; x--)
                        {
                            HandlePixel(x, y, ref context);
                            if (!hasChange && buffer[context.GetIndex(x, y)].lastFrame == frameIdx)
                                hasChange = true;
                        }
                    }
                }

                chunk.isDirty = hasChange;
            }

            [BurstCompile]
            private void HandlePixel(int x, int y, ref SimulationContext context)
            {
                var idx = context.GetIndex(x, y);                
                if (context.buffer[idx].lastFrame == frameIdx)
                    return;
                var config = pixelConfigMap.GetConfig(context.buffer[idx].type);
                context.currentPixelConfig = config;
                context.curLocalPos = new(x, y);                
            }
        }
    }
}
