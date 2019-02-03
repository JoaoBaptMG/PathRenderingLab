using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlush
{
	public sealed class AvlNodeEnumerator<TKey, TValue> : IEnumerator<AvlNode<TKey, TValue>>
	{
		private AvlNode<TKey, TValue> _root;
		private Action _action;
        private AvlNode<TKey, TValue> _right;

		public AvlNodeEnumerator(AvlNode<TKey, TValue> root)
		{
			_right = _root = root;
			_action = _root == null ? Action.End : Action.Right;
		}

		public bool MoveNext()
		{
			switch (_action)
			{
				case Action.Right:
					Current = _right;

					while (Current.Left != null)
					{
						Current = Current.Left;
					}

					_right = Current.Right;
					_action = _right != null ? Action.Right : Action.Parent;

					return true;

				case Action.Parent:
					while (Current.Parent != null)
					{
						AvlNode<TKey, TValue> previous = Current;

						Current = Current.Parent;

						if (Current.Left == previous)
						{
							_right = Current.Right;
							_action = _right != null ? Action.Right : Action.Parent;

							return true;
						}
					}

					_action = Action.End;

					return false;

				default:
					return false;
			}
		}

		public void Reset()
		{
			_right = _root;
			_action = _root == null ? Action.End : Action.Right;
		}

        public AvlNode<TKey, TValue> Current { get; private set; }

        object IEnumerator.Current
		{
			get
			{
				return Current;
			}
		}

		public void Dispose()
		{
		}

		enum Action
		{
			Parent,
			Right,
			End
		}
	}
}
