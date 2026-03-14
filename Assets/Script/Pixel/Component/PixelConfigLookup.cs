using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;

namespace Pixel
{
    public struct PixelConfigLookup : IComponentData, IDisposable
    {
        private NativeArray<PixelConfig> configs;
        //反应规则数组,将所有规则压入，扁平化处理访问时通过其Config中偏移设置访问
        private NativeList<ReactionRule> reactionRules;
        
        public PixelConfigLookup(Allocator allocator)
        {
            int size = Enum.GetValues(typeof(PixelType)).Length;
            configs = new(size, allocator);
            reactionRules = new(allocator);
        }

        public void AddConfig(PixelType type, PixelConfig config)
        {
            int key = (int)type;
            configs[key] = config;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PixelConfig GetConfig(PixelType type)
        {
            int key = (int)type;
            return configs[key];
        }

        public void AddReactionRule(ReactionRule reactionRule)
        {
            reactionRules.Add(reactionRule);
        }

        public ReactionRule GetReactionRule(int idx)
        {
            if (idx < 0 || idx >= reactionRules.Length)
                return new() { type = ReactionType.None };
            return reactionRules[idx];
        }

        public void Dispose()
        {
            if (configs.IsCreated)
                configs.Dispose();
            if (reactionRules.IsCreated)
                reactionRules.Dispose();
        }
    }
}