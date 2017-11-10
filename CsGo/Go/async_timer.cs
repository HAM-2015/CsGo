using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Go
{
    public class system_tick
    {
        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);
        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long frequency);

        private static system_tick _pcCycle = new system_tick();

        private double _sCycle;
        private double _msCycle;
        private double _usCycle;

        private system_tick()
        {
            long frep = 0;
            if (!QueryPerformanceFrequency(out frep))
            {
                _sCycle = 0;
                _msCycle = 0;
                _usCycle = 0;
                return;
            }
            _sCycle = 1.0 / (double)frep;
            _msCycle = 1000.0 / (double)frep;
            _usCycle = 1000000.0 / (double)frep;
        }

        public static long get_tick_us()
        {
            long quadPart = 0;
            QueryPerformanceCounter(out quadPart);
            return (long)((double)quadPart * _pcCycle._usCycle);
        }

        public static long get_tick_ms()
        {
            long quadPart = 0;
            QueryPerformanceCounter(out quadPart);
            return (long)((double)quadPart * _pcCycle._msCycle);
        }

        public static int get_tick_s()
        {
            long quadPart = 0;
            QueryPerformanceCounter(out quadPart);
            return (int)((double)quadPart * _pcCycle._sCycle);
        }
    }

    public class async_timer
    {
        abstract class HeapNode<TKey, TValue>
        {
            public abstract HeapNode<TKey, TValue> Next();
            public abstract HeapNode<TKey, TValue> Prev();
            public abstract TKey Key { get; }
            public abstract TValue Value { get; }
        }

        class TimeHeap<TKey, TValue>
        {
            enum rb_color
            {
                red,
                black
            }

            class Node : HeapNode<TKey, TValue>
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

                public override HeapNode<TKey, TValue> Next()
                {
                    Node pNode = next(this);
                    return pNode.nil ? null : pNode;
                }

                public override HeapNode<TKey, TValue> Prev()
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
                }
            }

            int _count;
            readonly Node _head;

            public TimeHeap()
            {
                _count = 0;
                _head = new Node(true);
                _head.color = rb_color.black;
                root = lmost = rmost = _head;
            }

            static public HeapNode<TKey, TValue> new_node(TKey key, TValue value)
            {
                Node newNode = new Node(false);
                newNode.key = key;
                newNode.value = value;
                newNode.color = rb_color.red;
                return newNode;
            }

            static bool comp_lt(TKey x, TKey y)
            {
                return Comparer<TKey>.Default.Compare(x, y) < 0;
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

            void insert(Node newNode)
            {
                _count++;
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

            public HeapNode<TKey, TValue> Find(TKey key)
            {
                return lbound(key);
            }

            public void Remove(HeapNode<TKey, TValue> node)
            {
                remove((Node)node);
            }

            public HeapNode<TKey, TValue> Insert(TKey key, TValue value)
            {
                Node newNode = (Node)new_node(key, value);
                insert(newNode);
                return newNode;
            }

            public void Insert(HeapNode<TKey, TValue> newNode)
            {
                insert((Node)newNode);
            }

            public HeapNode<TKey, TValue> First
            {
                get
                {
                    return is_nil(lmost) ? null : lmost;
                }
            }

            public HeapNode<TKey, TValue> Last
            {
                get
                {
                    return is_nil(rmost) ? null : rmost;
                }
            }
        }

        struct steady_timer_handle
        {
            public long absus;
            public long period;
            public HeapNode<long, async_timer> node;
        }

        public class steady_timer
        {
            struct waitable_event_handle
            {
                public int id;
                public steady_timer steadyTimer;

                public waitable_event_handle(int i, steady_timer h)
                {
                    id = i;
                    steadyTimer = h;
                }
            }

            class waitable_timer
            {
                [DllImport("kernel32.dll")]
                private static extern int CreateWaitableTimer(int lpTimerAttributes, int bManualReset, int lpTimerName);
                [DllImport("kernel32.dll")]
                private static extern int SetWaitableTimer(int hTimer, ref long pDueTime, int lPeriod, int pfnCompletionRoutine, int lpArgToCompletionRoutine, int fResume);
                [DllImport("kernel32.dll")]
                private static extern int CloseHandle(int hObject);
                [DllImport("kernel32.dll")]
                private static extern int WaitForSingleObject(int hHandle, int dwMilliseconds);
                [DllImport("NtDll.dll")]
                private static extern int NtQueryTimerResolution(out uint MaximumTime, out uint MinimumTime, out uint CurrentTime);
                [DllImport("NtDll.dll")]
                private static extern int NtSetTimerResolution(uint DesiredTime, uint SetResolution, out uint ActualTime);

                static public readonly waitable_timer timer = new waitable_timer();

                bool _exited;
                int _timerHandle;
                long _extMaxTick;
                long _extFinishTime;
                Thread _timerThread;
                work_engine _workEngine;
                work_strand _workStrand;
                TimeHeap<long, waitable_event_handle> _eventsQueue;

                waitable_timer()
                {
                    _exited = false;
                    _extMaxTick = 0;
                    _extFinishTime = long.MaxValue;
                    _eventsQueue = new TimeHeap<long, waitable_event_handle>();
                    _timerHandle = CreateWaitableTimer(0, 0, 0);
                    _workEngine = new work_engine();
                    _workStrand = new work_strand(_workEngine);
                    _timerThread = new Thread(timerThread);
                    _timerThread.Priority = ThreadPriority.Highest;
                    _timerThread.IsBackground = true;
                    _workEngine.run(1, ThreadPriority.Highest, true);
                    _timerThread.Start();
                    uint MaximumTime = 0, MinimumTime = 0, CurrentTime = 0, ActualTime = 0;
                    if (0 == NtQueryTimerResolution(out MaximumTime, out MinimumTime, out CurrentTime))
                    {
                        NtSetTimerResolution(MinimumTime, 1, out ActualTime);
                    }
                }

                ~waitable_timer()
                {
                    /*_workStrand.post(delegate ()
                    {
                        _exited = true;
                        long sleepTime = 0;
                        SetWaitableTimer(_timerHandle, ref sleepTime, 0, 0, 0, 0);
                    });
                    _timerThread.Join();
                    _workEngine.stop();*/
                    CloseHandle(_timerHandle);
                }

                public void appendEvent(long absus, waitable_event_handle eventHandle)
                {
                    _workStrand.post(delegate ()
                    {
                        if (absus > _extMaxTick)
                        {
                            _extMaxTick = absus;
                        }
                        eventHandle.steadyTimer._waitableNode = _eventsQueue.Insert(absus, eventHandle);
                        if (absus < _extFinishTime)
                        {
                            _extFinishTime = absus;
                            long sleepTime = -(absus - system_tick.get_tick_us()) * 10;
                            sleepTime = sleepTime < 0 ? sleepTime : 0;
                            SetWaitableTimer(_timerHandle, ref sleepTime, 0, 0, 0, 0);
                        }
                    });
                }

                public void removeEvent(steady_timer steadyTime)
                {
                    _workStrand.post(delegate ()
                    {
                        if (null != steadyTime._waitableNode)
                        {
                            long absus = steadyTime._waitableNode.Key;
                            _eventsQueue.Remove(steadyTime._waitableNode);
                            steadyTime._waitableNode = null;
                            if (0 == _eventsQueue.Count)
                            {
                                _extMaxTick = 0;
                                _extFinishTime = long.MaxValue;
                            }
                            else if (absus == _extMaxTick)
                            {
                                _extMaxTick = _eventsQueue.Last.Key;
                            }
                        }
                    });
                }

                private void timerThread()
                {
                    while (0 == WaitForSingleObject(_timerHandle, -1) && !_exited)
                    {
                        _workStrand.post(timerComplete);
                    }
                }

                private void timerComplete()
                {
                    _extFinishTime = long.MaxValue;
                    while (0 != _eventsQueue.Count)
                    {
                        HeapNode<long, waitable_event_handle> first = _eventsQueue.First;
                        long absus = first.Key;
                        steady_timer steadyTimer = first.Value.steadyTimer;
                        long ct = system_tick.get_tick_us();
                        if (absus > ct)
                        {
                            _extFinishTime = absus;
                            long sleepTime = -(absus - ct) * 10;
                            SetWaitableTimer(_timerHandle, ref sleepTime, 0, 0, 0, 0);
                            break;
                        }
                        _eventsQueue.Remove(steadyTimer._waitableNode);
                        steadyTimer._waitableNode = null;
                        steadyTimer.timer_handler(first.Value.id);
                    }
                }
            }

            bool _looping;
            int _timerCount;
            long _extMaxTick;
            long _extFinishTime;
            shared_strand _strand;
            HeapNode<long, waitable_event_handle> _waitableNode;
            TimeHeap<long, async_timer> _timerQueue;

            public steady_timer(shared_strand strand)
            {
                _timerCount = 0;
                _extMaxTick = 0;
                _looping = false;
                _extFinishTime = long.MaxValue;
                _strand = strand;
                _timerQueue = new TimeHeap<long, async_timer>();
            }

            public void timeout(async_timer asyncTimer)
            {
                long absus = asyncTimer._timerHandle.absus;
                if (absus > _extMaxTick)
                {
                    _extMaxTick = absus;
                }
                asyncTimer._timerHandle.node = _timerQueue.Insert(absus, asyncTimer);
                if (!_looping)
                {
                    _looping = true;
                    _extFinishTime = absus;
                    timer_loop(absus);
                }
                else if (absus < _extFinishTime)
                {
                    _timerCount++;
                    _extFinishTime = absus;
                    waitable_timer.timer.removeEvent(this);
                    timer_loop(absus);
                }
            }

            public void cancel(async_timer asyncTimer)
            {
                if (null != asyncTimer._timerHandle.node)
                {
                    long absus = asyncTimer._timerHandle.node.Key;
                    _timerQueue.Remove(asyncTimer._timerHandle.node);
                    asyncTimer._timerHandle.node = null;
                    if (0 == _timerQueue.Count)
                    {
                        _timerCount++;
                        _extMaxTick = 0;
                        _looping = false;
                        waitable_timer.timer.removeEvent(this);
                    }
                    else if (absus == _extMaxTick)
                    {
                        _extMaxTick = _timerQueue.Last.Key;
                    }
                }
            }

            public void timer_handler(int id)
            {
                _strand.post(delegate ()
                {
                    if (id == _timerCount)
                    {
                        _extFinishTime = 0;
                        while (0 != _timerQueue.Count)
                        {
                            HeapNode<long, async_timer> first = _timerQueue.First;
                            if (first.Key > system_tick.get_tick_us())
                            {
                                _extFinishTime = first.Key;
                                timer_loop(_extFinishTime);
                                return;
                            }
                            else
                            {
                                first.Value._timerHandle.node = null;
                                _timerQueue.Remove(first);
                                first.Value.timer_handler();
                            }
                        }
                        _looping = false;
                    }
                });
            }

            void timer_loop(long absus)
            {
                waitable_timer.timer.appendEvent(absus, new waitable_event_handle(++_timerCount, this));
            }
        }

        shared_strand _strand;
        functional.func _handler;
        steady_timer_handle _timerHandle;
        int _timerCount;
        long _beginTick;
        bool _isInterval;
        bool _onTopCall;

        public async_timer(shared_strand strand)
        {
            _strand = strand;
            _timerCount = 0;
            _beginTick = 0;
            _isInterval = false;
            _onTopCall = false;
        }

        public shared_strand self_strand()
        {
            return _strand;
        }

        private void timer_handler()
        {
            _onTopCall = true;
            if (_isInterval)
            {
                int lastTc = _timerCount;
                _handler();
                if (lastTc == _timerCount)
                {
                    begin_timer(_timerHandle.absus += _timerHandle.period, _timerHandle.period);
                }
            }
            else
            {
                functional.func handler = _handler;
                _handler = null;
                _strand.release_work();
                handler();
            }
            _onTopCall = false;
        }

        private void begin_timer(long absus, long period)
        {
            _timerCount++;
            _timerHandle.absus = absus;
            _timerHandle.period = period;
            _strand._timer.timeout(this);
        }

        public void timeout_us(long us, functional.func handler)
        {
#if DEBUG
            Trace.Assert(_strand.running_in_this_thread() && null == _handler && null != handler);
#endif
            _isInterval = false;
            _handler = handler;
            _strand.hold_work();
            _beginTick = system_tick.get_tick_us();
            begin_timer(_beginTick + us, us);
        }

        public void deadline_us(long us, functional.func handler)
        {
#if DEBUG
            Trace.Assert(_strand.running_in_this_thread() && null == _handler && null != handler);
#endif
            _isInterval = false;
            _handler = handler;
            _strand.hold_work();
            _beginTick = system_tick.get_tick_us();
            begin_timer(us, us - _beginTick);
        }

        public void timeout(int ms, functional.func handler)
        {
            timeout_us(ms * 1000, handler);
        }

        public void deadline(long ms, functional.func handler)
        {
            deadline_us(ms * 1000, handler);
        }

        public void interval(int ms, functional.func handler, bool immed = false)
        {
            interval_us(ms * 1000, handler, immed);
        }

        public void interval_us(long us, functional.func handler, bool immed = false)
        {
#if DEBUG
            Trace.Assert(_strand.running_in_this_thread() && null == _handler && null != handler);
#endif
            _isInterval = true;
            _handler = handler;
            _strand.hold_work();
            _beginTick = system_tick.get_tick_us();
            begin_timer(_beginTick + us, us);
            if (immed)
            {
                handler();
            }
        }

        public bool restart()
        {
#if DEBUG
            Trace.Assert(_strand.running_in_this_thread());
#endif
            if (null != _handler)
            {
                _strand._timer.cancel(this);
                _beginTick = system_tick.get_tick_us();
                begin_timer(_beginTick + _timerHandle.period, _timerHandle.period);
                return true;
            }
            return false;
        }

        public bool advance()
        {
#if DEBUG
            Trace.Assert(_strand.running_in_this_thread());
#endif
            if (null != _handler)
            {
                if (!_isInterval)
                {
                    functional.func handler = _handler;
                    cancel();
                    handler();
                    return true;
                }
                else if (!_onTopCall)
                {
                    _handler();
                    return true;
                }
            }
            return false;
        }

        public long cancel()
        {
            if (null != _handler)
            {
                _timerCount++;
                _strand._timer.cancel(this);
                long lastBegin = _beginTick;
                _beginTick = 0;
                _handler = null;
                _strand.release_work();
                return lastBegin;
            }
            return 0;
        }
    }
}
