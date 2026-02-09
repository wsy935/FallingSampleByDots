using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Pixel
{
    [UpdateBefore(typeof(SimulationSystem))]
    public partial class RenderSystem : SystemBase
    {
        WorldConfig worldConfig;
        DynamicBuffer<PixelData> buffer;
        DynamicBuffer<Chunk> chunks;
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
            buffer = SystemAPI.GetSingletonBuffer<PixelData>();
            chunks = SystemAPI.GetSingletonBuffer<Chunk>();
            worldConfig = SystemAPI.GetSingleton<WorldConfig>();
            isInit = true;
        }

        protected override void OnUpdate()
        {
            var renderBuffer = tex.GetRawTextureData<Color32>();
            var job = new ExtractPixelJob()
            {
                renderBuffer = renderBuffer,
                buffer = buffer,
                chunks = chunks,
                worldConfig = worldConfig
            };
            Dependency = job.Schedule(chunks.Length, 4, Dependency);
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
        [NativeDisableParallelForRestriction] public NativeArray<Color32> renderBuffer;
        [ReadOnly] public DynamicBuffer<PixelData> buffer;
        [ReadOnly] public DynamicBuffer<Chunk> chunks;
        [ReadOnly] public WorldConfig worldConfig;

        [BurstCompile]
        public void Execute(int index)
        {
            var chunk = chunks[index];
            if (!chunk.isDirty) return;

            int chunkSize = worldConfig.chunkSize;

            for (int i = 0; i < chunkSize; i++)
            {
                for (int j = 0; j < chunkSize; j++)
                {
                    int2 pos = worldConfig.GetCoordsByChunk(chunk.pos, j, i);
                    int idx = worldConfig.CoordsToIdx(pos.x, pos.y);
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
