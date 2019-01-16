using System;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace Go
{
    public class work_service
    {
        int _work;
        int _waiting;
        volatile bool _runSign;
        MsgQueue<Action> _opQueue;

        public work_service()
        {
            _work = 0;
            _waiting = 0;
            _runSign = true;
            _opQueue = new MsgQueue<Action>();
        }

        public void push_option(Action handler)
        {
            MsgQueueNode<Action> newNode = new MsgQueueNode<Action>(handler);
            Monitor.Enter(_opQueue);
            _opQueue.AddLast(newNode);
            if (0 != _waiting)
            {
                _waiting--;
                Monitor.Pulse(_opQueue);
            }
            Monitor.Exit(_opQueue);
        }

        public bool run_one()
        {
            Monitor.Enter(_opQueue);
            if (_runSign && 0 != _opQueue.Count)
            {
                MsgQueueNode<Action> firstNode = _opQueue.First;
                _opQueue.RemoveFirst();
                Monitor.Exit(_opQueue);
                functional.catch_invoke(firstNode.Value);
                return true;
            }
            Monitor.Exit(_opQueue);
            return false;
        }

        public long run()
        {
            long count = 0;
            while (_runSign)
            {
                Monitor.Enter(_opQueue);
                if (0 != _opQueue.Count)
                {
                    MsgQueueNode<Action> firstNode = _opQueue.First;
                    _opQueue.RemoveFirst();
                    Monitor.Exit(_opQueue);
                    count++;
                    functional.catch_invoke(firstNode.Value);
                }
                else if (0 != _work)
                {
                    _waiting++;
                    Monitor.Wait(_opQueue);
                    Monitor.Exit(_opQueue);
                }
                else
                {
                    Monitor.Exit(_opQueue);
                    break;
                }
            }
            return count;
        }

        public void stop()
        {
            _runSign = false;
        }

        public void reset()
        {
            _runSign = true;
        }

        public void hold_work()
        {
            Interlocked.Increment(ref _work);
        }

        public void release_work()
        {
            if (0 == Interlocked.Decrement(ref _work))
            {
                Monitor.Enter(_opQueue);
                if (0 != _waiting)
                {
                    _waiting = 0;
                    Monitor.PulseAll(_opQueue);
                }
                Monitor.Exit(_opQueue);
            }
        }

        public int count
        {
            get
            {
                return _opQueue.Count;
            }
        }
    }

    public class work_engine
    {
        work_service _service;
        Thread[] _runThreads;

        public work_engine()
        {
            _service = new work_service();
        }

        public void run(int threads = 1, ThreadPriority priority = ThreadPriority.Normal, bool background = false)
        {
            lock (this)
            {
                Trace.Assert(null == _runThreads, "work_engine 已经运行!");
                _service.reset();
                _service.hold_work();
                _runThreads = new Thread[threads];
                for (int i = 0; i < threads; ++i)
                {
                    _runThreads[i] = new Thread(() => _service.run());
                    _runThreads[i].Priority = priority;
                    _runThreads[i].IsBackground = background;
                    _runThreads[i].Name = "任务调度";
                    _runThreads[i].Start();
                }
            }
        }

        public void stop()
        {
            lock (this)
            {
                if (null != _runThreads)
                {
                    _service.release_work();
                    for (int i = 0; i < _runThreads.Length; i++)
                    {
                        _runThreads[i].Join();
                    }
                    _runThreads = null;
                }
            }
        }

        public void force_stop()
        {
            _service.stop();
            stop();
        }

        public int threads
        {
            get
            {
                return _runThreads.Length;
            }
        }

        public work_service service
        {
            get
            {
                return _service;
            }
        }
    }

    public class shared_strand
    {
        protected class curr_strand
        {
            public readonly bool work_back_thread;
            public readonly work_service work_service;
            public shared_strand strand;

            public curr_strand(bool workBackThread = false, work_service workService = null)
            {
                work_back_thread = workBackThread;
                work_service = workService;
            }
        }
        protected static readonly ThreadLocal<curr_strand> _currStrand = new ThreadLocal<curr_strand>();
        private static readonly shared_strand[] _defaultStrand = functional.init(delegate ()
        {
            shared_strand[] strands = new shared_strand[Environment.ProcessorCount];
            for (int i = 0; i < strands.Length; i++)
            {
                strands[i] = new shared_strand();
            }
            return strands;
        });

        internal readonly async_timer.steady_timer _sysTimer;
        internal readonly async_timer.steady_timer _utcTimer;
        internal generator currSelf = null;
        protected volatile bool _locked;
        protected volatile int _pauseState;
        protected MsgQueue<Action> _readyQueue;
        protected MsgQueue<Action> _waitQueue;
        protected Action _runTask;

        public shared_strand()
        {
            _locked = false;
            _pauseState = 0;
            _sysTimer = new async_timer.steady_timer(this, false);
            _utcTimer = new async_timer.steady_timer(this, true);
            _readyQueue = new MsgQueue<Action>();
            _waitQueue = new MsgQueue<Action>();
            _runTask = () => run_task();
        }

        protected bool running_a_round(curr_strand currStrand)
        {
            currStrand.strand = this;
            while (0 != _readyQueue.Count)
            {
                if (0 != _pauseState && 0 != Interlocked.CompareExchange(ref _pauseState, 2, 1))
                {
                    currStrand.strand = null;
                    return false;
                }
                Action stepHandler = _readyQueue.First.Value;
                _readyQueue.RemoveFirst();
                functional.catch_invoke(stepHandler);
            }
            MsgQueue<Action> waitQueue = _waitQueue;
            Monitor.Enter(this);
            if (0 != _waitQueue.Count)
            {
                _waitQueue = _readyQueue;
                Monitor.Exit(this);
                _readyQueue = waitQueue;
                currStrand.strand = null;
                next_a_round();
            }
            else
            {
                _locked = false;
                Monitor.Exit(this);
                currStrand.strand = null;
            }
            return true;
        }

        protected virtual void run_task()
        {
            curr_strand currStrand = _currStrand.Value;
            if (null == currStrand)
            {
                currStrand = new curr_strand(true);
                _currStrand.Value = currStrand;
            }
            running_a_round(currStrand);
        }

        protected virtual void next_a_round()
        {
            Task.Run(_runTask);
        }

        public int count
        {
            get
            {
                while (true)
                {
                    MsgQueue<Action> readyQueue = _readyQueue;
                    MsgQueue<Action> waitQueue = _waitQueue;
                    if (readyQueue != waitQueue)
                    {
                        return readyQueue.Count + waitQueue.Count;
                    }
                    Thread.Yield();
                }
            }
        }

        public void post(Action action)
        {
            MsgQueueNode<Action> newNode = new MsgQueueNode<Action>(action);
            Monitor.Enter(this);
            if (_locked)
            {
                _waitQueue.AddLast(newNode);
                Monitor.Exit(this);
            }
            else
            {
                _locked = true;
                _readyQueue.AddLast(newNode);
                Monitor.Exit(this);
                next_a_round();
            }
        }

        public void next_dispatch(Action action)
        {
            if (running_in_this_thread())
            {
                _readyQueue.AddFirst(action);
            }
            else
            {
                MsgQueueNode<Action> newNode = new MsgQueueNode<Action>(action);
                Monitor.Enter(this);
                if (_locked)
                {
                    _waitQueue.AddFirst(newNode);
                    Monitor.Exit(this);
                }
                else
                {
                    _locked = true;
                    _readyQueue.AddFirst(newNode);
                    Monitor.Exit(this);
                    next_a_round();
                }
            }
        }

        public void last_dispatch(Action action)
        {
            if (running_in_this_thread())
            {
                _readyQueue.AddLast(action);
            }
            else
            {
                MsgQueueNode<Action> newNode = new MsgQueueNode<Action>(action);
                Monitor.Enter(this);
                if (_locked)
                {
                    _waitQueue.AddLast(newNode);
                    Monitor.Exit(this);
                }
                else
                {
                    _locked = true;
                    _readyQueue.AddLast(newNode);
                    Monitor.Exit(this);
                    next_a_round();
                }
            }
        }

        public virtual bool dispatch(Action action)
        {
            curr_strand currStrand = _currStrand.Value;
            if (null != currStrand && this == currStrand.strand)
            {
                functional.catch_invoke(action);
                return true;
            }
            else
            {
                MsgQueueNode<Action> newNode = new MsgQueueNode<Action>(action);
                Monitor.Enter(this);
                if (_locked)
                {
                    _waitQueue.AddLast(newNode);
                    Monitor.Exit(this);
                }
                else
                {
                    _locked = true;
                    _readyQueue.AddLast(newNode);
                    Monitor.Exit(this);
                    if (null != currStrand && currStrand.work_back_thread && null == currStrand.strand)
                    {
                        return running_a_round(currStrand);
                    }
                    next_a_round();
                }
            }
            return false;
        }

        public void pause()
        {
            Interlocked.CompareExchange(ref _pauseState, 1, 0);
        }

        public void resume()
        {
            if (2 == Interlocked.Exchange(ref _pauseState, 0))
            {
                next_a_round();
            }
        }

        public bool running_in_this_thread()
        {
            return this == running_strand();
        }

        static public shared_strand running_strand()
        {
            curr_strand currStrand = _currStrand.Value;
            return null != currStrand ? currStrand.strand : null;
        }

        static public shared_strand default_strand()
        {
            curr_strand currStrand = _currStrand.Value;
            if (null != currStrand && null != currStrand.strand)
            {
                return currStrand.strand;
            }
            return _defaultStrand[mt19937.global.Next(0, _defaultStrand.Length)];
        }

        static public shared_strand global_strand()
        {
            return _defaultStrand[mt19937.global.Next(0, _defaultStrand.Length)];
        }

        static public void next_tick(Action action)
        {
            shared_strand currStrand = running_strand();
            Debug.Assert(null != currStrand, "不正确的 next_tick 调用!");
            currStrand._readyQueue.AddFirst(action);
        }

        static public void last_tick(Action action)
        {
            shared_strand currStrand = running_strand();
            Debug.Assert(null != currStrand, "不正确的 last_tick 调用!");
            currStrand._readyQueue.AddLast(action);
        }

        public void add_next(Action action)
        {
            Debug.Assert(running_in_this_thread(), "不正确的 add_next 调用!");
            _readyQueue.AddFirst(action);
        }

        public void add_last(Action action)
        {
            Debug.Assert(running_in_this_thread(), "不正确的 add_last 调用!");
            _readyQueue.AddLast(action);
        }

        public virtual bool wait_safe()
        {
            return !running_in_this_thread();
        }

        public virtual bool thread_safe()
        {
            return running_in_this_thread();
        }

        public virtual void hold_work()
        {
        }

        public virtual void release_work()
        {
        }

        public Action wrap(Action handler)
        {
            return () => dispatch(handler);
        }

        public Action<T1> wrap<T1>(Action<T1> handler)
        {
            return (T1 p1) => dispatch(() => handler(p1));
        }

        public Action<T1, T2> wrap<T1, T2>(Action<T1, T2> handler)
        {
            return (T1 p1, T2 p2) => dispatch(() => handler(p1, p2));
        }

        public Action<T1, T2, T3> wrap<T1, T2, T3>(Action<T1, T2, T3> handler)
        {
            return (T1 p1, T2 p2, T3 p3) => dispatch(() => handler(p1, p2, p3));
        }

        public Action wrap_post(Action handler)
        {
            return () => post(handler);
        }

        public Action<T1> wrap_post<T1>(Action<T1> handler)
        {
            return (T1 p1) => post(() => handler(p1));
        }

        public Action<T1, T2> wrap_post<T1, T2>(Action<T1, T2> handler)
        {
            return (T1 p1, T2 p2) => post(() => handler(p1, p2));
        }

        public Action<T1, T2, T3> wrap_post<T1, T2, T3>(Action<T1, T2, T3> handler)
        {
            return (T1 p1, T2 p2, T3 p3) => post(() => handler(p1, p2, p3));
        }
    }

    public class work_strand : shared_strand
    {
        work_service _service;

        public work_strand(work_service service) : base()
        {
            _service = service;
        }

        public work_strand(work_engine eng) : base()
        {
            _service = eng.service;
        }

        public override bool dispatch(Action action)
        {
            curr_strand currStrand = _currStrand.Value;
            if (null != currStrand && this == currStrand.strand)
            {
                functional.catch_invoke(action);
                return true;
            }
            else
            {
                MsgQueueNode<Action> newNode = new MsgQueueNode<Action>(action);
                Monitor.Enter(this);
                if (_locked)
                {
                    _waitQueue.AddLast(newNode);
                    Monitor.Exit(this);
                }
                else
                {
                    _locked = true;
                    _readyQueue.AddLast(newNode);
                    Monitor.Exit(this);
                    if (null != currStrand && _service == currStrand.work_service && null == currStrand.strand)
                    {
                        return running_a_round(currStrand);
                    }
                    next_a_round();
                }
            }
            return false;
        }

        protected override void run_task()
        {
            curr_strand currStrand = _currStrand.Value;
            if (null == currStrand)
            {
                currStrand = new curr_strand(false, _service);
                _currStrand.Value = currStrand;
            }
            running_a_round(currStrand);
            _service.release_work();
        }

        protected override void next_a_round()
        {
            _service.hold_work();
            _service.push_option(_runTask);
        }

        public override void hold_work()
        {
            _service.hold_work();
        }

        public override void release_work()
        {
            _service.release_work();
        }
    }

#if !NETCORE
    public class control_strand : shared_strand
    {
        public class repeat_exception : System.Exception
        {
            private control_strand _strand;

            internal repeat_exception(control_strand strand)
            {
                _strand = strand;
            }

            public control_strand strand
            {
                get
                {
                    return _strand;
                }
            }
        }

        static readonly ThreadLocal<control_strand> _UIThreadOnlyOneStrand = new ThreadLocal<control_strand>();
        System.Windows.Forms.Control _ctrl;
        bool _checkRequired;

        public control_strand(System.Windows.Forms.Control ctrl, bool checkRequired = true) : base()
        {
            control_strand checkUIStrand = _UIThreadOnlyOneStrand.Value;
            if (null != checkUIStrand)
            {
                throw new repeat_exception(checkUIStrand);
            }
            _UIThreadOnlyOneStrand.Value = this;
            _checkRequired = checkRequired;
            _ctrl = ctrl;
        }

        public override bool dispatch(Action action)
        {
            curr_strand currStrand = _currStrand.Value;
            if (null != currStrand && this == currStrand.strand)
            {
                functional.catch_invoke(action);
                return true;
            }
            else
            {
                MsgQueueNode<Action> newNode = new MsgQueueNode<Action>(action);
                Monitor.Enter(this);
                if (_locked)
                {
                    _waitQueue.AddLast(newNode);
                    Monitor.Exit(this);
                }
                else
                {
                    _locked = true;
                    _readyQueue.AddLast(newNode);
                    Monitor.Exit(this);
                    if (_checkRequired && null != currStrand && null == currStrand.strand && !_ctrl.InvokeRequired)
                    {
                        return running_a_round(currStrand);
                    }
                    next_a_round();
                }
            }
            return false;
        }

        protected override void run_task()
        {
            curr_strand currStrand = _currStrand.Value;
            if (null == currStrand)
            {
                currStrand = new curr_strand();
                _currStrand.Value = currStrand;
            }
            running_a_round(currStrand);
        }

        protected override void next_a_round()
        {
            try
            {
                _ctrl.BeginInvoke(_runTask);
            }
            catch (System.InvalidOperationException ec)
            {
                Trace.Fail(ec.Message, ec.StackTrace);
            }
        }

        public override bool wait_safe()
        {
            return _ctrl.InvokeRequired;
        }

        public override bool thread_safe()
        {
            return !_ctrl.InvokeRequired;
        }
    }
#endif
}
