using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditorInternal;
using UnityEngine;

namespace Pixel
{
    [UpdateAfter(typeof(SimulationSystem))]
    public partial class RenderSystem : SystemBase
    {
        Texture2D tex;
        WorldConfig worldConfig;
        PixelConfigLookup pixelConfigLookup;
        PixelBuffer pixelBuffer;
        DirtyChunkManager dirtyChunkManager;
        bool isInit;

        protected override void OnCreate()
        {
            isInit = false;
        }

        protected override void OnStartRunning()
        {
            if (isInit) return;
            tex = FallingSandRender.Instance.Tex;
            isInit = true;
            dirtyChunkManager = SystemAPI.GetSingleton<DirtyChunkManager>();
            worldConfig = SystemAPI.GetSingleton<WorldConfig>();
            pixelConfigLookup = SystemAPI.GetSingleton<PixelConfigLookup>();
            pixelBuffer = SystemAPI.GetSingleton<PixelBuffer>();
        }

        protected override void OnUpdate()
        {
            var renderBuffer = tex.GetRawTextureData<Color32>();                       
            var job = new ExtractPixelJob()
            {
                dirtyChunks = dirtyChunkManager.GetDirtyChunks(),
                renderBuffer = renderBuffer,
                pixelConfigLookup = pixelConfigLookup,
                buffer = pixelBuffer.buffer,
                worldConfig = worldConfig
            };
            Dependency = job.Schedule(Dependency);
            CompleteDependency();

            tex.Apply(false);
        }
    }

    [BurstCompile]
    public struct ExtractPixelJob : IJob
    {
        [NativeDisableParallelForRestriction] public NativeList<DirtyChunk> dirtyChunks;
        [NativeDisableParallelForRestriction] public NativeArray<Color32> renderBuffer;
        [ReadOnly] public PixelConfigLookup pixelConfigLookup;
        [ReadOnly] public NativeArray<PixelData> buffer;
        [ReadOnly] public WorldConfig worldConfig;

        [BurstCompile]
        public void Execute()
        {
            for (int k = 0; k < dirtyChunks.Length; k++)
            {
                var dirtyChunk = dirtyChunks[k];
                var rect = dirtyChunk.rect;                
                for (int i = rect.y; i < rect.MaxY; i++)
                {
                    for (int j = rect.x; j < rect.MaxX; j++)
                    {
                        int idx = worldConfig.CoordsToIdx(j, i);
                        var config = pixelConfigLookup.GetConfig(buffer[idx].type);
                        renderBuffer[idx] = config.color;
                    }
                }
            }
        }
    }
}