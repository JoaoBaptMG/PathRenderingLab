using System.Collections.Generic;

namespace PathRenderingLab
{
    public static class LinkedListUtils
    {
        public static bool CanReach<T>(this LinkedListNode<T> node1, LinkedListNode<T> node2)
        {
            for (var node = node1; node != null; node = node.Next)
                if (node == node2) return true;
            return false;
        }
    }
}
