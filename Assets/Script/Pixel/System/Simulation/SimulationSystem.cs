using Unity.Entities;
using Unity.Burst;
using Unity.Collections;

namespace Pixel
{        
    [BurstCompile]
    public partial struct SimulationSystem: ISystem
    {
        private PixelConfigMap pixelConfigMap;
                
        public void OnCreate(ref SystemState state)
        {
            pixelConfigMap = new();
            foreach (var e in SystemAPI.Query<RefRO<PixelSOConfig>>())
            {
                var config = new PixelConfig(e.ValueRO.type, e.ValueRO.interactionMask,SimulationHandlers.GetHandler(e.ValueRO.type));
                pixelConfigMap.AddConfig(e.ValueRO.type, config);
            }            
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
            if (!SystemAPI.TryGetSingleton<WorldConfig>(out var worldConfig))
                return;

            // 使用Job进行并行处理
            var job = new UpdateChunkJob
            {
                config = worldConfig
            };

            job.ScheduleParallel();
        }

        [BurstCompile]
        private partial struct UpdateChunkJob : IJobEntity
        {            
            [ReadOnly] public WorldConfig config;

            [BurstCompile]
            public void Execute(ref PixelChunk chunk, ref DynamicBuffer<PixelBuffer> buffer)
            {
                int chunkEdge = chunk.edgeSize;
                int chunkBorder = chunk.borderWidth;
                for (int y = chunkBorder; y < chunkEdge + chunkBorder; y++)
                {
                    for (int x = chunkBorder; x < chunkEdge + chunkBorder; x++)
                    {
                        // dispatcher.TryExecute(x, y, ref buffer, in config);
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
