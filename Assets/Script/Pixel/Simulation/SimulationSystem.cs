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

        public void OnStartRunning(ref SystemState state)
        {
            if (isInit) return;

            var fsw = FallingSandWorld.Instance;
            worldConfig = new()
            {
                width = fsw.WorldWidth,
                height = fsw.WorldHeight,
                chunkEdge = fsw.ChunkEdge,
                chunkCnt = fsw.ChunkCount,
            };
            chunkEntities = new NativeArray<Entity>(fsw.ChunkCount.x * fsw.ChunkCount.y, Allocator.Persistent);

            // 按照chunk位置填充数组
            foreach (var (chunk, entity) in SystemAPI.Query<RefRO<PixelChunk>>().WithEntityAccess())
            {
                int idx = worldConfig.ChunkPosToIdx(chunk.ValueRO.pos);
                chunkEntities[idx] = entity;
            }

            pixelConfigMap = new(fsw.PixelSet.pixels.Length);
            foreach (var e in fsw.PixelSet.pixels)
            {
                PixelConfig config = new(e.type, e.interactionMask, e.handler);
                pixelConfigMap.AddConfig(e.type, config);
            }
            isInit = true;
        }

        public void OnStopRunning(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
            pixelConfigMap.Dispose();

            if (chunkEntities.IsCreated)
                chunkEntities.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var blackChunkQuery = SystemAPI.QueryBuilder().WithAll<BlackChunkTag, PixelChunk, PixelBuffer>().Build();
            var whiteChunkQuery = SystemAPI.QueryBuilder().WithAll<WhiteChunkTag, PixelChunk, PixelBuffer>().Build();

            var updateChunkJob = new UpdateChunkJob()
            {
                pixelConfigMap = pixelConfigMap,
                worldConfig = worldConfig,
                chunkEntities = chunkEntities,
                frameIdx = frameIdx,
                bufferLookup = SystemAPI.GetBufferLookup<PixelBuffer>(false),
                chunkLookup = SystemAPI.GetComponentLookup<PixelChunk>(false)
            };

            // state.Dependency = updateChunkJob.ScheduleParallel(blackChunkQuery, state.Dependency);
            // state.Dependency = updateChunkJob.ScheduleParallel(whiteChunkQuery, state.Dependency);
            state.Dependency = updateChunkJob.Schedule(blackChunkQuery, state.Dependency);
            state.Dependency = updateChunkJob.Schedule(whiteChunkQuery, state.Dependency);
            // var blackEntity = blackChunkQuery.ToEntityArray(Allocator.TempJob);
            // var whiteEntity = whiteChunkQuery.ToEntityArray(Allocator.TempJob);

            // updateChunkJob.processingEntities = blackEntity;
            // state.Dependency = updateChunkJob.Schedule(blackEntity.Length, 32, state.Dependency);

            // var whiteChunkJob = updateChunkJob;
            // whiteChunkJob.processingEntities = whiteEntity;
            // state.Dependency = whiteChunkJob.Schedule(whiteEntity.Length, 32, state.Dependency);

            // state.Dependency = blackEntity.Dispose(state.Dependency);
            // state.Dependency = whiteEntity.Dispose(state.Dependency);

            frameIdx = frameIdx == uint.MaxValue ? 0 : frameIdx + 1;
        }

        // [BurstCompile]
        // private partial struct UpdateChunkJob : IJobParallelFor // Changed to IJobParallelFor
        // {
        //     [ReadOnly] public PixelConfigMap pixelConfigMap;
        //     [ReadOnly] public WorldConfig worldConfig;
        //     [ReadOnly] public NativeArray<Entity> chunkEntities;
        //     [ReadOnly] public uint frameIdx;
        //     [ReadOnly] public NativeArray<Entity> processingEntities;

        //     [NativeDisableContainerSafetyRestriction] public BufferLookup<PixelBuffer> bufferLookup;
        //     [NativeDisableContainerSafetyRestriction] public ComponentLookup<PixelChunk> chunkLookup;

        //     public void Execute(int index)
        //     {
        //         var entity = processingEntities[index];

        //         // Manually retrieve components
        //         var chunk = chunkLookup[entity];
        //         var buffer = bufferLookup[entity];

        //         // if (!chunk.isDirty) return;

        //         SimulationContext context = new()
        //         {
        //             buffer = buffer,
        //             worldConfig = worldConfig,
        //             frameIdx = frameIdx,
        //             random = new Random(math.hash(chunk.pos) + frameIdx),
        //             chunk = chunk,
        //             chunkEntities = chunkEntities,
        //             chunkLookup = chunkLookup,
        //             bufferLookup = bufferLookup,
        //         };

        //         bool hasChange = false;
        //         for (int y = 0; y < worldConfig.chunkEdge; y++)
        //         {
        //             bool direction = context.random.NextBool();
        //             if (direction)
        //             {
        //                 for (int x = 0; x < worldConfig.chunkEdge; x++)
        //                 {
        //                     HandlePixel(x, y, ref context);
        //                     if (!hasChange && buffer[context.GetIndex(x, y)].lastFrame == frameIdx)
        //                         hasChange = true;
        //                 }
        //             }
        //             else
        //             {
        //                 for (int x = worldConfig.chunkEdge - 1; x >= 0; x--)
        //                 {
        //                     HandlePixel(x, y, ref context);
        //                     if (!hasChange && buffer[context.GetIndex(x, y)].lastFrame == frameIdx)
        //                         hasChange = true;
        //                 }
        //             }
        //         }

        //         chunk.isDirty = hasChange;
        //         chunkLookup[entity] = chunk;
        //     }

        //     [BurstCompile]
        //     private void HandlePixel(int x, int y, ref SimulationContext context)
        //     {
        //         var idx = context.GetIndex(x, y);
        //         if ((context.buffer[idx].type & PixelType.NotReact) != 0 || context.buffer[idx].lastFrame == frameIdx)
        //             return;
        //         var config = pixelConfigMap.GetConfig(context.buffer[idx].type);
        //         context.currentPixelConfig = config;
        //         config.handler.Invoke(x, y, ref context);
        //     }
        // }

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
            public void Execute(ref PixelChunk chunk, ref DynamicBuffer<PixelBuffer> buffer)
            {
                // if (!chunk.isDirty) return;

                SimulationContext context = new()
                {
                    buffer = buffer,
                    worldConfig = worldConfig,
                    frameIdx = frameIdx,
                    random = new Random(math.hash(chunk.pos) + frameIdx),
                    chunk = chunk,
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
                if ((context.buffer[idx].type & PixelType.NotReact) != 0 || context.buffer[idx].lastFrame == frameIdx)
                    return;
                var config = pixelConfigMap.GetConfig(context.buffer[idx].type);
                context.currentPixelConfig = config;
                config.handler.Invoke(x, y, ref context);
            }
        }
    }
}
