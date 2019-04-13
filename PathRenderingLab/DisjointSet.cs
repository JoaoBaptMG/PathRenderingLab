using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PathRenderingLab.SwapUtils;

namespace PathRenderingLab
{
    /// <summary>
    /// Class to represent a disjoint set data structure
    /// </summary>
    public class DisjointSets
    {
        public readonly int Count;
        private readonly int[] parents;
        private readonly int[] sizes;

        public DisjointSets(int count)
        {
            Count = count;
            parents = new int[count];
            sizes = new int[count];

            for (int i = 0; i < count; i++)
            {
                parents[i] = i;
                sizes[i] = 1;
            }
        }

        // Use path compression to speed up the query
        public int FindParentOfSets(int i)
        {
            if (parents[i] == i) return i;
            return parents[i] = FindParentOfSets(parents[i]);
        }

        // Use ranking by size
        public void UnionSets(int i, int j)
        {
            int ir = FindParentOfSets(i);
            int jr = FindParentOfSets(j);

            if (ir == jr) return;

            if (sizes[ir] < sizes[jr]) Swap(ref ir, ref jr);
            parents[jr] = ir;
            sizes[ir] += sizes[jr];
        }
    }
}
