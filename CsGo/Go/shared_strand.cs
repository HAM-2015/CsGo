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
        LinkedList<functional.func> _opQueue;

        public work_service()
        {
            _work = 0;
            _waiting = 0;
            _runSign = true;
            _opQueue = new LinkedList<functional.func>();
        }

        public void push_option(functional.func handler)
        {
            Monitor.Enter(this);
            _opQueue.AddLast(handler);
            if (0 != _waiting)
            {
                _waiting--;
                Monitor.Pulse(this);
            }
            Monitor.Exit(this);
        }

        public bool run_one()
        {
            Monitor.Enter(this);
            if (_runSign && 0 != _opQueue.Count)
            {
                functional.func handler = _opQueue.First();
                _opQueue.RemoveFirst();
                Monitor.Exit(this);
                handler();
                return true;
            }
            Monitor.Exit(this);
            return false;
        }

        public long run()
        {
            long count = 0;
            while (_runSign)
            {
                Monitor.Enter(this);
                if (0 != _opQueue.Count)
                {
                    functional.func handler = _opQueue.First();
                    _opQueue.RemoveFirst();
                    Monitor.Exit(this);
                    count++;
                    handler();
                }
                else if (0 != _work)
                {
                    _waiting++;
                    Monitor.Wait(this);
                }
                else
                {
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
                Monitor.Enter(this);
                if (0 != _waiting)
                {
                    _waiting = 0;
                    Monitor.PulseAll(this);
                }
                Monitor.Exit(this);
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

        public void run(int threads = 1)
        {
            _service.reset();
            _service.hold_work();
            _runThreads = new Thread[threads];
            for (int i = 0; i < threads; ++i)
            {
                _runThreads[i] = new Thread(run_thread);
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
            foreach (Thread thread in _runThreads)
            {
                thread.Join();
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
        class curr_strand
        {
            public shared_strand strand;
        }
        static readonly ThreadLocal<curr_strand> _currStrand = new ThreadLocal<curr_strand>();

        public generator currSelf = null;
        protected volatile bool _locked;
        protected volatile int _pauseState;
        protected Mutex _mutex;
        protected LinkedList<functional.func> _readyQueue;
        protected LinkedList<functional.func> _waitQueue;

        public shared_strand()
        {
            _locked = false;
            _pauseState = 0;
            _mutex = new Mutex();
            _readyQueue = new LinkedList<functional.func>();
            _waitQueue = new LinkedList<functional.func>();
        }

        protected bool run_a_round1()
        {
            curr_strand currStrand = _currStrand.Value;
            if (null == currStrand)
            {
                currStrand = new curr_strand();
                _currStrand.Value = currStrand;
            }
            currStrand.strand = this;
            while (0 != _readyQueue.Count)
            {
                if (0 != _pauseState && 0 != Interlocked.CompareExchange(ref _pauseState, 2, 1))
                {
                    currStrand.strand = null;
                    return false;
                }
                try
                {
                    _readyQueue.First.Value.Invoke();
                }
                catch (System.Exception ec)
                {
                    MessageBox.Show(String.Format("Message:\n{0}\n{1}", ec.Message, ec.StackTrace), "shared_strand 内部未捕获的异常!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                _readyQueue.RemoveFirst();
            }
            _mutex.WaitOne();
            if (0 != _waitQueue.Count)
            {
                LinkedList<functional.func> t = _readyQueue;
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

        protected void run_a_round()
        {
            run_a_round1();
        }

        protected virtual void run_task()
        {
            Task.Run((Action)run_a_round);
        }

        public void post(functional.func action)
        {
            _mutex.WaitOne();
            if (_locked)
            {
                _waitQueue.AddLast(action);
                _mutex.ReleaseMutex();
            }
            else
            {
                _locked = true;
                _readyQueue.AddLast(action);
                _mutex.ReleaseMutex();
                run_task();
            }
        }

        public virtual bool distribute(functional.func action)
        {
            curr_strand currStrand = _currStrand.Value;
            if (null != currStrand && this == currStrand.strand)
            {
                functional.catch_invoke(action);
                return true;
            }
            else
            {
                _mutex.WaitOne();
                if (_locked)
                {
                    _waitQueue.AddLast(action);
                    _mutex.ReleaseMutex();
                }
                else
                {
                    _locked = true;
                    _readyQueue.AddLast(action);
                    _mutex.ReleaseMutex();
                    if (null == currStrand || null == currStrand.strand)
                    {
                        return run_a_round1();
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

        public functional.func wrap(functional.func handler)
        {
            return () => distribute(() => handler());
        }

        public functional.func<T1> wrap<T1>(functional.func<T1> handler)
        {
            return (T1 p1) => distribute(() => handler(p1));
        }

        public functional.func<T1, T2> wrap<T1, T2>(functional.func<T1, T2> handler)
        {
            return (T1 p1, T2 p2) => distribute(() => handler(p1, p2));
        }

        public functional.func<T1, T2, T3> wrap<T1, T2, T3>(functional.func<T1, T2, T3> handler)
        {
            return (T1 p1, T2 p2, T3 p3) => distribute(() => handler(p1, p2, p3));
        }

        public functional.func wrap_post(functional.func handler)
        {
            return () => post(() => handler());
        }

        public functional.func<T1> wrap_post<T1>(functional.func<T1> handler)
        {
            return (T1 p1) => post(() => handler(p1));
        }

        public functional.func<T1, T2> wrap_post<T1, T2>(functional.func<T1, T2> handler)
        {
            return (T1 p1, T2 p2) => post(() => handler(p1, p2));
        }

        public functional.func<T1, T2, T3> wrap_post<T1, T2, T3>(functional.func<T1, T2, T3> handler)
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

        protected override void run_task()
        {
            _service.hold_work();
            _service.push_option(delegate ()
            {
                run_a_round();
                _service.release_work();
            });
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

        public override bool distribute(functional.func action)
        {
            if (running_in_this_thread())
            {
                functional.catch_invoke(action);
                return true;
            }
            else if (_checkRequired && !_ctrl.InvokeRequired)
            {
                _mutex.WaitOne();
                if (_locked)
                {
                    _waitQueue.AddLast(action);
                    _mutex.ReleaseMutex();
                    return false;
                }
                else
                {
                    _locked = true;
                    _readyQueue.AddLast(action);
                    _mutex.ReleaseMutex();
                    return run_a_round1();
                }
            }
            else
            {
                post(action);
                return false;
            }
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
