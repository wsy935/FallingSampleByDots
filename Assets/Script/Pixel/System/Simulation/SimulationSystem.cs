using Unity.Entities;
using Unity.Burst;
using Unity.Collections;

namespace Pixel
{
    public struct ChunkConfig
    {
        public int edge;
        public int border;
    }
    [BurstCompile]
    public partial struct SimulationSystem : ISystem
    {
        private PixelConfigMap pixelConfigMap;
        private ChunkConfig chunkConfig;
        public void OnCreate(ref SystemState state)
        {
            var fsw = FallingSandWorld.Instance;
            pixelConfigMap = new();
            foreach (var e in fsw.PixelSet.pixels)
            {
                PixelConfig config = new(e.type, e.interactionMask, e.handler);
                pixelConfigMap.AddConfig(e.type, config);
            }
            chunkConfig = new()
            {
                edge = fsw.ChunkEdge,
                border = fsw.ChunkBorderSize
            };
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
        }

        [BurstCompile]
        private partial struct UpdateChunkJob : IJobEntity
        {
            [ReadOnly] PixelConfigMap configMap;
            [BurstCompile]
            public void Execute(ref PixelChunk chunk, ref DynamicBuffer<PixelBuffer> buffer)
            {
                int chunkEdge = chunk.edgeSize;
                int chunkBorder = chunk.borderWidth;
                for (int y = chunkBorder; y < chunkEdge + chunkBorder; y++)
                {
                    for (int x = chunkBorder; x < chunkEdge + chunkBorder; x++)
                    {

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
