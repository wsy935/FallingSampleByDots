using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

using Random = Unity.Mathematics.Random;
namespace Pixel
{
    [BurstCompile]
    public partial struct SimulationSystem : ISystem, ISystemStartStop
    {
        private PixelConfigMap pixelConfigMap;
        private NativeArray<Entity> chunkEntities;
        private uint frameIdx;

        //处理像素，确保每个像素每帧只会更新一次
        private NativeArray<uint> bitMap;
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

            int pixelCnt = fsw.WorldHeight * fsw.WorldWidth;
            const int bitsPerUint = sizeof(uint) * 8;
            bitMap = new((int)math.ceil((float)pixelCnt / bitsPerUint), Allocator.Persistent);
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
            if (bitMap.IsCreated)
                bitMap.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            for (int i = 0; i < bitMap.Length; i++)
                bitMap[i] = 0;

            var blackChunkQuery = SystemAPI.QueryBuilder().WithAll<BlackChunkTag, PixelChunk, PixelBuffer>().Build();
            var whiteChunkQuery = SystemAPI.QueryBuilder().WithAll<WhiteChunkTag, PixelChunk, PixelBuffer>().Build();

            var updateChunkJob = new UpdateChunkJob()
            {
                pixelConfigMap = pixelConfigMap,
                worldConfig = worldConfig,
                chunkEntities = chunkEntities,
                bitMap = bitMap,
                frameIdx = frameIdx,
                bufferLookup = SystemAPI.GetBufferLookup<PixelBuffer>(false),
                chunkLookup = SystemAPI.GetComponentLookup<PixelChunk>(false)
            };
            state.Dependency = updateChunkJob.ScheduleParallel(blackChunkQuery, state.Dependency);

            state.Dependency.Complete();

            updateChunkJob.bufferLookup = SystemAPI.GetBufferLookup<PixelBuffer>(false);
            state.Dependency = updateChunkJob.ScheduleParallel(whiteChunkQuery, state.Dependency);

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

            [NativeDisableContainerSafetyRestriction] public NativeArray<uint> bitMap;
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
                    random = new Random(math.hash(chunk.pos) + frameIdx),
                    chunkPos = chunk.pos,
                    bitMap = bitMap,
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
                            var idx = context.GetIndex(x, y);
                            if ((buffer[idx].type & PixelType.NotReact) != 0 || context.CheckBit(x, y, chunk.pos))
                                continue;
                            if (buffer[idx].lastFrame == frameIdx) continue;
                            var config = pixelConfigMap.GetConfig(buffer[idx].type);
                            context.currentPixelConfig = config;
                            config.handler.Invoke(x, y, ref context);

                            if (!hasChange && context.CheckBit(x, y, chunk.pos))
                                hasChange = true;
                        }
                    }
                    else
                    {
                        for (int x = worldConfig.chunkEdge - 1; x >= 0; x--)
                        {
                            var idx = context.GetIndex(x, y);
                            if ((buffer[idx].type & PixelType.NotReact) != 0 || context.CheckBit(x, y, chunk.pos))
                                continue;
                            if (buffer[idx].lastFrame == frameIdx) continue;
                            var config = pixelConfigMap.GetConfig(buffer[idx].type);
                            context.currentPixelConfig = config;
                            config.handler.Invoke(x, y, ref context);

                            if (!hasChange && context.CheckBit(x, y, chunk.pos))
                                hasChange = true;
                        }
                    }
                }
                chunk.isDirty = hasChange;
            }
        }
    }
}
