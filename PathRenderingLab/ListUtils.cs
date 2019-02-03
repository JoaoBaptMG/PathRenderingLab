using System;
using System.Collections.Generic;
using System.Linq;

namespace PathRenderingLab
{
    // Utility
    public static class ListUtils
    {
        public static T[] RemoveDuplicates<T>(T[] array) => RemoveDuplicates(array, Comparer<T>.Default);

        public static T[] RemoveDuplicates<T>(T[] array, IComparer<T> comparer)
        {
            if (array.Length == 0) return array;

            Array.Sort(array, comparer);

            var list = new List<T>(array.Length) { array[0] };

            for (int i = 1; i < array.Length; i++)
                if (comparer.Compare(array[i], array[i - 1]) != 0) list.Add(array[i]);

            return list.ToArray();
        }

        public static List<T> ExtractIndices<T>(this List<T> list, int[] indices)
        {
            indices = RemoveDuplicates(indices);
            indices = indices.Where(i => i >= 0).ToArray();
            if (indices.Length == 0) return new List<T>();

            var dest = new List<T>();

            int j = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (j > 0) list[i - j] = list[i];

                if (j < indices.Length && i == indices[j])
                {
                    dest.Add(list[i]);
                    j++;
                }
            }

            list.RemoveRange(list.Count - j, j);
            return dest;
        }

        public static List<T> ExtractObjects<T>(this List<T> list, T[] values)
            => ExtractIndices(list, values.Select(v => list.IndexOf(v)).ToArray());

        public static List<T> ExtractAll<T>(this List<T> list, Predicate<T> pred)
        {
            var indices = new List<int>();
            for (int i = 0; i < list.Count; i++)
                if (pred(list[i])) indices.Add(i);
            return ExtractIndices(list, indices.ToArray());
        }
    }
}