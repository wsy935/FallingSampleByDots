using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace Pixel
{
    [BurstCompile]
    public partial struct SimulationSystem : ISystem, ISystemStartStop
    {
        private SimulationHandler handler;
        private WorldConfig worldConfig;
        bool isInit;

        public void OnCreate(ref SystemState state)
        {
            isInit = false;
            state.RequireForUpdate<WorldConfig>();
            state.RequireForUpdate<PixelConfigLookup>();
        }

        public void OnStartRunning(ref SystemState state)
        {
            if (isInit) return;

            var fsw = FallingSandWorld.Instance;
            worldConfig = SystemAPI.GetSingleton<WorldConfig>();
            handler = new SimulationHandler
            {
                pixelConfigLookup = SystemAPI.GetSingleton<PixelConfigLookup>(),
                worldConfig = worldConfig,
                buffer = fsw.PixelBuffer,
                random = new Random(1)
            };
            isInit = true;     
        }

        public void OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            uint seed = (uint)SystemAPI.Time.ElapsedTime;
            for(int i = 0; i < worldConfig.height; i++)
            {
                for(int j = 0; j < worldConfig.width; j++)
                {
                    handler.random.InitState(seed + math.hash(new int2(j, i)));
                    handler.HandleMove(j, i);
                }
            }
        }
    }
}
