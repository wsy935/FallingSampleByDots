using System;
using UnityEngine;

namespace Pixel
{
    public enum ReactionType
    {
        None = 0,
        ContactWith = 1,
        LifetimeExpired = 2,
    }

    public enum ReactionEffect
    {
        None = 0,
        SpawnAbove = 1,
        SpawnSelf = 2,
        TransformAdjacent = 3,
    }

    [Serializable]
    public struct ReactionRule
    {
        public ReactionType type;
        public ReactionEffect effect;
        //反应规则所需参数对应的值区间，使用时随机生成区间值
        [Header("值反应")]
        public float min;
        public float max;
        public PixelType product;
        [Header("接触反应")]
        public PixelType targetType;
        public PixelType selfProduct;
        public PixelType targetProduct;        
    }
}