using Unity.Entities;
using Unity.Burst;
using Unity.Collections;

namespace Pixel
{
    public struct ChunkConfig
    {
        public int edge;
        public int border;
        public int RealEdge => edge + border*2;
    }

    [BurstCompile]    
    public partial struct SimulationSystem : ISystem,ISystemStartStop
    {
        private PixelConfigMap pixelConfigMap;
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
                    buffer = buffer
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
        // [BurstCompile]
        // private partial struct UpdateChunkBorderJob : IJobEntity
        // {
        //     [ReadOnly] public SimulationDispatcher dispatcher;
        //     [ReadOnly] public Config config;
        //     public void Execute()
        //     {

        //     }
        // }
    }
}
