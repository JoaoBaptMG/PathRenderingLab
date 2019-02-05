using System;
using System.Collections.Generic;

namespace PathRenderingLab
{
    public static class ArrayExtensions
    {
        public static T[] Merge<T>(T[] array1, T[] array2)
            where T : IComparable<T>
            => Merge(array1, array2, Comparer<T>.Default);

        public static T[] Merge<T>(T[] array1, T[] array2, IComparer<T> comparer)
        {
            int len1 = array1.Length, len2 = array2.Length;
            int i = 0, j = 0, k = 0;

            var result = new T[len1 + len2];

            while (i < len1 && j < len2)
            {
                if (comparer.Compare(array1[i], array2[j]) < 0)
                    result[k++] = array1[i++];
                else result[k++] = array2[j++];
            }

            if (i < len1)
            {
                Array.Copy(array1, i, result, k, len1 - i);
                k += len1 - i;
            }

            if (j < len2)
            {
                Array.Copy(array2, j, result, k, len2 - j);
                k += len2 - j;
            }

            return result;
        }

        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> range)
        {
            foreach (var item in range) collection.Add(item);
        }
    }
}