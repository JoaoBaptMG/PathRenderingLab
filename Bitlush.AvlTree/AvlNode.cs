using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlush
{
    [DebuggerDisplay("{Key} => {Value}")]
	public sealed class AvlNode<TKey, TValue>
	{
		public AvlNode<TKey, TValue> Parent;
		public AvlNode<TKey, TValue> Left;
		public AvlNode<TKey, TValue> Right;
		public TKey Key;
		public TValue Value;
		public int Balance;
	}
}
