using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Pixel
{
    [UpdateAfter(typeof(SimulationSystem))]
    public partial struct RenderSystem : ISystem, ISystemStartStop
    {
        WorldConfig worldConfig;
        PixelConfigMap pixelConfigMap;
        UnityObjectRef<Texture2D> tex;
        bool isInit;

        public void OnCreate(ref SystemState state)
        {
            isInit = false;
        }

        public void OnStopRunning(ref SystemState state) { }

        public void OnStartRunning(ref SystemState state)
        {
            if (isInit) return;
            tex = FallingSandRender.Instance.Tex;
            pixelConfigMap = SystemAPI.GetSingleton<PixelConfigMap>();
            worldConfig = SystemAPI.GetSingleton<WorldConfig>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var renderBuffer = tex.Value.GetRawTextureData<Color32>();            
            var job = new ExtractPixelJob()
            {
                renderBuffer = renderBuffer,
                pixelConfigMap = pixelConfigMap,
                worldConfig = worldConfig
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
            state.CompleteDependency();
            tex.Value.Apply(false);
        }
    }
    
    [BurstCompile]
    public partial struct ExtractPixelJob : IJobEntity
    {
        [NativeDisableParallelForRestriction] public  NativeArray<Color32> renderBuffer;
        [ReadOnly] public PixelConfigMap pixelConfigMap;
        [ReadOnly] public WorldConfig worldConfig;
        
        [BurstCompile]
        public void Execute(in PixelChunk chunk, in DynamicBuffer<PixelBuffer> buffer)
        {
            for(int i = 0; i < worldConfig.chunkEdge; i++)
            {
                for(int j = 0; j < worldConfig.chunkEdge; j++)
                {
                    int worldIdx = worldConfig.CoordsToWorldIdx(j, i, chunk.pos);
                    int chunkIdx = worldConfig.CoordsToChunkIdx(j, i);
                    var color = pixelConfigMap.GetConfig(buffer[chunkIdx].type).color;
                    renderBuffer[worldIdx] = color;
                }
            }
        }
    }
}