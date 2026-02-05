using Unity.Mathematics;

namespace Pixel
{
    /// <summary>
    /// 表示一个整数矩形区域
    /// </summary>
    public struct Rect : System.IEquatable<Rect>
    {
        /// <summary>左下角 x 坐标</summary>
        public int x;
        /// <summary>左下角 y 坐标</summary>
        public int y;
        /// <summary>矩形宽度</summary>
        public int width;
        /// <summary>矩形高度</summary>
        public int height;

        public Rect(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        /// <summary>右上角 x 坐标（不包含）</summary>
        public readonly int MaxX => x + width;

        /// <summary>右上角 y 坐标（不包含）</summary>
        public readonly int MaxY => y + height;

        /// <summary>左下角坐标</summary>
        public readonly int2 Min => new int2(x, y);

        /// <summary>右上角坐标</summary>
        public readonly int2 Max => new int2(MaxX, MaxY);

        /// <summary>矩形尺寸</summary>
        public readonly int2 Size => new int2(width, height);

        /// <summary>矩形中心点</summary>
        public readonly int2 Center => new int2(x + width / 2, y + height / 2);

        /// <summary>
        /// 扩展矩形边界（向外扩展）
        /// </summary>
        /// <param name="border">扩展的边界大小</param>
        /// <returns>扩展后的新矩形</returns>
        public readonly Rect Expand(int border)
        {
            return new Rect(
                x - border,
                y - border,
                width + border * 2,
                height + border * 2
            );
        }
        
        /// <summary>
        /// 依据四边距离扩展边界，左右上下
        /// </summary>
        /// <param name="borders"></param>
        /// <returns></returns>
        public readonly Rect Expand(int4 borders)
        {
            int startX = x - borders.w;
            int startY = y - borders.z;
            int newWidth = width + borders.w + borders.x;
            int newHeight = height + borders.y + borders.z;
            return new Rect(startX, startY, newWidth, newHeight);
        }

        /// <summary>
        /// 根据目标点的位置扩张矩形
        /// </summary>
        /// <param name="endX"></param>
        /// <param name="endY"></param>
        public void Expand(int endX,int endY)
        {            
            // 扩展左边界
            if (endX < x)
            {
                width += x - endX;
                x = endX;
            }
            // 扩展右边界
            if (endX >= MaxX)
            {
                width = endX - x + 1;
            }
            // 扩展下边界
            if (endY < y)
            {
                height += y - endY;
                y = endY;
            }
            // 扩展上边界
            if (endY >= MaxY)
            {
                height = endY - y + 1;
            }
        }

        /// <summary>
        /// 将矩形限制在指定边界内
        /// </summary>
        /// <param name="bounds">边界矩形</param>
        /// <returns>限制后的新矩形</returns>
        public readonly Rect Clamp(Rect bounds)
        {
            int newMinX = math.max(x, bounds.x);
            int newMinY = math.max(y, bounds.y);
            int newMaxX = math.min(MaxX, bounds.MaxX);
            int newMaxY = math.min(MaxY, bounds.MaxY);

            return new Rect(
                newMinX,
                newMinY,
                math.max(0, newMaxX - newMinX),
                math.max(0, newMaxY - newMinY)
            );
        }

        /// <summary>
        /// 判断点是否在矩形内
        /// </summary>
        public readonly bool Contains(int2 point)
        {
            return point.x >= x && point.x < MaxX &&
                   point.y >= y && point.y < MaxY;
        }

        /// <summary>
        /// 判断点是否在矩形内
        /// </summary>
        public readonly bool Contains(int px, int py)
        {
            return px >= x && px < MaxX &&
                   py >= y && py < MaxY;
        }

        /// <summary>
        /// 判断两个矩形是否相交（带边界扩展,默认为0）
        /// </summary>
        /// <param name="other">另一个矩形</param>
        /// <param name="border">边界扩展大小</param>
        /// <returns>是否相交</returns>
        public readonly bool Intersects(Rect other, int border = 0)
        {
            bool xIntersect = (x - border) <= (other.MaxX + border) &&
                              (MaxX + border) >= (other.x - border);
            bool yIntersect = (y - border) <= (other.MaxY + border) &&
                              (MaxY + border) >= (other.y - border);
            return xIntersect && yIntersect;
        }

        /// <summary>
        /// 合并两个矩形，返回包含两者的最小矩形
        /// </summary>
        public static Rect Union(Rect a, Rect b)
        {
            int newMinX = math.min(a.x, b.x);
            int newMinY = math.min(a.y, b.y);
            int newMaxX = math.max(a.MaxX, b.MaxX);
            int newMaxY = math.max(a.MaxY, b.MaxY);

            return new Rect(
                newMinX,
                newMinY,
                newMaxX - newMinX,
                newMaxY - newMinY
            );
        }

        public readonly bool Equals(Rect other)
        {
            return x == other.x && y == other.y &&
                   width == other.width && height == other.height;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is Rect other && Equals(other);
        }

        public override readonly int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + x;
            hash = hash * 31 + y;
            hash = hash * 31 + width;
            hash = hash * 31 + height;
            return hash;
        }

        public override readonly string ToString()
        {
            return $"PixelRect(x:{x}, y:{y}, w:{width}, h:{height})";
        }

        public static bool operator ==(Rect left, Rect right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Rect left, Rect right)
        {
            return !left.Equals(right);
        }
    }
}
