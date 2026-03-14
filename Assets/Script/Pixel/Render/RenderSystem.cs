using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Pixel
{
    [UpdateAfter(typeof(MoveSystem))]
    public partial class RenderSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var tex = FallingSandRender.Instance.Tex;
            var renderBuffer = tex.GetRawTextureData<Color32>();
            var chunks = SystemAPI.GetSingletonBuffer<Chunk>();
            var job = new ExtractPixelJob()
            {
                renderBuffer = renderBuffer,
                pixelConfigLookup = SystemAPI.GetSingleton<PixelConfigLookup>(),
                buffer = SystemAPI.GetSingletonBuffer<PixelData>(),
                chunks = chunks,
                worldConfig = SystemAPI.GetSingleton<WorldConfig>(),
                frameCount = UnityEngine.Time.frameCount
            };
            Dependency = job.Schedule(chunks.Length, 4, Dependency);
            CompleteDependency();
            tex.Apply(false);
        }
    }

    [BurstCompile]
    public struct ExtractPixelJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<Color32> renderBuffer;
        [ReadOnly] public DynamicBuffer<PixelData> buffer;
        [ReadOnly] public PixelConfigLookup pixelConfigLookup;
        [ReadOnly] public DynamicBuffer<Chunk> chunks;
        [ReadOnly] public WorldConfig worldConfig;
        [ReadOnly] public int frameCount;

        [BurstCompile]
        public void Execute(int index)
        {
            var chunk = chunks[index];
            if (!chunk.IsDirty(frameCount)) return;

            int chunkSize = worldConfig.chunkSize;

            for (int i = 0; i < chunkSize; i++)
            {
                for (int j = 0; j < chunkSize; j++)
                {
                    int2 pos = worldConfig.GetCoordsByChunk(chunk.pos, j, i);
                    int idx = worldConfig.CoordsToIdx(pos.x, pos.y);
                    var pixelData = buffer[idx];
                    var config = pixelConfigLookup.GetConfig(pixelData.type);
                    var finalColor = config.color;
                    // float blackLevel = 1 - math.unlerp(config.tempConfig.baseTemp, config.tempConfig.maxTemp, pixelData.temperature);
                    // finalColor.r = (byte)(finalColor.r * blackLevel);
                    // finalColor.g = (byte)(finalColor.g * blackLevel);
                    // finalColor.b = (byte)(finalColor.b * blackLevel);
                    renderBuffer[idx] = finalColor;
                }
            }
        }
    }
}
