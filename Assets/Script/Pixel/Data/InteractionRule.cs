using System;

namespace Pixel
{
    /// <summary>
    /// 负责处理粒子反应的结果
    /// 设定发生反应时，目标像素设为空，源像素设为结果
    /// </summary>
    [Serializable]
    public struct InteractionRule
    {
        public InteractionFlag flag1;
        public InteractionFlag flag2;
        public InteractionResult[] results;
    }

    [Serializable]
    public struct InteractionResult
    {
        public PixelType type1;
        public PixelType type2;
        public PixelType result;
    }

    [Serializable]
    public struct InteractionOutcome
    {
        public PixelType result;
    }
}