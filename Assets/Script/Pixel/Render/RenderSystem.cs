using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Pixel
{
    [UpdateAfter(typeof(SimulationSystem))]
    public partial class RenderSystem : SystemBase
    {
        WorldConfig worldConfig;
        PixelConfigLookup pixelConfigLookup;
        NativeArray<PixelType> buffer;
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
            buffer = FallingSandWorld.Instance.PixelBuffer;
            pixelConfigLookup = SystemAPI.GetSingleton<PixelConfigLookup>();
            worldConfig = SystemAPI.GetSingleton<WorldConfig>();
        }

        protected override void OnUpdate()
        {
            var renderBuffer = tex.GetRawTextureData<Color32>();
            for (int i = 0; i < worldConfig.height; i++)
            {
                for (int j = 0; j < worldConfig.width; j++)
                {
                    int idx = worldConfig.CoordsToIdx(j, i);
                    var config = pixelConfigLookup.GetConfig(buffer[idx]);
                    renderBuffer[idx] = config.color;
                }
            }
            tex.Apply(false);
        }    
    }
}