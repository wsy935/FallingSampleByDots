using System;
using Unity.Collections;
using Unity.Entities;

namespace Pixel
{
    //NativeArray是值类型，修改时需要重新赋值，所以放弃使用二维数组
    public struct PixelData : IBufferElementData
    {
        public PixelType type;
        public uint frameIdx;
        /// <summary>
        /// 像素种子 (0~255)，创建时基于空间位置+随机性生成，用于 Shader 确定最终颜色。
        /// 生成后不再改变，像素移动时颜色保持一致。
        /// </summary>
        public byte seed;
        /// <summary>
        /// 调制值 (0~255)，表示外部影响（火烧、侵蚀等）对颜色的叠加程度。
        /// 0 = 无影响, 255 = 最大影响。
        /// </summary>
        public byte modulate;
    }
}