using Unity.Mathematics;

namespace Pixel
{
    /// <summary>
    /// 表示一个需要更新的脏区块
    /// </summary>
    public struct DirtyChunk : System.IComparable<DirtyChunk>
    {
        /// <summary>区块的矩形区域</summary>
        public Rect rect;
        /// <summary>是否为脏区块</summary>
        public bool isDirty;
        public int notDirtyFrame;

        public DirtyChunk(Rect rect, bool isDirty = true)
        {
            this.rect = rect;
            this.isDirty = isDirty;
            notDirtyFrame = 0;
        }

        public DirtyChunk(int x, int y, int width, int height, bool isDirty = true)
        {
            this.rect = new Rect(x, y, width, height);
            this.isDirty = isDirty;
            notDirtyFrame = 0;
        }
        
        /// <summary>
        /// 排序比较：先按 y 坐标，再按 x 坐标
        /// </summary>
        public int CompareTo(DirtyChunk other)
        {
            if (rect.y != other.rect.y)            
                return rect.y.CompareTo(other.rect.y);            
            else
                return rect.x.CompareTo(other.rect.x);
        }

        public override string ToString()
        {
            return $"DirtyChunk({rect}, dirty:{isDirty})";
        }
    }
}
