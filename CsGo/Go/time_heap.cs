using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Go
{
    public abstract class MapNode<TKey, TValue>
    {
        public abstract MapNode<TKey, TValue> Next();
        public abstract MapNode<TKey, TValue> Prev();
        public abstract TKey Key { get; }
        public abstract TValue Value { get; set; }
        public abstract bool Isolated { get; }
    }

    public class Map<TKey, TValue>
    {
        enum rb_color
        {
            red,
            black
        }

        class Node : MapNode<TKey, TValue>
        {
            public TKey key;
            public TValue value;
            public Node parent;
            public Node left;
            public Node right;
            public rb_color color;
            public readonly bool nil;

            public Node(bool n)
            {
                nil = n;
            }

            public override MapNode<TKey, TValue> Next()
            {
                Node pNode = next(this);
                return pNode.nil ? null : pNode;
            }

            public override MapNode<TKey, TValue> Prev()
            {
                Node pNode = previous(this);
                return pNode.nil ? null : pNode;
            }

            public override TKey Key
            {
                get
                {
                    return key;
                }
            }

            public override TValue Value
            {
                get
                {
                    return value;
                }
                set
                {
                    this.value = value;
                }
            }

            public override bool Isolated
            {
                get
                {
                    return null == parent;
                }
            }
        }

        int _count;
        readonly bool _multi;
        readonly Node _head;

        public Map(bool multi = false)
        {
            _count = 0;
            _multi = multi;
            _head = new Node(true);
            _head.color = rb_color.black;
            root = lmost = rmost = _head;
        }

        static public MapNode<TKey, TValue> NewNode(TKey key, TValue value)
        {
            Node newNode = new Node(false);
            newNode.key = key;
            newNode.value = value;
            newNode.color = rb_color.red;
            return newNode;
        }

        public MapNode<TKey, TValue> ReNewNode(MapNode<TKey, TValue> oldNode, TKey key, TValue value)
        {
            if (!oldNode.Isolated)
            {
                Remove(oldNode);
            }
            Node newNode = (Node)oldNode;
            newNode.key = key;
            newNode.value = value;
            newNode.color = rb_color.red;
            return newNode;
        }

        static bool comp_lt<T>(T x, T y)
        {
            return Comparer<T>.Default.Compare(x, y) < 0;
        }

        static bool is_nil(Node node)
        {
            return node.nil;
        }

        Node root
        {
            get
            {
                return _head.parent;
            }
            set
            {
                _head.parent = value;
            }
        }

        Node lmost
        {
            get
            {
                return _head.left;
            }
            set
            {
                _head.left = value;
            }
        }

        Node rmost
        {
            get
            {
                return _head.right;
            }
            set
            {
                _head.right = value;
            }
        }

        void left_rotate(Node whereNode)
        {
            Node pNode = whereNode.right;
            whereNode.right = pNode.left;
            if (!is_nil(pNode.left))
            {
                pNode.left.parent = whereNode;
            }
            pNode.parent = whereNode.parent;
            if (whereNode == root)
            {
                root = pNode;
            }
            else if (whereNode == whereNode.parent.left)
            {
                whereNode.parent.left = pNode;
            }
            else
            {
                whereNode.parent.right = pNode;
            }
            pNode.left = whereNode;
            whereNode.parent = pNode;
        }

        void right_rotate(Node whereNode)
        {
            Node pNode = whereNode.left;
            whereNode.left = pNode.right;
            if (!is_nil(pNode.right))
            {
                pNode.right.parent = whereNode;
            }
            pNode.parent = whereNode.parent;
            if (whereNode == root)
            {
                root = pNode;
            }
            else if (whereNode == whereNode.parent.right)
            {
                whereNode.parent.right = pNode;
            }
            else
            {
                whereNode.parent.left = pNode;
            }
            pNode.right = whereNode;
            whereNode.parent = pNode;
        }

        void insert_at(bool addLeft, Node whereNode, Node newNode)
        {
            newNode.parent = whereNode;
            if (whereNode == _head)
            {
                root = lmost = rmost = newNode;
            }
            else if (addLeft)
            {
                whereNode.left = newNode;
                if (whereNode == lmost)
                {
                    lmost = newNode;
                }
            }
            else
            {
                whereNode.right = newNode;
                if (whereNode == rmost)
                {
                    rmost = newNode;
                }
            }
            for (Node pNode = newNode; rb_color.red == pNode.parent.color;)
            {
                if (pNode.parent == pNode.parent.parent.left)
                {
                    whereNode = pNode.parent.parent.right;
                    if (rb_color.red == whereNode.color)
                    {
                        pNode.parent.color = rb_color.black;
                        whereNode.color = rb_color.black;
                        pNode.parent.parent.color = rb_color.red;
                        pNode = pNode.parent.parent;
                    }
                    else
                    {
                        if (pNode == pNode.parent.right)
                        {
                            pNode = pNode.parent;
                            left_rotate(pNode);
                        }
                        pNode.parent.color = rb_color.black;
                        pNode.parent.parent.color = rb_color.red;
                        right_rotate(pNode.parent.parent);
                    }
                }
                else
                {
                    whereNode = pNode.parent.parent.left;
                    if (rb_color.red == whereNode.color)
                    {
                        pNode.parent.color = rb_color.black;
                        whereNode.color = rb_color.black;
                        pNode.parent.parent.color = rb_color.red;
                        pNode = pNode.parent.parent;
                    }
                    else
                    {
                        if (pNode == pNode.parent.left)
                        {
                            pNode = pNode.parent;
                            right_rotate(pNode);
                        }
                        pNode.parent.color = rb_color.black;
                        pNode.parent.parent.color = rb_color.red;
                        left_rotate(pNode.parent.parent);
                    }
                }
            }
            root.color = rb_color.black;
            _count++;
        }

        void insert(Node newNode)
        {
            newNode.parent = newNode.left = newNode.right = _head;
            Node tryNode = root;
            Node whereNode = _head;
            bool addLeft = true;
            while (!is_nil(tryNode))
            {
                whereNode = tryNode;
                addLeft = comp_lt(newNode.key, tryNode.key);
                tryNode = addLeft ? tryNode.left : tryNode.right;
            }
            if (_multi)
            {
                insert_at(addLeft, whereNode, newNode);
            }
            else
            {
                Node where = whereNode;
                if (!addLeft) { }
                else if (where == lmost)
                {
                    insert_at(true, whereNode, newNode);
                    return;
                }
                else
                {
                    where = previous(where);
                }
                if (comp_lt(where.value, newNode.value))
                {
                    insert_at(addLeft, whereNode, newNode);
                }
                else
                {
                    newNode.parent = newNode.left = newNode.right = null;
                }
            }
        }

        Node new_inter_node(TKey key, TValue value)
        {
            Node newNode = (Node)NewNode(key, value);
            newNode.parent = newNode.left = newNode.right = _head;
            return newNode;
        }

        Node insert(TKey key, TValue value, bool update = false)
        {
            Node tryNode = root;
            Node whereNode = _head;
            bool addLeft = true;
            while (!is_nil(tryNode))
            {
                whereNode = tryNode;
                addLeft = comp_lt(key, tryNode.key);
                tryNode = addLeft ? tryNode.left : tryNode.right;
            }
            Node newNode = null;
            if (_multi)
            {
                newNode = new_inter_node(key, value);
                insert_at(addLeft, whereNode, newNode);
            }
            else
            {
                Node where = whereNode;
                if (!addLeft) { }
                else if (where == lmost)
                {
                    newNode = new_inter_node(key, value);
                    insert_at(true, whereNode, newNode);
                    return newNode;
                }
                else
                {
                    where = previous(where);
                }
                if (comp_lt(where.value, value))
                {
                    newNode = new_inter_node(key, value);
                    insert_at(addLeft, whereNode, newNode);
                }
                else if (update)
                {
                    newNode = where;
                    where.value = value;
                }
            }
            return newNode;
        }

        void remove(Node where)
        {
            Node erasedNode = where;
            where = next(where);
            Node fixNode = null;
            Node fixNodeParent = null;
            Node pNode = erasedNode;
            if (is_nil(pNode.left))
            {
                fixNode = pNode.right;
            }
            else if (is_nil(pNode.right))
            {
                fixNode = pNode.left;
            }
            else
            {
                pNode = where;
                fixNode = pNode.right;
            }
            if (pNode == erasedNode)
            {
                fixNodeParent = erasedNode.parent;
                if (!is_nil(fixNode))
                {
                    fixNode.parent = fixNodeParent;
                }
                if (root == erasedNode)
                {
                    root = fixNode;
                }
                else if (fixNodeParent.left == erasedNode)
                {
                    fixNodeParent.left = fixNode;
                }
                else
                {
                    fixNodeParent.right = fixNode;
                }
                if (lmost == erasedNode)
                {
                    lmost = is_nil(fixNode) ? fixNodeParent : min(fixNode);
                }
                if (rmost == erasedNode)
                {
                    rmost = is_nil(fixNode) ? fixNodeParent : max(fixNode);
                }
            }
            else
            {
                erasedNode.left.parent = pNode;
                pNode.left = erasedNode.left;
                if (pNode == erasedNode.right)
                {
                    fixNodeParent = pNode;
                }
                else
                {
                    fixNodeParent = pNode.parent;
                    if (!is_nil(fixNode))
                    {
                        fixNode.parent = fixNodeParent;
                    }
                    fixNodeParent.left = fixNode;
                    pNode.right = erasedNode.right;
                    erasedNode.right.parent = pNode;
                }
                if (root == erasedNode)
                {
                    root = pNode;
                }
                else if (erasedNode.parent.left == erasedNode)
                {
                    erasedNode.parent.left = pNode;
                }
                else
                {
                    erasedNode.parent.right = pNode;
                }
                pNode.parent = erasedNode.parent;
                rb_color tcol = pNode.color;
                pNode.color = erasedNode.color;
                erasedNode.color = tcol;
            }
            if (rb_color.black == erasedNode.color)
            {
                for (; fixNode != root && rb_color.black == fixNode.color; fixNodeParent = fixNode.parent)
                {
                    if (fixNode == fixNodeParent.left)
                    {
                        pNode = fixNodeParent.right;
                        if (rb_color.red == pNode.color)
                        {
                            pNode.color = rb_color.black;
                            fixNodeParent.color = rb_color.red;
                            left_rotate(fixNodeParent);
                            pNode = fixNodeParent.right;
                        }
                        if (is_nil(pNode))
                        {
                            fixNode = fixNodeParent;
                        }
                        else if (rb_color.black == pNode.left.color && rb_color.black == pNode.right.color)
                        {
                            pNode.color = rb_color.red;
                            fixNode = fixNodeParent;
                        }
                        else
                        {
                            if (rb_color.black == pNode.right.color)
                            {
                                pNode.left.color = rb_color.black;
                                pNode.color = rb_color.red;
                                right_rotate(pNode);
                                pNode = fixNodeParent.right;
                            }
                            pNode.color = fixNodeParent.color;
                            fixNodeParent.color = rb_color.black;
                            pNode.right.color = rb_color.black;
                            left_rotate(fixNodeParent);
                            break;
                        }
                    }
                    else
                    {
                        pNode = fixNodeParent.left;
                        if (rb_color.red == pNode.color)
                        {
                            pNode.color = rb_color.black;
                            fixNodeParent.color = rb_color.red;
                            right_rotate(fixNodeParent);
                            pNode = fixNodeParent.left;
                        }
                        if (is_nil(pNode))
                        {
                            fixNode = fixNodeParent;
                        }
                        else if (rb_color.black == pNode.right.color && rb_color.black == pNode.left.color)
                        {
                            pNode.color = rb_color.red;
                            fixNode = fixNodeParent;
                        }
                        else
                        {
                            if (rb_color.black == pNode.left.color)
                            {
                                pNode.right.color = rb_color.black;
                                pNode.color = rb_color.red;
                                left_rotate(pNode);
                                pNode = fixNodeParent.left;
                            }
                            pNode.color = fixNodeParent.color;
                            fixNodeParent.color = rb_color.black;
                            pNode.left.color = rb_color.black;
                            right_rotate(fixNodeParent);
                            break;
                        }
                    }
                }
                fixNode.color = rb_color.black;
            }
            erasedNode.parent = erasedNode.left = erasedNode.right = null;
            _count--;
        }

        Node lbound(TKey key)
        {
            Node pNode = root;
            Node whereNode = _head;
            while (!is_nil(pNode))
            {
                if (comp_lt(pNode.key, key))
                {
                    pNode = pNode.right;
                }
                else
                {
                    whereNode = pNode;
                    pNode = pNode.left;
                }
            }
            return whereNode;
        }

        static Node max(Node pNode)
        {
            while (!is_nil(pNode.right))
            {
                pNode = pNode.right;
            }
            return pNode;
        }

        static Node min(Node pNode)
        {
            while (!is_nil(pNode.left))
            {
                pNode = pNode.left;
            }
            return pNode;
        }

        static Node next(Node ptr)
        {
            if (is_nil(ptr))
            {
                return ptr;
            }
            else if (!is_nil(ptr.right))
            {
                return min(ptr.right);
            }
            else
            {
                Node pNode;
                while (!is_nil(pNode = ptr.parent) && ptr == pNode.right)
                {
                    ptr = pNode;
                }
                return pNode;
            }
        }

        static Node previous(Node ptr)
        {
            if (is_nil(ptr))
            {
                return ptr;
            }
            else if (!is_nil(ptr.left))
            {
                return max(ptr.left);
            }
            else
            {
                Node pNode;
                while (!is_nil(pNode = ptr.parent) && ptr == pNode.left)
                {
                    ptr = pNode;
                }
                return pNode;
            }
        }

        static void erase(Node rootNode)
        {
            for (Node pNode = rootNode; !is_nil(pNode); rootNode = pNode)
            {
                erase(pNode.right);
                pNode = pNode.left;
                rootNode.parent = rootNode.left = rootNode.right = null;
            }
        }

        public int Count
        {
            get
            {
                return _count;
            }
        }

        public TValue this[TKey key]
        {
            set
            {
                Insert(key, value);
            }
        }

        public void Clear()
        {
            erase(root);
            root = lmost = rmost = _head;
            _count = 0;
        }

        public bool Has(TKey key)
        {
            return !is_nil(lbound(key));
        }

        public MapNode<TKey, TValue> Find(TKey key)
        {
            Node node = lbound(key);
            return is_nil(node) ? null : node;
        }

        public void Remove(MapNode<TKey, TValue> node)
        {
            remove((Node)node);
        }

        public MapNode<TKey, TValue> Insert(TKey key, TValue value)
        {
            return insert(key, value);
        }

        public MapNode<TKey, TValue> Update(TKey key, TValue value)
        {
            return insert(key, value, true);
        }

        public bool Insert(MapNode<TKey, TValue> newNode)
        {
            insert((Node)newNode);
            return !newNode.Isolated;
        }

        public MapNode<TKey, TValue> First
        {
            get
            {
                return is_nil(lmost) ? null : lmost;
            }
        }

        public MapNode<TKey, TValue> Last
        {
            get
            {
                return is_nil(rmost) ? null : rmost;
            }
        }
    }
}
