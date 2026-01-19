using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

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
            pixelConfigMap = new(fsw.PixelSet.pixels.Length);
            foreach (var e in fsw.PixelSet.pixels)
            {
                PixelConfig config = new(e.type, e.interactionMask, e.handler);
                pixelConfigMap.AddConfig(e.type, config);
            }
            chunkConfig = new()
            {
                edge = fsw.ChunkEdge,
                border = fsw.ChunkBorder
            };

            isInit = true;
        }

        public void OnStopRunning(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (pixelConfigMap.isCreated)
                pixelConfigMap.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new UpdateChunkJob()
            {
                pixelConfigMap = pixelConfigMap,
                chunkConfig = chunkConfig
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        private partial struct UpdateChunkJob : IJobEntity
        {
            [ReadOnly] public PixelConfigMap pixelConfigMap;
            [ReadOnly] public ChunkConfig chunkConfig;
            [BurstCompile]
            public void Execute(ref PixelChunk chunk, ref DynamicBuffer<PixelBuffer> buffer)
            {
                if (!chunk.isDirty) return;
                int chunkEdge = chunkConfig.edge;
                int chunkBorder = chunkConfig.border;
                SimulationContext context = new()
                {
                    chunkConfig = chunkConfig,
                    buffer = buffer,
                    random = new Random(math.hash(chunk.pos))
                };
                for (int y = chunkBorder; y < chunkConfig.RealEdge; y++)
                {
                    for (int x = chunkBorder; x < chunkConfig.RealEdge; x++)
                    {
                        var idx = context.GetIndex(x, y);
                        var config = pixelConfigMap.GetConfig(buffer[idx].type);
                        context.currentPixelConfig = config;
                        config.handler.Invoke(x, y, ref context);
                    }
                }
            }
        }

        [BurstCompile]
        private partial struct SyncBorderJob : IJobEntity
        {
            [ReadOnly] public NativeHashMap<int2, Entity> chunkMap;
            [ReadOnly] public ChunkConfig chunkConfig;
            public BufferLookup<PixelBuffer> bufferLookup;

            [BurstCompile]
            public void Execute(ref PixelChunk chunk, ref DynamicBuffer<PixelBuffer> buffer)
            {
                int border = chunkConfig.border;
                int edge = chunkConfig.edge;

                // 同步左边界：从左侧 chunk 复制数据
                if (chunkMap.TryGetValue(chunk.pos + new int2(-1, 0), out Entity leftEntity))
                {
                    var leftBuffer = bufferLookup[leftEntity];
                    for (int y = border; y < edge + border; y++)
                    {
                        for (int b = 0; b < border; b++)
                        {
                            // // int srcIdx = GetIndex(edge + border + b, y);
                            // // int dstIdx = GetIndex(b, y);
                            // buffer[dstIdx] = leftBuffer[srcIdx];
                        }
                    }
                }
            }

            [BurstCompile]
            public void SyncBorder(ref DynamicBuffer<PixelBuffer> buffer, int2 directions)
            {

            }
        }
    }
}
