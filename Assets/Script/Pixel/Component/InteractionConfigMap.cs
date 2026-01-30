using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Pixel
{    
    public struct InteractionLookupKey : IEquatable<InteractionLookupKey>
    {
        public int FlagA;
        public int FlagB;
        public int TypeA;
        public int TypeB;

        /// <summary>
        /// 创建对称的查找键，自动排序使得顺序无关
        /// </summary>
        public InteractionLookupKey(InteractionFlag flag1, InteractionFlag flag2,PixelType type1, PixelType type2)
        {
            int f1 = (int)flag1;
            int f2 = (int)flag2;
            int t1 = (int)type1;
            int t2 = (int)type2;

            // 按值排序，确保 (A,B) 和 (B,A) 生成相同的 Key
            FlagA = f1 <= f2 ? f1 : f2;
            FlagB = f1 <= f2 ? f2 : f1;
            TypeA = t1 <= t2 ? t1 : t2;
            TypeB = t1 <= t2 ? t2 : t1;
        }

        public bool Equals(InteractionLookupKey other)
        {
            return FlagA == other.FlagA && FlagB == other.FlagB
                && TypeA == other.TypeA && TypeB == other.TypeB;
        }

        public override int GetHashCode()
        {
            int hash = FlagA;
            hash = (hash * 397) ^ FlagB;
            hash = (hash * 397) ^ TypeA;
            hash = (hash * 397) ^ TypeB;
            return hash;
        }
    }
 
    public struct InteractionLookup : IComponentData, IDisposable
    {
        private NativeHashMap<InteractionLookupKey, PixelType> _lookupMap;

        public InteractionLookup(int capacity, Allocator allocator)
        {
            _lookupMap = new NativeHashMap<InteractionLookupKey, PixelType>(capacity, allocator);
        }

        public void BuildFromRules(InteractionRule[] rules)
        {
            foreach (var rule in rules)
            {
                foreach (var result in rule.results)
                {
                    var key = new InteractionLookupKey(rule.flag1, rule.flag2, result.type1, result.type2);
                    _lookupMap.TryAdd(key, result.result);
                }
            }
        }
        
        public bool TryGetResultType(InteractionFlag flag1, InteractionFlag flag2, PixelType type1, PixelType type2, out PixelType result)
        {
            var key = new InteractionLookupKey(flag1, flag2, type1, type2);
            if (_lookupMap.TryGetValue(key, out var type))
            {
                result = type;
                return true;
            }
            else
            {
                result = PixelType.Disable;
                return false;
            }

        }

        /// <summary>
        /// 检查是否存在匹配的交互规则 - 自动处理对称性
        /// </summary>
        public bool HasRule(InteractionFlag flag1, InteractionFlag flag2, PixelType type1, PixelType type2)
        {
            var key = new InteractionLookupKey(flag1, flag2, type1, type2);
            return _lookupMap.ContainsKey(key);
        }

        public void Dispose()
        {
            if (_lookupMap.IsCreated)
                _lookupMap.Dispose();
        }
    }
}
