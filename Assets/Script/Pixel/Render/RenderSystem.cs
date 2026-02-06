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
        WorldConfig worldConfig;
        PixelConfigLookup pixelConfigLookup;
        NativeArray<PixelData> buffer;
        DirtyChunkManager dirtyChunkManager;
        Texture2D tex;
        bool isInit;

        protected override void OnCreate()
        {
            isInit = false;
        }

        protected override void OnStartRunning()
        {
            if (isInit) return;
            tex = FallingSandRender.Instance.Tex;
            buffer = SystemAPI.GetSingleton<PixelBuffer>().buffer;
            dirtyChunkManager = SystemAPI.GetSingleton<DirtyChunkManager>();
            pixelConfigLookup = SystemAPI.GetSingleton<PixelConfigLookup>();
            worldConfig = SystemAPI.GetSingleton<WorldConfig>();
            isInit = true;
        }

        protected override void OnUpdate()
        {
            var renderBuffer = tex.GetRawTextureData<Color32>();
            var dirtyChunks = dirtyChunkManager.GetDirtyChunks();
            var job = new ExtractPixelJob()
            {
                dirtyChunks = dirtyChunks,
                renderBuffer = renderBuffer,
                pixelConfigLookup = pixelConfigLookup,
                buffer = buffer,
                worldConfig = worldConfig
            };
            Dependency = job.Schedule(dirtyChunks.Length, 1, Dependency);
            CompleteDependency();

            tex.Apply(false);
        }
    }

    [BurstCompile]
    public struct ExtractPixelJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeList<DirtyChunk> dirtyChunks;
        [NativeDisableParallelForRestriction] public NativeArray<Color32> renderBuffer;
        [ReadOnly] public PixelConfigLookup pixelConfigLookup;
        [ReadOnly] public NativeArray<PixelData> buffer;
        [ReadOnly] public WorldConfig worldConfig;

        [BurstCompile]
        public void Execute(int index)
        {
            var dirtyChunk = dirtyChunks[index];
            int2 minXY = new(dirtyChunk.rect.x, dirtyChunk.rect.y);
            int2 maxXY = new(minXY.x + dirtyChunk.rect.width, minXY.y + dirtyChunk.rect.height);
            for (int i = minXY.y; i < maxXY.y; i++)
            {
                for (int j = minXY.x; j < maxXY.x; j++)
                {
                    int idx = worldConfig.CoordsToIdx(j, i);
                    var config = pixelConfigLookup.GetConfig(buffer[idx].type);
                    renderBuffer[idx] = config.color;
                }
            }
        }
    }
}