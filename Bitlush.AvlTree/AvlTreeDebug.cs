using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Bitlush
{
    internal class AvlTreeDebug<TKey, TValue>
    {
        readonly AvlTree<TKey, TValue> tree;

        public AvlTreeDebug(AvlTree<TKey, TValue> tree)
        {
            this.tree = tree;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public AvlNode<TKey,TValue>[] Items => tree.ToArray();
    }
}