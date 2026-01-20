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
        private NativeHashMap<int2, Entity> chunkMap;
        private ChunkConfig chunkConfig;
        private bool isInit;

        public void OnCreate(ref SystemState state)
        {
            isInit = false;
        }

        public void OnStartRunning(ref SystemState state)
        {
            if (isInit) return;

            chunkMap = new NativeHashMap<int2, Entity>(100, Allocator.Persistent);
            foreach (var (chunk, entity) in SystemAPI.Query<RefRO<PixelChunk>>().WithEntityAccess())
            {
                chunkMap.TryAdd(chunk.ValueRO.pos, entity);
            }

            var fsw = FallingSandWorld.Instance;
            chunkConfig = new()
            {
                edge = fsw.ChunkEdge,
                border = fsw.ChunkBorder
            };

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
            if (chunkMap.IsCreated)
                chunkMap.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var blackChunkQuery = SystemAPI.QueryBuilder().WithAll<BlackChunkTag, PixelChunk, PixelBuffer>().Build();
            var whiteChunkQuery = SystemAPI.QueryBuilder().WithAll<WhiteChunkTag, PixelChunk, PixelBuffer>().Build();

            // 阶段1：从邻居实际数据区域读取并更新自己的border
            var syncBorderFromNeighborJob = new SyncBorderFromNeighborJob()
            {
                chunkConfig = chunkConfig,
                chunkMap = chunkMap,
                bufferLookUp = SystemAPI.GetBufferLookup<PixelBuffer>(true)
            };

            // 阶段2：从邻居border区域读取并合并到自己的实际数据区域（当前块优先）
            var mergeBorderToDataJob = new MergeBorderToDataJob()
            {
                chunkConfig = chunkConfig,
                chunkMap = chunkMap,
                bufferLookUp = SystemAPI.GetBufferLookup<PixelBuffer>(true)
            };

            var updateChunkJob = new UpdateChunkJob()
            {
                pixelConfigMap = pixelConfigMap,
                chunkConfig = chunkConfig
            };

            // 黑块：边界同步 -> 合并 -> 模拟更新
            state.Dependency = syncBorderFromNeighborJob.ScheduleParallel(blackChunkQuery, state.Dependency);
            state.Dependency = mergeBorderToDataJob.ScheduleParallel(blackChunkQuery, state.Dependency);
            state.Dependency = updateChunkJob.ScheduleParallel(blackChunkQuery, state.Dependency);

            // 白块：边界同步 -> 合并 -> 模拟更新
            state.Dependency = syncBorderFromNeighborJob.ScheduleParallel(whiteChunkQuery, state.Dependency);
            state.Dependency = mergeBorderToDataJob.ScheduleParallel(whiteChunkQuery, state.Dependency);
            state.Dependency = updateChunkJob.ScheduleParallel(whiteChunkQuery, state.Dependency);
        }

        /// <summary>
        /// 同步块
        /// </summary>
        [BurstCompile]
        private partial struct UpdateChunkJob : IJobEntity
        {
            [ReadOnly] public PixelConfigMap pixelConfigMap;
            [ReadOnly] public ChunkConfig chunkConfig;

            [BurstCompile]
            public void Execute(ref PixelChunk chunk, ref DynamicBuffer<PixelBuffer> buffer)
            {
                if (!chunk.isDirty) return;
                SimulationContext context = new()
                {
                    buffer = buffer,
                    chunkConfig = chunkConfig,
                    random = new Random(math.hash(chunk.pos))
                };
                bool hasChange = false;
                for (int y = chunkConfig.border; y < chunkConfig.edge + chunkConfig.border; y++)
                {
                    bool direction = context.random.NextBool();
                    if (direction)
                    {
                        for (int x = chunkConfig.border; x < chunkConfig.edge + chunkConfig.border; x++)
                        {
                            var idx = context.GetIndex(x, y);
                            var sourceType = buffer[idx].type;
                            var config = pixelConfigMap.GetConfig(buffer[idx].type);
                            context.currentPixelConfig = config;
                            config.handler.Invoke(x, y, ref context);

                            if (!hasChange && sourceType == buffer[idx].type)
                                hasChange = true;
                        }
                    }
                    else
                    {
                        for (int x = chunkConfig.edge + chunkConfig.border - 1; x >= chunkConfig.border; x--)
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

        /// <summary>
        /// 阶段1：从邻居块的实际数据区域读取数据，更新到当前块的border区域
        /// </summary>     
        [BurstCompile]
        private partial struct SyncBorderFromNeighborJob : IJobEntity
        {
            [ReadOnly] public NativeHashMap<int2, Entity> chunkMap;
            [ReadOnly, NativeDisableContainerSafetyRestriction] public BufferLookup<PixelBuffer> bufferLookUp;
            [ReadOnly] public ChunkConfig chunkConfig;

            [BurstCompile]
            public void Execute(ref PixelChunk chunk, ref DynamicBuffer<PixelBuffer> buffer)
            {
                SyncBorderFromNeighbor(new int2(0, 1), ref chunk, ref buffer);
                SyncBorderFromNeighbor(new int2(0, -1), ref chunk, ref buffer);
                SyncBorderFromNeighbor(new int2(1, 0), ref chunk, ref buffer);
                SyncBorderFromNeighbor(new int2(-1, 0), ref chunk, ref buffer);
            }

            [BurstCompile]
            private void SyncBorderFromNeighbor(int2 direction, ref PixelChunk chunk, ref DynamicBuffer<PixelBuffer> buffer)
            {
                int2 neighborPos = chunk.pos + direction;
                if (!chunkMap.TryGetValue(neighborPos, out var neighborEntity)) return;
                if (!bufferLookUp.TryGetBuffer(neighborEntity, out var neighborBuffer)) return;

                int b = chunkConfig.border;
                int e = chunkConfig.edge;
                int r = chunkConfig.RealEdge;

                // 从邻居的实际数据区域读取，写入当前块的border区域
                // neighborSrcX/Y: 邻居块中要读取的起始坐标
                // currentDstX/Y: 当前块中要写入的起始坐标
                (int neighborSrcX, int neighborSrcY, int currentDstX, int currentDstY, int w, int h) = direction switch
                {
                    { x: 0, y: 1 } => (b, b, b, r - b, e, b),           // 上：读取邻居下边界，写入当前上border
                    { x: 0, y: -1 } => (b, e + b - 1, b, 0, e, b),      // 下：读取邻居上边界，写入当前下border
                    { x: 1, y: 0 } => (b, b, r - b, b, b, e),           // 右：读取邻居左边界，写入当前右border
                    { x: -1, y: 0 } => (e + b - 1, b, 0, b, b, e),      // 左：读取邻居右边界，写入当前左border
                    _ => (-1, -1, -1, -1, -1, -1)
                };

                if (neighborSrcX < 0) return;

                // 执行从邻居实际数据区域到当前border的复制
                bool hasChange = false;
                for (int dy = 0; dy < h; dy++)
                {
                    for (int dx = 0; dx < w; dx++)
                    {
                        int neighborIdx = chunkConfig.CoordsToIdx(neighborSrcX + dx, neighborSrcY + dy);
                        int currentIdx = chunkConfig.CoordsToIdx(currentDstX + dx, currentDstY + dy);

                        if (!hasChange && buffer[currentIdx].type != neighborBuffer[neighborIdx].type)
                        {
                            hasChange = true;
                            chunk.isDirty = true;
                        }

                        buffer[currentIdx] = neighborBuffer[neighborIdx];
                    }
                }
            }
        }

        /// <summary>
        /// 阶段2：从邻居块的border区域读取数据，使用并集方式合并到当前块的实际数据区域（当前块优先）
        /// </summary>
        [BurstCompile]
        private partial struct MergeBorderToDataJob : IJobEntity
        {
            [ReadOnly] public NativeHashMap<int2, Entity> chunkMap;
            [ReadOnly, NativeDisableContainerSafetyRestriction] public BufferLookup<PixelBuffer> bufferLookUp;
            [ReadOnly] public ChunkConfig chunkConfig;

            [BurstCompile]
            public void Execute(ref PixelChunk chunk, ref DynamicBuffer<PixelBuffer> buffer)
            {
                MergeBorderFromNeighbor(new int2(0, 1), ref chunk, ref buffer);
                MergeBorderFromNeighbor(new int2(0, -1), ref chunk, ref buffer);
                MergeBorderFromNeighbor(new int2(1, 0), ref chunk, ref buffer);
                MergeBorderFromNeighbor(new int2(-1, 0), ref chunk, ref buffer);
            }

            [BurstCompile]
            private void MergeBorderFromNeighbor(int2 direction, ref PixelChunk chunk, ref DynamicBuffer<PixelBuffer> buffer)
            {
                int2 neighborPos = chunk.pos + direction;
                if (!chunkMap.TryGetValue(neighborPos, out var neighborEntity)) return;
                if (!bufferLookUp.TryGetBuffer(neighborEntity, out var neighborBuffer)) return;

                int b = chunkConfig.border;
                int e = chunkConfig.edge;

                // 从邻居的border区域读取，合并到当前块的实际数据边界
                // neighborBorderX/Y: 邻居块的border区域起始坐标
                // currentDataX/Y: 当前块的实际数据边界起始坐标
                (int neighborBorderX, int neighborBorderY, int currentDataX, int currentDataY, int w, int h) = direction switch
                {
                    { x: 0, y: 1 } => (b, 0, b, e + b - 1, e, b),       // 上：读取邻居下border，合并到当前上边界
                    { x: 0, y: -1 } => (b, e + b, b, b, e, b),          // 下：读取邻居上border，合并到当前下边界
                    { x: 1, y: 0 } => (0, b, e + b - 1, b, b, e),       // 右：读取邻居左border，合并到当前右边界
                    { x: -1, y: 0 } => (e + b, b, b, b, b, e),          // 左：读取邻居右border，合并到当前左边界
                    _ => (-1, -1, -1, -1, -1, -1)
                };

                if (neighborBorderX < 0) return;

                // 执行并集合并：当前块数据优先
                bool hasChange = false;
                for (int dy = 0; dy < h; dy++)
                {
                    for (int dx = 0; dx < w; dx++)
                    {
                        int neighborIdx = chunkConfig.CoordsToIdx(neighborBorderX + dx, neighborBorderY + dy);
                        int currentIdx = chunkConfig.CoordsToIdx(currentDataX + dx, currentDataY + dy);

                        var currentPixel = buffer[currentIdx];
                        var neighborPixel = neighborBuffer[neighborIdx];

                        // 并集合并策略：如果当前块不是Empty，保持当前值（优先级高）
                        if (currentPixel.type == PixelType.Empty && neighborPixel.type != PixelType.Empty)
                        {
                            if (!hasChange)
                            {
                                hasChange = true;
                                chunk.isDirty = true;
                            }
                            buffer[currentIdx] = neighborPixel;
                        }
                    }
                }
            }
        }
    }
}
