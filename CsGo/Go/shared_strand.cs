using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

namespace Go
{
    public class work_service
    {
        int _work;
        int _waiting;
        volatile bool _runSign;
        LinkedList<Action> _opQueue;

        public work_service()
        {
            _work = 0;
            _waiting = 0;
            _runSign = true;
            _opQueue = new LinkedList<Action>();
        }

        public void push_option(Action handler)
        {
            LinkedListNode<Action> newNode = new LinkedListNode<Action>(handler);
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
                LinkedListNode<Action> firstNode = _opQueue.First;
                _opQueue.RemoveFirst();
                Monitor.Exit(_opQueue);
                firstNode.Value();
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
                    LinkedListNode<Action> firstNode = _opQueue.First;
                    _opQueue.RemoveFirst();
                    Monitor.Exit(_opQueue);
                    count++;
                    firstNode.Value();
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
    }

    public class work_engine
    {
        work_service _service;
        Thread[] _runThreads;

        public work_engine()
        {
            _service = new work_service();
        }

        public void run(int threads = 1, ThreadPriority priority = ThreadPriority.Normal, bool IsBackground = false)
        {
            _service.reset();
            _service.hold_work();
            _runThreads = new Thread[threads];
            for (int i = 0; i < threads; ++i)
            {
                _runThreads[i] = new Thread(run_thread);
                _runThreads[i].Priority = priority;
                _runThreads[i].IsBackground = IsBackground;
                _runThreads[i].Name = "任务调度";
                _runThreads[i].Start();
            }
        }

        void run_thread()
        {
            _service.run();
        }

        public void stop()
        {
            _service.release_work();
            for (int i = 0; i < _runThreads.Length; i++)
            {
                _runThreads[i].Join();
            }
            _runThreads = null;
        }

        public void force_stop()
        {
            _service.stop();
            stop();
        }

        public int threads()
        {
            return _runThreads.Length;
        }

        public work_service service()
        {
            return _service;
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

        internal readonly async_timer.steady_timer _timer;
        internal generator currSelf = null;
        protected volatile bool _locked;
        protected volatile int _pauseState;
        protected Mutex _mutex;
        protected LinkedList<Action> _readyQueue;
        protected LinkedList<Action> _waitQueue;
        protected LinkedList<Action> _nextTick;

        public shared_strand()
        {
            _locked = false;
            _pauseState = 0;
            _mutex = new Mutex();
            _timer = new async_timer.steady_timer(this);
            _readyQueue = new LinkedList<Action>();
            _waitQueue = new LinkedList<Action>();
            _nextTick = new LinkedList<Action>();
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
                functional.catch_invoke(_readyQueue.First.Value);
                _readyQueue.RemoveFirst();
                while (0 != _nextTick.Count)
                {
                    Action next = _nextTick.First.Value;
                    _nextTick.RemoveFirst();
                    functional.catch_invoke(next);
                }
            }
            _mutex.WaitOne();
            if (0 != _waitQueue.Count)
            {
                LinkedList<Action> t = _readyQueue;
                _readyQueue = _waitQueue;
                _waitQueue = t;
                _mutex.ReleaseMutex();
                currStrand.strand = null;
                run_task();
            }
            else
            {
                _locked = false;
                _mutex.ReleaseMutex();
                currStrand.strand = null;
            }
            return true;
        }

        protected virtual void run_task()
        {
            Task.Run((Action)run_a_round);
        }

        void run_a_round()
        {
            curr_strand currStrand = _currStrand.Value;
            if (null == currStrand)
            {
                currStrand = new curr_strand(true);
                _currStrand.Value = currStrand;
            }
            running_a_round(currStrand);
        }

        public void post(Action action)
        {
            LinkedListNode<Action> newNode = new LinkedListNode<Action>(action);
            _mutex.WaitOne();
            if (_locked)
            {
                _waitQueue.AddLast(newNode);
                _mutex.ReleaseMutex();
            }
            else
            {
                _locked = true;
                _readyQueue.AddLast(newNode);
                _mutex.ReleaseMutex();
                run_task();
            }
        }

        public virtual bool distribute(Action action)
        {
            curr_strand currStrand = _currStrand.Value;
            if (null != currStrand && this == currStrand.strand)
            {
                functional.catch_invoke(action);
                return true;
            }
            else
            {
                LinkedListNode<Action> newNode = new LinkedListNode<Action>(action);
                _mutex.WaitOne();
                if (_locked)
                {
                    _waitQueue.AddLast(newNode);
                    _mutex.ReleaseMutex();
                }
                else
                {
                    _locked = true;
                    _readyQueue.AddLast(newNode);
                    _mutex.ReleaseMutex();
                    if (null != currStrand && currStrand.work_back_thread && null == currStrand.strand)
                    {
                        return running_a_round(currStrand);
                    }
                    run_task();
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
                run_task();
            }
        }

        public bool running_in_this_thread()
        {
            return this == work_strand();
        }

        static public shared_strand work_strand()
        {
            curr_strand currStrand = _currStrand.Value;
            return null != currStrand ? currStrand.strand : null;
        }

        static public void next_tick(Action action)
        {
            shared_strand currStrand = work_strand();
#if DEBUG
            Trace.Assert(null != currStrand, "不正确的 next_tick 调用!");
#endif
            currStrand._nextTick.AddFirst(action);
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
            return () => distribute(() => handler());
        }

        public Action<T1> wrap<T1>(Action<T1> handler)
        {
            return (T1 p1) => distribute(() => handler(p1));
        }

        public Action<T1, T2> wrap<T1, T2>(Action<T1, T2> handler)
        {
            return (T1 p1, T2 p2) => distribute(() => handler(p1, p2));
        }

        public Action<T1, T2, T3> wrap<T1, T2, T3>(Action<T1, T2, T3> handler)
        {
            return (T1 p1, T2 p2, T3 p3) => distribute(() => handler(p1, p2, p3));
        }

        public Action wrap_post(Action handler)
        {
            return () => post(() => handler());
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
            _service = eng.service();
        }

        public override bool distribute(Action action)
        {
            curr_strand currStrand = _currStrand.Value;
            if (null != currStrand && this == currStrand.strand)
            {
                functional.catch_invoke(action);
                return true;
            }
            else
            {
                LinkedListNode<Action> newNode = new LinkedListNode<Action>(action);
                _mutex.WaitOne();
                if (_locked)
                {
                    _waitQueue.AddLast(newNode);
                    _mutex.ReleaseMutex();
                }
                else
                {
                    _locked = true;
                    _readyQueue.AddLast(newNode);
                    _mutex.ReleaseMutex();
                    if (null != currStrand && _service == currStrand.work_service && null == currStrand.strand)
                    {
                        return running_a_round(currStrand);
                    }
                    run_task();
                }
            }
            return false;
        }

        protected override void run_task()
        {
            _service.hold_work();
            _service.push_option(run_a_round);
        }

        void run_a_round()
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

        public override void hold_work()
        {
            _service.hold_work();
        }

        public override void release_work()
        {
            _service.release_work();
        }
    }

    public class control_strand : shared_strand
    {
        Control _ctrl;
        bool _checkRequired;

        public control_strand(Control ctrl, bool checkRequired = true) : base()
        {
            _ctrl = ctrl;
            _checkRequired = checkRequired;
        }

        public override bool distribute(Action action)
        {
            curr_strand currStrand = _currStrand.Value;
            if (null != currStrand && this == currStrand.strand)
            {
                functional.catch_invoke(action);
                return true;
            }
            else
            {
                LinkedListNode<Action> newNode = new LinkedListNode<Action>(action);
                _mutex.WaitOne();
                if (_locked)
                {
                    _waitQueue.AddLast(newNode);
                    _mutex.ReleaseMutex();
                }
                else
                {
                    _locked = true;
                    _readyQueue.AddLast(newNode);
                    _mutex.ReleaseMutex();
                    if (_checkRequired && null != currStrand && null == currStrand.strand && !_ctrl.InvokeRequired)
                    {
                        return running_a_round(currStrand);
                    }
                    run_task();
                }
            }
            return false;
        }

        protected override void run_task()
        {
            try
            {
                _ctrl.BeginInvoke((MethodInvoker)run_a_round);
            }
            catch (System.InvalidOperationException ec)
            {
                Trace.Fail(ec.Message, ec.StackTrace);
            }
        }

        void run_a_round()
        {
            curr_strand currStrand = _currStrand.Value;
            if (null == currStrand)
            {
                currStrand = new curr_strand();
                _currStrand.Value = currStrand;
            }
            running_a_round(currStrand);
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
}
