using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Go
{
    struct multi_check
    {
        public bool callbacked;
        public bool beginQuit;

        public bool check()
        {
            bool t = callbacked;
            callbacked = true;
            return t;
        }
    }

    public class async_result_wrap<T1>
    {
        T1 p1;

        public virtual T1 value1
        {
            get { return p1; }
            set { p1 = value; }
        }

        public void clear()
        {
            p1 = default(T1);
        }
    }

    public class async_result_wrap<T1, T2>
    {
        T1 p1;
        T2 p2;

        public virtual T1 value1
        {
            get { return p1; }
            set { p1 = value; }
        }

        public virtual T2 value2
        {
            get { return p2; }
            set { p2 = value; }
        }

        public void clear()
        {
            p1 = default(T1);
            p2 = default(T2);
        }
    }

    public class async_result_wrap<T1, T2, T3>
    {
        T1 p1;
        T2 p2;
        T3 p3;

        public virtual T1 value1
        {
            get { return p1; }
            set { p1 = value; }
        }

        public virtual T2 value2
        {
            get { return p2; }
            set { p2 = value; }
        }

        public virtual T3 value3
        {
            get { return p3; }
            set { p3 = value; }
        }

        public void clear()
        {
            p1 = default(T1);
            p2 = default(T2);
            p3 = default(T3);
        }
    }

    public class async_result_ignore_wrap<T1> : async_result_wrap<T1>
    {
        static public async_result_ignore_wrap<T1> value = new async_result_ignore_wrap<T1>();

        public override T1 value1
        {
            set { }
        }
    }

    public class async_result_ignore_wrap<T1, T2> : async_result_wrap<T1, T2>
    {
        static public async_result_ignore_wrap<T1, T2> value = new async_result_ignore_wrap<T1, T2>();

        public override T1 value1
        {
            set { }
        }

        public override T2 value2
        {
            set { }
        }
    }

    public class async_result_ignore_wrap<T1, T2, T3> : async_result_wrap<T1, T2, T3>
    {
        static public async_result_ignore_wrap<T1, T2, T3> value = new async_result_ignore_wrap<T1, T2, T3>();

        public override T1 value1
        {
            set { }
        }

        public override T2 value2
        {
            set { }
        }

        public override T3 value3
        {
            set { }
        }
    }

    public class chan_lost_msg<T>
    {
        bool _has = false;
        T _msg;

        internal void set(T m)
        {
            _has = true;
            _msg = m;
        }

        public void clear()
        {
            _has = false;
            _msg = default(T);
        }

        public bool has
        {
            get
            {
                return _has;
            }
        }

        public T msg
        {
            get
            {
                return _msg;
            }
        }
    }

    public class chan_lost_msg<T1, T2> : chan_lost_msg<tuple<T1, T2>> { }
    public class chan_lost_msg<T1, T2, T3> : chan_lost_msg<tuple<T1, T2, T3>> { }

    public class chan_exception : System.Exception
    {
        public readonly chan_async_state state;

        public chan_exception(chan_async_state st)
        {
            state = st;
        }
    }

    public class csp_fail_exception : System.Exception
    {
        public static readonly csp_fail_exception val = new csp_fail_exception();
    }

    public struct chan_recv_wrap<T>
    {
        public chan_async_state state;
        public T msg;

        public static implicit operator T(chan_recv_wrap<T> rval)
        {
            if (chan_async_state.async_ok != rval.state)
            {
                throw new chan_exception(rval.state);
            }
            return rval.msg;
        }

        public static implicit operator chan_async_state(chan_recv_wrap<T> rval)
        {
            return rval.state;
        }

        public void check()
        {
            if (chan_async_state.async_ok != state)
            {
                throw new chan_exception(state);
            }
        }

        public override string ToString()
        {
            return chan_async_state.async_ok == state ?
                string.Format("chan_recv_wrap<{0}>.msg={1}", typeof(T).Name, msg) :
                string.Format("chan_recv_wrap<{0}>.state={1}", typeof(T).Name, state);
        }
    }

    public struct chan_send_wrap
    {
        public chan_async_state state;

        public static implicit operator chan_async_state(chan_send_wrap rval)
        {
            return rval.state;
        }

        public void check()
        {
            if (chan_async_state.async_ok != state)
            {
                throw new chan_exception(state);
            }
        }

        public override string ToString()
        {
            return state.ToString();
        }
    }

    public struct csp_invoke_wrap<T>
    {
        public chan_async_state state;
        public T result;

        public static implicit operator T(csp_invoke_wrap<T> rval)
        {
            if (chan_async_state.async_ok != rval.state)
            {
                throw new chan_exception(rval.state);
            }
            return rval.result;
        }

        public static implicit operator chan_async_state(csp_invoke_wrap<T> rval)
        {
            return rval.state;
        }

        public void check()
        {
            if (chan_async_state.async_ok != state)
            {
                throw new chan_exception(state);
            }
        }

        public override string ToString()
        {
            return chan_async_state.async_ok == state ?
                string.Format("csp_invoke_wrap<{0}>.result={1}", typeof(T).Name, result) :
                string.Format("csp_invoke_wrap<{0}>.state={1}", typeof(T).Name, state);
        }
    }

    public struct csp_wait_wrap<R, T>
    {
        public csp_chan<R, T>.csp_result result;
        public chan_async_state state;
        public T msg;

        public static implicit operator T(csp_wait_wrap<R, T> rval)
        {
            if (chan_async_state.async_ok != rval.state)
            {
                throw new chan_exception(rval.state);
            }
            return rval.msg;
        }

        public static implicit operator chan_async_state(csp_wait_wrap<R, T> rval)
        {
            return rval.state;
        }

        public void check()
        {
            if (chan_async_state.async_ok != state)
            {
                throw new chan_exception(state);
            }
        }

        public bool complete(R res)
        {
            return result.complete(res);
        }

        public void fail()
        {
            result.fail();
        }

        public bool empty()
        {
            return null == result;
        }

        public override string ToString()
        {
            return chan_async_state.async_ok == state ?
                string.Format("csp_wait_wrap<{0},{1}>.msg={2}", typeof(R).Name, typeof(T).Name, msg) :
                string.Format("csp_wait_wrap<{0},{1}>.state={2}", typeof(R).Name, typeof(T).Name, state);
        }
    }

    public struct ValueTask : INotifyCompletion
    {
        internal Task task;

        public static implicit operator ValueTask(Task rval)
        {
            return new ValueTask { task = rval };
        }

        public ValueTask GetAwaiter()
        {
            return this;
        }

        public void OnCompleted(Action continuation)
        {
            task.GetAwaiter().OnCompleted(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            task.GetAwaiter().UnsafeOnCompleted(continuation);
        }

        public bool IsCompleted
        {
            get
            {
                return null == task ? true : task.IsCompleted;
            }
        }

        public void GetResult()
        {
        }

        private ValueTask Case()
        {
            return this;
        }
    }

    public struct ValueTask<T> : INotifyCompletion
    {
        T value;
        Task<T> task;

        public static implicit operator ValueTask<T>(T rval)
        {
            return new ValueTask<T> { value = rval };
        }

        public static implicit operator ValueTask<T>(Task<T> rval)
        {
            return new ValueTask<T> { task = rval };
        }

        public static implicit operator ValueTask(ValueTask<T> rval)
        {
            return new ValueTask { task = rval.task };
        }

        public ValueTask<T> GetAwaiter()
        {
            return this;
        }

        public void OnCompleted(Action continuation)
        {
            task.GetAwaiter().OnCompleted(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            task.GetAwaiter().UnsafeOnCompleted(continuation);
        }

        public bool IsCompleted
        {
            get
            {
                return null == task ? true : task.IsCompleted;
            }
        }

        public T GetResult()
        {
            return null == task ? value : task.Result;
        }

        private ValueTask Case()
        {
            return new ValueTask { task = task };
        }
    }

    public class generator
    {
        public class stop_exception : System.Exception
        {
            public static readonly stop_exception val = new stop_exception();
        }

        public class stop_this_case_exception : System.Exception
        {
            public static readonly stop_this_case_exception val = new stop_this_case_exception();
        }

        public class stop_select_exception : System.Exception
        {
            public static readonly stop_select_exception val = new stop_select_exception();
        }

        public class stop_this_receive_exception : System.Exception
        {
            public static readonly stop_this_receive_exception val = new stop_this_receive_exception();
        }

        public class stop_all_receive_exception : System.Exception
        {
            public static readonly stop_all_receive_exception val = new stop_all_receive_exception();
        }

        class nil_task<R>
        {
            static Func<R> _func = () => default(R);
            static readonly ThreadLocal<Task<R>> _task = new ThreadLocal<Task<R>>();
            static readonly System.Reflection.FieldInfo _resField = typeof(Task<R>).GetField("m_result", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            public static Task<R> task(R value)
            {
                Task<R> currTask = _task.Value;
                if (null == currTask)
                {
                    currTask = new Task<R>(_func);
                    currTask.RunSynchronously();
                    _task.Value = currTask;
                }
                _resField.SetValue(currTask, value);
                return currTask;
            }
        }

        class type_hash<T>
        {
            public static readonly int code = Interlocked.Increment(ref _hashCount);
        }

        class mail_pck
        {
            public chan_base mailbox;
            public child agentAction;

            public mail_pck(chan_base mb)
            {
                mailbox = mb;
            }
        }

        class pull_task : INotifyCompletion
        {
            bool _completed = false;
            bool _activated = false;
            Action _continuation;

            public pull_task GetAwaiter()
            {
                return this;
            }

            public void GetResult()
            {
            }

            public void OnCompleted(Action continuation)
            {
                _continuation = continuation;
            }

            public bool IsCompleted
            {
                get
                {
                    return _completed;
                }
            }

            public bool is_awaiting()
            {
                return null != _continuation;
            }

            public bool activated
            {
                get
                {
                    return _activated;
                }
                set
                {
                    _activated = value;
                }
            }

            public void new_task()
            {
#if DEBUG
                Trace.Assert(_completed && _activated, "不对称的推入操作!");
#endif
                _completed = false;
                _activated = false;
            }

            public void ahead_complete()
            {
#if DEBUG
                Trace.Assert(!_completed, "不对称的拉取操作!");
#endif
                _completed = true;
            }

            public void complete()
            {
#if DEBUG
                Trace.Assert(!_completed, "不对称的拉取操作!");
#endif
                _completed = true;
                Action continuation = _continuation;
                _continuation = null;
                continuation();
            }
        }

#if DEBUG
        public class call_stack_info
        {
            public readonly string time;
            public readonly string file;
            public readonly int line;

            public call_stack_info(string t, string f, int l)
            {
                time = t;
                file = f;
                line = l;
            }

            public override string ToString()
            {
                return string.Format("<file>{0} <line>{1} <time>{2}", file, line, time);
            }
        }
        LinkedList<call_stack_info[]> _makeStack;
        long _beginStepTick;
        static readonly int _stepMaxCycle = 100;
#endif

        static int _hashCount = 0;
        static long _idCount = 0;
        static Task _nilTask = functional.init(() => { Task task = new Task(nil_action.action); task.RunSynchronously(); return task; });
        static ReaderWriterLockSlim _nameMutex = new ReaderWriterLockSlim();
        static Dictionary<string, generator> _nameGens = new Dictionary<string, generator>();

        Dictionary<long, mail_pck> _mailboxMap;
        LinkedList<LinkedList<select_chan_base>> _topSelectChans;
        LinkedList<children> _children;
        LinkedList<Action> _callbacks;
        Action<bool> _suspendCb;
        Action _setOvertime;
        chan_notify_sign _ioSign;
        System.Exception _excep;
        pull_task _pullTask;
        children _agentMng;
        async_timer _timer;
        object _selfValue;
        string _name;
        long _lastTm;
        long _yieldCount;
        long _lastYieldCount;
        long _id;
        int _lockCount;
        int _lockSuspendCount;
        bool _beginQuit;
        bool _isSuspend;
        bool _holdSuspend;
        bool _hasBlock;
        bool _overtime;
        bool _isForce;
        bool _isStop;
        bool _isRun;

        public delegate Task action();

        generator() { }

        static public generator make(shared_strand strand, action handler, Action callback = null, Action<bool> suspendCb = null)
        {
            return (new generator()).init(strand, handler, callback, suspendCb);
        }

        static public void go(shared_strand strand, action handler, Action callback = null, Action<bool> suspendCb = null)
        {
            (new generator()).init(strand, handler, callback, suspendCb).run();
        }

        static public generator tgo(shared_strand strand, action handler, Action callback = null, Action<bool> suspendCb = null)
        {
            return (new generator()).init(strand, handler, callback, suspendCb).trun();
        }

        static public generator make(string name, shared_strand strand, action handler, Action callback = null, Action<bool> suspendCb = null)
        {
            generator newGen = (new generator()).init(strand, handler, callback, suspendCb);
            newGen._name = name;
            try
            {
                _nameMutex.EnterWriteLock();
                _nameGens.Add(name, newGen);
                _nameMutex.ExitWriteLock();
            }
            catch (System.ArgumentException)
            {
                _nameMutex.ExitWriteLock();
#if DEBUG
                Trace.Fail(string.Format("generator {0}重名", name));
#endif
            }
            return newGen;
        }

        static public void go(string name, shared_strand strand, action handler, Action callback = null, Action<bool> suspendCb = null)
        {
            make(name, strand, handler, callback, suspendCb).run();
        }

        static public generator tgo(string name, shared_strand strand, action handler, Action callback = null, Action<bool> suspendCb = null)
        {
            return make(name, strand, handler, callback, suspendCb).trun();
        }

        static public generator find(string name)
        {
            generator gen = null;
            _nameMutex.EnterReadLock();
            _nameGens.TryGetValue(name, out gen);
            _nameMutex.ExitReadLock();
            return gen;
        }

        public string name()
        {
            return _name;
        }

#if DEBUG
        static void up_stack_frame(LinkedList<call_stack_info[]> callStack, int offset = 0, int count = 1)
        {
            offset += 2;
            StackFrame[] sts = (new StackTrace(true)).GetFrames();
            string time = string.Format("{0:D2}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}.{6:D3}",
                DateTime.Now.Year % 100, DateTime.Now.Month, DateTime.Now.Day,
                DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond);
            call_stack_info[] snap = new call_stack_info[count];
            callStack.AddFirst(snap);
            for (int i = 0; i < count; ++i, ++offset)
            {
                if (offset < sts.Length)
                {
                    snap[i] = new call_stack_info(time, sts[offset].GetFileName(), sts[offset].GetFileLineNumber());
                }
                else
                {
                    snap[i] = new call_stack_info(time, "null", -1);
                }
            }
        }
#endif

#if DEBUG
        generator init(shared_strand strand, action handler, Action callback, Action<bool> suspendCb, LinkedList<call_stack_info[]> makeStack = null)
        {
            if (null != makeStack)
            {
                _makeStack = makeStack;
            }
            else
            {
                _makeStack = new LinkedList<call_stack_info[]>();
                up_stack_frame(_makeStack, 1, 6);
            }
            _beginStepTick = system_tick.get_tick_ms();
#else
        generator init(shared_strand strand, action handler, Action callback, Action<bool> suspendCb)
        {
#endif
            _id = Interlocked.Increment(ref _idCount);
            _isForce = false;
            _isStop = false;
            _isRun = false;
            _overtime = false;
            _isSuspend = false;
            _holdSuspend = false;
            _hasBlock = false;
            _beginQuit = false;
            _lockCount = -1;
            _lockSuspendCount = 0;
            _lastTm = 0;
            _yieldCount = 0;
            _lastYieldCount = 0;
            _suspendCb = suspendCb;
            _pullTask = new pull_task();
            _ioSign = new chan_notify_sign();
            _timer = new async_timer(strand);
            strand.hold_work();
            strand.distribute(async delegate ()
            {
                try
                {
                    try
                    {
                        _lockCount = 0;
                        await async_wait();
                        await handler();
                    }
                    finally
                    {
                        _lockSuspendCount = -1;
                        if (null != _mailboxMap)
                        {
                            foreach (KeyValuePair<long, mail_pck> ele in _mailboxMap)
                            {
                                ele.Value.mailbox.close();
                            }
                        }
                    }
                    if (!_isForce && null != _children)
                    {
                        while (0 != _children.Count)
                        {
                            await _children.First.Value.wait_all();
                        }
                    }
                }
                catch (stop_exception) { }
                catch (System.Exception ec)
                {
#if DEBUG
#if NETCORE
                    Debug.WriteLine(string.Format("{0}\nMessage:\n{1}\n{2}", "generator 内部未捕获的异常!", ec.Message, ec.StackTrace));
#else
                    System.Windows.Forms.MessageBox.Show(string.Format("Message:\n{0}\n{1}", ec.Message, ec.StackTrace), "generator 内部未捕获的异常!", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
#endif
#endif
                    _excep = ec;
                }
                finally
                {
                    if (_isForce || null != _excep)
                    {
                        _timer.cancel();
                        if (null != _children && 0 != _children.Count)
                        {
                            children[] childs = new children[_children.Count];
                            _children.CopyTo(childs, 0);
                            await children.stop(childs);
                        }
                    }
                }
                if (null != _name)
                {
                    _nameMutex.EnterWriteLock();
                    _nameGens.Remove(_name);
                    _nameMutex.ExitWriteLock();
                }
                _isStop = true;
                _suspendCb = null;
                strand.currSelf = null;
                functional.catch_invoke(callback);
                if (null != _callbacks)
                {
                    while (0 != _callbacks.Count)
                    {
                        Action ntf = _callbacks.First.Value;
                        _callbacks.RemoveFirst();
                        functional.catch_invoke(ntf);
                    }
                }
                strand.release_work();
            });
            return this;
        }

        void no_check_next()
        {
            if (_isSuspend)
            {
                _hasBlock = true;
            }
            else if (!_pullTask.is_awaiting())
            {
                _pullTask.ahead_complete();
            }
            else
            {
                _yieldCount++;
                generator oldGen = strand.currSelf;
                if (null == oldGen || !oldGen._pullTask.activated)
                {
                    strand.currSelf = this;
                    _pullTask.complete();
                    strand.currSelf = oldGen;
                }
                else
                {
                    oldGen._pullTask.activated = false;
                    strand.currSelf = this;
                    _pullTask.complete();
                    strand.currSelf = oldGen;
                    oldGen._pullTask.activated = true;
                }
            }
        }

        void next(bool beginQuit)
        {
            if (!_isStop && _beginQuit == beginQuit)
            {
                no_check_next();
            }
        }

        void quit_next()
        {
            next(true);
        }

        void no_quit_next()
        {
            next(false);
        }

        multi_check new_multi_check()
        {
            _pullTask.new_task();
            return new multi_check { callbacked = false, beginQuit = _beginQuit };
        }

        public void run()
        {
            strand.distribute(delegate ()
            {
                if (-1 == _lockCount)
                {
                    trun();
                }
                else if (!_isRun && !_isStop)
                {
                    _isRun = true;
                    no_check_next();
                }
            });
        }

        public generator trun()
        {
            strand.post(delegate ()
            {
                if (!_isRun && !_isStop)
                {
                    _isRun = true;
                    no_check_next();
                }
            });
            return this;
        }

        private void _suspend_cb(bool isSuspend, Action cb = null, bool canSuspendCb = true)
        {
            if (null != _children && 0 != _children.Count)
            {
                int count = _children.Count;
                Action suspendCb = delegate ()
                {
                    if (0 == --count)
                    {
                        functional.catch_invoke(canSuspendCb ? _suspendCb : null, isSuspend);
                        functional.catch_invoke(cb);
                    }
                };
                children[] tempChildren = new children[_children.Count];
                _children.CopyTo(tempChildren, 0);
                for (int i = 0; i < tempChildren.Length; i++)
                {
                    tempChildren[i].suspend(isSuspend, suspendCb);
                }
            }
            else
            {
                functional.catch_invoke(canSuspendCb ? _suspendCb : null, isSuspend);
                functional.catch_invoke(cb);
            }
        }

        private void _suspend(Action cb = null)
        {
            if (!_isStop && !_beginQuit && !_isSuspend)
            {
                if (0 == _lockSuspendCount)
                {
                    _isSuspend = true;
                    if (0 != _lastTm)
                    {
                        _lastTm -= system_tick.get_tick_us() - _timer.cancel();
                        if (_lastTm <= 0)
                        {
                            _lastTm = 0;
                            _hasBlock = true;
                        }
                    }
                    _suspend_cb(true, cb);
                }
                else
                {
                    _holdSuspend = true;
                    _suspend_cb(true, cb, false);
                }
            }
            else
            {
                functional.catch_invoke(cb);
            }
        }

        public void tick_suspend(Action cb = null)
        {
            strand.post(() => _suspend(cb));
        }

        public void suspend(Action cb = null)
        {
            strand.distribute(delegate ()
            {
                if (-1 == _lockCount)
                {
                    tick_suspend(cb);
                }
                else
                {
                    _suspend(cb);
                }
            });
        }

        private void _resume(Action cb = null)
        {
            if (!_isStop && !_beginQuit)
            {
                if (_isSuspend)
                {
                    _isSuspend = false;
                    long lastYieldCount = _yieldCount;
                    _suspend_cb(false, cb);
                    if (lastYieldCount == _yieldCount && !_isStop && !_beginQuit && !_isSuspend)
                    {
                        if (_hasBlock)
                        {
                            _hasBlock = false;
                            no_quit_next();
                        }
                        else if (0 != _lastTm)
                        {
                            _timer.timeout_us(_lastTm, no_check_next);
                        }
                    }
                }
                else
                {
                    _holdSuspend = false;
                    _suspend_cb(false, cb, false);
                }
            }
            else
            {
                functional.catch_invoke(cb);
            }
        }

        public void tick_resume(Action cb = null)
        {
            strand.post(() => _resume(cb));
        }

        public void resume(Action cb = null)
        {
            strand.distribute(delegate ()
            {
                if (-1 == _lockCount)
                {
                    tick_resume(cb);
                }
                else
                {
                    _resume(cb);
                }
            });
        }

        private void _stop()
        {
            _isForce = true;
            if (0 == _lockCount)
            {
                _isSuspend = false;
                if (_pullTask.activated)
                {
                    _lockSuspendCount = 0;
                    _holdSuspend = false;
                    _beginQuit = true;
                    _suspendCb = null;
                    _timer.cancel();
                    throw stop_exception.val;
                }
                else if (_pullTask.is_awaiting())
                {
                    no_quit_next();
                }
                else
                {
                    delay_stop();
                }
            }
        }

        public void delay_stop()
        {
            strand.post(delegate ()
            {
                if (!_isStop)
                {
                    _stop();
                }
            });
        }

        public void stop()
        {
            if (strand.running_in_this_thread())
            {
                if (-1 == _lockCount)
                {
                    delay_stop();
                }
                else if (!_isStop)
                {
                    _stop();
                }
            }
            else
            {
                delay_stop();
            }
        }

        public void delay_stop(Action continuation)
        {
            strand.post(delegate ()
            {
                if (!_isStop)
                {
                    if (null == _callbacks)
                    {
                        _callbacks = new LinkedList<Action>();
                    }
                    _callbacks.AddLast(continuation);
                    _stop();
                }
                else
                {
                    functional.catch_invoke(continuation);
                }
            });
        }

        public void stop(Action continuation)
        {
            if (strand.running_in_this_thread())
            {
                if (-1 == _lockCount)
                {
                    delay_stop(continuation);
                }
                else if (!_isStop)
                {
                    if (null == _callbacks)
                    {
                        _callbacks = new LinkedList<Action>();
                    }
                    _callbacks.AddLast(continuation);
                    _stop();
                }
                else
                {
                    functional.catch_invoke(continuation);
                }
            }
            else
            {
                delay_stop(continuation);
            }
        }

        public void append_stop_callback(Action continuation, Action<LinkedListNode<Action>> removeCb = null)
        {
            strand.distribute(delegate ()
            {
                if (!_isStop)
                {
                    if (null == _callbacks)
                    {
                        _callbacks = new LinkedList<Action>();
                    }
                    functional.catch_invoke(removeCb, _callbacks.AddLast(continuation));
                }
                else
                {
                    functional.catch_invoke(continuation);
                    functional.catch_invoke(removeCb, null);
                }
            });
        }

        public void remove_stop_callback(LinkedListNode<Action> node, Action cb = null)
        {
            strand.distribute(delegate ()
            {
                if (null != node.List)
                {
                    _callbacks.Remove(node);
                }
                functional.catch_invoke(cb);
            });
        }

        public bool is_force()
        {
#if DEBUG
            Trace.Assert(_isStop, "不正确的 is_force 调用，generator 还没有结束");
#endif
            return _isForce;
        }

        public bool is_exception()
        {
#if DEBUG
            Trace.Assert(_isStop, "不正确的 is_exception 调用，generator 还没有结束");
#endif
            return null != _excep;
        }

        public System.Exception exception()
        {
#if DEBUG
            Trace.Assert(_isStop, "不正确的 exception 调用，generator 还没有结束");
#endif
            return _excep;
        }

        public bool is_completed()
        {
            return _isStop;
        }

        static public bool begin_quit()
        {
            generator this_ = self;
            return this_._beginQuit;
        }

        static public bool check_quit()
        {
            generator this_ = self;
            return this_._isForce;
        }

        static public bool check_suspend()
        {
            generator this_ = self;
            return this_._holdSuspend;
        }

        public void sync_wait()
        {
#if DEBUG
            Trace.Assert(strand.wait_safe(), "不正确的 sync_wait 调用!");
#endif
            wait_group wg = new wait_group(1);
            stop(wg.wrap_done());
            wg.sync_wait();
        }

        public bool sync_timed_wait(int ms)
        {
#if DEBUG
            Trace.Assert(strand.wait_safe(), "不正确的 sync_timed_wait 调用!");
#endif
            wait_group wg = new wait_group(1);
            stop(wg.wrap_done());
            return wg.sync_timed_wait(ms);
        }

        static public R sync_go<R>(shared_strand strand, Func<Task<R>> handler)
        {
#if DEBUG
            Trace.Assert(strand.wait_safe(), "不正确的 sync_go 调用!");
#endif
            R res = default(R);
            System.Exception hasExcep = null;
            wait_group wg = new wait_group(1);
            go(strand, async delegate ()
            {
                try
                {
                    res = await handler();
                }
                catch (stop_exception)
                {
                    throw;
                }
                catch (System.Exception ec)
                {
                    hasExcep = ec;
                }
            }, wg.wrap_done());
            wg.sync_wait();
            if (null != hasExcep)
            {
                throw hasExcep;
            }
            return res;
        }

        static public void sync_go(shared_strand strand, action handler)
        {
#if DEBUG
            Trace.Assert(strand.wait_safe(), "不正确的 sync_go 调用!");
#endif
            System.Exception hasExcep = null;
            wait_group wg = new wait_group(1);
            go(strand, async delegate ()
            {
                try
                {
                    await handler();
                }
                catch (stop_exception)
                {
                    throw;
                }
                catch (System.Exception ec)
                {
                    hasExcep = ec;
                }
            }, wg.wrap_done());
            wg.sync_wait();
            if (null != hasExcep)
            {
                throw hasExcep;
            }
        }

        static public Task hold()
        {
            generator this_ = self;
            this_._pullTask.new_task();
            return this_.async_wait();
        }

        static public Task pause_self()
        {
            generator this_ = self;
            if (!this_._beginQuit)
            {
                if (0 == this_._lockSuspendCount)
                {
                    this_._isSuspend = true;
                    this_._suspend_cb(true);
                    if (this_._isSuspend)
                    {
                        this_._hasBlock = true;
                        this_._pullTask.new_task();
                        return this_.async_wait();
                    }
                }
                else
                {
                    this_._holdSuspend = true;
                }
            }
            return non_async();
        }

        static public Task halt_self()
        {
            generator this_ = self;
            this_.stop();
            return non_async();
        }

        static public void lock_stop()
        {
            generator this_ = self;
            if (!this_._beginQuit)
            {
                this_._lockCount++;
            }
        }

        static public void unlock_stop()
        {
            generator this_ = self;
#if DEBUG
            if (!this_._beginQuit)
            {
                Trace.Assert(this_._lockCount > 0, "unlock_stop 不匹配");
            }
#endif
            if (!this_._beginQuit && 0 == --this_._lockCount && this_._isForce)
            {
                this_._lockSuspendCount = 0;
                this_._holdSuspend = false;
                this_._beginQuit = true;
                this_._suspendCb = null;
                this_._timer.cancel();
                throw stop_exception.val;
            }
        }

        static public async Task lock_stop(Func<Task> handler)
        {
            lock_stop();
            try
            {
                await handler();
            }
            finally
            {
                unlock_stop();
            }
        }

        static public async Task<R> lock_stop<R>(Func<Task<R>> handler)
        {
            lock_stop();
            try
            {
                return await handler();
            }
            finally
            {
                unlock_stop();
            }
        }

        static public void lock_suspend()
        {
            generator this_ = self;
            if (!this_._beginQuit)
            {
                this_._lockSuspendCount++;
            }
        }

        static public Task unlock_suspend()
        {
            generator this_ = self;
#if DEBUG
            if (!this_._beginQuit)
            {
                Trace.Assert(this_._lockSuspendCount > 0, "unlock_suspend 不匹配");
            }
#endif
            if (!this_._beginQuit && 0 == --this_._lockSuspendCount && this_._holdSuspend)
            {
                this_._holdSuspend = false;
                this_._isSuspend = true;
                this_._suspend_cb(true);
                if (this_._isSuspend)
                {
                    this_._hasBlock = true;
                    this_._pullTask.new_task();
                    return this_.async_wait();
                }
            }
            return non_async();
        }

        static public async Task lock_suspend(Func<Task> handler)
        {
            lock_suspend();
            try
            {
                await handler();
            }
            finally
            {
                await unlock_suspend();
            }
        }

        static public async Task<R> lock_suspend<R>(Func<Task<R>> handler)
        {
            lock_suspend();
            try
            {
                return await handler();
            }
            finally
            {
                await unlock_suspend();
            }
        }

        static public void lock_suspend_and_stop()
        {
            generator this_ = self;
            if (!this_._beginQuit)
            {
                this_._lockSuspendCount++;
                this_._lockCount++;
            }
        }

        static public Task unlock_suspend_and_stop()
        {
            generator this_ = self;
#if DEBUG
            if (!this_._beginQuit)
            {
                Trace.Assert(this_._lockCount > 0, "unlock_stop 不匹配");
                Trace.Assert(this_._lockSuspendCount > 0, "unlock_suspend 不匹配");
            }
#endif
            if (!this_._beginQuit && 0 == --this_._lockCount && this_._isForce)
            {
                this_._lockSuspendCount = 0;
                this_._holdSuspend = false;
                this_._beginQuit = true;
                this_._suspendCb = null;
                this_._timer.cancel();
                throw stop_exception.val;
            }
            if (!this_._beginQuit && 0 == --this_._lockSuspendCount && this_._holdSuspend)
            {
                this_._holdSuspend = false;
                this_._isSuspend = true;
                this_._suspend_cb(true);
                if (this_._isSuspend)
                {
                    this_._hasBlock = true;
                    this_._pullTask.new_task();
                    return this_.async_wait();
                }
            }
            return non_async();
        }

        static public async Task lock_suspend_and_stop(Func<Task> handler)
        {
            lock_suspend_and_stop();
            try
            {
                await handler();
            }
            finally
            {
                await unlock_suspend_and_stop();
            }
        }

        static public async Task<R> lock_suspend_and_stop<R>(Func<Task<R>> handler)
        {
            lock_suspend_and_stop();
            try
            {
                return await handler();
            }
            finally
            {
                await unlock_suspend_and_stop();
            }
        }

        private void enter_push()
        {
#if DEBUG
            Trace.Assert(strand.running_in_this_thread(), "异常的 await 调用!");
            if (!system_tick.check_step_debugging() && system_tick.get_tick_ms() - _beginStepTick > _stepMaxCycle)
            {
                call_stack_info[] stackHead = _makeStack.Last.Value;
                Debug.WriteLine(string.Format("单步超时:\n{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n",
                    stackHead[0], stackHead[1], stackHead[2], stackHead[3], stackHead[4], stackHead[5], new StackTrace(true)));
            }
#endif
        }

        private void leave_push()
        {
#if DEBUG
            _beginStepTick = system_tick.get_tick_ms();
#endif
            _lastTm = 0;
            _pullTask.activated = true;
            if (!_beginQuit && 0 == _lockCount && _isForce)
            {
                _lockSuspendCount = 0;
                _holdSuspend = false;
                _beginQuit = true;
                _suspendCb = null;
                _timer.cancel();
                throw stop_exception.val;
            }
        }

        private async Task push_task()
        {
            enter_push();
            await _pullTask;
            leave_push();
        }

        private async Task<T> push_task<T>(async_result_wrap<T> res)
        {
            enter_push();
            await _pullTask;
            leave_push();
            return res.value1;
        }

        private async Task<tuple<T1, T2>> push_task<T1, T2>(async_result_wrap<T1, T2> res)
        {
            enter_push();
            await _pullTask;
            leave_push();
            return tuple.make(res.value1, res.value2);
        }

        private async Task<tuple<T1, T2, T3>> push_task<T1, T2, T3>(async_result_wrap<T1, T2, T3> res)
        {
            enter_push();
            await _pullTask;
            leave_push();
            return tuple.make(res.value1, res.value2, res.value3);
        }

        private bool new_task_completed()
        {
            if (_pullTask.IsCompleted)
            {
                enter_push();
                leave_push();
                return true;
            }
            return false;
        }

        public Task async_wait()
        {
            if (!new_task_completed())
            {
                return push_task();
            }
            return non_async();
        }

        public ValueTask<T> async_wait<T>(async_result_wrap<T> res)
        {
            if (!new_task_completed())
            {
                return push_task(res);
            }
            return res.value1;
        }

        public ValueTask<tuple<T1, T2>> async_wait<T1, T2>(async_result_wrap<T1, T2> res)
        {
            if (!new_task_completed())
            {
                return push_task(res);
            }
            return tuple.make(res.value1, res.value2);
        }

        public ValueTask<tuple<T1, T2, T3>> async_wait<T1, T2, T3>(async_result_wrap<T1, T2, T3> res)
        {
            if (!new_task_completed())
            {
                return push_task(res);
            }
            return tuple.make(res.value1, res.value2, res.value3);
        }

        public SameAction unsafe_async_same_callback()
        {
            _pullTask.new_task();
            return _beginQuit ? (SameAction)delegate (object[] args)
            {
                strand.distribute(quit_next);
            }
            : delegate (object[] args)
            {
                strand.distribute(no_quit_next);
            };
        }

        public SameAction unsafe_async_same_callback(SameAction handler)
        {
            _pullTask.new_task();
            return _beginQuit ? (SameAction)delegate (object[] args)
            {
                handler(args);
                strand.distribute(quit_next);
            }
            : delegate (object[] args)
            {
                handler(args);
                strand.distribute(no_quit_next);
            };
        }

        public SameAction timed_async_same_callback(int ms, Action timedHandler = null, SameAction lostHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    if (null != timedHandler)
                    {
                        timedHandler();
                    }
                    else if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate (object[] args)
            {
                if (multiCheck.callbacked)
                {
                    lostHandler?.Invoke(args);
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        no_check_next();
                    }
                    else lostHandler?.Invoke(args);
                });
            };
        }

        public SameAction timed_async_same_callback2(int ms, Action timedHandler = null, SameAction lostHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    functional.catch_invoke(timedHandler);
                    if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate (object[] args)
            {
                if (multiCheck.callbacked)
                {
                    lostHandler?.Invoke(args);
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        no_check_next();
                    }
                    else lostHandler?.Invoke(args);
                });
            };
        }

        public SameAction timed_async_same_callback(int ms, SameAction handler, Action timedHandler = null, SameAction lostHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    if (null != timedHandler)
                    {
                        timedHandler();
                    }
                    else if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate (object[] args)
            {
                if (multiCheck.callbacked)
                {
                    lostHandler?.Invoke(args);
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        handler(args);
                        no_check_next();
                    }
                    else lostHandler?.Invoke(args);
                });
            };
        }

        public SameAction timed_async_same_callback2(int ms, SameAction handler, Action timedHandler = null, SameAction lostHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    functional.catch_invoke(timedHandler);
                    if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate (object[] args)
            {
                if (multiCheck.callbacked)
                {
                    lostHandler?.Invoke(args);
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        handler(args);
                        no_check_next();
                    }
                    else lostHandler?.Invoke(args);
                });
            };
        }

        public Action unsafe_async_callback(Action handler)
        {
            _pullTask.new_task();
            return _beginQuit ? (Action)delegate ()
            {
                handler();
                strand.distribute(quit_next);
            }
            : delegate ()
            {
                handler();
                strand.distribute(no_quit_next);
            };
        }

        public Action<T1> unsafe_async_callback<T1>(Action<T1> handler)
        {
            _pullTask.new_task();
            return _beginQuit ? (Action<T1>)delegate (T1 p1)
            {
                handler(p1);
                strand.distribute(quit_next);
            }
            : delegate (T1 p1)
            {
                handler(p1);
                strand.distribute(no_quit_next);
            };
        }

        public Action<T1, T2> unsafe_async_callback<T1, T2>(Action<T1, T2> handler)
        {
            _pullTask.new_task();
            return _beginQuit ? (Action<T1, T2>)delegate (T1 p1, T2 p2)
            {
                handler(p1, p2);
                strand.distribute(quit_next);
            }
            : delegate (T1 p1, T2 p2)
            {
                handler(p1, p2);
                strand.distribute(no_quit_next);
            };
        }

        public Action<T1, T2, T3> unsafe_async_callback<T1, T2, T3>(Action<T1, T2, T3> handler)
        {
            _pullTask.new_task();
            return _beginQuit ? (Action<T1, T2, T3>)delegate (T1 p1, T2 p2, T3 p3)
            {
                handler(p1, p2, p3);
                strand.distribute(quit_next);
            }
            : delegate (T1 p1, T2 p2, T3 p3)
            {
                handler(p1, p2, p3);
                strand.distribute(no_quit_next);
            };
        }

        public Action timed_async_callback(int ms, Action handler, Action timedHandler = null, Action lostHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    if (null != timedHandler)
                    {
                        timedHandler();
                    }
                    else if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate ()
            {
                if (multiCheck.callbacked)
                {
                    lostHandler?.Invoke();
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        handler();
                        no_check_next();
                    }
                    else lostHandler?.Invoke();
                });
            };
        }

        public Action timed_async_callback2(int ms, Action handler, Action timedHandler = null, Action lostHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    functional.catch_invoke(timedHandler);
                    if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate ()
            {
                if (multiCheck.callbacked)
                {
                    lostHandler?.Invoke();
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        handler();
                        no_check_next();
                    }
                    else lostHandler?.Invoke();
                });
            };
        }

        public Action<T1> timed_async_callback<T1>(int ms, Action<T1> handler, Action timedHandler = null, Action<T1> lostHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    if (null != timedHandler)
                    {
                        timedHandler();
                    }
                    else if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate (T1 p1)
            {
                if (multiCheck.callbacked)
                {
                    lostHandler?.Invoke(p1);
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        handler(p1);
                        no_check_next();
                    }
                    else lostHandler?.Invoke(p1);
                });
            };
        }

        public Action<T1> timed_async_callback2<T1>(int ms, Action<T1> handler, Action timedHandler = null, Action<T1> lostHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    functional.catch_invoke(timedHandler);
                    if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate (T1 p1)
            {
                if (multiCheck.callbacked)
                {
                    lostHandler?.Invoke(p1);
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        handler(p1);
                        no_check_next();
                    }
                    else lostHandler?.Invoke(p1);
                });
            };
        }

        public Action<T1, T2> timed_async_callback<T1, T2>(int ms, Action<T1, T2> handler, Action timedHandler = null, Action<T1, T2> lostHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    if (null != timedHandler)
                    {
                        timedHandler();
                    }
                    else if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate (T1 p1, T2 p2)
            {
                if (multiCheck.callbacked)
                {
                    lostHandler?.Invoke(p1, p2);
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        handler(p1, p2);
                        no_check_next();
                    }
                    else lostHandler?.Invoke(p1, p2);
                });
            };
        }

        public Action<T1, T2> timed_async_callback2<T1, T2>(int ms, Action<T1, T2> handler, Action timedHandler = null, Action<T1, T2> lostHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    functional.catch_invoke(timedHandler);
                    if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate (T1 p1, T2 p2)
            {
                if (multiCheck.callbacked)
                {
                    lostHandler?.Invoke(p1, p2);
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        handler(p1, p2);
                        no_check_next();
                    }
                    else lostHandler?.Invoke(p1, p2);
                });
            };
        }

        public Action<T1, T2, T3> timed_async_callback<T1, T2, T3>(int ms, Action<T1, T2, T3> handler, Action timedHandler = null, Action<T1, T2, T3> lostHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    if (null != timedHandler)
                    {
                        timedHandler();
                    }
                    else if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                if (multiCheck.callbacked)
                {
                    lostHandler?.Invoke(p1, p2, p3);
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        handler(p1, p2, p3);
                        no_check_next();
                    }
                    else lostHandler?.Invoke(p1, p2, p3);
                });
            };
        }

        public Action<T1, T2, T3> timed_async_callback2<T1, T2, T3>(int ms, Action<T1, T2, T3> handler, Action timedHandler = null, Action<T1, T2, T3> lostHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    functional.catch_invoke(timedHandler);
                    if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                if (multiCheck.callbacked)
                {
                    lostHandler?.Invoke(p1, p2, p3);
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        handler(p1, p2, p3);
                        no_check_next();
                    }
                    else lostHandler?.Invoke(p1, p2, p3);
                });
            };
        }

        public SameAction async_same_callback(SameAction lostHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            return delegate (object[] args)
            {
                if (multiCheck.callbacked)
                {
                    lostHandler?.Invoke(args);
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                    else lostHandler?.Invoke(args);
                });
            };
        }

        public SameAction async_same_callback(SameAction handler, SameAction lostHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            return delegate (object[] args)
            {
                if (multiCheck.callbacked)
                {
                    lostHandler?.Invoke(args);
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        handler(args);
                        no_check_next();
                    }
                    else lostHandler?.Invoke(args);
                });
            };
        }

        public Action async_callback(Action handler, Action lostHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            return delegate ()
            {
                if (multiCheck.callbacked)
                {
                    lostHandler?.Invoke();
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        handler();
                        no_check_next();
                    }
                    else lostHandler?.Invoke();
                });
            };
        }

        public Action<T1> async_callback<T1>(Action<T1> handler, Action<T1> lostHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            return delegate (T1 p1)
            {
                if (multiCheck.callbacked)
                {
                    lostHandler?.Invoke(p1);
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        handler(p1);
                        no_check_next();
                    }
                    else lostHandler?.Invoke(p1);
                });
            };
        }

        public Action<T1, T2> async_callback<T1, T2>(Action<T1, T2> handler, Action<T1, T2> lostHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            return delegate (T1 p1, T2 p2)
            {
                if (multiCheck.callbacked)
                {
                    lostHandler?.Invoke(p1, p2);
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        handler(p1, p2);
                        no_check_next();
                    }
                    else lostHandler?.Invoke(p1, p2);
                });
            };
        }

        public Action<T1, T2, T3> async_callback<T1, T2, T3>(Action<T1, T2, T3> handler, Action<T1, T2, T3> lostHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                if (multiCheck.callbacked)
                {
                    lostHandler?.Invoke(p1, p2, p3);
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        handler(p1, p2, p3);
                        no_check_next();
                    }
                    else lostHandler?.Invoke(p1, p2, p3);
                });
            };
        }

        private Action _async_result()
        {
            _pullTask.new_task();
            return _beginQuit ? (Action)quit_next : no_quit_next;
        }

        public Action unsafe_async_result()
        {
            _pullTask.new_task();
            return _beginQuit ? strand.wrap(quit_next) : strand.wrap(no_quit_next);
        }

        public Action<T1> unsafe_async_result<T1>(async_result_wrap<T1> res)
        {
            _pullTask.new_task();
            return _beginQuit ? (Action<T1>)delegate (T1 p1)
            {
                res.value1 = p1;
                strand.distribute(quit_next);
            }
            : delegate (T1 p1)
            {
                res.value1 = p1;
                strand.distribute(no_quit_next);
            };
        }

        public Action<T1, T2> unsafe_async_result<T1, T2>(async_result_wrap<T1, T2> res)
        {
            _pullTask.new_task();
            return _beginQuit ? (Action<T1, T2>)delegate (T1 p1, T2 p2)
            {
                res.value1 = p1;
                res.value2 = p2;
                strand.distribute(quit_next);
            }
            : delegate (T1 p1, T2 p2)
            {
                res.value1 = p1;
                res.value2 = p2;
                strand.distribute(no_quit_next);
            };
        }

        public Action<T1, T2, T3> unsafe_async_result<T1, T2, T3>(async_result_wrap<T1, T2, T3> res)
        {
            _pullTask.new_task();
            return _beginQuit ? (Action<T1, T2, T3>)delegate (T1 p1, T2 p2, T3 p3)
            {
                res.value1 = p1;
                res.value2 = p2;
                res.value3 = p3;
                strand.distribute(quit_next);
            }
            : delegate (T1 p1, T2 p2, T3 p3)
            {
                res.value1 = p1;
                res.value2 = p2;
                res.value3 = p3;
                strand.distribute(no_quit_next);
            };
        }

        public Action<T1> unsafe_async_ignore<T1>()
        {
            return unsafe_async_result(async_result_ignore_wrap<T1>.value);
        }

        public Action<T1, T2> unsafe_async_ignore<T1, T2>()
        {
            return unsafe_async_result(async_result_ignore_wrap<T1, T2>.value);
        }

        public Action<T1, T2, T3> unsafe_async_ignore<T1, T2, T3>()
        {
            return unsafe_async_result(async_result_ignore_wrap<T1, T2, T3>.value);
        }

        public Action async_result()
        {
            multi_check multiCheck = new_multi_check();
            return delegate ()
            {
                if (multiCheck.callbacked)
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            };
        }

        public Action<T1> async_result<T1>(async_result_wrap<T1> res)
        {
            multi_check multiCheck = new_multi_check();
            return delegate (T1 p1)
            {
                if (multiCheck.callbacked)
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        res.value1 = p1;
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1, T2> async_result<T1, T2>(async_result_wrap<T1, T2> res)
        {
            multi_check multiCheck = new_multi_check();
            return delegate (T1 p1, T2 p2)
            {
                if (multiCheck.callbacked)
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        res.value1 = p1;
                        res.value2 = p2;
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1, T2, T3> async_result<T1, T2, T3>(async_result_wrap<T1, T2, T3> res)
        {
            multi_check multiCheck = new_multi_check();
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                if (multiCheck.callbacked)
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        res.value1 = p1;
                        res.value2 = p2;
                        res.value3 = p3;
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1> async_ignore<T1>()
        {
            return async_result(async_result_ignore_wrap<T1>.value);
        }

        public Action<T1, T2> async_ignore<T1, T2>()
        {
            return async_result(async_result_ignore_wrap<T1, T2>.value);
        }

        public Action<T1, T2, T3> async_ignore<T1, T2, T3>()
        {
            return async_result(async_result_ignore_wrap<T1, T2, T3>.value);
        }

        public Action timed_async_result(int ms, Action timedHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    if (null != timedHandler)
                    {
                        timedHandler();
                    }
                    else if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate ()
            {
                if (multiCheck.callbacked)
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1> timed_async_result<T1>(int ms, async_result_wrap<T1> res, Action timedHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    if (null != timedHandler)
                    {
                        timedHandler();
                    }
                    else if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate (T1 p1)
            {
                if (multiCheck.callbacked)
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        res.value1 = p1;
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1, T2> timed_async_result<T1, T2>(int ms, async_result_wrap<T1, T2> res, Action timedHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    if (null != timedHandler)
                    {
                        timedHandler();
                    }
                    else if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate (T1 p1, T2 p2)
            {
                if (multiCheck.callbacked)
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        res.value1 = p1;
                        res.value2 = p2;
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1, T2, T3> timed_async_result<T1, T2, T3>(int ms, async_result_wrap<T1, T2, T3> res, Action timedHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    if (null != timedHandler)
                    {
                        timedHandler();
                    }
                    else if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                if (multiCheck.callbacked)
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        res.value1 = p1;
                        res.value2 = p2;
                        res.value3 = p3;
                        no_check_next();
                    }
                });
            };
        }

        public Action timed_async_result2(int ms, Action timedHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    functional.catch_invoke(timedHandler);
                    if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate ()
            {
                if (multiCheck.callbacked)
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1> timed_async_result2<T1>(int ms, async_result_wrap<T1> res, Action timedHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    functional.catch_invoke(timedHandler);
                    if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate (T1 p1)
            {
                if (multiCheck.callbacked)
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        res.value1 = p1;
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1, T2> timed_async_result2<T1, T2>(int ms, async_result_wrap<T1, T2> res, Action timedHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    functional.catch_invoke(timedHandler);
                    if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate (T1 p1, T2 p2)
            {
                if (multiCheck.callbacked)
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        res.value1 = p1;
                        res.value2 = p2;
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1, T2, T3> timed_async_result2<T1, T2, T3>(int ms, async_result_wrap<T1, T2, T3> res, Action timedHandler = null)
        {
            multi_check multiCheck = new_multi_check();
            if (ms >= 0)
            {
                _timer.timeout(ms, delegate ()
                {
                    functional.catch_invoke(timedHandler);
                    if (!multiCheck.check())
                    {
                        next(multiCheck.beginQuit);
                    }
                });
            }
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                if (multiCheck.callbacked)
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCheck.check() && !_isStop && _beginQuit == multiCheck.beginQuit)
                    {
                        _timer.cancel();
                        res.value1 = p1;
                        res.value2 = p2;
                        res.value3 = p3;
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1> timed_async_ignore<T1>(int ms, Action timedHandler = null)
        {
            return timed_async_result(ms, async_result_ignore_wrap<T1>.value, timedHandler);
        }

        public Action<T1, T2> timed_async_ignore<T1, T2>(int ms, Action timedHandler = null)
        {
            return timed_async_result(ms, async_result_ignore_wrap<T1, T2>.value, timedHandler);
        }

        public Action<T1, T2, T3> timed_async_ignore<T1, T2, T3>(int ms, Action timedHandler = null)
        {
            return timed_async_result(ms, async_result_ignore_wrap<T1, T2, T3>.value, timedHandler);
        }

        public Action<T1> timed_async_ignore2<T1>(int ms, Action timedHandler = null)
        {
            return timed_async_result2(ms, async_result_ignore_wrap<T1>.value, timedHandler);
        }

        public Action<T1, T2> timed_async_ignore2<T1, T2>(int ms, Action timedHandler = null)
        {
            return timed_async_result2(ms, async_result_ignore_wrap<T1, T2>.value, timedHandler);
        }

        public Action<T1, T2, T3> timed_async_ignore2<T1, T2, T3>(int ms, Action timedHandler = null)
        {
            return timed_async_result2(ms, async_result_ignore_wrap<T1, T2, T3>.value, timedHandler);
        }

        static public Task usleep(long us)
        {
            if (us > 0)
            {
                generator this_ = self;
                this_._lastTm = us;
                this_._timer.timeout_us(us, this_._async_result());
                return this_.async_wait();
            }
            else if (us < 0)
            {
                return hold();
            }
            else
            {
                return yield();
            }
        }

        static public Task sleep(int ms)
        {
            return usleep((long)ms * 1000);
        }

        static public Task deadline(long ms)
        {
            generator this_ = self;
            this_._timer.deadline(ms, this_._async_result());
            return this_.async_wait();
        }

        static public Task udeadline(long us)
        {
            generator this_ = self;
            this_._timer.deadline_us(us, this_._async_result());
            return this_.async_wait();
        }

        static public Task yield()
        {
            generator this_ = self;
            this_.strand.post(this_._async_result());
            return this_.async_wait();
        }

        static public Task try_yield()
        {
            generator this_ = self;
            if (this_._lastYieldCount == this_._yieldCount)
            {
                this_._lastYieldCount = this_._yieldCount + 1;
                this_.strand.post(this_._async_result());
                return this_.async_wait();
            }
            this_._lastYieldCount = this_._yieldCount;
            return non_async();
        }

        static public generator self
        {
            get
            {
                shared_strand currStrand = shared_strand.running_strand();
                return null != currStrand ? currStrand.currSelf : null;
            }
        }

        static public shared_strand self_strand()
        {
            generator this_ = self;
            return null != this_ ? this_.strand : null;
        }

        static public async_timer self_timer()
        {
            generator this_ = self;
            return this_._timer;
        }

        public shared_strand strand
        {
            get
            {
                return _timer.self_strand();
            }
        }

        static public long self_id()
        {
            generator this_ = self;
            return null != this_ ? this_._id : 0;
        }

        static public generator self_parent()
        {
            generator this_ = self;
            return this_.parent();
        }

        public virtual generator parent()
        {
            return null;
        }

        public long id
        {
            get
            {
                return _id;
            }
        }

        public long yield_count()
        {
            return _yieldCount;
        }

        static public long self_count()
        {
            generator this_ = self;
            return this_._yieldCount;
        }

        static public Task suspend_other(generator otherGen)
        {
            generator this_ = self;
            otherGen.suspend(this_.unsafe_async_result());
            return this_.async_wait();
        }

        static public Task resume_other(generator otherGen)
        {
            generator this_ = self;
            otherGen.resume(this_.unsafe_async_result());
            return this_.async_wait();
        }

        static public Task chan_clear(chan_base chan)
        {
            generator this_ = self;
            chan.async_clear(this_.unsafe_async_result());
            return this_.async_wait();
        }

        static public Task chan_close(chan_base chan, bool isClear = false)
        {
            generator this_ = self;
            chan.async_close(this_.unsafe_async_result(), isClear);
            return this_.async_wait();
        }

        static public Task chan_cancel(chan_base chan, bool isClear = false)
        {
            generator this_ = self;
            chan.async_cancel(this_.unsafe_async_result(), isClear);
            return this_.async_wait();
        }

        private async Task<bool> chan_is_closed_(chan_base chan)
        {
            bool is_closed = false;
            Action continuation = unsafe_async_result();
            chan.self_strand().post(delegate ()
            {
                is_closed = chan.is_closed();
                continuation();
            });
            await push_task();
            return is_closed;
        }

        static public ValueTask<bool> chan_is_closed(chan_base chan)
        {
            generator this_ = self;
            bool is_closed = chan.is_closed();
            if (!is_closed && chan.self_strand() != this_.strand)
            {
                return this_.chan_is_closed_(chan);
            }
            return is_closed;
        }

        static public Task unsafe_chan_is_closed(async_result_wrap<bool> res, chan_base chan)
        {
            generator this_ = self;
            res.value1 = chan.is_closed();
            if (!res.value1 && chan.self_strand() != this_.strand)
            {
                Action continuation = this_.unsafe_async_result();
                chan.self_strand().post(delegate ()
                {
                    res.value1 = chan.is_closed();
                    continuation();
                });
                return this_.async_wait();
            }
            return non_async();
        }

        static public async Task<chan_async_state> chan_wait_free(chan_base chan)
        {
            generator this_ = self;
#if DEBUG
            Trace.Assert(!this_._ioSign._ntfNode.effect && !this_._ioSign._success, "重叠的 chan_wait 操作!");
#endif
            try
            {
                chan_async_state result = chan_async_state.async_undefined;
                lock_suspend();
                chan.async_append_send_notify(this_.unsafe_async_callback((chan_async_state state) => result = state), this_._ioSign);
                await this_.async_wait();
                await unlock_suspend();
                return result;
            }
            catch (stop_exception)
            {
                chan.async_remove_send_notify(this_.unsafe_async_callback(nil_action<chan_async_state>.action), this_._ioSign);
                await this_.async_wait();
                throw;
            }
        }

        static public async Task<chan_async_state> chan_timed_wait_free(chan_base chan, int ms)
        {
            generator this_ = self;
#if DEBUG
            Trace.Assert(!this_._ioSign._ntfNode.effect && !this_._ioSign._success, "重叠的 chan_wait 操作!");
#endif
            try
            {
                chan_async_state result = chan_async_state.async_undefined;
                lock_suspend();
                chan.async_append_send_notify(this_.unsafe_async_callback((chan_async_state state) => result = state), this_._ioSign, ms);
                await this_.async_wait();
                await unlock_suspend();
                return result;
            }
            catch (stop_exception)
            {
                chan.async_remove_send_notify(this_.unsafe_async_callback(nil_action<chan_async_state>.action), this_._ioSign);
                await this_.async_wait();
                throw;
            }
        }

        static public async Task<chan_async_state> chan_wait_has(chan_base chan, broadcast_token token)
        {
            generator this_ = self;
#if DEBUG
            Trace.Assert(!this_._ioSign._ntfNode.effect && !this_._ioSign._success, "重叠的 chan_wait 操作!");
#endif
            try
            {
                chan_async_state result = chan_async_state.async_undefined;
                lock_suspend();
                chan.async_append_recv_notify(this_.unsafe_async_callback((chan_async_state state) => result = state), token, this_._ioSign);
                await this_.async_wait();
                await unlock_suspend();
                return result;
            }
            catch (stop_exception)
            {
                chan.async_remove_recv_notify(this_.unsafe_async_callback(nil_action<chan_async_state>.action), this_._ioSign);
                await this_.async_wait();
                throw;
            }
        }

        static public Task<chan_async_state> chan_wait_has(chan_base chan)
        {
            return chan_wait_has(chan, broadcast_token._defToken);
        }

        static public async Task<chan_async_state> chan_timed_wait_has(chan_base chan, int ms, broadcast_token token)
        {
            generator this_ = self;
#if DEBUG
            Trace.Assert(!this_._ioSign._ntfNode.effect && !this_._ioSign._success, "重叠的 chan_wait 操作!");
#endif
            try
            {
                chan_async_state result = chan_async_state.async_undefined;
                lock_suspend();
                chan.async_append_recv_notify(this_.unsafe_async_callback((chan_async_state state) => result = state), token, this_._ioSign, ms);
                await this_.async_wait();
                await unlock_suspend();
                return result;
            }
            catch (stop_exception)
            {
                chan.async_remove_recv_notify(this_.unsafe_async_callback(nil_action<chan_async_state>.action), this_._ioSign);
                await this_.async_wait();
                throw;
            }
        }

        static public Task<chan_async_state> chan_timed_wait_has(chan_base chan, int ms)
        {
            return chan_timed_wait_has(chan, ms, broadcast_token._defToken);
        }

        static public async Task chan_cancel_wait_free(chan_base chan)
        {
            generator this_ = self;
            lock_suspend_and_stop();
            chan.async_remove_send_notify(this_.unsafe_async_callback(nil_action<chan_async_state>.action), this_._ioSign);
            await this_.async_wait();
            await unlock_suspend_and_stop();
        }

        static public async Task chan_cancel_wait_has(chan_base chan)
        {
            generator this_ = self;
            lock_suspend_and_stop();
            chan.async_remove_recv_notify(this_.unsafe_async_callback(nil_action<chan_async_state>.action), this_._ioSign);
            await this_.async_wait();
            await unlock_suspend_and_stop();
        }

        static public Task unsafe_chan_send<T>(async_result_wrap<chan_send_wrap> res, chan<T> chan, T msg)
        {
            generator this_ = self;
            res.value1 = new chan_send_wrap { state = chan_async_state.async_undefined };
            chan.async_send(this_.unsafe_async_callback((chan_async_state state) => res.value1 = new chan_send_wrap { state = state }), msg);
            return this_.async_wait();
        }

        private async Task<chan_send_wrap> chan_send_<T>(async_result_wrap<chan_async_state> res, chan<T> chan, T msg, chan_lost_msg<T> lostMsg)
        {
            try
            {
                await push_task();
                return new chan_send_wrap { state = res.value1 };
            }
            catch (stop_exception)
            {
                chan.async_remove_send_notify(unsafe_async_callback(nil_action<chan_async_state>.action), _ioSign);
                await async_wait();
                if (chan_async_state.async_ok != res.value1)
                {
                    lostMsg?.set(msg);
                }
                throw;
            }
        }

        static public ValueTask<chan_send_wrap> chan_send<T>(chan<T> chan, T msg, chan_lost_msg<T> lostMsg = null)
        {
            generator this_ = self;
            async_result_wrap<chan_async_state> result = new async_result_wrap<chan_async_state> { value1 = chan_async_state.async_undefined };
            chan.async_send(this_.unsafe_async_callback((chan_async_state state) => result.value1 = state), msg, this_._ioSign);
            if (!this_.new_task_completed())
            {
                return this_.chan_send_(result, chan, msg, lostMsg);
            }
            return new chan_send_wrap { state = result.value1 };
        }

        static public Task unsafe_chan_send(async_result_wrap<chan_send_wrap> res, chan<void_type> chan)
        {
            return unsafe_chan_send(res, chan, default(void_type));
        }

        static public ValueTask<chan_send_wrap> chan_send(chan<void_type> chan, chan_lost_msg<void_type> lostMsg = null)
        {
            return chan_send(chan, default(void_type), lostMsg);
        }

        static public Task unsafe_chan_force_send<T>(async_result_wrap<chan_send_wrap> res, limit_chan<T> chan, T msg, chan_lost_msg<T> outMsg = null)
        {
            generator this_ = self;
            res.value1 = new chan_send_wrap { state = chan_async_state.async_undefined };
            chan.async_force_send(this_.unsafe_async_callback(delegate (chan_async_state state, bool hasOut, T freeMsg)
            {
                res.value1 = new chan_send_wrap { state = state };
                if (hasOut)
                {
                    outMsg?.set(freeMsg);
                }
            }), msg);
            return this_.async_wait();
        }

        private async Task<chan_send_wrap> chan_force_send_<T>(async_result_wrap<chan_async_state> res, limit_chan<T> chan, T msg, chan_lost_msg<T> lostMsg)
        {
            try
            {
                await push_task();
                return new chan_send_wrap { state = res.value1 };
            }
            catch (stop_exception)
            {
                chan.self_strand().distribute(_async_result());
                await async_wait();
                if (chan_async_state.async_ok != res.value1)
                {
                    lostMsg?.set(msg);
                }
                throw;
            }
        }

        static public ValueTask<chan_send_wrap> chan_force_send<T>(limit_chan<T> chan, T msg, chan_lost_msg<T> outMsg = null, chan_lost_msg<T> lostMsg = null)
        {
            generator this_ = self;
            async_result_wrap<chan_async_state> result = new async_result_wrap<chan_async_state> { value1 = chan_async_state.async_undefined };
            outMsg?.clear();
            chan.async_force_send(this_.unsafe_async_callback(delegate (chan_async_state state, bool hasOut, T freeMsg)
            {
                result.value1 = state;
                if (hasOut)
                {
                    outMsg?.set(freeMsg);
                }
            }), msg);
            if (!this_.new_task_completed())
            {
                return this_.chan_force_send_(result, chan, msg, lostMsg);
            }
            return new chan_send_wrap { state = result.value1 };
        }

        static public Task unsafe_chan_receive<T>(async_result_wrap<chan_recv_wrap<T>> res, chan<T> chan)
        {
            return unsafe_chan_receive(res, chan, broadcast_token._defToken);
        }

        static public ValueTask<chan_recv_wrap<T>> chan_receive<T>(chan<T> chan, chan_lost_msg<T> lostMsg = null)
        {
            return chan_receive(chan, broadcast_token._defToken, lostMsg);
        }

        static public Task unsafe_chan_receive<T>(async_result_wrap<chan_recv_wrap<T>> res, chan<T> chan, broadcast_token token)
        {
            generator this_ = self;
            res.value1 = default(chan_recv_wrap<T>);
            chan.async_recv(this_.unsafe_async_callback((chan_async_state state, T msg) => res.value1 = new chan_recv_wrap<T> { state = state, msg = msg }), token);
            return this_.async_wait();
        }

        private async Task<chan_recv_wrap<T>> chan_receive_<T>(async_result_wrap<chan_recv_wrap<T>> res, chan<T> chan, chan_lost_msg<T> lostMsg)
        {
            try
            {
                await push_task();
                return res.value1;
            }
            catch (stop_exception)
            {
                chan.async_remove_recv_notify(unsafe_async_callback(nil_action<chan_async_state>.action), _ioSign);
                await async_wait();
                if (chan_async_state.async_ok == res.value1.state)
                {
                    lostMsg?.set(res.value1.msg);
                }
                throw;
            }
        }

        static public ValueTask<chan_recv_wrap<T>> chan_receive<T>(chan<T> chan, broadcast_token token, chan_lost_msg<T> lostMsg = null)
        {
            generator this_ = self;
            async_result_wrap<chan_recv_wrap<T>> result = new async_result_wrap<chan_recv_wrap<T>> { value1 = new chan_recv_wrap<T> { state = chan_async_state.async_undefined } };
            chan.async_recv(this_.unsafe_async_callback(delegate (chan_async_state state, T msg)
            {
                result.value1 = new chan_recv_wrap<T> { state = state, msg = msg };
            }), token, this_._ioSign);
            if (!this_.new_task_completed())
            {
                return this_.chan_receive_(result, chan, lostMsg);
            }
            return result.value1;
        }

        static public Task unsafe_chan_try_send<T>(async_result_wrap<chan_send_wrap> res, chan<T> chan, T msg)
        {
            generator this_ = self;
            res.value1 = new chan_send_wrap { state = chan_async_state.async_undefined };
            chan.async_try_send(this_.unsafe_async_callback((chan_async_state state) => res.value1 = new chan_send_wrap { state = state }), msg);
            return this_.async_wait();
        }

        private async Task<chan_send_wrap> chan_try_send_<T>(async_result_wrap<chan_async_state> res, chan<T> chan, T msg, chan_lost_msg<T> lostMsg)
        {
            try
            {
                await push_task();
                return new chan_send_wrap { state = res.value1 };
            }
            catch (stop_exception)
            {
                chan.async_remove_send_notify(unsafe_async_callback(nil_action<chan_async_state>.action), _ioSign);
                await async_wait();
                if (chan_async_state.async_ok != res.value1)
                {
                    lostMsg?.set(msg);
                }
                throw;
            }
        }

        static public ValueTask<chan_send_wrap> chan_try_send<T>(chan<T> chan, T msg, chan_lost_msg<T> lostMsg = null)
        {
            generator this_ = self;
            async_result_wrap<chan_async_state> result = new async_result_wrap<chan_async_state> { value1 = chan_async_state.async_undefined };
            chan.async_try_send(this_.unsafe_async_callback((chan_async_state state) => result.value1 = state), msg, this_._ioSign);
            if (!this_.new_task_completed())
            {
                return this_.chan_try_send_(result, chan, msg, lostMsg);
            }
            return new chan_send_wrap { state = result.value1 };
        }

        static public Task unsafe_chan_try_send(async_result_wrap<chan_send_wrap> res, chan<void_type> chan)
        {
            return unsafe_chan_try_send(res, chan, default(void_type));
        }

        static public ValueTask<chan_send_wrap> chan_try_send(chan<void_type> chan, chan_lost_msg<void_type> lostMsg = null)
        {
            return chan_try_send(chan, default(void_type), lostMsg);
        }

        static public Task unsafe_chan_try_receive<T>(async_result_wrap<chan_recv_wrap<T>> res, chan<T> chan)
        {
            return unsafe_chan_try_receive(res, chan, broadcast_token._defToken);
        }

        static public ValueTask<chan_recv_wrap<T>> chan_try_receive<T>(chan<T> chan, chan_lost_msg<T> lostMsg = null)
        {
            return chan_try_receive(chan, broadcast_token._defToken, lostMsg);
        }

        static public Task unsafe_chan_try_receive<T>(async_result_wrap<chan_recv_wrap<T>> res, chan<T> chan, broadcast_token token)
        {
            generator this_ = self;
            res.value1 = default(chan_recv_wrap<T>);
            chan.async_try_recv(this_.unsafe_async_callback((chan_async_state state, T msg) => res.value1 = new chan_recv_wrap<T> { state = state, msg = msg }), token);
            return this_.async_wait();
        }

        private async Task<chan_recv_wrap<T>> chan_try_receive_<T>(async_result_wrap<chan_recv_wrap<T>> res, chan<T> chan, chan_lost_msg<T> lostMsg)
        {
            try
            {
                await push_task();
                return res.value1;
            }
            catch (stop_exception)
            {
                chan.async_remove_recv_notify(unsafe_async_callback(nil_action<chan_async_state>.action), _ioSign);
                await async_wait();
                if (chan_async_state.async_ok == res.value1.state)
                {
                    lostMsg?.set(res.value1.msg);
                }
                throw;
            }
        }

        static public ValueTask<chan_recv_wrap<T>> chan_try_receive<T>(chan<T> chan, broadcast_token token, chan_lost_msg<T> lostMsg = null)
        {
            generator this_ = self;
            async_result_wrap<chan_recv_wrap<T>> result = new async_result_wrap<chan_recv_wrap<T>> { value1 = new chan_recv_wrap<T> { state = chan_async_state.async_undefined } };
            chan.async_try_recv(this_.unsafe_async_callback(delegate (chan_async_state state, T msg)
            {
                result.value1 = new chan_recv_wrap<T> { state = state, msg = msg };
            }), token, this_._ioSign);
            if (!this_.new_task_completed())
            {
                return this_.chan_try_receive_(result, chan, lostMsg);
            }
            return result.value1;
        }

        static public Task unsafe_chan_timed_send<T>(async_result_wrap<chan_send_wrap> res, chan<T> chan, int ms, T msg)
        {
            generator this_ = self;
            res.value1 = new chan_send_wrap { state = chan_async_state.async_undefined };
            chan.async_timed_send(ms, this_.unsafe_async_callback((chan_async_state state) => res.value1 = new chan_send_wrap { state = state }), msg);
            return this_.async_wait();
        }

        private async Task<chan_send_wrap> chan_timed_send_<T>(async_result_wrap<chan_async_state> res, chan<T> chan, int ms, T msg, chan_lost_msg<T> lostMsg)
        {
            try
            {
                await push_task();
                return new chan_send_wrap { state = res.value1 };
            }
            catch (stop_exception)
            {
                chan.async_remove_send_notify(unsafe_async_callback(nil_action<chan_async_state>.action), _ioSign);
                await async_wait();
                if (chan_async_state.async_ok != res.value1)
                {
                    lostMsg?.set(msg);
                }
                throw;
            }
        }

        static public ValueTask<chan_send_wrap> chan_timed_send<T>(chan<T> chan, int ms, T msg, chan_lost_msg<T> lostMsg = null)
        {
            generator this_ = self;
            async_result_wrap<chan_async_state> result = new async_result_wrap<chan_async_state> { value1 = chan_async_state.async_undefined };
            chan.async_timed_send(ms, this_.unsafe_async_callback((chan_async_state state) => result.value1 = state), msg, this_._ioSign);
            if (!this_.new_task_completed())
            {
                return this_.chan_timed_send_(result, chan, ms, msg, lostMsg);
            }
            return new chan_send_wrap { state = result.value1 };
        }

        static public Task unsafe_chan_timed_send(async_result_wrap<chan_send_wrap> res, chan<void_type> chan, int ms)
        {
            return unsafe_chan_timed_send(res, chan, ms, default(void_type));
        }

        static public ValueTask<chan_send_wrap> chan_timed_send(chan<void_type> chan, int ms, chan_lost_msg<void_type> lostMsg = null)
        {
            return chan_timed_send(chan, ms, default(void_type), lostMsg);
        }

        static public Task unsafe_chan_timed_receive<T>(async_result_wrap<chan_recv_wrap<T>> res, chan<T> chan, int ms)
        {
            return unsafe_chan_timed_receive(res, chan, ms, broadcast_token._defToken);
        }

        static public ValueTask<chan_recv_wrap<T>> chan_timed_receive<T>(chan<T> chan, int ms, chan_lost_msg<T> lostMsg = null)
        {
            return chan_timed_receive(chan, ms, broadcast_token._defToken, lostMsg);
        }

        static public Task unsafe_chan_timed_receive<T>(async_result_wrap<chan_recv_wrap<T>> res, chan<T> chan, int ms, broadcast_token token)
        {
            generator this_ = self;
            res.value1 = default(chan_recv_wrap<T>);
            chan.async_timed_recv(ms, this_.unsafe_async_callback((chan_async_state state, T msg) => res.value1 = new chan_recv_wrap<T> { state = state, msg = msg }), token);
            return this_.async_wait();
        }

        private async Task<chan_recv_wrap<T>> chan_timed_receive_<T>(async_result_wrap<chan_recv_wrap<T>> res, chan<T> chan, int ms, chan_lost_msg<T> lostMsg)
        {
            try
            {
                await push_task();
                return res.value1;
            }
            catch (stop_exception)
            {
                chan.async_remove_recv_notify(unsafe_async_callback(nil_action<chan_async_state>.action), _ioSign);
                await async_wait();
                if (chan_async_state.async_ok == res.value1.state)
                {
                    lostMsg?.set(res.value1.msg);
                }
                throw;
            }
        }

        static public ValueTask<chan_recv_wrap<T>> chan_timed_receive<T>(chan<T> chan, int ms, broadcast_token token, chan_lost_msg<T> lostMsg = null)
        {
            generator this_ = self;
            async_result_wrap<chan_recv_wrap<T>> result = new async_result_wrap<chan_recv_wrap<T>> { value1 = new chan_recv_wrap<T> { state = chan_async_state.async_undefined } };
            chan.async_timed_recv(ms, this_.unsafe_async_callback(delegate (chan_async_state state, T msg)
            {
                result.value1 = new chan_recv_wrap<T> { state = state, msg = msg };
            }), token, this_._ioSign);
            if (!this_.new_task_completed())
            {
                return this_.chan_timed_receive_(result, chan, ms, lostMsg);
            }
            return result.value1;
        }

        static public Task unsafe_csp_invoke<R, T>(async_result_wrap<csp_invoke_wrap<R>> res, csp_chan<R, T> chan, T msg, int invokeMs = -1)
        {
            generator this_ = self;
            res.value1 = new csp_invoke_wrap<R> { state = chan_async_state.async_undefined };
            chan.async_send(invokeMs, this_.unsafe_async_callback((chan_async_state state, R resVal) => res.value1 = new csp_invoke_wrap<R> { state = state, result = resVal }), msg);
            return this_.async_wait();
        }

        private async Task<csp_invoke_wrap<R>> csp_invoke_<R, T>(async_result_wrap<csp_invoke_wrap<R>> res, csp_chan<R, T> chan, T msg, chan_lost_msg<T> lostMsg)
        {
            try
            {
                await push_task();
                return res.value1;
            }
            catch (stop_exception)
            {
                chan.async_remove_send_notify(unsafe_async_callback(null == lostMsg ? nil_action<chan_async_state>.action : (chan_async_state state) => res.value1 = new csp_invoke_wrap<R> { state = state }), _ioSign);
                await async_wait();
                if (chan_async_state.async_ok == res.value1.state)
                {
                    lostMsg?.set(msg);
                }
                throw;
            }
        }

        static public ValueTask<csp_invoke_wrap<R>> csp_invoke<R, T>(csp_chan<R, T> chan, T msg, int invokeMs = -1, Action<R> lostHandler = null, chan_lost_msg<T> lostMsg = null)
        {
            generator this_ = self;
            async_result_wrap<csp_invoke_wrap<R>> result = new async_result_wrap<csp_invoke_wrap<R>> { value1 = new csp_invoke_wrap<R> { state = chan_async_state.async_undefined } };
            chan.async_send(invokeMs, null == lostHandler ? this_.unsafe_async_callback(delegate (chan_async_state state, R resVal)
            {
                result.value1 = new csp_invoke_wrap<R> { state = state, result = resVal };
            }) : this_.async_callback(delegate (chan_async_state state, R resVal)
            {
                result.value1 = new csp_invoke_wrap<R> { state = state, result = resVal };
            }, delegate (chan_async_state state, R resVal)
            {
                if (chan_async_state.async_ok == state)
                {
                    lostHandler(resVal);
                }
            }), msg, this_._ioSign);
            if (!this_.new_task_completed())
            {
                return this_.csp_invoke_(result, chan, msg, lostMsg);
            }
            return result.value1;
        }

        static public Task unsafe_csp_invoke<R>(async_result_wrap<csp_invoke_wrap<R>> res, csp_chan<R, void_type> chan, int invokeMs = -1)
        {
            return unsafe_csp_invoke(res, chan, default(void_type), invokeMs);
        }

        static public ValueTask<csp_invoke_wrap<R>> csp_invoke<R>(csp_chan<R, void_type> chan, int invokeMs = -1, Action<R> lostHandler = null, chan_lost_msg<void_type> lostMsg = null)
        {
            return csp_invoke(chan, default(void_type), invokeMs, lostHandler, lostMsg);
        }

        static public Task unsafe_csp_wait<R, T>(async_result_wrap<csp_wait_wrap<R, T>> res, csp_chan<R, T> chan)
        {
            generator this_ = self;
            res.value1 = new csp_wait_wrap<R, T> { state = chan_async_state.async_undefined };
            chan.async_recv(this_.async_callback(delegate (chan_async_state state, T msg, csp_chan<R, T>.csp_result cspRes)
            {
                res.value1 = new csp_wait_wrap<R, T> { state = state, msg = msg, result = cspRes };
                cspRes?.start_invoke_timer(this_);
            }));
            return this_.async_wait();
        }

        private async Task<csp_wait_wrap<R, T>> csp_wait_<R, T>(async_result_wrap<csp_wait_wrap<R, T>> res, csp_chan<R, T> chan, chan_lost_msg<T> lostMsg)
        {
            try
            {
                await push_task();
                if (chan_async_state.async_ok == res.value1.state)
                {
                    res.value1.result.start_invoke_timer(self);
                }
                return res.value1;
            }
            catch (stop_exception)
            {
                chan.async_remove_recv_notify(unsafe_async_callback(nil_action<chan_async_state>.action), _ioSign);
                await async_wait();
                if (chan_async_state.async_ok == res.value1.state)
                {
                    lostMsg?.set(res.value1.msg);
                    res.value1.fail();
                }
                throw;
            }
        }

        static public ValueTask<csp_wait_wrap<R, T>> csp_wait<R, T>(csp_chan<R, T> chan, chan_lost_msg<T> lostMsg = null)
        {
            generator this_ = self;
            async_result_wrap<csp_wait_wrap<R, T>> result = new async_result_wrap<csp_wait_wrap<R, T>> { value1 = new csp_wait_wrap<R, T> { state = chan_async_state.async_undefined } };
            chan.async_recv(this_.unsafe_async_callback(delegate (chan_async_state state, T msg, csp_chan<R, T>.csp_result cspRes)
            {
                result.value1 = new csp_wait_wrap<R, T> { state = state, msg = msg, result = cspRes };
            }), this_._ioSign);
            if (!this_.new_task_completed())
            {
                return this_.csp_wait_(result, chan, lostMsg);
            }
            return result.value1;
        }

        static public void csp_fail()
        {
            throw csp_fail_exception.val;
        }

        static public async Task<chan_async_state> csp_wait<R, T>(csp_chan<R, T> chan, Func<T, Task<R>> handler, chan_lost_msg<T> lostMsg = null)
        {
            csp_wait_wrap<R, T> result = await csp_wait(chan, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    result.complete(await handler(result.msg));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_wait<R, T1, T2>(csp_chan<R, tuple<T1, T2>> chan, Func<T1, T2, Task<R>> handler, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
        {
            csp_wait_wrap<R, tuple<T1, T2>> result = await csp_wait(chan, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    result.complete(await handler(result.msg.value1, result.msg.value2));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_wait<R, T1, T2, T3>(csp_chan<R, tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task<R>> handler, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
        {
            csp_wait_wrap<R, tuple<T1, T2, T3>> result = await csp_wait(chan, lostMsg);
            if (chan_async_state.async_ok == result.state)
                if (chan_async_state.async_ok == result.state)
                {
                    try
                    {
                        result.complete(await handler(result.msg.value1, result.msg.value2, result.msg.value3));
                    }
                    catch (csp_fail_exception)
                    {
                        result.fail();
                    }
                    catch (stop_exception)
                    {
                        result.fail();
                        throw;
                    }
                }
            return result.state;
        }

        static public async Task<chan_async_state> csp_wait<R>(csp_chan<R, void_type> chan, Func<Task<R>> handler, chan_lost_msg<void_type> lostMsg = null)
        {
            csp_wait_wrap<R, void_type> result = await csp_wait(chan, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    result.complete(await handler());
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_wait<T>(csp_chan<void_type, T> chan, Func<T, Task> handler, chan_lost_msg<T> lostMsg = null)
        {
            csp_wait_wrap<void_type, T> result = await csp_wait(chan, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    await handler(result.msg);
                    result.complete(default(void_type));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_wait<T1, T2>(csp_chan<void_type, tuple<T1, T2>> chan, Func<T1, T2, Task> handler, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
        {
            csp_wait_wrap<void_type, tuple<T1, T2>> result = await csp_wait(chan, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    await handler(result.msg.value1, result.msg.value2);
                    result.complete(default(void_type));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_wait<T1, T2, T3>(csp_chan<void_type, tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
        {
            csp_wait_wrap<void_type, tuple<T1, T2, T3>> result = await csp_wait(chan, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    await handler(result.msg.value1, result.msg.value2, result.msg.value3);
                    result.complete(default(void_type));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_wait(csp_chan<void_type, void_type> chan, Func<Task> handler, chan_lost_msg<void_type> lostMsg = null)
        {
            csp_wait_wrap<void_type, void_type> result = await csp_wait(chan, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    await handler();
                    result.complete(default(void_type));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public Task unsafe_csp_try_invoke<R, T>(async_result_wrap<csp_invoke_wrap<R>> res, csp_chan<R, T> chan, T msg, int invokeMs = -1)
        {
            generator this_ = self;
            res.value1 = new csp_invoke_wrap<R> { state = chan_async_state.async_undefined };
            chan.async_try_send(invokeMs, this_.unsafe_async_callback((chan_async_state state, R resVal) => res.value1 = new csp_invoke_wrap<R> { state = state, result = resVal }), msg);
            return this_.async_wait();
        }

        private async Task<csp_invoke_wrap<R>> csp_try_invoke_<R, T>(async_result_wrap<csp_invoke_wrap<R>> res, csp_chan<R, T> chan, T msg, chan_lost_msg<T> lostMsg)
        {
            try
            {
                await push_task();
                return res.value1;
            }
            catch (stop_exception)
            {
                chan.async_remove_send_notify(unsafe_async_callback(null == lostMsg ? nil_action<chan_async_state>.action : (chan_async_state state) => res.value1 = new csp_invoke_wrap<R> { state = state }), _ioSign);
                await async_wait();
                if (chan_async_state.async_ok == res.value1.state)
                {
                    lostMsg?.set(msg);
                }
                throw;
            }
        }

        static public ValueTask<csp_invoke_wrap<R>> csp_try_invoke<R, T>(csp_chan<R, T> chan, T msg, int invokeMs = -1, Action<R> lostHandler = null, chan_lost_msg<T> lostMsg = null)
        {
            generator this_ = self;
            async_result_wrap<csp_invoke_wrap<R>> result = new async_result_wrap<csp_invoke_wrap<R>> { value1 = new csp_invoke_wrap<R> { state = chan_async_state.async_undefined } };
            chan.async_try_send(invokeMs, null == lostHandler ? this_.unsafe_async_callback(delegate (chan_async_state state, R resVal)
            {
                result.value1 = new csp_invoke_wrap<R> { state = state, result = resVal };
            }) : this_.async_callback(delegate (chan_async_state state, R resVal)
            {
                result.value1 = new csp_invoke_wrap<R> { state = state, result = resVal };
            }, delegate (chan_async_state state, R resVal)
            {
                if (chan_async_state.async_ok == state)
                {
                    lostHandler(resVal);
                }
            }), msg, this_._ioSign);
            if (!this_.new_task_completed())
            {
                return this_.csp_try_invoke_(result, chan, msg, lostMsg);
            }
            return result.value1;
        }

        static public Task unsafe_csp_try_invoke<R>(async_result_wrap<csp_invoke_wrap<R>> res, csp_chan<R, void_type> chan, int invokeMs = -1)
        {
            return unsafe_csp_try_invoke(res, chan, default(void_type), invokeMs);
        }

        static public ValueTask<csp_invoke_wrap<R>> csp_try_invoke<R>(csp_chan<R, void_type> chan, int invokeMs = -1, Action<R> lostHandler = null, chan_lost_msg<void_type> lostMsg = null)
        {
            return csp_try_invoke(chan, default(void_type), invokeMs, lostHandler, lostMsg);
        }

        static public Task unsafe_csp_try_wait<R, T>(async_result_wrap<csp_wait_wrap<R, T>> res, csp_chan<R, T> chan)
        {
            generator this_ = self;
            res.value1 = new csp_wait_wrap<R, T> { state = chan_async_state.async_undefined };
            chan.async_try_recv(this_.async_callback(delegate (chan_async_state state, T msg, csp_chan<R, T>.csp_result cspRes)
            {
                res.value1 = new csp_wait_wrap<R, T> { state = state, msg = msg, result = cspRes };
                cspRes?.start_invoke_timer(this_);
            }));
            return this_.async_wait();
        }

        private async Task<csp_wait_wrap<R, T>> csp_try_wait_<R, T>(async_result_wrap<csp_wait_wrap<R, T>> res, csp_chan<R, T> chan, chan_lost_msg<T> lostMsg)
        {
            try
            {
                await push_task();
                if (chan_async_state.async_ok == res.value1.state)
                {
                    res.value1.result.start_invoke_timer(self);
                }
                return res.value1;
            }
            catch (stop_exception)
            {
                chan.async_remove_recv_notify(unsafe_async_callback(nil_action<chan_async_state>.action), _ioSign);
                await async_wait();
                if (chan_async_state.async_ok == res.value1.state)
                {
                    lostMsg?.set(res.value1.msg);
                    res.value1.fail();
                }
                throw;
            }
        }

        static public ValueTask<csp_wait_wrap<R, T>> csp_try_wait<R, T>(csp_chan<R, T> chan, chan_lost_msg<T> lostMsg = null)
        {
            generator this_ = self;
            async_result_wrap<csp_wait_wrap<R, T>> result = new async_result_wrap<csp_wait_wrap<R, T>> { value1 = new csp_wait_wrap<R, T> { state = chan_async_state.async_undefined } };
            chan.async_try_recv(this_.unsafe_async_callback(delegate (chan_async_state state, T msg, csp_chan<R, T>.csp_result cspRes)
            {
                result.value1 = new csp_wait_wrap<R, T> { state = state, msg = msg, result = cspRes };
            }), this_._ioSign);
            if (!this_.new_task_completed())
            {
                return this_.csp_try_wait_(result, chan, lostMsg);
            }
            return result.value1;
        }

        static public async Task<chan_async_state> csp_try_wait<R, T>(csp_chan<R, T> chan, Func<T, Task<R>> handler, chan_lost_msg<T> lostMsg = null)
        {
            csp_wait_wrap<R, T> result = await csp_try_wait(chan, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    result.complete(await handler(result.msg));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_try_wait<R, T1, T2>(csp_chan<R, tuple<T1, T2>> chan, Func<T1, T2, Task<R>> handler, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
        {
            csp_wait_wrap<R, tuple<T1, T2>> result = await csp_try_wait(chan, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    result.complete(await handler(result.msg.value1, result.msg.value2));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_try_wait<R, T1, T2, T3>(csp_chan<R, tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task<R>> handler, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
        {
            csp_wait_wrap<R, tuple<T1, T2, T3>> result = await csp_try_wait(chan, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    result.complete(await handler(result.msg.value1, result.msg.value2, result.msg.value3));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_try_wait<R>(csp_chan<R, void_type> chan, Func<Task<R>> handler, chan_lost_msg<void_type> lostMsg = null)
        {
            csp_wait_wrap<R, void_type> result = await csp_try_wait(chan, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    result.complete(await handler());
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_try_wait<T>(csp_chan<void_type, T> chan, Func<T, Task> handler, chan_lost_msg<T> lostMsg = null)
        {
            csp_wait_wrap<void_type, T> result = await csp_try_wait(chan, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    await handler(result.msg);
                    result.complete(default(void_type));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_try_wait<T1, T2>(csp_chan<void_type, tuple<T1, T2>> chan, Func<T1, T2, Task> handler, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
        {
            csp_wait_wrap<void_type, tuple<T1, T2>> result = await csp_try_wait(chan, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    await handler(result.msg.value1, result.msg.value2);
                    result.complete(default(void_type));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_try_wait<T1, T2, T3>(csp_chan<void_type, tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
        {
            csp_wait_wrap<void_type, tuple<T1, T2, T3>> result = await csp_try_wait(chan, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    await handler(result.msg.value1, result.msg.value2, result.msg.value3);
                    result.complete(default(void_type));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_try_wait(csp_chan<void_type, void_type> chan, Func<Task> handler, chan_lost_msg<void_type> lostMsg = null)
        {
            csp_wait_wrap<void_type, void_type> result = await csp_try_wait(chan, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    await handler();
                    result.complete(default(void_type));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public Task unsafe_csp_timed_invoke<R, T>(async_result_wrap<csp_invoke_wrap<R>> res, csp_chan<R, T> chan, tuple<int, int> ms, T msg)
        {
            generator this_ = self;
            res.value1 = new csp_invoke_wrap<R> { state = chan_async_state.async_undefined };
            chan.async_timed_send(ms.value1, ms.value2, this_.unsafe_async_callback((chan_async_state state, R resVal) => res.value1 = new csp_invoke_wrap<R> { state = state, result = resVal }), msg);
            return this_.async_wait();
        }

        private async Task<csp_invoke_wrap<R>> csp_timed_invoke_<R, T>(async_result_wrap<csp_invoke_wrap<R>> res, csp_chan<R, T> chan, T msg, chan_lost_msg<T> lostMsg)
        {
            try
            {
                await push_task();
                return res.value1;
            }
            catch (stop_exception)
            {
                chan.async_remove_send_notify(unsafe_async_callback(null == lostMsg ? nil_action<chan_async_state>.action : (chan_async_state state) => res.value1 = new csp_invoke_wrap<R> { state = state }), _ioSign);
                await async_wait();
                if (chan_async_state.async_ok == res.value1.state)
                {
                    lostMsg?.set(msg);
                }
                throw;
            }
        }

        static public ValueTask<csp_invoke_wrap<R>> csp_timed_invoke<R, T>(csp_chan<R, T> chan, tuple<int, int> ms, T msg, Action<R> lostHandler = null, chan_lost_msg<T> lostMsg = null)
        {
            generator this_ = self;
            async_result_wrap<csp_invoke_wrap<R>> result = new async_result_wrap<csp_invoke_wrap<R>> { value1 = new csp_invoke_wrap<R> { state = chan_async_state.async_undefined } };
            chan.async_timed_send(ms.value1, ms.value2, null == lostHandler ? this_.unsafe_async_callback(delegate (chan_async_state state, R resVal)
            {
                result.value1 = new csp_invoke_wrap<R> { state = state, result = resVal };
            }) : this_.async_callback(delegate (chan_async_state state, R resVal)
            {
                result.value1 = new csp_invoke_wrap<R> { state = state, result = resVal };
            }, delegate (chan_async_state state, R resVal)
            {
                if (chan_async_state.async_ok == state)
                {
                    lostHandler(resVal);
                }
            }), msg, this_._ioSign);
            if (!this_.new_task_completed())
            {
                return this_.csp_timed_invoke_(result, chan, msg, lostMsg);
            }
            return result.value1;
        }

        static public Task unsafe_csp_timed_invoke<R, T>(async_result_wrap<csp_invoke_wrap<R>> res, csp_chan<R, T> chan, int ms, T msg)
        {
            return unsafe_csp_timed_invoke(res, chan, tuple.make(ms, -1), msg);
        }

        static public ValueTask<csp_invoke_wrap<R>> csp_timed_invoke<R, T>(csp_chan<R, T> chan, int ms, T msg, Action<R> lostHandler = null, chan_lost_msg<T> lostMsg = null)
        {
            return csp_timed_invoke(chan, tuple.make(ms, -1), msg, lostHandler, lostMsg);
        }

        static public Task unsafe_csp_timed_invoke<R>(async_result_wrap<csp_invoke_wrap<R>> res, csp_chan<R, void_type> chan, tuple<int, int> ms)
        {
            return unsafe_csp_timed_invoke(res, chan, ms, default(void_type));
        }

        static public ValueTask<csp_invoke_wrap<R>> csp_timed_invoke<R>(csp_chan<R, void_type> chan, tuple<int, int> ms, Action<R> lostHandler = null, chan_lost_msg<void_type> lostMsg = null)
        {
            return csp_timed_invoke(chan, ms, default(void_type), lostHandler, lostMsg);
        }

        static public Task unsafe_csp_timed_invoke<R>(async_result_wrap<csp_invoke_wrap<R>> res, csp_chan<R, void_type> chan, int ms)
        {
            return unsafe_csp_timed_invoke(res, chan, ms, default(void_type));
        }

        static public ValueTask<csp_invoke_wrap<R>> csp_timed_invoke<R>(csp_chan<R, void_type> chan, int ms, Action<R> lostHandler = null, chan_lost_msg<void_type> lostMsg = null)
        {
            return csp_timed_invoke(chan, ms, default(void_type), lostHandler, lostMsg);
        }

        static public Task unsafe_csp_timed_wait<R, T>(async_result_wrap<csp_wait_wrap<R, T>> res, csp_chan<R, T> chan, int ms)
        {
            generator this_ = self;
            res.value1 = new csp_wait_wrap<R, T> { state = chan_async_state.async_undefined };
            chan.async_timed_recv(ms, this_.async_callback(delegate (chan_async_state state, T msg, csp_chan<R, T>.csp_result cspRes)
            {
                res.value1 = new csp_wait_wrap<R, T> { state = state, msg = msg, result = cspRes };
                cspRes?.start_invoke_timer(this_);
            }));
            return this_.async_wait();
        }

        private async Task<csp_wait_wrap<R, T>> csp_timed_wait_<R, T>(async_result_wrap<csp_wait_wrap<R, T>> res, csp_chan<R, T> chan, chan_lost_msg<T> lostMsg = null)
        {
            try
            {
                await push_task();
                if (chan_async_state.async_ok == res.value1.state)
                {
                    res.value1.result.start_invoke_timer(self);
                }
                return res.value1;
            }
            catch (stop_exception)
            {
                chan.async_remove_recv_notify(unsafe_async_callback(nil_action<chan_async_state>.action), _ioSign);
                await async_wait();
                if (chan_async_state.async_ok == res.value1.state)
                {
                    lostMsg?.set(res.value1.msg);
                    res.value1.fail();
                }
                throw;
            }
        }

        static public ValueTask<csp_wait_wrap<R, T>> csp_timed_wait<R, T>(csp_chan<R, T> chan, int ms, chan_lost_msg<T> lostMsg = null)
        {
            generator this_ = self;
            async_result_wrap<csp_wait_wrap<R, T>> result = new async_result_wrap<csp_wait_wrap<R, T>> { value1 = new csp_wait_wrap<R, T> { state = chan_async_state.async_undefined } };
            chan.async_timed_recv(ms, this_.unsafe_async_callback(delegate (chan_async_state state, T msg, csp_chan<R, T>.csp_result cspRes)
            {
                result.value1 = new csp_wait_wrap<R, T> { state = state, msg = msg, result = cspRes };
            }), this_._ioSign);
            if (!this_.new_task_completed())
            {
                return this_.csp_timed_wait_(result, chan, lostMsg);
            }
            return result.value1;
        }

        static public async Task<chan_async_state> csp_timed_wait<R, T>(csp_chan<R, T> chan, int ms, Func<T, Task<R>> handler, chan_lost_msg<T> lostMsg = null)
        {
            csp_wait_wrap<R, T> result = await csp_timed_wait(chan, ms, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    result.complete(await handler(result.msg));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_timed_wait<R, T1, T2>(csp_chan<R, tuple<T1, T2>> chan, int ms, Func<T1, T2, Task<R>> handler, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
        {
            csp_wait_wrap<R, tuple<T1, T2>> result = await csp_timed_wait(chan, ms, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    result.complete(await handler(result.msg.value1, result.msg.value2));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_timed_wait<R, T1, T2, T3>(csp_chan<R, tuple<T1, T2, T3>> chan, int ms, Func<T1, T2, T3, Task<R>> handler, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
        {
            csp_wait_wrap<R, tuple<T1, T2, T3>> result = await csp_timed_wait(chan, ms, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    result.complete(await handler(result.msg.value1, result.msg.value2, result.msg.value3));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_timed_wait<R>(csp_chan<R, void_type> chan, int ms, Func<Task<R>> handler, chan_lost_msg<void_type> lostMsg = null)
        {
            csp_wait_wrap<R, void_type> result = await csp_timed_wait(chan, ms, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    result.complete(await handler());
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_timed_wait<T>(csp_chan<void_type, T> chan, int ms, Func<T, Task> handler, chan_lost_msg<T> lostMsg = null)
        {
            csp_wait_wrap<void_type, T> result = await csp_timed_wait(chan, ms, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    await handler(result.msg);
                    result.complete(default(void_type));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_timed_wait<T1, T2>(csp_chan<void_type, tuple<T1, T2>> chan, int ms, Func<T1, T2, Task> handler, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
        {
            csp_wait_wrap<void_type, tuple<T1, T2>> result = await csp_timed_wait(chan, ms, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    await handler(result.msg.value1, result.msg.value2);
                    result.complete(default(void_type));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_timed_wait<T1, T2, T3>(csp_chan<void_type, tuple<T1, T2, T3>> chan, int ms, Func<T1, T2, T3, Task> handler, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
        {
            csp_wait_wrap<void_type, tuple<T1, T2, T3>> result = await csp_timed_wait(chan, ms, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    await handler(result.msg.value1, result.msg.value2, result.msg.value3);
                    result.complete(default(void_type));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_timed_wait(csp_chan<void_type, void_type> chan, int ms, Func<Task> handler, chan_lost_msg<void_type> lostMsg = null)
        {
            csp_wait_wrap<void_type, void_type> result = await csp_timed_wait(chan, ms, lostMsg);
            if (chan_async_state.async_ok == result.state)
            {
                try
                {
                    await handler();
                    result.complete(default(void_type));
                }
                catch (csp_fail_exception)
                {
                    result.fail();
                }
                catch (stop_exception)
                {
                    result.fail();
                    throw;
                }
            }
            return result.state;
        }

        static public async Task<int> chans_broadcast<T>(T msg, params chan<T>[] chans)
        {
            generator this_ = self;
            int count = 0;
            wait_group wg = new wait_group(chans.Length);
            for (int i = 0; i < chans.Length; i++)
            {
                chans[i].async_send(delegate (chan_async_state state)
                {
                    if (chan_async_state.async_ok == state)
                    {
                        Interlocked.Increment(ref count);
                    }
                    wg.done();
                }, msg, null);
            }
            wg.async_wait(this_.unsafe_async_result());
            await this_.async_wait();
            return count;
        }

        static public async Task<int> chans_try_broadcast<T>(T msg, params chan<T>[] chans)
        {
            generator this_ = self;
            int count = 0;
            wait_group wg = new wait_group(chans.Length);
            for (int i = 0; i < chans.Length; i++)
            {
                chans[i].async_try_send(delegate (chan_async_state state)
                {
                    if (chan_async_state.async_ok == state)
                    {
                        Interlocked.Increment(ref count);
                    }
                    wg.done();
                }, msg, null);
            }
            wg.async_wait(this_.unsafe_async_result());
            await this_.async_wait();
            return count;
        }

        static public async Task<int> chans_timed_broadcast<T>(int ms, T msg, params chan<T>[] chans)
        {
            generator this_ = self;
            int count = 0;
            wait_group wg = new wait_group(chans.Length);
            for (int i = 0; i < chans.Length; i++)
            {
                chans[i].async_timed_send(ms, delegate (chan_async_state state)
                {
                    if (chan_async_state.async_ok == state)
                    {
                        Interlocked.Increment(ref count);
                    }
                    wg.done();
                }, msg, null);
            }
            wg.async_wait(this_.unsafe_async_result());
            await this_.async_wait();
            return count;
        }

        static public Task non_async()
        {
            return _nilTask;
        }

        static public Task<R> non_async<R>(R value)
        {
            return nil_task<R>.task(value);
        }

        static public Task mutex_cancel(mutex mtx)
        {
            generator this_ = self;
            mtx.async_cancel(this_._id, this_.unsafe_async_result());
            return this_.async_wait();
        }

        static public Task mutex_lock(mutex mtx)
        {
            generator this_ = self;
            mtx.async_lock(this_._id, this_.unsafe_async_result());
            return this_.async_wait();
        }

        static public async Task mutex_lock(mutex mtx, Func<Task> handler)
        {
            await mutex_lock(mtx);
            try
            {
                await handler();
            }
            finally
            {
                await mutex_unlock(mtx);
            }
        }

        static public async Task<R> mutex_lock<R>(mutex mtx, Func<Task<R>> handler)
        {
            await mutex_lock(mtx);
            try
            {
                return await handler();
            }
            finally
            {
                await mutex_unlock(mtx);
            }
        }

        static public Task mutex_try_lock(async_result_wrap<bool> res, mutex mtx)
        {
            generator this_ = self;
            res.value1 = false;
            mtx.async_try_lock(this_._id, this_.async_result(res));
            return this_.async_wait();
        }

        static public ValueTask<bool> mutex_try_lock(mutex mtx)
        {
            generator this_ = self;
            async_result_wrap<bool> res = new async_result_wrap<bool>();
            mtx.async_try_lock(this_._id, this_.unsafe_async_result(res));
            return this_.async_wait(res);
        }

        static public async Task<bool> mutex_try_lock(mutex mtx, Func<Task> handler)
        {
            if (await mutex_try_lock(mtx))
            {
                try
                {
                    await handler();
                }
                finally
                {
                    await mutex_unlock(mtx);
                }
                return true;
            }
            return false;
        }

        static public Task mutex_timed_lock(async_result_wrap<bool> res, mutex mtx, int ms)
        {
            generator this_ = self;
            res.value1 = false;
            mtx.async_timed_lock(this_._id, ms, this_.async_result(res));
            return this_.async_wait();
        }

        static public ValueTask<bool> mutex_timed_lock(mutex mtx, int ms)
        {
            generator this_ = self;
            async_result_wrap<bool> res = new async_result_wrap<bool>();
            mtx.async_timed_lock(this_._id, ms, this_.unsafe_async_result(res));
            return this_.async_wait(res);
        }

        static public async Task<bool> mutex_timed_lock(mutex mtx, int ms, Func<Task> handler)
        {
            if (await mutex_timed_lock(mtx, ms))
            {
                try
                {
                    await handler();
                }
                finally
                {
                    await mutex_unlock(mtx);
                }
                return true;
            }
            return false;
        }

        static public Task mutex_unlock(mutex mtx)
        {
            generator this_ = self;
            mtx.async_unlock(this_._id, this_.unsafe_async_result());
            return this_.async_wait();
        }

        static public Task mutex_lock_shared(shared_mutex mtx)
        {
            generator this_ = self;
            mtx.async_lock_shared(this_._id, this_.unsafe_async_result());
            return this_.async_wait();
        }

        static public async Task mutex_lock_shared(shared_mutex mtx, Func<Task> handler)
        {
            await mutex_lock_shared(mtx);
            try
            {
                await handler();
            }
            finally
            {
                await mutex_unlock_shared(mtx);
            }
        }

        static public async Task<R> mutex_lock_shared<R>(shared_mutex mtx, Func<Task<R>> handler)
        {
            await mutex_lock_shared(mtx);
            try
            {
                return await handler();
            }
            finally
            {
                await mutex_unlock_shared(mtx);
            }
        }

        static public Task mutex_lock_pess_shared(shared_mutex mtx)
        {
            generator this_ = self;
            mtx.async_lock_pess_shared(this_._id, this_.unsafe_async_result());
            return this_.async_wait();
        }

        static public async Task mutex_lock_pess_shared(shared_mutex mtx, Func<Task> handler)
        {
            await mutex_lock_pess_shared(mtx);
            try
            {
                await handler();
            }
            finally
            {
                await mutex_unlock_shared(mtx);
            }
        }

        static public async Task<R> mutex_lock_pess_shared<R>(shared_mutex mtx, Func<Task<R>> handler)
        {
            await mutex_lock_pess_shared(mtx);
            try
            {
                return await handler();
            }
            finally
            {
                await mutex_unlock_shared(mtx);
            }
        }

        static public Task mutex_lock_upgrade(shared_mutex mtx)
        {
            generator this_ = self;
            mtx.async_lock_upgrade(this_._id, this_.unsafe_async_result());
            return this_.async_wait();
        }

        static public async Task mutex_lock_upgrade(shared_mutex mtx, Func<Task> handler)
        {
            await mutex_lock_upgrade(mtx);
            try
            {
                await handler();
            }
            finally
            {
                await mutex_unlock_upgrade(mtx);
            }
        }

        static public async Task<R> mutex_lock_upgrade<R>(shared_mutex mtx, Func<Task<R>> handler)
        {
            await mutex_lock_upgrade(mtx);
            try
            {
                return await handler();
            }
            finally
            {
                await mutex_unlock_upgrade(mtx);
            }
        }

        static public Task mutex_try_lock_shared(async_result_wrap<bool> res, shared_mutex mtx)
        {
            generator this_ = self;
            res.value1 = false;
            mtx.async_try_lock_shared(this_._id, this_.async_result(res));
            return this_.async_wait();
        }

        static public ValueTask<bool> mutex_try_lock_shared(shared_mutex mtx)
        {
            generator this_ = self;
            async_result_wrap<bool> res = new async_result_wrap<bool>();
            mtx.async_try_lock_shared(this_._id, this_.unsafe_async_result(res));
            return this_.async_wait(res);
        }

        static public async Task<bool> mutex_try_lock_shared(shared_mutex mtx, Func<Task> handler)
        {
            if (await mutex_try_lock_shared(mtx))
            {
                try
                {
                    await handler();
                }
                finally
                {
                    await mutex_unlock_shared(mtx);
                }
                return true;
            }
            return false;
        }

        static public Task mutex_try_lock_upgrade(async_result_wrap<bool> res, shared_mutex mtx)
        {
            generator this_ = self;
            res.value1 = false;
            mtx.async_try_lock_upgrade(this_._id, this_.async_result(res));
            return this_.async_wait();
        }

        static public ValueTask<bool> mutex_try_lock_upgrade(shared_mutex mtx)
        {
            generator this_ = self;
            async_result_wrap<bool> res = new async_result_wrap<bool>();
            mtx.async_try_lock_upgrade(this_._id, this_.unsafe_async_result(res));
            return this_.async_wait(res);
        }

        static public async Task<bool> mutex_try_lock_upgrade(shared_mutex mtx, Func<Task> handler)
        {
            if (await mutex_try_lock_upgrade(mtx))
            {
                try
                {
                    await handler();
                }
                finally
                {
                    await mutex_unlock_upgrade(mtx);
                }
                return true;
            }
            return false;
        }

        static public Task mutex_timed_lock_shared(async_result_wrap<bool> res, shared_mutex mtx, int ms)
        {
            generator this_ = self;
            res.value1 = false;
            mtx.async_timed_lock_shared(this_._id, ms, this_.async_result(res));
            return this_.async_wait();
        }

        static public ValueTask<bool> mutex_timed_lock_shared(shared_mutex mtx, int ms)
        {
            generator this_ = self;
            async_result_wrap<bool> res = new async_result_wrap<bool>();
            mtx.async_timed_lock_shared(this_._id, ms, this_.unsafe_async_result(res));
            return this_.async_wait(res);
        }

        static public async Task<bool> mutex_timed_lock_shared(shared_mutex mtx, int ms, Func<Task> handler)
        {
            if (await mutex_timed_lock_shared(mtx, ms))
            {
                try
                {
                    await handler();
                }
                finally
                {
                    await mutex_unlock_shared(mtx);
                }
                return true;
            }
            return false;
        }

        static public Task mutex_unlock_shared(shared_mutex mtx)
        {
            generator this_ = self;
            mtx.async_unlock_shared(this_._id, this_.unsafe_async_result());
            return this_.async_wait();
        }

        static public Task mutex_unlock_upgrade(shared_mutex mtx)
        {
            generator this_ = self;
            mtx.async_unlock_upgrade(this_._id, this_.unsafe_async_result());
            return this_.async_wait();
        }

        static public Task condition_wait(condition_variable conVar, mutex mutex)
        {
            generator this_ = self;
            conVar.async_wait(this_._id, mutex, this_.unsafe_async_result());
            return this_.async_wait();
        }

        static public Task condition_timed_wait(async_result_wrap<bool> res, condition_variable conVar, mutex mutex, int ms)
        {
            generator this_ = self;
            res.value1 = false;
            conVar.async_timed_wait(this_._id, ms, mutex, this_.async_result(res));
            return this_.async_wait();
        }

        static public ValueTask<bool> condition_timed_wait(condition_variable conVar, mutex mutex, int ms)
        {
            generator this_ = self;
            async_result_wrap<bool> res = new async_result_wrap<bool>();
            conVar.async_timed_wait(this_._id, ms, mutex, this_.unsafe_async_result(res));
            return this_.async_wait(res);
        }

        static public Task condition_cancel(condition_variable conVar)
        {
            generator this_ = self;
            conVar.async_cancel(this_.id, this_.unsafe_async_result());
            return this_.async_wait();
        }

        static public Task unsafe_send_strand(shared_strand strand, Action handler)
        {
            generator this_ = self;
            if (this_.strand == strand)
            {
                handler();
                return non_async();
            }
            strand.post(this_.unsafe_async_callback(handler));
            return this_.async_wait();
        }

        private async Task send_strand_(shared_strand strand, Action handler)
        {
            System.Exception hasExcep = null;
            strand.post(unsafe_async_callback(delegate ()
            {
                try
                {
                    handler();
                }
                catch (System.Exception ec)
                {
                    hasExcep = ec;
                }
            }));
            await async_wait();
            if (null != hasExcep)
            {
                throw hasExcep;
            }
        }

        static public Task send_strand(shared_strand strand, Action handler)
        {
            generator this_ = self;
            if (this_.strand == strand)
            {
                handler();
                return non_async();
            }
            return this_.send_strand_(strand, handler);
        }

        static public Task unsafe_send_strand<R>(async_result_wrap<R> res, shared_strand strand, Func<R> handler)
        {
            generator this_ = self;
            if (this_.strand == strand)
            {
                res.value1 = handler();
                return non_async();
            }
            res.clear();
            strand.post(this_.unsafe_async_callback(delegate ()
            {
                res.value1 = handler();
            }));
            return this_.async_wait();
        }

        private async Task<R> send_strand_<R>(shared_strand strand, Func<R> handler)
        {
            R res = default(R);
            System.Exception hasExcep = null;
            strand.post(unsafe_async_callback(delegate ()
            {
                try
                {
                    res = handler();
                }
                catch (System.Exception ec)
                {
                    hasExcep = ec;
                }
            }));
            await async_wait();
            if (null != hasExcep)
            {
                throw hasExcep;
            }
            return res;
        }

        static public ValueTask<R> send_strand<R>(shared_strand strand, Func<R> handler)
        {
            generator this_ = self;
            if (this_.strand == strand)
            {
                return handler();
            }
            return this_.send_strand_(strand, handler);
        }

        static public Func<Task> wrap_send_strand(shared_strand strand, Action handler)
        {
            return () => send_strand(strand, handler);
        }

        static public Func<T, Task> wrap_send_strand<T>(shared_strand strand, Action<T> handler)
        {
            return (T p) => send_strand(strand, () => handler(p));
        }

        static public Func<ValueTask<R>> wrap_send_strand<R>(shared_strand strand, Func<R> handler)
        {
            return delegate ()
            {
                return send_strand(strand, handler);
            };
        }

        static public Func<T, ValueTask<R>> wrap_send_strand<R, T>(shared_strand strand, Func<T, R> handler)
        {
            return delegate (T p)
            {
                return send_strand(strand, () => handler(p));
            };
        }
#if NETCORE
#else
        static public void post_control(System.Windows.Forms.Control ctrl, Action handler)
        {
            try
            {
                ctrl.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate ()
                {
                    try
                    {
                        handler();
                    }
                    catch (System.Exception ec)
                    {
                        Trace.Fail(ec.Message, ec.StackTrace);
                    }
                });
            }
            catch (System.InvalidOperationException ec)
            {
                Trace.Fail(ec.Message, ec.StackTrace);
            }
        }

        static public Task unsafe_send_control(System.Windows.Forms.Control ctrl, Action handler)
        {
            if (!ctrl.InvokeRequired)
            {
                handler();
                return non_async();
            }
            generator this_ = self;
            post_control(ctrl, this_.unsafe_async_callback(handler));
            return this_.async_wait();
        }

        static public async Task send_control(System.Windows.Forms.Control ctrl, Action handler)
        {
            if (!ctrl.InvokeRequired)
            {
                handler();
                return;
            }
            generator this_ = self;
            System.Exception hasExcep = null;
            post_control(ctrl, this_.unsafe_async_callback(delegate ()
            {
                try
                {
                    handler();
                }
                catch (System.Exception ec)
                {
                    hasExcep = ec;
                }
            }));
            await this_.async_wait();
            if (null != hasExcep)
            {
                throw hasExcep;
            }
        }

        static public Task unsafe_send_control<R>(async_result_wrap<R> res, System.Windows.Forms.Control ctrl, Func<R> handler)
        {
            if (!ctrl.InvokeRequired)
            {
                res.value1 = handler();
                return non_async();
            }
            res.clear();
            generator this_ = self;
            post_control(ctrl, this_.unsafe_async_callback(delegate ()
            {
                res.value1 = handler();
            }));
            return this_.async_wait();
        }

        static public async Task<R> send_control<R>(System.Windows.Forms.Control ctrl, Func<R> handler)
        {
            if (!ctrl.InvokeRequired)
            {
                return handler();
            }
            generator this_ = self;
            R res = default(R);
            System.Exception hasExcep = null;
            post_control(ctrl, this_.unsafe_async_callback(delegate ()
            {
                try
                {
                    res = handler();
                }
                catch (System.Exception ec)
                {
                    hasExcep = ec;
                }
            }));
            await this_.async_wait();
            if (null != hasExcep)
            {
                throw hasExcep;
            }
            return res;
        }

        static public Action wrap_post_control(System.Windows.Forms.Control ctrl, Action handler)
        {
            return () => post_control(ctrl, handler);
        }

        static public Func<Task> wrap_send_control(System.Windows.Forms.Control ctrl, Action handler)
        {
            return () => send_control(ctrl, handler);
        }

        static public Func<T, Task> wrap_send_control<T>(System.Windows.Forms.Control ctrl, Action<T> handler)
        {
            return (T p) => send_control(ctrl, () => handler(p));
        }

        static public Func<Task<R>> wrap_send_control<R>(System.Windows.Forms.Control ctrl, Func<R> handler)
        {
            return async delegate ()
            {
                R res = default(R);
                await send_control(ctrl, () => res = handler());
                return res;
            };
        }

        static public Func<T, Task<R>> wrap_send_control<R, T>(System.Windows.Forms.Control ctrl, Func<T, R> handler)
        {
            return async delegate (T p)
            {
                R res = default(R);
                await send_control(ctrl, () => res = handler(p));
                return res;
            };
        }
#endif
        static public Task unsafe_send_task(Action handler)
        {
            generator this_ = self;
            this_._pullTask.new_task();
            bool beginQuit = this_._beginQuit;
            Task _ = Task.Run(delegate ()
            {
                handler();
                this_.strand.post(beginQuit ? (Action)this_.quit_next : this_.no_quit_next);
            });
            return this_.async_wait();
        }

        static public async Task send_task(Action handler)
        {
            generator this_ = self;
            System.Exception hasExcep = null;
            this_._pullTask.new_task();
            bool beginQuit = this_._beginQuit;
            Task _ = Task.Run(delegate ()
            {
                try
                {
                    handler();
                }
                catch (System.Exception ec)
                {
                    hasExcep = ec;
                }
                this_.strand.post(beginQuit ? (Action)this_.quit_next : this_.no_quit_next);
            });
            await this_.async_wait();
            if (null != hasExcep)
            {
                throw hasExcep;
            }
        }

        static public Task unsafe_send_task<R>(async_result_wrap<R> res, Func<R> handler)
        {
            generator this_ = self;
            this_._pullTask.new_task();
            bool beginQuit = this_._beginQuit;
            Task _ = Task.Run(delegate ()
            {
                res.value1 = handler();
                this_.strand.post(beginQuit ? (Action)this_.quit_next : this_.no_quit_next);
            });
            return this_.async_wait();
        }

        static public async Task<R> send_task<R>(Func<R> handler)
        {
            generator this_ = self;
            R res = default(R);
            System.Exception hasExcep = null;
            this_._pullTask.new_task();
            bool beginQuit = this_._beginQuit;
            Task _ = Task.Run(delegate ()
            {
                try
                {
                    res = handler();
                }
                catch (System.Exception ec)
                {
                    hasExcep = ec;
                }
                this_.strand.post(beginQuit ? (Action)this_.quit_next : this_.no_quit_next);
            });
            await this_.async_wait();
            if (null != hasExcep)
            {
                throw hasExcep;
            }
            return res;
        }

        static public Func<Task> wrap_send_task(Action handler)
        {
            return () => send_task(handler);
        }

        static public Func<T, Task> wrap_send_task<T>(Action<T> handler)
        {
            return (T p) => send_task(() => handler(p));
        }

        static public Func<Task<R>> wrap_send_task<R>(Func<R> handler)
        {
            return async delegate ()
            {
                R res = default(R);
                await send_task(() => res = handler());
                return res;
            };
        }

        static public Func<T, Task<R>> wrap_send_task<R, T>(Func<T, R> handler)
        {
            return async delegate (T p)
            {
                R res = default(R);
                await send_task(() => res = handler(p));
                return res;
            };
        }

        static public async Task send_async_queue(async_queue queue, shared_strand strand, generator.action action)
        {
            generator this_ = self;
            System.Exception hasExcep = null;
            queue.post(strand, async delegate ()
            {
                try
                {
                    await action();
                }
                catch (System.Exception ec)
                {
                    hasExcep = ec;
                }
            }, this_.unsafe_async_result());
            await self.async_wait();
            if (null != hasExcep)
            {
                throw hasExcep;
            }
        }

        static public Func<Task> wrap_send_async_queue(async_queue queue, shared_strand strand, generator.action action)
        {
            return () => send_async_queue(queue, strand, action);
        }

        static public Func<T, Task> wrap_send_async_queue<T>(async_queue queue, shared_strand strand, Func<T, Task> action)
        {
            return (T p) => send_async_queue(queue, strand, () => action(p));
        }

        static public Func<Task<R>> wrap_send_async_queue<R>(async_queue queue, shared_strand strand, Func<Task<R>> action)
        {
            return async delegate ()
            {
                R res = default(R);
                await send_async_queue(queue, strand, async () => res = await action());
                return res;
            };
        }

        static public Func<T, Task<R>> wrap_send_async_queue<R, T>(async_queue queue, shared_strand strand, Func<T, Task<R>> action)
        {
            return async delegate (T p)
            {
                R res = default(R);
                await send_async_queue(queue, strand, async () => res = await action(p));
                return res;
            };
        }

        static public async Task send_async_strand(async_strand queue, generator.action action)
        {
            generator this_ = self;
            System.Exception hasExcep = null;
            queue.post(async delegate ()
            {
                try
                {
                    await action();
                }
                catch (System.Exception ec)
                {
                    hasExcep = ec;
                }
            }, this_.unsafe_async_result());
            await self.async_wait();
            if (null != hasExcep)
            {
                throw hasExcep;
            }
        }

        static public Func<Task> wrap_send_async_strand(async_strand queue, generator.action action)
        {
            return () => send_async_strand(queue, action);
        }

        static public Func<T, Task> wrap_send_async_strand<T>(async_strand queue, Func<T, Task> action)
        {
            return (T p) => send_async_strand(queue, () => action(p));
        }

        static public Func<Task<R>> wrap_send_async_strand<R>(async_strand queue, Func<Task<R>> action)
        {
            return async delegate ()
            {
                R res = default(R);
                await send_async_strand(queue, async () => res = await action());
                return res;
            };
        }

        static public Func<T, Task<R>> wrap_send_async_strand<R, T>(async_strand queue, Func<T, Task<R>> action)
        {
            return async delegate (T p)
            {
                R res = default(R);
                await send_async_strand(queue, async () => res = await action(p));
                return res;
            };
        }

        static public Task wait_task(Task task)
        {
            if (!task.IsCompleted)
            {
                generator this_ = self;
                task.GetAwaiter().OnCompleted(this_.unsafe_async_result());
                return this_.async_wait();
            }
            return non_async();
        }

        static public async Task<R> wait_task<R>(Task<R> task)
        {
            if (!task.IsCompleted)
            {
                generator this_ = self;
                task.GetAwaiter().OnCompleted(this_.unsafe_async_result());
                await this_.async_wait();
            }
            return task.Result;
        }

        static public Task unsafe_wait_task<R>(async_result_wrap<R> res, Task<R> task)
        {
            if (!task.IsCompleted)
            {
                generator this_ = self;
                res.clear();
                task.GetAwaiter().OnCompleted(this_.async_callback(() => res.value1 = task.Result));
                return this_.async_wait();
            }
            res.value1 = task.Result;
            return non_async();
        }

        private Action set_overtime()
        {
            _overtime = false;
            if (null == _setOvertime)
            {
                _setOvertime = () => _overtime = true;
            }
            return _setOvertime;
        }

        static public async Task<bool> timed_wait_task(int ms, Task task)
        {
            if (!task.IsCompleted)
            {
                generator this_ = self;
                task.GetAwaiter().OnCompleted(this_.timed_async_result2(ms, this_.set_overtime()));
                await this_.async_wait();
                return !this_._overtime;
            }
            return true;
        }

        static public Task unsafe_timed_wait_task(async_result_wrap<bool> res, int ms, Task task)
        {
            if (!task.IsCompleted)
            {
                generator this_ = self;
                res.value1 = false;
                task.GetAwaiter().OnCompleted(this_.timed_async_callback2(ms, () => res.value1 = true));
                return this_.async_wait();
            }
            res.value1 = true;
            return non_async();
        }

        static public async Task<tuple<bool, R>> timed_wait_task<R>(int ms, Task<R> task)
        {
            if (!task.IsCompleted)
            {
                generator this_ = self;
                task.GetAwaiter().OnCompleted(this_.timed_async_result2(ms, this_.set_overtime()));
                await this_.async_wait();
                return tuple.make(!this_._overtime, this_._overtime ? default(R) : task.Result);
            }
            return tuple.make(true, task.Result);
        }

        static public Task unsafe_timed_wait_task<R>(async_result_wrap<bool, R> res, int ms, Task<R> task)
        {
            if (!task.IsCompleted)
            {
                generator this_ = self;
                res.value1 = false;
                task.GetAwaiter().OnCompleted(this_.timed_async_callback2(ms, delegate ()
                {
                    res.value1 = true;
                    res.value2 = task.Result;
                }));
                return this_.async_wait();
            }
            res.value1 = true;
            res.value2 = task.Result;
            return non_async();
        }

        static public Task stop_other(generator otherGen)
        {
            generator this_ = self;
            otherGen.stop(this_.unsafe_async_result());
            return this_.async_wait();
        }

        static public async Task stop_others(params generator[] otherGens)
        {
            if (0 != otherGens.Length)
            {
                generator this_ = self;
                unlimit_chan<void_type> waitStop = new unlimit_chan<void_type>(this_.strand);
                Action ntf = waitStop.wrap_default();
                int count = otherGens.Length;
                for (int i = 0; i < count; i++)
                {
                    otherGens[i].stop(ntf);
                }
                while (0 != count--)
                {
                    await chan_receive(waitStop);
                }
            }
        }

        static public Task wait_other(generator otherGen)
        {
            generator this_ = self;
            otherGen.append_stop_callback(this_.unsafe_async_result());
            return this_.async_wait();
        }

        static public async Task wait_others(params generator[] otherGens)
        {
            if (0 != otherGens.Length)
            {
                generator this_ = self;
                unlimit_chan<void_type> waitStop = new unlimit_chan<void_type>(this_.strand);
                Action ntf = waitStop.wrap_default();
                int count = otherGens.Length;
                for (int i = 0; i < count; i++)
                {
                    otherGens[i].append_stop_callback(ntf);
                }
                while (0 != count--)
                {
                    await chan_receive(waitStop);
                }
            }
        }

        static public async Task<bool> timed_wait_other(int ms, generator otherGen)
        {
            generator this_ = self;
            nil_chan<LinkedListNode<Action>> waitRemove = new nil_chan<LinkedListNode<Action>>();
            otherGen.append_stop_callback(this_.timed_async_result2(ms, this_.set_overtime()), waitRemove.wrap());
            await this_.async_wait();
            if (this_._overtime)
            {
                LinkedListNode<Action> node = await chan_receive(waitRemove);
                if (null != node)
                {
                    otherGen.remove_stop_callback(node);
                }
            }
            return !this_._overtime;
        }

        static public async Task<generator> wait_others_one(params generator[] otherGens)
        {
            if (0 != otherGens.Length)
            {
                generator this_ = self;
                unlimit_chan<tuple<generator, LinkedListNode<Action>>> waitRemove = new unlimit_chan<tuple<generator, LinkedListNode<Action>>>(this_.strand);
                async_result_wrap<generator> res = new async_result_wrap<generator>();
                Action<generator> ntf = this_.async_result(res);
                Action<tuple<generator, LinkedListNode<Action>>> removeNtf = waitRemove.wrap();
                int count = otherGens.Length;
                for (int i = 0; i < count; i++)
                {
                    generator ele = otherGens[i];
                    ele.append_stop_callback(() => ntf(ele), (LinkedListNode<Action> node) => removeNtf(tuple.make(ele, node)));
                }
                await this_.async_wait();
                while (0 != count--)
                {
                    tuple<generator, LinkedListNode<Action>> node = (await chan_receive(waitRemove)).msg;
                    if (null != node.value2)
                    {
                        node.value1.remove_stop_callback(node.value2);
                    }
                }
                return res.value1;
            }
            return null;
        }

        static public async Task<generator> timed_wait_others_one(int ms, params generator[] otherGens)
        {
            if (0 != otherGens.Length)
            {
                generator this_ = self;
                unlimit_chan<tuple<generator, LinkedListNode<Action>>> waitRemove = new unlimit_chan<tuple<generator, LinkedListNode<Action>>>(this_.strand);
                async_result_wrap<generator> res = new async_result_wrap<generator>();
                Action<generator> ntf = this_.timed_async_result(ms, res);
                Action<tuple<generator, LinkedListNode<Action>>> removeNtf = waitRemove.wrap();
                int count = otherGens.Length;
                for (int i = 0; i < count; i++)
                {
                    generator ele = otherGens[i];
                    ele.append_stop_callback(() => ntf(ele), (LinkedListNode<Action> node) => removeNtf(tuple.make(ele, node)));
                }
                await this_.async_wait();
                while (0 != count--)
                {
                    tuple<generator, LinkedListNode<Action>> node = (await chan_receive(waitRemove)).msg;
                    if (null != node.value2)
                    {
                        node.value1.remove_stop_callback(node.value2);
                    }
                }
                return res.value1;
            }
            return null;
        }

        static public Task wait_group(wait_group wg)
        {
            generator this_ = self;
            wg.async_wait(this_.unsafe_async_result());
            return this_.async_wait();
        }

        static public async Task<bool> timed_wait_group(int ms, wait_group wg)
        {
            generator this_ = self;
            wg.async_wait(this_.timed_async_result2(ms, this_.set_overtime()));
            await this_.async_wait();
            return !this_._overtime;
        }

        static public Task unsafe_async_call(Action<Action> handler)
        {
            generator this_ = self;
            handler(this_.unsafe_async_result());
            return this_.async_wait();
        }

        static public Task unsafe_async_call<R>(async_result_wrap<R> res, Action<Action<R>> handler)
        {
            generator this_ = self;
            res.clear();
            handler(this_.unsafe_async_result(res));
            return this_.async_wait();
        }

        static public Task unsafe_async_call<R1, R2>(async_result_wrap<R1, R2> res, Action<Action<R1, R2>> handler)
        {
            generator this_ = self;
            res.clear();
            handler(this_.unsafe_async_result(res));
            return this_.async_wait();
        }

        static public Task unsafe_async_call<R1, R2, R3>(async_result_wrap<R1, R2, R3> res, Action<Action<R1, R2, R3>> handler)
        {
            generator this_ = self;
            res.clear();
            handler(this_.unsafe_async_result(res));
            return this_.async_wait();
        }

        static public Task async_call(Action<Action> handler)
        {
            generator this_ = self;
            handler(this_.async_result());
            return this_.async_wait();
        }

        static public async Task<R> async_call<R>(Action<Action<R>> handler)
        {
            generator this_ = self;
            async_result_wrap<R> res = new async_result_wrap<R>();
            handler(this_.async_result(res));
            await this_.async_wait();
            return res.value1;
        }

        static public async Task<tuple<R1, R2>> async_call<R1, R2>(Action<Action<R1, R2>> handler)
        {
            generator this_ = self;
            async_result_wrap<R1, R2> res = new async_result_wrap<R1, R2>();
            handler(this_.async_result(res));
            await this_.async_wait();
            return tuple.make(res.value1, res.value2);
        }

        static public async Task<tuple<R1, R2, R3>> async_call<R1, R2, R3>(Action<Action<R1, R2, R3>> handler)
        {
            generator this_ = self;
            async_result_wrap<R1, R2, R3> res = new async_result_wrap<R1, R2, R3>();
            handler(this_.async_result(res));
            await this_.async_wait();
            return tuple.make(res.value1, res.value2, res.value3);
        }

        static public Task unsafe_timed_async_call(async_result_wrap<bool> res, int ms, Action<Action> handler, Action timedHandler = null)
        {
            generator this_ = self;
            res.value1 = false;
            this_._overtime = false;
            handler(this_.timed_async_callback(ms, () => res.value1 = !this_._overtime, null == timedHandler ? this_.set_overtime() : delegate ()
            {
                this_._overtime = true;
                timedHandler();
            }));
            return this_.async_wait();
        }

        static public async Task<bool> timed_async_call(int ms, Action<Action> handler, Action timedHandler = null)
        {
            generator this_ = self;
            this_._overtime = false;
            handler(this_.timed_async_callback(ms, nil_action.action, null == timedHandler ? this_.set_overtime() : delegate ()
            {
                this_._overtime = true;
                timedHandler();
            }));
            await this_.async_wait();
            return !this_._overtime;
        }

        static public Task unsafe_timed_async_call<R>(async_result_wrap<bool, R> res, int ms, Action<Action<R>> handler, Action timedHandler = null)
        {
            generator this_ = self;
            res.value1 = false;
            this_._overtime = false;
            handler(this_.timed_async_callback(ms, delegate (R res1)
            {
                res.value1 = !this_._overtime;
                res.value2 = res1;
            }, null == timedHandler ? this_.set_overtime() : delegate ()
            {
                this_._overtime = true;
                timedHandler();
            }));
            return this_.async_wait();
        }

        static public async Task<tuple<bool, R>> timed_async_call<R>(int ms, Action<Action<R>> handler, Action timedHandler = null)
        {
            generator this_ = self;
            R res = default(R);
            this_._overtime = false;
            handler(this_.timed_async_callback(ms, (R res1) => res = res1, null == timedHandler ? this_.set_overtime() : delegate ()
            {
                this_._overtime = true;
                timedHandler();
            }));
            await this_.async_wait();
            return tuple.make(!this_._overtime, res);
        }

        static public Task unsafe_timed_async_call<R1, R2>(async_result_wrap<bool, tuple<R1, R2>> res, int ms, Action<Action<R1, R2>> handler, Action timedHandler = null)
        {
            generator this_ = self;
            res.value1 = false;
            this_._overtime = false;
            handler(this_.timed_async_callback(ms, delegate (R1 res1, R2 res2)
            {
                res.value1 = !this_._overtime;
                res.value2 = tuple.make(res1, res2);
            }, null == timedHandler ? this_.set_overtime() : delegate ()
            {
                this_._overtime = true;
                timedHandler();
            }));
            return this_.async_wait();
        }

        static public async Task<tuple<bool, tuple<R1, R2>>> timed_async_call<R1, R2>(int ms, Action<Action<R1, R2>> handler, Action timedHandler = null)
        {
            generator this_ = self;
            tuple<R1, R2> res = default(tuple<R1, R2>);
            this_._overtime = false;
            handler(this_.timed_async_callback(ms, (R1 res1, R2 res2) => res = tuple.make(res1, res2), null == timedHandler ? this_.set_overtime() : delegate ()
            {
                this_._overtime = true;
                timedHandler();
            }));
            await this_.async_wait();
            return tuple.make(!this_._overtime, res);
        }

        static public Task unsafe_timed_async_call<R1, R2, R3>(async_result_wrap<bool, tuple<R1, R2, R3>> res, int ms, Action<Action<R1, R2, R3>> handler, Action timedHandler = null)
        {
            generator this_ = self;
            res.value1 = false;
            this_._overtime = false;
            handler(this_.timed_async_callback(ms, delegate (R1 res1, R2 res2, R3 res3)
            {
                res.value1 = !this_._overtime;
                res.value2 = tuple.make(res1, res2, res3);
            }, null == timedHandler ? this_.set_overtime() : delegate ()
            {
                this_._overtime = true;
                timedHandler();
            }));
            return this_.async_wait();
        }

        static public async Task<tuple<bool, tuple<R1, R2, R3>>> timed_async_call<R1, R2, R3>(int ms, Action<Action<R1, R2, R3>> handler, Action timedHandler = null)
        {
            generator this_ = self;
            tuple<R1, R2, R3> res = default(tuple<R1, R2, R3>);
            this_._overtime = false;
            handler(this_.timed_async_callback(ms, (R1 res1, R2 res2, R3 res3) => res = tuple.make(res1, res2, res3), null == timedHandler ? this_.set_overtime() : delegate ()
            {
                this_._overtime = true;
                timedHandler();
            }));
            await this_.async_wait();
            return tuple.make(!this_._overtime, res);
        }
#if DEBUG
        static public async Task call(action handler)
        {
            generator this_ = self;
            up_stack_frame(this_._makeStack, 2);
            try
            {
                await handler();
            }
            catch (System.Exception)
            {
                this_._makeStack.RemoveFirst();
                throw;
            }
        }

        static public async Task<R> call<R>(Func<Task<R>> handler)
        {
            generator this_ = self;
            up_stack_frame(this_._makeStack, 2);
            try
            {
                return await handler();
            }
            catch (System.Exception)
            {
                this_._makeStack.RemoveFirst();
                throw;
            }
        }

        static public async Task depth_call(shared_strand strand, action handler)
        {
            generator this_ = self;
            up_stack_frame(this_._makeStack, 2);
            (new generator()).init(strand, handler, this_.unsafe_async_result(), null, this_._makeStack).run();
            await lock_stop(() => this_.async_wait());
            this_._makeStack.RemoveFirst();
        }

        static public async Task<R> depth_call<R>(shared_strand strand, Func<Task<R>> handler)
        {
            generator this_ = self;
            R res = default(R);
            up_stack_frame(this_._makeStack, 2);
            (new generator()).init(strand, async () => res = await handler(), this_.unsafe_async_result(), null, this_._makeStack).run();
            await lock_stop(() => this_.async_wait());
            this_._makeStack.RemoveFirst();
            return res;
        }
#else
        static public Task call(action handler)
        {
            return handler();
        }

        static public Task<R> call<R>(Func<Task<R>> handler)
        {
            return handler();
        }

        static public Task depth_call(shared_strand strand, action handler)
        {
            generator this_ = self;
            go(strand, handler, this_.unsafe_async_result());
            return lock_stop(() => this_.async_wait());
        }

        static public async Task<R> depth_call<R>(shared_strand strand, Func<Task<R>> handler)
        {
            generator this_ = self;
            R res = default(R);
            go(strand, async () => res = await handler(), this_.unsafe_async_result(), null);
            await lock_stop(() => this_.async_wait());
            return res;
        }
#endif

#if DEBUG
        static public LinkedList<call_stack_info[]> call_stack
        {
            get
            {
                return self._makeStack;
            }
        }
#endif

        static long calc_hash<T>(int id)
        {
            return (long)id << 32 | (uint)type_hash<T>.code;
        }

        static public chan<T> self_mailbox<T>(int id = 0)
        {
            generator this_ = self;
            if (null == this_._mailboxMap)
            {
                this_._mailboxMap = new Dictionary<long, mail_pck>();
            }
            mail_pck mb = null;
            if (!this_._mailboxMap.TryGetValue(calc_hash<T>(id), out mb))
            {
                mb = new mail_pck(new unlimit_chan<T>(this_.strand));
                this_._mailboxMap.Add(calc_hash<T>(id), mb);
            }
            return (chan<T>)mb.mailbox;
        }

        public ValueTask<chan<T>> get_mailbox<T>(int id = 0)
        {
            return send_strand(strand, delegate ()
            {
                if (-1 == _lockSuspendCount)
                {
                    return null;
                }
                if (null == _mailboxMap)
                {
                    _mailboxMap = new Dictionary<long, mail_pck>();
                }
                mail_pck mb = null;
                if (!_mailboxMap.TryGetValue(calc_hash<T>(id), out mb))
                {
                    mb = new mail_pck(new unlimit_chan<T>(strand));
                    _mailboxMap.Add(calc_hash<T>(id), mb);
                }
                return (chan<T>)mb.mailbox;
            });
        }

        static public async Task<bool> agent_mail<T>(generator agentGen, int id = 0)
        {
            generator this_ = self;
            if (null == this_._agentMng)
            {
                this_._agentMng = new children();
            }
            if (null == this_._mailboxMap)
            {
                this_._mailboxMap = new Dictionary<long, mail_pck>();
            }
            mail_pck mb = null;
            if (!this_._mailboxMap.TryGetValue(calc_hash<T>(id), out mb))
            {
                mb = new mail_pck(new unlimit_chan<T>(this_.strand));
                this_._mailboxMap.Add(calc_hash<T>(id), mb);
            }
            else if (null != mb.agentAction)
            {
                await this_._agentMng.stop(mb.agentAction);
                mb.agentAction = null;
            }
            chan<T> agentMb = await agentGen.get_mailbox<T>();
            if (null == agentMb)
            {
                return false;
            }
            mb.agentAction = this_._agentMng.make(async delegate ()
            {
                chan<T> selfMb = (chan<T>)mb.mailbox;
                chan_notify_sign ntfSign = new chan_notify_sign();
                generator self = generator.self;
                try
                {
                    nil_chan<chan_async_state> waitHasChan = new nil_chan<chan_async_state>();
                    Action<chan_async_state> waitHasNtf = waitHasChan.wrap();
                    chan_recv_wrap<T> recvRes = new chan_recv_wrap<T> { state = chan_async_state.async_undefined };
                    Action<chan_async_state, T> tryPopHandler = delegate (chan_async_state state, T msg)
                    {
                        recvRes.state = state;
                        recvRes.msg = msg;
                    };
                    selfMb.async_append_recv_notify(waitHasNtf, ntfSign);
                    while (true)
                    {
                        await chan_receive(waitHasChan);
                        try
                        {
                            lock_suspend_and_stop();
                            recvRes = new chan_recv_wrap<T> { state = chan_async_state.async_undefined };
                            selfMb.async_try_recv_and_append_notify(self.unsafe_async_callback(tryPopHandler), waitHasNtf, ntfSign);
                            await self.async_wait();
                            if (chan_async_state.async_ok == recvRes.state)
                            {
                                recvRes.state = await chan_send(agentMb, recvRes.msg);
                            }
                            if (chan_async_state.async_closed == recvRes.state)
                            {
                                break;
                            }
                        }
                        finally
                        {
                            await unlock_suspend_and_stop();
                        }
                    }
                }
                catch (stop_exception)
                {
                    lock_suspend_and_stop();
                    selfMb.async_remove_recv_notify(self.unsafe_async_ignore<chan_async_state>(), ntfSign);
                    await self.async_wait();
                    await unlock_suspend_and_stop();
                }
            });
            mb.agentAction.run();
            return true;
        }

        static public async Task<bool> cancel_agent<T>(int id = 0)
        {
            generator this_ = self;
            mail_pck mb = null;
            if (null != this_._agentMng && null != this_._mailboxMap &&
                this_._mailboxMap.TryGetValue(calc_hash<T>(id), out mb) && null != mb.agentAction)
            {
                await this_._agentMng.stop(mb.agentAction);
                mb.agentAction = null;
                return true;
            }
            return false;
        }

        static public ValueTask<chan_recv_wrap<T>> recv_msg<T>(int id = 0)
        {
            return chan_receive(self_mailbox<T>(id));
        }

        static public ValueTask<chan_recv_wrap<T>> try_recv_msg<T>(int id = 0)
        {
            return chan_try_receive(self_mailbox<T>(id));
        }

        static public ValueTask<chan_recv_wrap<T>> timed_recv_msg<T>(int ms, int id = 0)
        {
            return chan_timed_receive(self_mailbox<T>(id), ms);
        }

        public async Task<chan_send_wrap> send_msg<T>(int id, T msg)
        {
            chan<T> mb = await get_mailbox<T>(id);
            return null != mb ? await chan_send(mb, msg) : new chan_send_wrap { state = chan_async_state.async_fail };
        }

        public Task<chan_send_wrap> send_msg<T>(T msg)
        {
            return send_msg(0, msg);
        }

        public Task<chan_send_wrap> send_void_msg(int id)
        {
            return send_msg(id, default(void_type));
        }

        public Task<chan_send_wrap> send_void_msg()
        {
            return send_msg(0, default(void_type));
        }

        public class receive_mail
        {
            bool _run = true;
            shared_mutex _mutex;
            children _children = new children();

            internal receive_mail(bool forceStopAll)
            {
                generator self = generator.self;
                if (null == self._mailboxMap)
                {
                    self._mailboxMap = new Dictionary<long, mail_pck>();
                }
                _mutex = forceStopAll ? null : new shared_mutex(self.strand);
            }

            public receive_mail case_of(chan<void_type> chan, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                return case_of(chan, (void_type _) => handler(), errHandler, lostMsg);
            }

            public receive_mail case_of<T>(chan<T> chan, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                _children.go(async delegate ()
                {
                    generator self = generator.self;
                    if (null == _mutex)
                    {
                        try
                        {
                            self._mailboxMap = _children.parent()._mailboxMap;
                            while (_run)
                            {
                                chan_recv_wrap<T> recvRes = await chan_receive(chan, lostMsg);
                                if (chan_async_state.async_ok == recvRes.state)
                                {
                                    await handler(recvRes.msg);
                                }
                                else if (null != errHandler && await errHandler(recvRes.state))
                                {
                                    break;
                                }
                                else if (chan_async_state.async_closed == recvRes.state)
                                {
                                    break;
                                }
                            }
                        }
                        catch (stop_this_receive_exception) { }
                        catch (stop_all_receive_exception)
                        {
                            _run = false;
                        }
                        finally
                        {
                            self._mailboxMap = null;
                        }
                    }
                    else
                    {
                        chan_notify_sign ntfSign = new chan_notify_sign();
                        try
                        {
                            lock_suspend();
                            self._mailboxMap = _children.parent()._mailboxMap;
                            nil_chan<chan_async_state> waitHasChan = new nil_chan<chan_async_state>();
                            Action<chan_async_state> waitHasNtf = waitHasChan.wrap();
                            chan_recv_wrap<T> recvRes = new chan_recv_wrap<T> { state = chan_async_state.async_undefined };
                            Action<chan_async_state, T> tryPopHandler = delegate (chan_async_state state, T msg)
                            {
                                recvRes.state = state;
                                recvRes.msg = msg;
                            };
                            chan.async_append_recv_notify(waitHasNtf, ntfSign);
                            while (_run)
                            {
                                await chan_receive(waitHasChan);
                                await mutex_lock_shared(_mutex);
                                try
                                {
                                    try
                                    {
                                        recvRes = new chan_recv_wrap<T> { state = chan_async_state.async_undefined };
                                        chan.async_try_recv_and_append_notify(self.unsafe_async_callback(tryPopHandler), waitHasNtf, ntfSign);
                                        await self.async_wait();
                                    }
                                    catch (stop_exception)
                                    {
                                        chan.async_remove_recv_notify(self.unsafe_async_ignore<chan_async_state>(), ntfSign);
                                        await self.async_wait();
                                        if (chan_async_state.async_ok == recvRes.state)
                                        {
                                            lostMsg?.set(recvRes.msg);
                                        }
                                        throw;
                                    }
                                    try
                                    {
                                        await unlock_suspend();
                                        if (chan_async_state.async_ok == recvRes.state)
                                        {
                                            await handler(recvRes.msg);
                                        }
                                        else if (null != errHandler && await errHandler(recvRes.state))
                                        {
                                            break;
                                        }
                                        else if (chan_async_state.async_closed == recvRes.state)
                                        {
                                            break;
                                        }
                                    }
                                    finally
                                    {
                                        lock_suspend();
                                    }
                                }
                                finally
                                {
                                    await mutex_unlock_shared(_mutex);
                                }
                            }
                        }
                        catch (stop_this_receive_exception) { }
                        catch (stop_all_receive_exception)
                        {
                            _run = false;
                        }
                        finally
                        {
                            lock_stop();
                            self._mailboxMap = null;
                            chan.async_remove_recv_notify(self.unsafe_async_ignore<chan_async_state>(), ntfSign);
                            await self.async_wait();
                            await unlock_suspend_and_stop();
                        }
                    }
                });
                return this;
            }

            public receive_mail case_of<T1, T2>(chan<tuple<T1, T2>> chan, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return case_of(chan, (tuple<T1, T2> msg) => handler(msg.value1, msg.value2), errHandler, lostMsg);
            }

            public receive_mail case_of<T1, T2, T3>(chan<tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return case_of(chan, (tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), errHandler, lostMsg);
            }

            public receive_mail timed_case_of(chan<void_type> chan, int ms, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                return timed_case_of(chan, ms, (void_type _) => handler(), errHandler, lostMsg);
            }

            public receive_mail timed_case_of<T>(chan<T> chan, int ms, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                _children.go(async delegate ()
                {
                    generator self = generator.self;
                    if (null == _mutex)
                    {
                        chan_recv_wrap<T> recvRes = await chan_timed_receive(chan, ms, lostMsg);
                        try
                        {
                            self._mailboxMap = _children.parent()._mailboxMap;
                            if (chan_async_state.async_ok == recvRes.state)
                            {
                                await handler(recvRes.msg);
                            }
                            else if (null != errHandler && await errHandler(recvRes.state)) { }
                        }
                        catch (stop_this_receive_exception) { }
                        catch (stop_all_receive_exception)
                        {
                            _run = false;
                        }
                        finally
                        {
                            self._mailboxMap = null;
                        }
                    }
                    else
                    {
                        chan_notify_sign ntfSign = new chan_notify_sign();
                        try
                        {
                            lock_suspend();
                            self._mailboxMap = _children.parent()._mailboxMap;
                            long endTick = system_tick.get_tick_ms() + ms;
                            while (_run)
                            {
                                chan_async_state result = chan_async_state.async_undefined;
                                chan.async_append_recv_notify(self.unsafe_async_callback((chan_async_state state) => result = state), ntfSign, ms);
                                await self.async_wait();
                                await mutex_lock_shared(_mutex);
                                try
                                {
                                    if (chan_async_state.async_overtime != result)
                                    {
                                        chan_recv_wrap<T> recvRes = await chan_try_receive(chan, lostMsg);
                                        try
                                        {
                                            await unlock_suspend();
                                            if (chan_async_state.async_ok == recvRes.state)
                                            {
                                                await handler(recvRes.msg); break;
                                            }
                                            else if ((null != errHandler && await errHandler(recvRes.state)) || chan_async_state.async_closed == recvRes.state) { break; }
                                            if (0 <= ms && 0 >= (ms = (int)(endTick - system_tick.get_tick_ms())))
                                            {
                                                ms = -1;
                                                if (null == errHandler || await errHandler(chan_async_state.async_overtime))
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                        finally
                                        {
                                            lock_suspend();
                                        }
                                    }
                                    else
                                    {
                                        ms = -1;
                                        if (null == errHandler || await errHandler(chan_async_state.async_overtime))
                                        {
                                            break;
                                        }
                                    }
                                }
                                finally
                                {
                                    await mutex_unlock_shared(_mutex);
                                }
                            }
                        }
                        catch (stop_this_receive_exception) { }
                        catch (stop_all_receive_exception)
                        {
                            _run = false;
                        }
                        finally
                        {
                            lock_stop();
                            self._mailboxMap = null;
                            chan.async_remove_recv_notify(self.unsafe_async_ignore<chan_async_state>(), ntfSign);
                            await self.async_wait();
                            await unlock_suspend_and_stop();
                        }
                    }
                });
                return this;
            }

            public receive_mail timed_case_of<T1, T2>(chan<tuple<T1, T2>> chan, int ms, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return timed_case_of(chan, ms, (tuple<T1, T2> msg) => handler(msg.value1, msg.value2), errHandler, lostMsg);
            }

            public receive_mail timed_case_of<T1, T2, T3>(chan<tuple<T1, T2, T3>> chan, int ms, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return timed_case_of(chan, ms, (tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), errHandler, lostMsg);
            }

            public receive_mail try_case_of(chan<void_type> chan, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                return try_case_of(chan, (void_type _) => handler(), errHandler, lostMsg);
            }

            public receive_mail try_case_of<T>(chan<T> chan, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                _children.go(async delegate ()
                {
                    generator self = generator.self;
                    if (null != _mutex)
                    {
                        await mutex_lock_shared(_mutex);
                    }
                    try
                    {
                        self._mailboxMap = _children.parent()._mailboxMap;
                        chan_recv_wrap<T> recvRes = await chan_try_receive(chan, lostMsg);
                        if (chan_async_state.async_ok == recvRes.state)
                        {
                            await handler(recvRes.msg);
                        }
                        else if (null != errHandler && await errHandler(recvRes.state)) { }
                    }
                    catch (stop_this_receive_exception) { }
                    catch (stop_all_receive_exception)
                    {
                        _run = false;
                    }
                    finally
                    {
                        self._mailboxMap = null;
                        if (null != _mutex)
                        {
                            await mutex_unlock_shared(_mutex);
                        }
                    }
                });
                return this;
            }

            public receive_mail try_case_of<T1, T2>(chan<tuple<T1, T2>> chan, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return try_case_of(chan, (tuple<T1, T2> msg) => handler(msg.value1, msg.value2), errHandler, lostMsg);
            }

            public receive_mail try_case_of<T1, T2, T3>(chan<tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return try_case_of(chan, (tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), errHandler, lostMsg);
            }

            public receive_mail case_of(broadcast_chan<void_type> chan, Func<Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                return case_of(chan, (void_type _) => handler(), token, errHandler, lostMsg);
            }

            public receive_mail case_of<T1, T2>(broadcast_chan<tuple<T1, T2>> chan, Func<T1, T2, Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return case_of(chan, (tuple<T1, T2> msg) => handler(msg.value1, msg.value2), token, errHandler, lostMsg);
            }

            public receive_mail case_of<T1, T2, T3>(broadcast_chan<tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return case_of(chan, (tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), token, errHandler, lostMsg);
            }

            public receive_mail case_of<T>(broadcast_chan<T> chan, Func<T, Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                _children.go(async delegate ()
                {
                    generator self = generator.self;
                    token = null != token ? token : new broadcast_token();
                    if (null == _mutex)
                    {
                        try
                        {
                            self._mailboxMap = _children.parent()._mailboxMap;
                            while (_run)
                            {
                                chan_recv_wrap<T> recvRes = await chan_receive(chan, token, lostMsg);
                                if (chan_async_state.async_ok == recvRes.state)
                                {
                                    await handler(recvRes.msg);
                                }
                                else if (null != errHandler && await errHandler(recvRes.state))
                                {
                                    break;
                                }
                            }
                        }
                        catch (stop_this_receive_exception) { }
                        catch (stop_all_receive_exception)
                        {
                            _run = false;
                        }
                        finally
                        {
                            self._mailboxMap = null;
                        }
                    }
                    else
                    {
                        chan_notify_sign ntfSign = new chan_notify_sign();
                        try
                        {
                            lock_suspend();
                            self._mailboxMap = _children.parent()._mailboxMap;
                            nil_chan<chan_async_state> waitHasChan = new nil_chan<chan_async_state>();
                            Action<chan_async_state> waitHasNtf = waitHasChan.wrap();
                            chan_recv_wrap<T> recvRes = new chan_recv_wrap<T> { state = chan_async_state.async_undefined };
                            Action<chan_async_state, T> tryPopHandler = delegate (chan_async_state state, T msg)
                            {
                                recvRes.state = state;
                                recvRes.msg = msg;
                            };
                            chan.async_append_recv_notify(waitHasNtf, ntfSign);
                            while (_run)
                            {
                                await chan_receive(waitHasChan);
                                await mutex_lock_shared(_mutex);
                                try
                                {
                                    try
                                    {
                                        recvRes = new chan_recv_wrap<T> { state = chan_async_state.async_undefined };
                                        chan.async_try_recv_and_append_notify(self.unsafe_async_callback(tryPopHandler), waitHasNtf, ntfSign);
                                        await self.async_wait();
                                    }
                                    catch (stop_exception)
                                    {
                                        chan.async_remove_recv_notify(self.unsafe_async_ignore<chan_async_state>(), ntfSign);
                                        await self.async_wait();
                                        if (chan_async_state.async_ok == recvRes.state)
                                        {
                                            lostMsg?.set(recvRes.msg);
                                        }
                                        throw;
                                    }
                                    try
                                    {
                                        await unlock_suspend();
                                        if (chan_async_state.async_ok == recvRes.state)
                                        {
                                            await handler(recvRes.msg);
                                        }
                                        else if (null != errHandler && await errHandler(recvRes.state))
                                        {
                                            break;
                                        }
                                        else if (chan_async_state.async_closed == recvRes.state)
                                        {
                                            break;
                                        }
                                    }
                                    finally
                                    {
                                        lock_suspend();
                                    }
                                }
                                finally
                                {
                                    await mutex_unlock_shared(_mutex);
                                }
                            }
                        }
                        catch (stop_this_receive_exception) { }
                        catch (stop_all_receive_exception)
                        {
                            _run = false;
                        }
                        finally
                        {
                            lock_stop();
                            self._mailboxMap = null;
                            chan.async_remove_recv_notify(self.unsafe_async_ignore<chan_async_state>(), ntfSign);
                            await self.async_wait();
                            await unlock_suspend_and_stop();
                        }
                    }
                });
                return this;
            }

            public receive_mail timed_case_of(broadcast_chan<void_type> chan, int ms, Func<Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                return timed_case_of(chan, ms, (void_type _) => handler(), token, errHandler, lostMsg);
            }

            public receive_mail timed_case_of<T1, T2>(broadcast_chan<tuple<T1, T2>> chan, int ms, Func<T1, T2, Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return timed_case_of(chan, ms, (tuple<T1, T2> msg) => handler(msg.value1, msg.value2), token, errHandler, lostMsg);
            }

            public receive_mail timed_case_of<T1, T2, T3>(broadcast_chan<tuple<T1, T2, T3>> chan, int ms, Func<T1, T2, T3, Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return timed_case_of(chan, ms, (tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), token, errHandler, lostMsg);
            }

            public receive_mail timed_case_of<T>(broadcast_chan<T> chan, int ms, Func<T, Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                _children.go(async delegate ()
                {
                    generator self = generator.self;
                    token = null != token ? token : new broadcast_token();
                    if (null == _mutex)
                    {
                        chan_recv_wrap<T> recvRes = await chan_timed_receive(chan, ms, token, lostMsg);
                        try
                        {
                            self._mailboxMap = _children.parent()._mailboxMap;
                            if (chan_async_state.async_ok == recvRes.state)
                            {
                                await handler(recvRes.msg);
                            }
                            else if (null != errHandler && await errHandler(recvRes.state)) { }
                        }
                        catch (stop_this_receive_exception) { }
                        catch (stop_all_receive_exception)
                        {
                            _run = false;
                        }
                        finally
                        {
                            self._mailboxMap = null;
                        }
                    }
                    else
                    {
                        chan_notify_sign ntfSign = new chan_notify_sign();
                        try
                        {
                            lock_suspend();
                            self._mailboxMap = _children.parent()._mailboxMap;
                            long endTick = system_tick.get_tick_ms() + ms;
                            while (_run)
                            {
                                chan_async_state result = chan_async_state.async_undefined;
                                chan.async_append_recv_notify(self.unsafe_async_callback((chan_async_state state) => result = state), ntfSign, ms);
                                await self.async_wait();
                                await mutex_lock_shared(_mutex);
                                try
                                {
                                    if (chan_async_state.async_overtime != result)
                                    {
                                        chan_recv_wrap<T> recvRes = await chan_try_receive(chan, lostMsg);
                                        try
                                        {
                                            await unlock_suspend();
                                            if (chan_async_state.async_ok == recvRes.state)
                                            {
                                                await handler(recvRes.msg); break;
                                            }
                                            else if ((null != errHandler && await errHandler(recvRes.state)) || chan_async_state.async_closed == recvRes.state) { break; }
                                            if (0 <= ms && 0 >= (ms = (int)(endTick - system_tick.get_tick_ms())))
                                            {
                                                ms = -1;
                                                if (null == errHandler || await errHandler(chan_async_state.async_overtime))
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                        finally
                                        {
                                            lock_suspend();
                                        }
                                    }
                                    else
                                    {
                                        ms = -1;
                                        if (null == errHandler || await errHandler(chan_async_state.async_overtime))
                                        {
                                            break;
                                        }
                                    }
                                }
                                finally
                                {
                                    await mutex_unlock_shared(_mutex);
                                }
                            }
                        }
                        catch (stop_this_receive_exception) { }
                        catch (stop_all_receive_exception)
                        {
                            _run = false;
                        }
                        finally
                        {
                            lock_stop();
                            self._mailboxMap = null;
                            chan.async_remove_recv_notify(self.unsafe_async_ignore<chan_async_state>(), ntfSign);
                            await self.async_wait();
                            await unlock_suspend_and_stop();
                        }
                    }
                });
                return this;
            }

            public receive_mail try_case_of(broadcast_chan<void_type> chan, Func<Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                return try_case_of(chan, (void_type _) => handler(), token, errHandler, lostMsg);
            }

            public receive_mail try_case_of<T1, T2>(broadcast_chan<tuple<T1, T2>> chan, Func<T1, T2, Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return try_case_of(chan, (tuple<T1, T2> msg) => handler(msg.value1, msg.value2), token, errHandler, lostMsg);
            }

            public receive_mail try_case_of<T1, T2, T3>(broadcast_chan<tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return try_case_of(chan, (tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), token, errHandler, lostMsg);
            }

            public receive_mail try_case_of<T>(broadcast_chan<T> chan, Func<T, Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                _children.go(async delegate ()
                {
                    generator self = generator.self;
                    if (null != _mutex)
                    {
                        await mutex_lock_shared(_mutex);
                    }
                    try
                    {
                        self._mailboxMap = _children.parent()._mailboxMap;
                        chan_recv_wrap<T> recvRes = await chan_try_receive(chan, null != token ? token : new broadcast_token(), lostMsg);
                        if (chan_async_state.async_ok == recvRes.state)
                        {
                            await handler(recvRes.msg);
                        }
                        else if (null != errHandler && await errHandler(recvRes.state)) { }
                    }
                    catch (stop_this_receive_exception) { }
                    catch (stop_all_receive_exception)
                    {
                        _run = false;
                    }
                    finally
                    {
                        self._mailboxMap = null;
                        if (null != _mutex)
                        {
                            await mutex_unlock_shared(_mutex);
                        }
                    }
                });
                return this;
            }

            public receive_mail case_of<T>(csp_chan<void_type, T> chan, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                return case_of(chan, async (T msg) => { await handler(msg); return default(void_type); }, errHandler, lostMsg);
            }

            public receive_mail case_of<T1, T2>(csp_chan<void_type, tuple<T1, T2>> chan, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return case_of(chan, async (tuple<T1, T2> msg) => { await handler(msg.value1, msg.value2); return default(void_type); }, errHandler, lostMsg);
            }

            public receive_mail case_of<T1, T2, T3>(csp_chan<void_type, tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return case_of(chan, async (tuple<T1, T2, T3> msg) => { await handler(msg.value1, msg.value2, msg.value3); return default(void_type); }, errHandler, lostMsg);
            }

            public receive_mail case_of(csp_chan<void_type, void_type> chan, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                return case_of(chan, async (void_type _) => { await handler(); return default(void_type); }, errHandler, lostMsg);
            }

            public receive_mail case_of<R>(csp_chan<R, void_type> chan, Func<Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                return case_of(chan, (void_type _) => handler(), errHandler, lostMsg);
            }

            public receive_mail case_of<R, T1, T2>(csp_chan<R, tuple<T1, T2>> chan, Func<T1, T2, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return case_of(chan, (tuple<T1, T2> msg) => handler(msg.value1, msg.value2), errHandler, lostMsg);
            }

            public receive_mail case_of<R, T1, T2, T3>(csp_chan<R, tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return case_of(chan, (tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), errHandler, lostMsg);
            }

            public receive_mail case_of<R, T>(csp_chan<R, T> chan, Func<T, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                _children.go(async delegate ()
                {
                    generator self = generator.self;
                    if (null == _mutex)
                    {
                        try
                        {
                            self._mailboxMap = _children.parent()._mailboxMap;
                            while (_run)
                            {
                                csp_wait_wrap<R, T> recvRes = await csp_wait(chan, lostMsg);
                                if (chan_async_state.async_ok == recvRes.state)
                                {
                                    try
                                    {
                                        recvRes.complete(await handler(recvRes.msg));
                                    }
                                    catch (csp_fail_exception)
                                    {
                                        recvRes.fail();
                                    }
                                    catch (stop_exception)
                                    {
                                        recvRes.fail();
                                        throw;
                                    }
                                }
                                else if (null != errHandler && await errHandler(recvRes.state))
                                {
                                    break;
                                }
                                else if (chan_async_state.async_closed == recvRes.state)
                                {
                                    break;
                                }
                            }
                        }
                        catch (stop_this_receive_exception) { }
                        catch (stop_all_receive_exception)
                        {
                            _run = false;
                        }
                        finally
                        {
                            self._mailboxMap = null;
                        }
                    }
                    else
                    {
                        chan_notify_sign ntfSign = new chan_notify_sign();
                        try
                        {
                            lock_suspend();
                            self._mailboxMap = _children.parent()._mailboxMap;
                            nil_chan<chan_async_state> waitHasChan = new nil_chan<chan_async_state>();
                            Action<chan_async_state> waitHasNtf = waitHasChan.wrap();
                            csp_wait_wrap<R, T> recvRes = new csp_wait_wrap<R, T> { state = chan_async_state.async_undefined };
                            Action<chan_async_state, T, csp_chan<R, T>.csp_result> tryPopHandler = delegate (chan_async_state state, T msg, csp_chan<R, T>.csp_result cspRes)
                            {
                                recvRes.state = state;
                                recvRes.msg = msg;
                                recvRes.result = cspRes;
                            };
                            chan.async_append_recv_notify(waitHasNtf, ntfSign);
                            while (_run)
                            {
                                await chan_receive(waitHasChan);
                                await mutex_lock_shared(_mutex);
                                try
                                {
                                    recvRes = new csp_wait_wrap<R, T> { state = chan_async_state.async_undefined };
                                    try
                                    {
                                        chan.async_try_recv_and_append_notify(self.unsafe_async_callback(tryPopHandler), waitHasNtf, ntfSign);
                                        await self.async_wait();
                                    }
                                    catch (stop_exception)
                                    {
                                        chan.async_remove_recv_notify(self.unsafe_async_ignore<chan_async_state>(), ntfSign);
                                        await self.async_wait();
                                        if (chan_async_state.async_ok == recvRes.state)
                                        {
                                            lostMsg?.set(recvRes.msg);
                                            recvRes.fail();
                                        }
                                        throw;
                                    }
                                    try
                                    {
                                        await unlock_suspend();
                                        if (chan_async_state.async_ok == recvRes.state)
                                        {
                                            try
                                            {
                                                recvRes.complete(await handler(recvRes.msg));
                                            }
                                            catch (csp_fail_exception)
                                            {
                                                recvRes.fail();
                                            }
                                            catch (stop_exception)
                                            {
                                                recvRes.fail();
                                                throw;
                                            }
                                        }
                                        else if (null != errHandler && await errHandler(recvRes.state))
                                        {
                                            break;
                                        }
                                        else if (chan_async_state.async_closed == recvRes.state)
                                        {
                                            break;
                                        }
                                    }
                                    finally
                                    {
                                        lock_suspend();
                                    }
                                }
                                finally
                                {
                                    await mutex_unlock_shared(_mutex);
                                }
                            }
                        }
                        catch (stop_this_receive_exception) { }
                        catch (stop_all_receive_exception)
                        {
                            _run = false;
                        }
                        finally
                        {
                            lock_stop();
                            self._mailboxMap = null;
                            chan.async_remove_recv_notify(self.unsafe_async_ignore<chan_async_state>(), ntfSign);
                            await self.async_wait();
                            await unlock_suspend_and_stop();
                        }
                    }
                });
                return this;
            }

            public receive_mail timed_case_of<T>(csp_chan<void_type, T> chan, int ms, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                return timed_case_of(chan, ms, async (T msg) => { await handler(msg); return default(void_type); }, errHandler, lostMsg);
            }

            public receive_mail timed_case_of<T1, T2>(csp_chan<void_type, tuple<T1, T2>> chan, int ms, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return timed_case_of(chan, ms, async (tuple<T1, T2> msg) => { await handler(msg.value1, msg.value2); return default(void_type); }, errHandler, lostMsg);
            }

            public receive_mail timed_case_of<T1, T2, T3>(csp_chan<void_type, tuple<T1, T2, T3>> chan, int ms, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return timed_case_of(chan, ms, async (tuple<T1, T2, T3> msg) => { await handler(msg.value1, msg.value2, msg.value3); return default(void_type); }, errHandler, lostMsg);
            }

            public receive_mail timed_case_of(csp_chan<void_type, void_type> chan, int ms, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                return timed_case_of(chan, ms, async (void_type _) => { await handler(); return default(void_type); }, errHandler, lostMsg);
            }

            public receive_mail timed_case_of<R>(csp_chan<R, void_type> chan, int ms, Func<Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                return timed_case_of(chan, ms, (void_type _) => handler(), errHandler, lostMsg);
            }

            public receive_mail timed_case_of<R, T1, T2>(csp_chan<R, tuple<T1, T2>> chan, int ms, Func<T1, T2, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return timed_case_of(chan, ms, (tuple<T1, T2> msg) => handler(msg.value1, msg.value2), errHandler, lostMsg);
            }

            public receive_mail timed_case_of<R, T1, T2, T3>(csp_chan<R, tuple<T1, T2, T3>> chan, int ms, Func<T1, T2, T3, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return timed_case_of(chan, ms, (tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), errHandler, lostMsg);
            }

            public receive_mail timed_case_of<R, T>(csp_chan<R, T> chan, int ms, Func<T, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                _children.go(async delegate ()
                {
                    generator self = generator.self;
                    if (null == _mutex)
                    {
                        csp_wait_wrap<R, T> recvRes = await csp_timed_wait(chan, ms, lostMsg);
                        try
                        {
                            self._mailboxMap = _children.parent()._mailboxMap;
                            if (chan_async_state.async_ok == recvRes.state)
                            {
                                try
                                {
                                    recvRes.complete(await handler(recvRes.msg));
                                }
                                catch (csp_fail_exception)
                                {
                                    recvRes.fail();
                                }
                                catch (stop_exception)
                                {
                                    recvRes.fail();
                                    throw;
                                }
                            }
                            else if (null != errHandler && await errHandler(recvRes.state)) { }
                        }
                        catch (stop_this_receive_exception) { }
                        catch (stop_all_receive_exception)
                        {
                            _run = false;
                        }
                        finally
                        {
                            self._mailboxMap = null;
                        }
                    }
                    else
                    {
                        chan_notify_sign ntfSign = new chan_notify_sign();
                        try
                        {
                            lock_suspend();
                            self._mailboxMap = _children.parent()._mailboxMap;
                            long endTick = system_tick.get_tick_ms() + ms;
                            while (_run)
                            {
                                chan_async_state result = chan_async_state.async_undefined;
                                chan.async_append_recv_notify(self.unsafe_async_callback((chan_async_state state) => result = state), ntfSign, ms);
                                await self.async_wait();
                                await mutex_lock_shared(_mutex);
                                try
                                {
                                    if (chan_async_state.async_overtime != result)
                                    {
                                        csp_wait_wrap<R, T> recvRes = await csp_try_wait(chan, lostMsg);
                                        try
                                        {
                                            await unlock_suspend();
                                            if (chan_async_state.async_ok == recvRes.state)
                                            {
                                                try
                                                {
                                                    recvRes.complete(await handler(recvRes.msg)); break;
                                                }
                                                catch (csp_fail_exception)
                                                {
                                                    recvRes.fail();
                                                }
                                                catch (stop_exception)
                                                {
                                                    recvRes.fail();
                                                    throw;
                                                }
                                            }
                                            else if ((null != errHandler && await errHandler(recvRes.state)) || chan_async_state.async_closed == recvRes.state) { break; }
                                            if (0 <= ms && 0 >= (ms = (int)(endTick - system_tick.get_tick_ms())))
                                            {
                                                ms = -1;
                                                if (null == errHandler || await errHandler(chan_async_state.async_overtime))
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                        finally
                                        {
                                            lock_suspend();
                                        }
                                    }
                                    else
                                    {
                                        ms = -1;
                                        if (null == errHandler || await errHandler(chan_async_state.async_overtime))
                                        {
                                            break;
                                        }
                                    }
                                }
                                finally
                                {
                                    await mutex_unlock_shared(_mutex);
                                }
                            }
                        }
                        catch (stop_this_receive_exception) { }
                        catch (stop_all_receive_exception)
                        {
                            _run = false;
                        }
                        finally
                        {
                            lock_stop();
                            self._mailboxMap = null;
                            chan.async_remove_recv_notify(self.unsafe_async_ignore<chan_async_state>(), ntfSign);
                            await self.async_wait();
                            await unlock_suspend_and_stop();
                        }
                    }
                });
                return this;
            }

            public receive_mail try_case_of<T>(csp_chan<void_type, T> chan, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                return try_case_of(chan, async (T msg) => { await handler(msg); return default(void_type); }, errHandler, lostMsg);
            }

            public receive_mail try_case_of<T1, T2>(csp_chan<void_type, tuple<T1, T2>> chan, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return try_case_of(chan, async (tuple<T1, T2> msg) => { await handler(msg.value1, msg.value2); return default(void_type); }, errHandler, lostMsg);
            }

            public receive_mail try_case_of<T1, T2, T3>(csp_chan<void_type, tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return try_case_of(chan, async (tuple<T1, T2, T3> msg) => { await handler(msg.value1, msg.value2, msg.value3); return default(void_type); }, errHandler, lostMsg);
            }

            public receive_mail try_case_of(csp_chan<void_type, void_type> chan, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                return try_case_of(chan, async (void_type _) => { await handler(); return default(void_type); }, errHandler, lostMsg);
            }

            public receive_mail try_case_of<R>(csp_chan<R, void_type> chan, Func<Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                return try_case_of(chan, (void_type _) => handler(), errHandler, lostMsg);
            }

            public receive_mail try_case_of<R, T1, T2>(csp_chan<R, tuple<T1, T2>> chan, Func<T1, T2, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return try_case_of(chan, (tuple<T1, T2> msg) => handler(msg.value1, msg.value2), errHandler, lostMsg);
            }

            public receive_mail try_case_of<R, T1, T2, T3>(csp_chan<R, tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return try_case_of(chan, (tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), errHandler, lostMsg);
            }

            public receive_mail try_case_of<R, T>(csp_chan<R, T> chan, Func<T, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                _children.go(async delegate ()
                {
                    generator self = generator.self;
                    if (null != _mutex)
                    {
                        await mutex_lock_shared(_mutex);
                    }
                    try
                    {
                        self._mailboxMap = _children.parent()._mailboxMap;
                        csp_wait_wrap<R, T> recvRes = await csp_try_wait(chan, lostMsg);
                        if (chan_async_state.async_ok == recvRes.state)
                        {
                            try
                            {
                                recvRes.complete(await handler(recvRes.msg));
                            }
                            catch (csp_fail_exception)
                            {
                                recvRes.fail();
                            }
                            catch (stop_exception)
                            {
                                recvRes.fail();
                                throw;
                            }
                        }
                        else if (null != errHandler && await errHandler(recvRes.state)) { }
                    }
                    catch (stop_this_receive_exception) { }
                    catch (stop_all_receive_exception)
                    {
                        _run = false;
                    }
                    finally
                    {
                        self._mailboxMap = null;
                        if (null != _mutex)
                        {
                            await mutex_unlock_shared(_mutex);
                        }
                    }
                });
                return this;
            }

            public receive_mail case_of<T>(int id, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                return case_of(self_mailbox<T>(id), handler, errHandler, lostMsg);
            }

            public receive_mail case_of<T1, T2>(int id, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return case_of(self_mailbox<tuple<T1, T2>>(id), handler, errHandler, lostMsg);
            }

            public receive_mail case_of<T1, T2, T3>(int id, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return case_of(self_mailbox<tuple<T1, T2, T3>>(id), handler, errHandler, lostMsg);
            }

            public receive_mail timed_case_of<T>(int id, int ms, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                return timed_case_of(self_mailbox<T>(id), ms, handler, errHandler, lostMsg);
            }

            public receive_mail timed_case_of<T1, T2>(int id, int ms, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return timed_case_of(self_mailbox<tuple<T1, T2>>(id), ms, handler, errHandler, lostMsg);
            }

            public receive_mail timed_case_of<T1, T2, T3>(int id, int ms, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return timed_case_of(self_mailbox<tuple<T1, T2, T3>>(id), ms, handler, errHandler, lostMsg);
            }

            public receive_mail try_case_of<T>(int id, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                return try_case_of(self_mailbox<T>(id), handler, errHandler, lostMsg);
            }

            public receive_mail try_case_of<T1, T2>(int id, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return try_case_of(self_mailbox<tuple<T1, T2>>(id), handler, errHandler, lostMsg);
            }

            public receive_mail try_case_of<T1, T2, T3>(int id, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return try_case_of(self_mailbox<tuple<T1, T2, T3>>(id), handler, errHandler, lostMsg);
            }

            public receive_mail case_of<T>(Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                return case_of(0, handler, errHandler, lostMsg);
            }

            public receive_mail case_of<T1, T2>(Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return case_of(0, handler, errHandler, lostMsg);
            }

            public receive_mail case_of<T1, T2, T3>(Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return case_of(0, handler, errHandler, lostMsg);
            }

            public receive_mail timed_case_of<T>(int ms, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                return timed_case_of(0, ms, handler, errHandler, lostMsg);
            }

            public receive_mail timed_case_of<T1, T2>(int ms, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return timed_case_of(0, ms, handler, errHandler, lostMsg);
            }

            public receive_mail timed_case_of<T1, T2, T3>(int ms, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return timed_case_of(0, ms, handler, errHandler, lostMsg);
            }

            public receive_mail try_case_of<T>(Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                return try_case_of(0, handler, errHandler, lostMsg);
            }

            public receive_mail try_case_of<T1, T2>(Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return try_case_of(0, handler, errHandler, lostMsg);
            }

            public receive_mail try_case_of<T1, T2, T3>(Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return try_case_of(0, handler, errHandler, lostMsg);
            }

            public receive_mail case_of(int id, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                return case_of(self_mailbox<void_type>(id), handler, errHandler, lostMsg);
            }

            public receive_mail timed_case_of(int id, int ms, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                return timed_case_of(self_mailbox<void_type>(id), ms, handler, errHandler, lostMsg);
            }

            public receive_mail try_case_of(int id, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                return try_case_of(self_mailbox<void_type>(id), handler, errHandler, lostMsg);
            }

            public receive_mail case_of(Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                return case_of(0, handler, errHandler, lostMsg);
            }

            public receive_mail timed_case_of(int ms, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                return timed_case_of(0, ms, handler, errHandler, lostMsg);
            }

            public receive_mail try_case_of(Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                return try_case_of(0, handler, errHandler, lostMsg);
            }

            public async Task end()
            {
                while (0 != _children.count())
                {
                    await _children.wait_one();
                    if (!_run)
                    {
                        if (null != _mutex)
                        {
                            await mutex_lock(_mutex);
                            await _children.stop();
                            await mutex_unlock(_mutex);
                        }
                        else
                        {
                            await _children.stop();
                        }
                    }
                }
            }

            public Func<Task> wrap()
            {
                return end;
            }
        }

        static public receive_mail receive(bool forceStopAll = true)
        {
            return new receive_mail(forceStopAll);
        }

        static public void stop_this_receive()
        {
#if DEBUG
            generator this_ = self;
            Trace.Assert(null != this_ && null != this_.parent() && this_.parent()._mailboxMap == this_._mailboxMap, "不正确的 stop_this_receive 调用!");
#endif
            throw stop_this_receive_exception.val;
        }

        static public void stop_all_receive()
        {
#if DEBUG
            generator this_ = self;
            Trace.Assert(null != this_ && null != this_.parent() && this_.parent()._mailboxMap == this_._mailboxMap, "不正确的 stop_all_receive 调用!");
#endif
            throw stop_all_receive_exception.val;
        }

        public struct select_chans
        {
            internal bool _when;
            internal bool _random;
            internal LinkedList<select_chan_base> _chans;
            internal LinkedListNode<select_chan_base> _lastChansNode;
            internal unlimit_chan<tuple<chan_async_state, select_chan_base>> _selectChans;

            public select_chans case_recv_mail<T>(Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                return case_receive(self_mailbox<T>(), handler, errHandler, lostMsg);
            }

            public select_chans case_recv_mail<T1, T2>(Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return case_receive(self_mailbox<tuple<T1, T2>>(), handler, errHandler, lostMsg);
            }

            public select_chans case_recv_mail<T1, T2, T3>(Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return case_receive(self_mailbox<tuple<T1, T2, T3>>(), handler, errHandler, lostMsg);
            }

            public select_chans case_recv_mail<T>(int id, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                return case_receive(self_mailbox<T>(id), handler, errHandler, lostMsg);
            }

            public select_chans case_recv_mail<T1, T2>(int id, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return case_receive(self_mailbox<tuple<T1, T2>>(id), handler, errHandler, lostMsg);
            }

            public select_chans case_recv_mail<T1, T2, T3>(int id, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return case_receive(self_mailbox<tuple<T1, T2, T3>>(id), handler, errHandler, lostMsg);
            }

            public select_chans case_receive<T>(chan<T> chan, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(handler, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_receive<T1, T2>(chan<tuple<T1, T2>> chan, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader((tuple<T1, T2> msg) => handler(msg.value1, msg.value2), null, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_receive<T1, T2, T3>(chan<tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader((tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), null, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_receive(chan<void_type> chan, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader((void_type _) => handler(), errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_send<T>(chan<T> chan, async_result_wrap<T> msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_writer(msg, handler, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_send<T>(chan<T> chan, T msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_writer(msg, handler, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_send(chan<void_type> chan, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_writer(default(void_type), handler, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_receive<T1, T2>(broadcast_chan<tuple<T1, T2>> chan, Func<T1, T2, Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader((tuple<T1, T2> msg) => handler(msg.value1, msg.value2), token, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_receive<T1, T2, T3>(broadcast_chan<tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader((tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), token, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_receive<T>(broadcast_chan<T> chan, Func<T, Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(handler, token, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_receive(broadcast_chan<void_type> chan, Func<Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader((void_type _) => handler(), token, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_receive<R, T>(csp_chan<R, T> chan, Func<T, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(handler, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_receive<R, T1, T2>(csp_chan<R, tuple<T1, T2>> chan, Func<T1, T2, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader((tuple<T1, T2> msg) => handler(msg.value1, msg.value2), errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_receive<R, T1, T2, T3>(csp_chan<R, tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader((tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_receive<R>(csp_chan<R, void_type> chan, Func<Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader((void_type _) => handler(), errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_receive<T>(csp_chan<void_type, T> chan, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(async (T msg) => { await handler(msg); return default(void_type); }, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_receive<T1, T2>(csp_chan<void_type, tuple<T1, T2>> chan, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(async (tuple<T1, T2> msg) => { await handler(msg.value1, msg.value2); return default(void_type); }, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_receive<T1, T2, T3>(csp_chan<void_type, tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(async (tuple<T1, T2, T3> msg) => { await handler(msg.value1, msg.value2, msg.value3); return default(void_type); }, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_receive(csp_chan<void_type, void_type> chan, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(async (void_type _) => { await handler(); return default(void_type); }, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_send<R, T>(csp_chan<R, T> chan, async_result_wrap<T> msg, Func<R, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, Action<R> lostHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_writer(msg, handler, errHandler, lostHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_send<R, T>(csp_chan<R, T> chan, T msg, Func<R, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, Action<R> lostHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_writer(msg, handler, errHandler, lostHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_send<R>(csp_chan<R, void_type> chan, Func<R, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, Action<R> lostHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_writer(default(void_type), handler, errHandler, lostHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_send<T>(csp_chan<void_type, T> chan, async_result_wrap<T> msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, Action<void_type> lostHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_writer(msg, (void_type _) => handler(), errHandler, lostHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_send<T>(csp_chan<void_type, T> chan, T msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, Action<void_type> lostHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_writer(msg, (void_type _) => handler(), errHandler, lostHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_send(csp_chan<void_type, void_type> chan, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, Action<void_type> lostHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_writer(default(void_type), (void_type _) => handler(), errHandler, lostHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_recv_mail<T>(int ms, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                return case_timed_receive(self_mailbox<T>(), ms, handler, errHandler, lostMsg);
            }

            public select_chans case_timed_recv_mail<T1, T2>(int ms, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return case_timed_receive(self_mailbox<tuple<T1, T2>>(), ms, handler, errHandler, lostMsg);
            }

            public select_chans case_timed_recv_mail<T1, T2, T3>(int ms, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return case_timed_receive(self_mailbox<tuple<T1, T2, T3>>(), ms, handler, errHandler, lostMsg);
            }

            public select_chans case_timed_recv_mail<T>(int id, int ms, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                return case_timed_receive(self_mailbox<T>(id), ms, handler, errHandler, lostMsg);
            }

            public select_chans case_timed_recv_mail<T1, T2>(int id, int ms, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                return case_timed_receive(self_mailbox<tuple<T1, T2>>(id), ms, handler, errHandler, lostMsg);
            }

            public select_chans case_timed_recv_mail<T1, T2, T3>(int id, int ms, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                return case_timed_receive(self_mailbox<tuple<T1, T2, T3>>(id), ms, handler, errHandler, lostMsg);
            }

            public select_chans case_timed_receive<T>(chan<T> chan, int ms, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(ms, handler, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_receive<T1, T2>(chan<tuple<T1, T2>> chan, int ms, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(ms, (tuple<T1, T2> msg) => handler(msg.value1, msg.value2), null, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_receive<T1, T2, T3>(chan<tuple<T1, T2, T3>> chan, int ms, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(ms, (tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), null, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_receive(chan<void_type> chan, int ms, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(ms, (void_type _) => handler(), errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_send<T>(chan<T> chan, int ms, async_result_wrap<T> msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_writer(ms, msg, handler, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_send<T>(chan<T> chan, int ms, T msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_writer(ms, msg, handler, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_send(chan<void_type> chan, int ms, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_writer(ms, default(void_type), handler, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_receive<T1, T2>(broadcast_chan<tuple<T1, T2>> chan, int ms, Func<T1, T2, Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(ms, (tuple<T1, T2> msg) => handler(msg.value1, msg.value2), token, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_receive<T1, T2, T3>(broadcast_chan<tuple<T1, T2, T3>> chan, int ms, Func<T1, T2, T3, Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(ms, (tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), token, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_receive<T>(broadcast_chan<T> chan, int ms, Func<T, Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(ms, handler, token, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_receive(broadcast_chan<void_type> chan, int ms, Func<Task> handler, broadcast_token token = null, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(ms, (void_type _) => handler(), token, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_receive<R, T>(csp_chan<R, T> chan, int ms, Func<T, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(ms, handler, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_receive<R, T1, T2>(csp_chan<R, tuple<T1, T2>> chan, int ms, Func<T1, T2, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(ms, (tuple<T1, T2> msg) => handler(msg.value1, msg.value2), errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_receive<R, T1, T2, T3>(csp_chan<R, tuple<T1, T2, T3>> chan, int ms, Func<T1, T2, T3, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(ms, (tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_receive<R>(csp_chan<R, void_type> chan, int ms, Func<Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(ms, (void_type _) => handler(), errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_receive<T>(csp_chan<void_type, T> chan, int ms, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(ms, async (T msg) => { await handler(msg); return default(void_type); }, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_receive<T1, T2>(csp_chan<void_type, tuple<T1, T2>> chan, int ms, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2>> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(ms, async (tuple<T1, T2> msg) => { await handler(msg.value1, msg.value2); return default(void_type); }, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_receive<T1, T2, T3>(csp_chan<void_type, tuple<T1, T2, T3>> chan, int ms, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<tuple<T1, T2, T3>> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(ms, async (tuple<T1, T2, T3> msg) => { await handler(msg.value1, msg.value2, msg.value3); return default(void_type); }, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_receive(csp_chan<void_type, void_type> chan, int ms, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_reader(ms, async (void_type _) => { await handler(); return default(void_type); }, errHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_send<R, T>(csp_chan<R, T> chan, int ms, async_result_wrap<T> msg, Func<R, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, Action<R> lostHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_writer(ms, msg, handler, errHandler, lostHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_send<R, T>(csp_chan<R, T> chan, int ms, T msg, Func<R, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, Action<R> lostHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_writer(ms, msg, handler, errHandler, lostHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_send<R>(csp_chan<R, void_type> chan, int ms, Func<R, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, Action<R> lostHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_writer(ms, default(void_type), handler, errHandler, lostHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_send<T>(csp_chan<void_type, T> chan, int ms, async_result_wrap<T> msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, Action<void_type> lostHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_writer(ms, msg, (void_type _) => handler(), errHandler, lostHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_send<T>(csp_chan<void_type, T> chan, int ms, T msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, Action<void_type> lostHandler = null, chan_lost_msg<T> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_writer(ms, msg, (void_type _) => handler(), errHandler, lostHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public select_chans case_timed_send(csp_chan<void_type, void_type> chan, int ms, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null, Action<void_type> lostHandler = null, chan_lost_msg<void_type> lostMsg = null)
            {
                LinkedListNode<select_chan_base> chansNode = !_when ? null : _chans.AddLast(chan.make_select_writer(ms, default(void_type), (void_type _) => handler(), errHandler, lostHandler, lostMsg));
                return new select_chans { _when = true, _random = _random, _chans = _chans, _lastChansNode = chansNode, _selectChans = _selectChans };
            }

            public bool effective()
            {
                return null != _lastChansNode;
            }

            public void remove()
            {
                _chans.Remove(_lastChansNode);
            }

            public void restore()
            {
                _chans.AddLast(_lastChansNode);
            }

            public select_chans when(bool when)
            {
                return new select_chans { _when = when, _random = _random, _chans = _chans, _selectChans = _selectChans };
            }

            static private select_chan_base[] shuffle(LinkedList<select_chan_base> chans)
            {
                int count = chans.Count;
                select_chan_base[] shuffChans = new select_chan_base[count];
                chans.CopyTo(shuffChans, 0);
                mt19937 randGen = mt19937.global();
                for (int i = 0; i < count; i++)
                {
                    int rand = randGen.Next(i, count);
                    select_chan_base t = shuffChans[rand];
                    shuffChans[rand] = shuffChans[i];
                    shuffChans[i] = t;
                }
                return shuffChans;
            }

            public async Task<bool> loop(action eachAferDo = null)
            {
                generator this_ = self;
                LinkedList<select_chan_base> chans = _chans;
                unlimit_chan<tuple<chan_async_state, select_chan_base>> selectChans = _selectChans;
                try
                {
                    lock_suspend_and_stop();
                    if (null == this_._topSelectChans)
                    {
                        this_._topSelectChans = new LinkedList<LinkedList<select_chan_base>>();
                    }
                    this_._topSelectChans.AddFirst(chans);
                    if (_random)
                    {
                        await send_task(delegate ()
                        {
                            select_chan_base[] shuffChans = shuffle(chans);
                            int len = shuffChans.Length;
                            for (int i = 0; i < len; i++)
                            {
                                select_chan_base chan = shuffChans[i];
                                chan.ntfSign._selectOnce = false;
                                chan.nextSelect = (chan_async_state state) => selectChans.post(tuple.make(state, chan));
                                chan.begin(this_);
                            }
                        });
                    }
                    else
                    {
                        for (LinkedListNode<select_chan_base> it = chans.First; null != it; it = it.Next)
                        {
                            select_chan_base chan = it.Value;
                            chan.ntfSign._selectOnce = false;
                            chan.nextSelect = (chan_async_state state) => selectChans.post(tuple.make(state, chan));
                            chan.begin(this_);
                        }
                    }
                    unlock_stop();
                    int count = chans.Count;
                    bool selected = false;
                    Func<Task> stepOne = null == eachAferDo ? (Func<Task>)null : delegate ()
                    {
                        selected = true;
                        return non_async();
                    };
                    while (0 != count)
                    {
                        tuple<chan_async_state, select_chan_base> selectedChan = (await chan_receive(selectChans)).msg;
                        if (chan_async_state.async_ok != selectedChan.value1)
                        {
                            if (await selectedChan.value2.errInvoke(selectedChan.value1))
                            {
                                count--;
                            }
                            continue;
                        }
                        else if (selectedChan.value2.disabled())
                        {
                            continue;
                        }
                        try
                        {
                            select_chan_state selState = await selectedChan.value2.invoke(stepOne);
                            if (!selState.nextRound)
                            {
                                count--;
                            }
                        }
                        catch (stop_this_case_exception)
                        {
                            count--;
                            await selectedChan.value2.end();
                        }
                        if (selected)
                        {
                            try
                            {
                                selected = false;
                                await unlock_suspend();
                                await eachAferDo();
                            }
                            finally
                            {
                                lock_suspend();
                            }
                        }
                    }
                    return true;
                }
                catch (stop_select_exception)
                {
                    return false;
                }
                finally
                {
                    lock_stop();
                    this_._topSelectChans.RemoveFirst();
                    for (LinkedListNode<select_chan_base> it = chans.First; null != it; it = it.Next)
                    {
                        await it.Value.end();
                    }
                    selectChans.clear();
                    await unlock_suspend_and_stop();
                }
            }

            public async Task<bool> end()
            {
                generator this_ = self;
                LinkedList<select_chan_base> chans = _chans;
                unlimit_chan<tuple<chan_async_state, select_chan_base>> selectChans = _selectChans;
                bool selected = false;
                try
                {
                    lock_suspend_and_stop();
                    if (null == this_._topSelectChans)
                    {
                        this_._topSelectChans = new LinkedList<LinkedList<select_chan_base>>();
                    }
                    this_._topSelectChans.AddFirst(chans);
                    if (_random)
                    {
                        await send_task(delegate ()
                        {
                            select_chan_base[] shuffChans = shuffle(chans);
                            int len = shuffChans.Length;
                            for (int i = 0; i < len; i++)
                            {
                                select_chan_base chan = shuffChans[i];
                                chan.ntfSign._selectOnce = true;
                                chan.nextSelect = (chan_async_state state) => selectChans.post(tuple.make(state, chan));
                                chan.begin(this_);
                            }
                        });
                    }
                    else
                    {
                        for (LinkedListNode<select_chan_base> it = chans.First; null != it; it = it.Next)
                        {
                            select_chan_base chan = it.Value;
                            chan.ntfSign._selectOnce = true;
                            chan.nextSelect = (chan_async_state state) => selectChans.post(tuple.make(state, chan));
                            chan.begin(this_);
                        }
                    }
                    unlock_stop();
                    int count = chans.Count;
                    while (0 != count)
                    {
                        tuple<chan_async_state, select_chan_base> selectedChan = (await chan_receive(selectChans)).msg;
                        if (chan_async_state.async_ok != selectedChan.value1)
                        {
                            if (await selectedChan.value2.errInvoke(selectedChan.value1))
                            {
                                count--;
                            }
                            continue;
                        }
                        else if (selectedChan.value2.disabled())
                        {
                            continue;
                        }
                        try
                        {
                            select_chan_state selState = await selectedChan.value2.invoke(async delegate ()
                            {
                                for (LinkedListNode<select_chan_base> it = chans.First; null != it; it = it.Next)
                                {
                                    if (selectedChan.value2 != it.Value)
                                    {
                                        await it.Value.end();
                                    }
                                }
                                selected = true;
                            });
                            if (!selState.failed)
                            {
                                break;
                            }
                            else if (!selState.nextRound)
                            {
                                count--;
                            }
                        }
                        catch (stop_this_case_exception)
                        {
                            if (selected)
                            {
                                break;
                            }
                            else
                            {
                                count--;
                                await selectedChan.value2.end();
                            }
                        }
                    }
                }
                catch (stop_select_exception) { }
                finally
                {
                    lock_stop();
                    this_._topSelectChans.RemoveFirst();
                    if (!selected)
                    {
                        for (LinkedListNode<select_chan_base> it = chans.First; null != it; it = it.Next)
                        {
                            await it.Value.end();
                        }
                    }
                    selectChans.clear();
                    await unlock_suspend_and_stop();
                }
                return selected;
            }

            public async Task<bool> timed(int ms)
            {
                generator this_ = self;
                LinkedList<select_chan_base> chans = _chans;
                unlimit_chan<tuple<chan_async_state, select_chan_base>> selectChans = _selectChans;
                bool selected = false;
                try
                {
                    lock_suspend_and_stop();
                    if (null == this_._topSelectChans)
                    {
                        this_._topSelectChans = new LinkedList<LinkedList<select_chan_base>>();
                    }
                    this_._topSelectChans.AddFirst(chans);
                    if (ms >= 0)
                    {
                        this_._timer.timeout(ms, selectChans.wrap_default());
                    }
                    if (_random)
                    {
                        await send_task(delegate ()
                        {
                            select_chan_base[] shuffChans = shuffle(chans);
                            int len = shuffChans.Length;
                            for (int i = 0; i < len; i++)
                            {
                                select_chan_base chan = shuffChans[i];
                                chan.ntfSign._selectOnce = true;
                                chan.nextSelect = (chan_async_state state) => selectChans.post(tuple.make(state, chan));
                                chan.begin(this_);
                            }
                        });
                    }
                    else
                    {
                        for (LinkedListNode<select_chan_base> it = chans.First; null != it; it = it.Next)
                        {
                            select_chan_base chan = it.Value;
                            chan.ntfSign._selectOnce = true;
                            chan.nextSelect = (chan_async_state state) => selectChans.post(tuple.make(state, chan));
                            chan.begin(this_);
                        }
                    }
                    unlock_stop();
                    int count = chans.Count;
                    while (0 != count)
                    {
                        tuple<chan_async_state, select_chan_base> selectedChan = (await chan_receive(selectChans)).msg;
                        if (null != selectedChan.value2)
                        {
                            if (chan_async_state.async_ok != selectedChan.value1)
                            {
                                if (await selectedChan.value2.errInvoke(selectedChan.value1))
                                {
                                    count--;
                                }
                                continue;
                            }
                            else if (selectedChan.value2.disabled())
                            {
                                continue;
                            }
                            try
                            {
                                select_chan_state selState = await selectedChan.value2.invoke(async delegate ()
                                {
                                    this_._timer.cancel();
                                    for (LinkedListNode<select_chan_base> it = chans.First; null != it; it = it.Next)
                                    {
                                        if (selectedChan.value2 != it.Value)
                                        {
                                            await it.Value.end();
                                        }
                                    }
                                    selected = true;
                                });
                                if (!selState.failed)
                                {
                                    break;
                                }
                                else if (!selState.nextRound)
                                {
                                    count--;
                                }
                            }
                            catch (stop_this_case_exception)
                            {
                                if (selected)
                                {
                                    break;
                                }
                                else
                                {
                                    count--;
                                    await selectedChan.value2.end();
                                }
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                catch (stop_select_exception) { }
                finally
                {
                    lock_stop();
                    this_._topSelectChans.RemoveFirst();
                    if (!selected)
                    {
                        this_._timer.cancel();
                        for (LinkedListNode<select_chan_base> it = chans.First; null != it; it = it.Next)
                        {
                            await it.Value.end();
                        }
                    }
                    selectChans.clear();
                    await unlock_suspend_and_stop();
                }
                return true;
            }

            public Func<action, Task<bool>> wrap_loop()
            {
                return loop;
            }

            public Func<Task<bool>> wrap()
            {
                return end;
            }

            public Func<int, Task<bool>> wrap_timed()
            {
                return timed;
            }

            public Func<Task<bool>> wrap_loop(action eachAferDo)
            {
                return functional.bind(loop, eachAferDo);
            }

            public Func<Task<bool>> wrap_timed(int ms)
            {
                return functional.bind(timed, ms);
            }
        }

        static public select_chans select(bool random = false)
        {
            return new select_chans { _when = true, _random = random, _chans = new LinkedList<select_chan_base>(), _selectChans = new unlimit_chan<tuple<chan_async_state, select_chan_base>>(self_strand()) };
        }

        static public void stop_select()
        {
#if DEBUG
            generator this_ = self;
            Trace.Assert(null != this_ && null != this_._topSelectChans && 0 != this_._topSelectChans.Count, "不正确的 stop_select 调用!");
#endif
            throw stop_select_exception.val;
        }

        static public void stop_this_case()
        {
#if DEBUG
            generator this_ = self;
            Trace.Assert(null != this_ && null != this_._topSelectChans && 0 != this_._topSelectChans.Count, "不正确的 stop_this_case 调用!");
#endif
            throw stop_this_case_exception.val;
        }

        static public async Task disable_other_case(chan_base otherChan, bool disable = true)
        {
            generator this_ = self;
            if (null != this_._topSelectChans && 0 != this_._topSelectChans.Count)
            {
                LinkedList<select_chan_base> currSelect = this_._topSelectChans.First.Value;
                for (LinkedListNode<select_chan_base> it = currSelect.First; null != it; it = it.Next)
                {
                    select_chan_base chan = it.Value;
                    if (chan.channel() == otherChan && chan.disabled() != disable)
                    {
                        if (disable)
                        {
                            await chan.end();
                        }
                        else
                        {
                            chan.begin(this_);
                        }
                    }
                }
            }
        }

        static public async Task disable_other_case_receive(chan_base otherChan, bool disable = true)
        {
            generator this_ = self;
            if (null != this_._topSelectChans && 0 != this_._topSelectChans.Count)
            {
                LinkedList<select_chan_base> currSelect = this_._topSelectChans.First.Value;
                for (LinkedListNode<select_chan_base> it = currSelect.First; null != it; it = it.Next)
                {
                    select_chan_base chan = it.Value;
                    if (chan.channel() == otherChan && chan.is_read() && chan.disabled() != disable)
                    {
                        if (disable)
                        {
                            await chan.end();
                        }
                        else
                        {
                            chan.begin(this_);
                        }
                    }
                }
            }
        }

        static public async Task disable_other_case_send(chan_base otherChan, bool disable = true)
        {
            generator this_ = self;
            if (null != this_._topSelectChans && 0 != this_._topSelectChans.Count)
            {
                LinkedList<select_chan_base> currSelect = this_._topSelectChans.First.Value;
                for (LinkedListNode<select_chan_base> it = currSelect.First; null != it; it = it.Next)
                {
                    select_chan_base chan = it.Value;
                    if (chan.channel() == otherChan && !chan.is_read() && chan.disabled() != disable)
                    {
                        if (disable)
                        {
                            await chan.end();
                        }
                        else
                        {
                            chan.begin(this_);
                        }
                    }
                }
            }
        }

        static public Task enable_other_case(chan_base otherChan)
        {
            return disable_other_case(otherChan, false);
        }

        static public Task enable_other_case_receive(chan_base otherChan)
        {
            return disable_other_case_receive(otherChan, false);
        }

        static public Task enable_other_case_send(chan_base otherChan)
        {
            return disable_other_case_send(otherChan, false);
        }

        static public object self_value
        {
            get
            {
                return self._selfValue;
            }
            set
            {
                self._selfValue = value;
            }
        }

        public object value
        {
            get
            {
                return _selfValue;
            }
            set
            {
                _selfValue = value;
            }
        }

        static public System.Version version
        {
            get
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        public class child : generator
        {
            bool _isFree;
            internal children _childrenMgr;
            internal LinkedListNode<child> _childNode;

            child(children childrenMgr, bool isFree = false) : base()
            {
                _isFree = isFree;
                _childrenMgr = childrenMgr;
            }

            static internal child make(children childrenMgr, shared_strand strand, action handler, Action callback = null, Action<bool> suspendCb = null)
            {
                return (child)(new child(childrenMgr)).init(strand, handler, callback, suspendCb);
            }

            static internal child free_make(children childrenMgr, shared_strand strand, action handler, Action callback = null, Action<bool> suspendCb = null)
            {
                return (child)(new child(childrenMgr, true)).init(strand, handler, callback, suspendCb);
            }

            public override generator parent()
            {
                return null != _childrenMgr ? _childrenMgr.parent() : null;
            }

            public bool is_free()
            {
                return _isFree;
            }

            public new child trun()
            {
                return (child)base.trun();
            }
        }

        public class children
        {
            bool _ignoreSuspend;
            int _freeCount;
            generator _parent;
            LinkedList<child> _children;
            LinkedListNode<children> _node;

            public children()
            {
                _parent = self;
                _freeCount = 0;
                _ignoreSuspend = false;
                _children = new LinkedList<child>();
                if (null == _parent._children)
                {
                    _parent._children = new LinkedList<children>();
                }
            }

            void check_append_node()
            {
                if (0 == _children.Count)
                {
                    _node = _parent._children.AddLast(this);
                }
            }

            void check_remove_node()
            {
                if (0 == _children.Count && null != _node)
                {
                    _parent._children.Remove(_node);
                    _node = null;
                }
            }

            public child make(shared_strand strand, action handler, Action callback = null, Action<bool> suspendCb = null)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                check_append_node();
                child newGen = child.make(this, strand, handler, callback, suspendCb);
                newGen._childNode = _children.AddLast(newGen);
                return newGen;
            }

            public child free_make(shared_strand strand, action handler, Action callback = null, Action<bool> suspendCb = null)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                check_append_node();
                child newGen = null;
                _freeCount++;
                newGen = child.free_make(this, strand, handler, delegate ()
                {
                    _freeCount--;
                    _parent.strand.distribute(delegate ()
                    {
                        if (null != newGen._childNode)
                        {
                            _children.Remove(newGen._childNode);
                            newGen._childNode = null;
                            newGen._childrenMgr = null;
                            check_remove_node();
                        }
                    });
                    callback?.Invoke();
                }, suspendCb);
                newGen._childNode = _children.AddLast(newGen);
                return newGen;
            }

            public void go(shared_strand strand, action handler, Action callback = null, Action<bool> suspendCb = null)
            {
                make(strand, handler, callback, suspendCb).run();
            }

            public void free_go(shared_strand strand, action handler, Action callback = null, Action<bool> suspendCb = null)
            {
                free_make(strand, handler, callback, suspendCb).run();
            }

            public child tgo(shared_strand strand, action handler, Action callback = null, Action<bool> suspendCb = null)
            {
                return make(strand, handler, callback, suspendCb).trun();
            }

            public child free_tgo(shared_strand strand, action handler, Action callback = null, Action<bool> suspendCb = null)
            {
                return free_make(strand, handler, callback, suspendCb).trun();
            }

            public child make(action handler, Action callback = null, Action<bool> suspendCb = null)
            {
                return make(_parent.strand, handler, callback, suspendCb);
            }

            public child free_make(action handler, Action callback = null, Action<bool> suspendCb = null)
            {
                return free_make(_parent.strand, handler, callback, suspendCb);
            }

            public void go(action handler, Action callback = null, Action<bool> suspendCb = null)
            {
                go(_parent.strand, handler, callback, suspendCb);
            }

            public void free_go(action handler, Action callback = null, Action<bool> suspendCb = null)
            {
                free_go(_parent.strand, handler, callback, suspendCb);
            }

            public child tgo(action handler, Action callback = null, Action<bool> suspendCb = null)
            {
                return tgo(_parent.strand, handler, callback, suspendCb);
            }

            public child free_tgo(action handler, Action callback = null, Action<bool> suspendCb = null)
            {
                return free_tgo(_parent.strand, handler, callback, suspendCb);
            }

            public void ignore_suspend(bool igonre = true)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                _ignoreSuspend = igonre;
            }

            public void suspend(bool isSuspend, Action cb)
            {
                if (!_ignoreSuspend && 0 != _children.Count)
                {
                    int count = _children.Count;
                    Action suspendCb = _parent.strand.wrap(delegate ()
                    {
                        if (0 == --count)
                        {
                            cb();
                        }
                    });
                    child[] tempChildren = new child[_children.Count];
                    _children.CopyTo(tempChildren, 0);
                    for (int i = 0; i < tempChildren.Length; i++)
                    {
                        (isSuspend ? (Action<Action>)tempChildren[i].suspend : tempChildren[i].resume)(suspendCb);
                    }
                }
                else
                {
                    cb();
                }
            }

            public int count()
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                return _children.Count;
            }

            public generator parent()
            {
                return _parent;
            }

            public int discard(params child[] gens)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                int count = 0;
                if (0 != gens.Length)
                {
                    for (int i = 0; i < gens.Length; i++)
                    {
                        child ele = gens[i];
                        if (null != ele._childNode)
                        {
#if DEBUG
                            Trace.Assert(ele._childNode.List == _children, "此 child 不属于当前 children");
#endif
                            count++;
                            _children.Remove(ele._childNode);
                            ele._childNode = null;
                            ele._childrenMgr = null;
                        }
                    }
                    check_remove_node();
                }
                return count;
            }

            static public int discard(params children[] childrens)
            {
                int count = 0;
                if (0 != childrens.Length)
                {
#if DEBUG
                    generator self = generator.self;
#endif
                    for (int i = 0; i < childrens.Length; i++)
                    {
                        children childs = childrens[i];
#if DEBUG
                        Trace.Assert(self == childs._parent, "此 children 不属于当前 generator");
#endif
                        for (LinkedListNode<child> it = childs._children.First; null != it; it = it.Next)
                        {
                            count++;
                            childs._children.Remove(it.Value._childNode);
                            it.Value._childNode = null;
                            it.Value._childrenMgr = null;
                            childs.check_remove_node();
                        }
                    }
                }
                return count;
            }

            public async Task<bool> stop(child gen)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
                Trace.Assert(null == gen._childNode || gen._childNode.List == _children, "此 child 不属于当前 children");
#endif
                if (null != gen._childNode)
                {
                    gen.stop(_parent.unsafe_async_result());
                    await _parent.async_wait();
                    if (null != gen._childNode)
                    {
                        _children.Remove(gen._childNode);
                        gen._childNode = null;
                        gen._childrenMgr = null;
                        check_remove_node();
                    }
                    return true;
                }
                return false;
            }

            public async Task<int> stop(params child[] gens)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                int count = 0;
                if (0 != gens.Length)
                {
                    unlimit_chan<child> waitStop = new unlimit_chan<child>(_parent.strand);
                    for (int i = 0; i < gens.Length; i++)
                    {
                        child ele = gens[i];
                        if (null != ele._childNode)
                        {
#if DEBUG
                            Trace.Assert(ele._childNode.List == _children, "此 child 不属于当前 children");
#endif
                            count++;
                            ele.stop(() => waitStop.post(ele));
                        }
                    }
                    for (int i = 0; i < count; i++)
                    {
                        child gen = (await chan_receive(waitStop)).msg;
                        if (null != gen._childNode)
                        {
                            _children.Remove(gen._childNode);
                            gen._childNode = null;
                            gen._childrenMgr = null;
                        }
                    }
                    check_remove_node();
                }
                return count;
            }

            public async Task<bool> wait(child gen)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
                Trace.Assert(null == gen._childNode || gen._childNode.List == _children, "此 child 不属于当前 children");
#endif
                if (null != gen._childNode)
                {
                    gen.append_stop_callback(_parent.unsafe_async_result());
                    await _parent.async_wait();
                    if (null != gen._childNode)
                    {
                        _children.Remove(gen._childNode);
                        gen._childNode = null;
                        gen._childrenMgr = null;
                        check_remove_node();
                    }
                    return true;
                }
                return false;
            }

            public async Task<int> wait(params child[] gens)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                int count = 0;
                if (0 != gens.Length)
                {
                    unlimit_chan<child> waitStop = new unlimit_chan<child>(_parent.strand);
                    for (int i = 0; i < gens.Length; i++)
                    {
                        child ele = gens[i];
                        if (null != ele._childNode)
                        {
#if DEBUG
                            Trace.Assert(ele._childNode.List == _children, "此 child 不属于当前 children");
#endif
                            count++;
                            ele.append_stop_callback(() => waitStop.post(ele));
                        }
                    }
                    for (int i = 0; i < count; i++)
                    {
                        child gen = (await chan_receive(waitStop)).msg;
                        if (null != gen._childNode)
                        {
                            _children.Remove(gen._childNode);
                            gen._childNode = null;
                            gen._childrenMgr = null;
                        }
                    }
                    check_remove_node();
                }
                return count;
            }

            public async Task<bool> timed_wait(int ms, child gen)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
                Trace.Assert(null == gen._childNode || gen._childNode.List == _children, "此 child 不属于当前 children");
#endif
                bool overtime = false;
                if (null != gen._childNode)
                {
                    overtime = !await timed_wait_other(ms, gen);
                    if (!overtime && null != gen._childNode)
                    {
                        _children.Remove(gen._childNode);
                        gen._childNode = null;
                        gen._childrenMgr = null;
                        check_remove_node();
                    }
                }
                return !overtime;
            }

            public async Task stop(bool containFree = true)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                if (0 != _children.Count)
                {
                    unlimit_chan<child> waitStop = new unlimit_chan<child>(_parent.strand);
                    int count = _children.Count;
                    if (0 == _freeCount)
                    {
                        for (LinkedListNode<child> it = _children.First; null != it; it = it.Next)
                        {
                            child ele = it.Value;
                            ele.stop(() => waitStop.post(ele));
                        }
                    }
                    else
                    {
                        child[] tempChilds = new child[_children.Count];
                        _children.CopyTo(tempChilds, 0);
                        for (int i = 0; i < tempChilds.Length; i++)
                        {
                            child ele = tempChilds[i];
                            if (!containFree && ele.is_free())
                            {
                                count--;
                                continue;
                            }
                            ele.stop(() => waitStop.post(ele));
                        }
                    }
                    while (0 != count--)
                    {
                        child gen = (await chan_receive(waitStop)).msg;
                        if (null != gen._childNode)
                        {
                            _children.Remove(gen._childNode);
                            gen._childNode = null;
                            gen._childrenMgr = null;
                        }
                    }
                    check_remove_node();
                }
            }

            static public async Task stop(params children[] childrens)
            {
                if (0 != childrens.Length)
                {
                    generator self = generator.self;
                    unlimit_chan<Tuple<children, child>> waitStop = new unlimit_chan<Tuple<children, child>>(self.strand);
                    int count = 0;
                    for (int i = 0; i < childrens.Length; i++)
                    {
                        children childs = childrens[i];
#if DEBUG
                        Trace.Assert(self == childs._parent, "此 children 不属于当前 generator");
#endif
                        if (0 != childs._children.Count)
                        {
                            count += childs._children.Count;
                            if (0 == childs._freeCount)
                            {
                                for (LinkedListNode<child> it = childs._children.First; null != it; it = it.Next)
                                {
                                    child ele = it.Value;
                                    ele.stop(() => waitStop.post(new Tuple<children, child>(childs, ele)));
                                }
                            }
                            else
                            {
                                child[] tempChilds = new child[childs._children.Count];
                                childs._children.CopyTo(tempChilds, 0);
                                for (int j = 0; j < tempChilds.Length; j++)
                                {
                                    child ele = tempChilds[j];
                                    ele.stop(() => waitStop.post(new Tuple<children, child>(childs, ele)));
                                }
                            }
                        }
                    }
                    for (int i = 0; i < count; i++)
                    {
                        Tuple<children, child> oneRes = (await chan_receive(waitStop)).msg;
                        if (null != oneRes.Item2._childNode)
                        {
                            oneRes.Item1._children.Remove(oneRes.Item2._childNode);
                            oneRes.Item2._childNode = null;
                            oneRes.Item2._childrenMgr = null;
                            oneRes.Item1.check_remove_node();
                        }
                    }
                }
            }

            public async Task<child> wait_one(bool containFree = false)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                if (0 != _children.Count)
                {
                    unlimit_chan<tuple<child, LinkedListNode<Action>>> waitRemove = new unlimit_chan<tuple<child, LinkedListNode<Action>>>(_parent.strand);
                    async_result_wrap<child> res = new async_result_wrap<child>();
                    Action<child> ntf = _parent.async_result(res);
                    Action<tuple<child, LinkedListNode<Action>>> removeNtf = waitRemove.wrap();
                    int count = 0;
                    for (LinkedListNode<child> it = _children.First; null != it; it = it.Next)
                    {
                        child ele = it.Value;
                        if (!containFree && ele.is_free())
                        {
                            continue;
                        }
                        count++;
                        ele.append_stop_callback(() => ntf(ele), (LinkedListNode<Action> node) => removeNtf(tuple.make(ele, node)));
                    }
                    if (0 != count)
                    {
                        await _parent.async_wait();
                        if (null != res.value1._childNode)
                        {
                            _children.Remove(res.value1._childNode);
                            res.value1._childNode = null;
                            res.value1._childrenMgr = null;
                            check_remove_node();
                        }
                        while (0 != count--)
                        {
                            tuple<child, LinkedListNode<Action>> node = (await chan_receive(waitRemove)).msg;
                            if (null != node.value2)
                            {
                                node.value1.remove_stop_callback(node.value2);
                            }
                        }
                        return res.value1;
                    }
                }
                return null;
            }

            public async Task<child> timed_wait_one(int ms, bool containFree = false)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                if (0 != _children.Count)
                {
                    unlimit_chan<tuple<child, LinkedListNode<Action>>> waitRemove = new unlimit_chan<tuple<child, LinkedListNode<Action>>>(_parent.strand);
                    async_result_wrap<child> res = new async_result_wrap<child>();
                    Action<child> ntf = _parent.timed_async_result(ms, res);
                    Action<tuple<child, LinkedListNode<Action>>> removeNtf = waitRemove.wrap();
                    int count = 0;
                    for (LinkedListNode<child> it = _children.First; null != it; it = it.Next)
                    {
                        child ele = it.Value;
                        if (!containFree && ele.is_free())
                        {
                            continue;
                        }
                        count++;
                        ele.append_stop_callback(() => ntf(ele), (LinkedListNode<Action> node) => removeNtf(tuple.make(ele, node)));
                    }
                    if (0 != count)
                    {
                        await _parent.async_wait();
                        if (null != res.value1 && null != res.value1._childNode)
                        {
                            _children.Remove(res.value1._childNode);
                            res.value1._childNode = null;
                            res.value1._childrenMgr = null;
                            check_remove_node();
                        }
                        while (0 != count--)
                        {
                            tuple<child, LinkedListNode<Action>> node = (await chan_receive(waitRemove)).msg;
                            if (null != node.value2)
                            {
                                node.value1.remove_stop_callback(node.value2);
                            }
                        }
                        return res.value1;
                    }
                }
                return null;
            }

            public async Task wait_all(bool containFree = true)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                if (0 != _children.Count)
                {
                    unlimit_chan<child> waitStop = new unlimit_chan<child>(_parent.strand);
                    int count = 0;
                    for (LinkedListNode<child> it = _children.First; null != it; it = it.Next)
                    {
                        child ele = it.Value;
                        if (!containFree && ele.is_free())
                        {
                            continue;
                        }
                        count++;
                        ele.append_stop_callback(() => waitStop.post(ele));
                    }
                    while (0 != count--)
                    {
                        child gen = (await chan_receive(waitStop)).msg;
                        if (null != gen._childNode)
                        {
                            _children.Remove(gen._childNode);
                            gen._childNode = null;
                            gen._childrenMgr = null;
                        }
                    }
                    check_remove_node();
                }
            }
        }
    }

    public class async_queue
    {
        struct gen_pck
        {
            public shared_strand strand;
            public generator.action action;

            public gen_pck(shared_strand st, generator.action act)
            {
                strand = st;
                action = act;
            }
        }

        unlimit_chan<gen_pck> _queue;
        generator _runGen;

        public async_queue()
        {
            shared_strand strand = new shared_strand();
            _queue = new unlimit_chan<gen_pck>(strand);
            _runGen = generator.make(strand, async delegate ()
            {
                while (true)
                {
                    chan_recv_wrap<gen_pck> pck = await generator.chan_receive(_queue);
                    await generator.depth_call(pck.msg.strand, pck.msg.action);
                }
            });
            _runGen.run();
        }

        ~async_queue()
        {
            _runGen.stop();
        }

        public void post(shared_strand strand, generator.action action)
        {
            _queue.post(new gen_pck(strand, action));
        }

        public void post(shared_strand strand, generator.action action, Action cb)
        {
            post(strand, async delegate ()
            {
                await action();
                functional.catch_invoke(cb);
            });
        }
    }

    public class async_strand
    {
        unlimit_chan<generator.action> _queue;
        generator _runGen;

        public async_strand(shared_strand strand)
        {
            _queue = new unlimit_chan<generator.action>(strand);
            _runGen = generator.make(strand, async delegate ()
            {
                while (true)
                {
                    chan_recv_wrap<generator.action> pck = await generator.chan_receive(_queue);
                    generator.lock_stop();
                    try
                    {
                        await pck.msg();
                    }
                    catch (Exception ec)
                    {
                        Debug.WriteLine(ec.StackTrace);
                    }
                    generator.unlock_stop();
                }
            });
            _runGen.run();
        }

        ~async_strand()
        {
            _runGen.stop();
        }

        public void post(generator.action action)
        {
            _queue.post(action);
        }

        public void post(generator.action action, Action cb)
        {
            post(async delegate ()
            {
                await action();
                functional.catch_invoke(cb);
            });
        }

        public void stop()
        {
            _runGen.stop();
        }
    }

    public class wait_group
    {
        int _tasks;
        Action _pulse;
        LinkedList<Action> _waitList;

        public wait_group(int initTasks = 0)
        {
            _tasks = initTasks;
            _waitList = new LinkedList<Action>();
            _pulse = delegate ()
            {
                Monitor.Enter(this);
                Monitor.Pulse(this);
                Monitor.Exit(this);
            };
        }

        public void reset()
        {
#if DEBUG
            Trace.Assert(0 == _tasks, "不正确的 reset 调用!");
#endif
            Monitor.Enter(this);
            if (null != _waitList)
            {
                _waitList.AddLast(_pulse);
                Monitor.Wait(this);
            }
            Monitor.Exit(this);
            _waitList = new LinkedList<Action>();
        }

        public int add(int delta = 1)
        {
            int tasks = 0;
            if (0 != delta && 0 == (tasks = Interlocked.Add(ref _tasks, delta)))
            {
                Monitor.Enter(this);
                LinkedList<Action> snapList = _waitList;
                _waitList = null;
                Monitor.Exit(this);
                for (LinkedListNode<Action> it = snapList.First; null != it; it = it.Next)
                {
                    functional.catch_invoke(it.Value);
                }
            }
            return tasks;
        }

        public void done()
        {
            add(-1);
        }

        public Action wrap_done()
        {
            return done;
        }

        public bool is_done()
        {
            return 0 == _tasks;
        }

        public void async_wait(Action continuation)
        {
            if (0 == _tasks)
            {
                functional.catch_invoke(continuation);
            }
            else
            {
                LinkedListNode<Action> newNode = new LinkedListNode<Action>(continuation);
                Monitor.Enter(this);
                if (null != _waitList)
                {
                    _waitList.AddLast(newNode);
                    Monitor.Exit(this);
                }
                else
                {
                    Monitor.Exit(this);
                    functional.catch_invoke(continuation);
                }
            }
        }

        public bool is_completed()
        {
            return 0 == _tasks;
        }

        public void sync_wait()
        {
            if (0 != _tasks)
            {
                LinkedListNode<Action> newNode = new LinkedListNode<Action>(_pulse);
                Monitor.Enter(this);
                if (null != _waitList)
                {
                    _waitList.AddLast(newNode);
                    Monitor.Wait(this);
                }
                Monitor.Exit(this);
            }
        }

        public bool sync_timed_wait(int ms)
        {
            bool ok = true;
            if (0 != _tasks)
            {
                LinkedListNode<Action> newNode = new LinkedListNode<Action>(_pulse);
                Monitor.Enter(this);
                if (null != _waitList)
                {
                    _waitList.AddLast(newNode);
                    if (!(ok = Monitor.Wait(this, ms) || null == _waitList))
                    {
                        _waitList.Remove(newNode);
                    }
                }
                Monitor.Exit(this);
            }
            return ok;
        }
    }
}
