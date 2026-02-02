using System;
using Unity.Burst;
using Unity.Collections;

namespace Pixel
{
    /// <summary>
    /// 并查集结构，用于高效的连通性分析
    /// </summary>
    [BurstCompile]
    public struct UnionFind : IDisposable
    {
        private NativeArray<int> parent;  // 父节点数组
        private NativeArray<int> rank;    // 秩数组（用于按秩合并）

        public UnionFind(int size, Allocator allocator)
        {
            parent = new NativeArray<int>(size, allocator);
            rank = new NativeArray<int>(size, allocator);

            // 初始化：每个节点的父节点是自己，秩为0
            for (int i = 0; i < size; i++)
            {
                parent[i] = i;
                rank[i] = 0;
            }
        }

        /// <summary>
        /// 查找操作，带路径压缩优化
        /// </summary>
        [BurstCompile]
        public int Find(int x)
        {
            if (parent[x] != x)
            {
                // 路径压缩：将路径上所有节点直接连接到根节点
                parent[x] = Find(parent[x]);
            }
            return parent[x];
        }

        /// <summary>
        /// 合并操作，带按秩合并优化
        /// </summary>
        [BurstCompile]
        public void Union(int x, int y)
        {
            int rootX = Find(x);
            int rootY = Find(y);

            if (rootX == rootY)
                return;

            // 按秩合并：将秩小的树连接到秩大的树上
            if (rank[rootX] < rank[rootY])
            {
                parent[rootX] = rootY;
            }
            else if (rank[rootX] > rank[rootY])
            {
                parent[rootY] = rootX;
            }
            else
            {
                parent[rootY] = rootX;
                rank[rootX]++;
            }
        }

        public void Dispose()
        {
            if (parent.IsCreated)
                parent.Dispose();
            if (rank.IsCreated)
                rank.Dispose();
        }
    }
}