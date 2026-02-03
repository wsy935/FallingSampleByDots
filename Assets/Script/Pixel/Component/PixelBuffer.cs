using System;
using Unity.Collections;
using Unity.Entities;

namespace Pixel
{
    public struct PixelBuffer : IComponentData,IDisposable
    {
        //NativeArray是值类型，修改时需要重新赋值，所以放弃使用二维数组
        public NativeArray<PixelData> buffer;

        public void Dispose()
        {
            if (buffer.IsCreated)
                buffer.Dispose();
        }
    }
}