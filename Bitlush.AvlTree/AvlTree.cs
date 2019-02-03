using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Bitlush
{
    [DebuggerDisplay("Count = {_count}")]
    [DebuggerTypeProxy(typeof(AvlTreeDebug<,>))]
    public class AvlTree<TKey, TValue> : IEnumerable<AvlNode<TKey, TValue>>
    {
        private IComparer<TKey> _comparer;
        private int _count;

        public AvlTree(IComparer<TKey> comparer)
        {
            _comparer = comparer;
            _count = 0;
        }

        public AvlTree()
           : this(Comparer<TKey>.Default)
        {

        }

        public AvlNode<TKey, TValue> Root { get; private set; }

        public IEnumerator<AvlNode<TKey, TValue>> GetEnumerator()
        {
            return new AvlNodeEnumerator<TKey, TValue>(Root);
        }

        public bool Search(TKey key, out TValue value)
        {
            AvlNode<TKey, TValue> node = Root;

            while (node != null)
            {
                if (_comparer.Compare(key, node.Key) < 0)
                {
                    node = node.Left;
                }
                else if (_comparer.Compare(key, node.Key) > 0)
                {
                    node = node.Right;
                }
                else
                {
                    value = node.Value;

                    return true;
                }
            }

            value = default(TValue);

            return false;
        }

        public bool SearchLeftRight(TKey key, out TValue vleft, out TValue vright)
        {
            AvlNode<TKey, TValue> node = Root;
            AvlNode<TKey, TValue> leftnode = null;
            AvlNode<TKey, TValue> rightnode = null;

            while (node != null)
            {
                if (_comparer.Compare(key, node.Key) < 0)
                {
                    rightnode = node;
                    node = node.Left;
                }
                else if (_comparer.Compare(key, node.Key) > 0)
                {
                    leftnode = node;
                    node = node.Right;
                }
                else
                {
                    vleft = node.Value;
                    vright = node.Value;
                    return true;
                }
            }

            if (leftnode != null) vleft = leftnode.Value;
            else vleft = default(TValue);

            if (rightnode != null) vright = rightnode.Value;
            else vright = default(TValue);

            return false;
        }

        public TValue First
        {
            get
            {
                AvlNode<TKey, TValue> node = Root;
                TValue value = default(TValue);

                while (node != null)
                {
                    value = node.Value;
                    node = node.Left;
                }

                return value;
            }
        }

        public TValue Last
        {
            get
            {
                AvlNode<TKey, TValue> node = Root;
                TValue value = default(TValue);

                while (node != null)
                {
                    value = node.Value;
                    node = node.Right;
                }

                return value;
            }
        }

        public bool Insert(TKey key, TValue value)
        {
            AvlNode<TKey, TValue> node = Root;

            while (node != null)
            {
                int compare = _comparer.Compare(key, node.Key);

                if (compare < 0)
                {
                    AvlNode<TKey, TValue> left = node.Left;

                    if (left == null)
                    {
                        node.Left = new AvlNode<TKey, TValue> { Key = key, Value = value, Parent = node };

                        InsertBalance(node, 1);

                        return true;
                    }
                    else
                    {
                        node = left;
                    }
                }
                else if (compare > 0)
                {
                    AvlNode<TKey, TValue> right = node.Right;

                    if (right == null)
                    {
                        node.Right = new AvlNode<TKey, TValue> { Key = key, Value = value, Parent = node };

                        InsertBalance(node, -1);

                        return true;
                    }
                    else
                    {
                        node = right;
                    }
                }
                else
                {
                    node.Value = value;

                    return false;
                }
            }

            Root = new AvlNode<TKey, TValue> { Key = key, Value = value };

            _count++;
            return true;
        }

        private void InsertBalance(AvlNode<TKey, TValue> node, int balance)
        {
            while (node != null)
            {
                balance = (node.Balance += balance);

                if (balance == 0)
                {
                    _count++;
                    return;
                }
                else if (balance == 2)
                {
                    if (node.Left.Balance == 1)
                    {
                        RotateRight(node);
                    }
                    else
                    {
                        RotateLeftRight(node);
                    }

                    _count++;
                    return;
                }
                else if (balance == -2)
                {
                    if (node.Right.Balance == -1)
                    {
                        RotateLeft(node);
                    }
                    else
                    {
                        RotateRightLeft(node);
                    }

                    _count++;
                    return;
                }

                AvlNode<TKey, TValue> parent = node.Parent;

                if (parent != null)
                {
                    balance = parent.Left == node ? 1 : -1;
                }

                node = parent;
            }

            _count++;
        }

        private AvlNode<TKey, TValue> RotateLeft(AvlNode<TKey, TValue> node)
        {
            AvlNode<TKey, TValue> right = node.Right;
            AvlNode<TKey, TValue> rightLeft = right.Left;
            AvlNode<TKey, TValue> parent = node.Parent;

            right.Parent = parent;
            right.Left = node;
            node.Right = rightLeft;
            node.Parent = right;

            if (rightLeft != null)
            {
                rightLeft.Parent = node;
            }

            if (node == Root)
            {
                Root = right;
            }
            else if (parent.Right == node)
            {
                parent.Right = right;
            }
            else
            {
                parent.Left = right;
            }

            right.Balance++;
            node.Balance = -right.Balance;

            return right;
        }

        private AvlNode<TKey, TValue> RotateRight(AvlNode<TKey, TValue> node)
        {
            AvlNode<TKey, TValue> left = node.Left;
            AvlNode<TKey, TValue> leftRight = left.Right;
            AvlNode<TKey, TValue> parent = node.Parent;

            left.Parent = parent;
            left.Right = node;
            node.Left = leftRight;
            node.Parent = left;

            if (leftRight != null)
            {
                leftRight.Parent = node;
            }

            if (node == Root)
            {
                Root = left;
            }
            else if (parent.Left == node)
            {
                parent.Left = left;
            }
            else
            {
                parent.Right = left;
            }

            left.Balance--;
            node.Balance = -left.Balance;

            return left;
        }

        private AvlNode<TKey, TValue> RotateLeftRight(AvlNode<TKey, TValue> node)
        {
            AvlNode<TKey, TValue> left = node.Left;
            AvlNode<TKey, TValue> leftRight = left.Right;
            AvlNode<TKey, TValue> parent = node.Parent;
            AvlNode<TKey, TValue> leftRightRight = leftRight.Right;
            AvlNode<TKey, TValue> leftRightLeft = leftRight.Left;

            leftRight.Parent = parent;
            node.Left = leftRightRight;
            left.Right = leftRightLeft;
            leftRight.Left = left;
            leftRight.Right = node;
            left.Parent = leftRight;
            node.Parent = leftRight;

            if (leftRightRight != null)
            {
                leftRightRight.Parent = node;
            }

            if (leftRightLeft != null)
            {
                leftRightLeft.Parent = left;
            }

            if (node == Root)
            {
                Root = leftRight;
            }
            else if (parent.Left == node)
            {
                parent.Left = leftRight;
            }
            else
            {
                parent.Right = leftRight;
            }

            if (leftRight.Balance == -1)
            {
                node.Balance = 0;
                left.Balance = 1;
            }
            else if (leftRight.Balance == 0)
            {
                node.Balance = 0;
                left.Balance = 0;
            }
            else
            {
                node.Balance = -1;
                left.Balance = 0;
            }

            leftRight.Balance = 0;

            return leftRight;
        }

        private AvlNode<TKey, TValue> RotateRightLeft(AvlNode<TKey, TValue> node)
        {
            AvlNode<TKey, TValue> right = node.Right;
            AvlNode<TKey, TValue> rightLeft = right.Left;
            AvlNode<TKey, TValue> parent = node.Parent;
            AvlNode<TKey, TValue> rightLeftLeft = rightLeft.Left;
            AvlNode<TKey, TValue> rightLeftRight = rightLeft.Right;

            rightLeft.Parent = parent;
            node.Right = rightLeftLeft;
            right.Left = rightLeftRight;
            rightLeft.Right = right;
            rightLeft.Left = node;
            right.Parent = rightLeft;
            node.Parent = rightLeft;

            if (rightLeftLeft != null)
            {
                rightLeftLeft.Parent = node;
            }

            if (rightLeftRight != null)
            {
                rightLeftRight.Parent = right;
            }

            if (node == Root)
            {
                Root = rightLeft;
            }
            else if (parent.Right == node)
            {
                parent.Right = rightLeft;
            }
            else
            {
                parent.Left = rightLeft;
            }

            if (rightLeft.Balance == 1)
            {
                node.Balance = 0;
                right.Balance = -1;
            }
            else if (rightLeft.Balance == 0)
            {
                node.Balance = 0;
                right.Balance = 0;
            }
            else
            {
                node.Balance = 1;
                right.Balance = 0;
            }

            rightLeft.Balance = 0;

            return rightLeft;
        }

        public bool Delete(TKey key)
        {
            AvlNode<TKey, TValue> node = Root;

            while (node != null)
            {
                if (_comparer.Compare(key, node.Key) < 0)
                {
                    node = node.Left;
                }
                else if (_comparer.Compare(key, node.Key) > 0)
                {
                    node = node.Right;
                }
                else
                {
                    AvlNode<TKey, TValue> left = node.Left;
                    AvlNode<TKey, TValue> right = node.Right;

                    if (left == null)
                    {
                        if (right == null)
                        {
                            if (node == Root)
                            {
                                Root = null;
                            }
                            else
                            {
                                AvlNode<TKey, TValue> parent = node.Parent;

                                if (parent.Left == node)
                                {
                                    parent.Left = null;

                                    DeleteBalance(parent, -1);
                                }
                                else
                                {
                                    parent.Right = null;

                                    DeleteBalance(parent, 1);
                                }
                            }
                        }
                        else
                        {
                            Replace(node, right);

                            DeleteBalance(node, 0);
                        }
                    }
                    else if (right == null)
                    {
                        Replace(node, left);

                        DeleteBalance(node, 0);
                    }
                    else
                    {
                        AvlNode<TKey, TValue> successor = right;

                        if (successor.Left == null)
                        {
                            AvlNode<TKey, TValue> parent = node.Parent;

                            successor.Parent = parent;
                            successor.Left = left;
                            successor.Balance = node.Balance;
                            left.Parent = successor;

                            if (node == Root)
                            {
                                Root = successor;
                            }
                            else
                            {
                                if (parent.Left == node)
                                {
                                    parent.Left = successor;
                                }
                                else
                                {
                                    parent.Right = successor;
                                }
                            }

                            DeleteBalance(successor, 1);
                        }
                        else
                        {
                            while (successor.Left != null)
                            {
                                successor = successor.Left;
                            }

                            AvlNode<TKey, TValue> parent = node.Parent;
                            AvlNode<TKey, TValue> successorParent = successor.Parent;
                            AvlNode<TKey, TValue> successorRight = successor.Right;

                            if (successorParent.Left == successor)
                            {
                                successorParent.Left = successorRight;
                            }
                            else
                            {
                                successorParent.Right = successorRight;
                            }

                            if (successorRight != null)
                            {
                                successorRight.Parent = successorParent;
                            }

                            successor.Parent = parent;
                            successor.Left = left;
                            successor.Balance = node.Balance;
                            successor.Right = right;
                            right.Parent = successor;
                            left.Parent = successor;

                            if (node == Root)
                            {
                                Root = successor;
                            }
                            else
                            {
                                if (parent.Left == node)
                                {
                                    parent.Left = successor;
                                }
                                else
                                {
                                    parent.Right = successor;
                                }
                            }

                            DeleteBalance(successorParent, -1);
                        }
                    }

                    _count--;
                    return true;
                }
            }

            return false;
        }

        private void DeleteBalance(AvlNode<TKey, TValue> node, int balance)
        {
            while (node != null)
            {
                balance = (node.Balance += balance);

                if (balance == 2)
                {
                    if (node.Left.Balance >= 0)
                    {
                        node = RotateRight(node);

                        if (node.Balance == -1)
                        {
                            return;
                        }
                    }
                    else
                    {
                        node = RotateLeftRight(node);
                    }
                }
                else if (balance == -2)
                {
                    if (node.Right.Balance <= 0)
                    {
                        node = RotateLeft(node);

                        if (node.Balance == 1)
                        {
                            return;
                        }
                    }
                    else
                    {
                        node = RotateRightLeft(node);
                    }
                }
                else if (balance != 0)
                {
                    return;
                }

                AvlNode<TKey, TValue> parent = node.Parent;

                if (parent != null)
                {
                    balance = parent.Left == node ? -1 : 1;
                }

                node = parent;
            }
        }

        private static void Replace(AvlNode<TKey, TValue> target, AvlNode<TKey, TValue> source)
        {
            AvlNode<TKey, TValue> left = source.Left;
            AvlNode<TKey, TValue> right = source.Right;

            target.Balance = source.Balance;
            target.Key = source.Key;
            target.Value = source.Value;
            target.Left = left;
            target.Right = right;

            if (left != null)
            {
                left.Parent = target;
            }

            if (right != null)
            {
                right.Parent = target;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
