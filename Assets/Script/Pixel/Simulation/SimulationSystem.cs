using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace Pixel
{
    [BurstCompile]
    public partial struct SimulationSystem : ISystem, ISystemStartStop
    {
        private PixelConfigMap pixelConfigMap;
        private NativeArray<Entity> chunkEntities; // 使用Array代替HashMap，提升cache性能
        private ChunkConfig chunkConfig;
        private bool isInit;

        public void OnCreate(ref SystemState state)
        {
            isInit = false;
        }

        public void OnStartRunning(ref SystemState state)
        {
            if (isInit) return;

            var fsw = FallingSandWorld.Instance;

            // 计算chunk数量
            int2 chunkCount = new(
                (int)math.ceil((float)fsw.WorldWidth / fsw.ChunkEdge),
                (int)math.ceil((float)fsw.WorldHeight / fsw.ChunkEdge)
            );

            chunkConfig = new()
            {
                edge = fsw.ChunkEdge,
                chunkCount = chunkCount
            };

            // 使用NativeArray代替HashMap，提升cache性能
            chunkEntities = new NativeArray<Entity>(chunkCount.x * chunkCount.y, Allocator.Persistent);

            // 按照chunk位置填充数组
            foreach (var (chunk, entity) in SystemAPI.Query<RefRO<PixelChunk>>().WithEntityAccess())
            {
                int idx = chunkConfig.ChunkPosToIdx(chunk.ValueRO.pos);
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
            if (pixelConfigMap.isCreated)
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
                chunkConfig = chunkConfig,
                chunkEntities = chunkEntities,
                elapsedTime = (uint)SystemAPI.Time.ElapsedTime,
                bufferLookup = SystemAPI.GetBufferLookup<PixelBuffer>(false),
                chunkLookup = SystemAPI.GetComponentLookup<PixelChunk>(false)
            };

            // 黑块：读写白块邻居（白块此时不执行，安全）
            state.Dependency = updateChunkJob.ScheduleParallel(blackChunkQuery, state.Dependency);

            // 白块：读写黑块邻居（黑块已完成，安全）
            state.Dependency = updateChunkJob.ScheduleParallel(whiteChunkQuery, state.Dependency);
        }

        /// <summary>
        /// 更新Chunk：通过黑白交替调度保证并发安全
        /// </summary>
        [BurstCompile]
        private partial struct UpdateChunkJob : IJobEntity
        {
            [ReadOnly] public PixelConfigMap pixelConfigMap;
            [ReadOnly] public ChunkConfig chunkConfig;
            [ReadOnly] public NativeArray<Entity> chunkEntities;
            [ReadOnly] public uint elapsedTime;
            [NativeDisableParallelForRestriction] public BufferLookup<PixelBuffer> bufferLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<PixelChunk> chunkLookup;

            [BurstCompile]
            public void Execute(ref PixelChunk chunk, ref DynamicBuffer<PixelBuffer> buffer)
            {
                if (!chunk.isDirty) return;

                SimulationContext context = new()
                {
                    buffer = buffer,
                    chunkConfig = chunkConfig,
                    random = new Random(math.hash(chunk.pos) + elapsedTime),
                    chunkPos = chunk.pos,
                    chunkEntities = chunkEntities,
                    bufferLookup = bufferLookup,
                    chunkLookup = chunkLookup
                };

                bool hasChange = false;
                for (int y = 0; y < chunkConfig.edge; y++)
                {
                    bool direction = context.random.NextBool();
                    if (direction)
                    {
                        for (int x = 0; x < chunkConfig.edge; x++)
                        {
                            var idx = context.GetIndex(x, y);
                            var sourceType = buffer[idx].type;
                            var config = pixelConfigMap.GetConfig(buffer[idx].type);
                            context.currentPixelConfig = config;
                            config.handler.Invoke(x, y, ref context);

                            if (!hasChange && sourceType != buffer[idx].type)
                                hasChange = true;
                        }
                    }
                    else
                    {
                        for (int x = chunkConfig.edge - 1; x >= 0; x--)
                        {
                            var idx = context.GetIndex(x, y);
                            var sourceType = buffer[idx].type;
                            var config = pixelConfigMap.GetConfig(buffer[idx].type);
                            context.currentPixelConfig = config;
                            config.handler.Invoke(x, y, ref context);

                            if (!hasChange && sourceType != buffer[idx].type)
                                hasChange = true;
                        }
                    }
                }
                chunk.isDirty = hasChange;
            }
        }
    }
}
