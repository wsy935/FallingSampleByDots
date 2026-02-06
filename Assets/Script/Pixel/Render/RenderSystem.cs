using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Pixel
{
    [UpdateAfter(typeof(SimulationSystem))]
    public partial class RenderSystem : SystemBase
    {
        WorldConfig worldConfig;
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
                buffer = buffer,
                worldConfig = worldConfig
            };
            Dependency = job.Schedule(dirtyChunks.Length, 1, Dependency);
            CompleteDependency();

            tex.Apply(false);
        }
    }

    /// <summary>
    /// 将像素数据编码写入纹理，供 Shader 解码渲染：
    /// R = PixelType ID (0~255)
    /// G = seed (0~255) 像素种子，决定基础颜色变化
    /// B = modulate (0~255) 调制值，外部影响（火烧/侵蚀）
    /// A = 255 (不透明标记，Shader 中根据 type 决定透明度)
    /// </summary>
    [BurstCompile]
    public struct ExtractPixelJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeList<DirtyChunk> dirtyChunks;
        [NativeDisableParallelForRestriction] public NativeArray<Color32> renderBuffer;
        [ReadOnly] public NativeArray<PixelData> buffer;
        [ReadOnly] public WorldConfig worldConfig;

        [BurstCompile]
        public void Execute(int index)
        {
            var dirtyChunk = dirtyChunks[index];
            int minX = dirtyChunk.rect.x;
            int minY = dirtyChunk.rect.y;
            int maxX = minX + dirtyChunk.rect.width;
            int maxY = minY + dirtyChunk.rect.height;

            for (int y = minY; y < maxY; y++)
            {
                for (int x = minX; x < maxX; x++)
                {
                    int idx = worldConfig.CoordsToIdx(x, y);
                    var pixelData = buffer[idx];

                    // 编码像素数据到 RGBA 通道
                    renderBuffer[idx] = new Color32(
                        (byte)pixelData.type,       // R: 像素类型 ID
                        pixelData.seed,             // G: 像素种子
                        pixelData.modulate,         // B: 调制值
                        255                         // A: 不透明标记
                    );
                }
            }
        }
    }
}
