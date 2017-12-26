using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Windows.Forms;

namespace Go
{
    class mutli_callback
    {
        bool _callbacked = false;
        public bool check()
        {
            bool t = _callbacked;
            _callbacked = true;
            return t;
        }

        public bool callbacked()
        {
            return _callbacked;
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

    public class chan_exception : System.Exception
    {
        public readonly chan_async_state state;
        public readonly object obj;
        public chan_exception(chan_async_state st, object o)
        {
            state = st;
            obj = o;
        }
    }

    public struct chan_recv_wrap<T>
    {
        public chan_async_state state;
        public T result;

        public static implicit operator T(chan_recv_wrap<T> rval)
        {
            if (chan_async_state.async_ok != rval.state)
            {
                throw new chan_exception(rval.state, rval);
            }
            return rval.result;
        }

        public override string ToString()
        {
            return chan_async_state.async_ok == state ?
                string.Format("chan_recv_wrap<{0}>.result={1}", typeof(T).Name, result) :
                string.Format("chan_recv_wrap<{0}>.state={1}", typeof(T).Name, state);
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
                throw new chan_exception(rval.state, rval);
            }
            return rval.result;
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

        public void complete(R res)
        {
            result.complete(res);
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

        class static_init
        {
            public static_init()
            {
                _nilTask.RunSynchronously();
            }
        }

        class type_hash<T>
        {
            public static readonly int code = Interlocked.Increment(ref _hashCount);
        }

        class mail_pck
        {
            public channel_base mailbox;
            public child agentAction;

            public mail_pck(channel_base mb)
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
        LinkedList<call_stack_info> _makeStack;
        long _beginStepTick;
        static readonly int _stepMaxCycle = 100;
#endif

        static int _hashCount = 0;
        static long _idCount = 0;
        static Task _nilTask = new Task(nil_action.action);
        static ReaderWriterLockSlim _nameMutex = new ReaderWriterLockSlim();
        static Dictionary<string, generator> _nameGens = new Dictionary<string, generator>();
        static static_init _init = new static_init();

        LinkedList<LinkedList<select_chan_base>> _selectChans;
        LinkedList<Action> _callbacks;
        Dictionary<long, mail_pck> _mailboxMap;
        Action<bool> _suspendCb;
        LinkedList<children> _children;
        mutli_callback _multiCb;
        pull_task _pullTask;
        children _agentMng;
        async_timer _timer;
        object _selfValue;
        string _name;
        long _lastTm;
        long _yieldCount;
        long _id;
        int _lockCount;
        int _lockSuspendCount;
        bool _beginQuit;
        bool _isSuspend;
        bool _holdSuspend;
        bool _hasBlock;
        bool _isForce;
        bool _isExcep;
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
        static void up_stack_frame(LinkedList<call_stack_info> callStack, int offset = 0, int count = 1)
        {
            offset += 2;
            StackFrame[] sts = (new StackTrace(true)).GetFrames();
            string time = string.Format("{0:D2}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}.{6:D3}",
                DateTime.Now.Year % 100, DateTime.Now.Month, DateTime.Now.Day,
                DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond);
            for (int i = 0; i < count; ++i, ++offset)
            {
                if (offset < sts.Length)
                {
                    callStack.AddLast(new call_stack_info(time, sts[offset].GetFileName(), sts[offset].GetFileLineNumber()));
                }
                else
                {
                    callStack.AddLast(new call_stack_info(time, "null", -1));
                }
            }
        }
#endif

#if DEBUG
        generator init(shared_strand strand, action handler, Action callback, Action<bool> suspendCb, LinkedList<call_stack_info> makeStack = null)
        {
            if (null != makeStack)
            {
                _makeStack = makeStack;
            }
            else
            {
                _makeStack = new LinkedList<call_stack_info>();
                up_stack_frame(_makeStack, 1, 6);
            }
            _beginStepTick = system_tick.get_tick_ms();
#else
        generator init(shared_strand strand, action handler, Action callback, Action<bool> suspendCb)
        {
#endif
            _id = Interlocked.Increment(ref _idCount);
            _isForce = false;
            _isExcep = false;
            _isStop = false;
            _isRun = false;
            _isSuspend = false;
            _holdSuspend = false;
            _hasBlock = false;
            _beginQuit = false;
            _lockCount = -1;
            _lockSuspendCount = 0;
            _lastTm = 0;
            _yieldCount = 0;
            _suspendCb = suspendCb;
            _pullTask = new pull_task();
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
                    MessageBox.Show(String.Format("Message:\n{0}\n{1}", ec.Message, ec.StackTrace), "generator 内部未捕获的异常!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _isExcep = true;
                }
                finally
                {
                    if (_isForce || _isExcep)
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

        mutli_callback new_multi_task()
        {
            if (null == _multiCb)
            {
                _multiCb = new mutli_callback();
                _pullTask.new_task();
            }
            return _multiCb;
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
            return _isExcep;
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
            return nil_wait();
        }

        static public Task halt_self()
        {
            generator this_ = self;
            this_.stop();
            return nil_wait();
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
            return nil_wait();
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
            return nil_wait();
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

        public async Task async_wait()
        {
#if DEBUG
            Trace.Assert(strand.running_in_this_thread(), "异常的 await 调用!");
            if (!system_tick.check_step_debugging() && system_tick.get_tick_ms() - _beginStepTick > _stepMaxCycle)
            {
                LinkedListNode<call_stack_info> it;
                Debug.WriteLine(string.Format("单步超时:\n{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n",
                    (it = _makeStack.First).Value, (it = it.Next).Value, (it = it.Next).Value, (it = it.Next).Value, (it = it.Next).Value, it.Value, new StackTrace(true)));
            }
            await _pullTask;
            _beginStepTick = system_tick.get_tick_ms();
#else
            await _pullTask;
#endif
            _multiCb = null;
            _lastTm = 0;
            _yieldCount++;
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

        public Task async_wait(Action handler)
        {
            handler();
            return async_wait();
        }

        public async Task<R> wait_result<R>(Action<async_result_wrap<R>> handler)
        {
            async_result_wrap<R> res = new async_result_wrap<R>();
            handler(res);
            await async_wait();
            return res.value1;
        }

        public SameAction async_same_callback()
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return delegate (object[] args)
            {
                strand.distribute(beginQuit ? (Action)quit_next : no_quit_next);
            };
        }

        public SameAction async_same_callback(SameAction handler)
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return delegate (object[] args)
            {
                handler(args);
                strand.distribute(beginQuit ? (Action)quit_next : no_quit_next);
            };
        }

        public SameAction timed_async_same_callback(int ms, Action timedHandler = null)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                if (null != timedHandler)
                {
                    timedHandler();
                }
                else if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate (object[] args)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        no_check_next();
                    }
                });
            };
        }

        public SameAction timed_async_same_callback2(int ms, Action timedHandler = null)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                functional.catch_invoke(timedHandler);
                if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate (object[] args)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        no_check_next();
                    }
                });
            };
        }

        public SameAction timed_async_same_callback(int ms, SameAction handler, Action timedHandler = null)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                if (null != timedHandler)
                {
                    timedHandler();
                }
                else if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate (object[] args)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        handler(args);
                        no_check_next();
                    }
                });
            };
        }

        public SameAction timed_async_same_callback2(int ms, SameAction handler, Action timedHandler = null)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                functional.catch_invoke(timedHandler);
                if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate (object[] args)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        handler(args);
                        no_check_next();
                    }
                });
            };
        }

        public Action async_callback(Action handler)
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return delegate ()
            {
                handler();
                strand.distribute(beginQuit ? (Action)quit_next : no_quit_next);
            };
        }

        public Action<T1> async_callback<T1>(Action<T1> handler)
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1)
            {
                handler(p1);
                strand.distribute(beginQuit ? (Action)quit_next : no_quit_next);
            };
        }

        public Action<T1, T2> async_callback<T1, T2>(Action<T1, T2> handler)
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2)
            {
                handler(p1, p2);
                strand.distribute(beginQuit ? (Action)quit_next : no_quit_next);
            };
        }

        public Action<T1, T2, T3> async_callback<T1, T2, T3>(Action<T1, T2, T3> handler)
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                handler(p1, p2, p3);
                strand.distribute(beginQuit ? (Action)quit_next : no_quit_next);
            };
        }

        public Action timed_async_callback(int ms, Action handler, Action timedHandler = null)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                if (null != timedHandler)
                {
                    timedHandler();
                }
                else if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate ()
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        handler();
                        no_check_next();
                    }
                });
            };
        }

        public Action timed_async_callback2(int ms, Action handler, Action timedHandler = null)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                functional.catch_invoke(timedHandler);
                if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate ()
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        handler();
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1> timed_async_callback<T1>(int ms, Action<T1> handler, Action timedHandler = null)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                if (null != timedHandler)
                {
                    timedHandler();
                }
                else if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate (T1 p1)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        handler(p1);
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1> timed_async_callback2<T1>(int ms, Action<T1> handler, Action timedHandler = null)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                functional.catch_invoke(timedHandler);
                if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate (T1 p1)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        handler(p1);
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1, T2> timed_async_callback<T1, T2>(int ms, Action<T1, T2> handler, Action timedHandler = null)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                if (null != timedHandler)
                {
                    timedHandler();
                }
                else if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate (T1 p1, T2 p2)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        handler(p1, p2);
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1, T2> timed_async_callback2<T1, T2>(int ms, Action<T1, T2> handler, Action timedHandler = null)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                functional.catch_invoke(timedHandler);
                if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate (T1 p1, T2 p2)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        handler(p1, p2);
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1, T2, T3> timed_async_callback<T1, T2, T3>(int ms, Action<T1, T2, T3> handler, Action timedHandler = null)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                if (null != timedHandler)
                {
                    timedHandler();
                }
                else if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        handler(p1, p2, p3);
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1, T2, T3> timed_async_callback2<T1, T2, T3>(int ms, Action<T1, T2, T3> handler, Action timedHandler = null)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                functional.catch_invoke(timedHandler);
                if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        handler(p1, p2, p3);
                        no_check_next();
                    }
                });
            };
        }

        public SameAction safe_async_same_callback()
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate (object[] args)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check())
                    {
                        next(beginQuit);
                    }
                });
            };
        }

        public SameAction safe_async_same_callback(SameAction handler)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate (object[] args)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        handler(args);
                        no_check_next();
                    }
                });
            };
        }

        public Action safe_async_callback(Action handler)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate ()
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        handler();
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1> safe_async_callback<T1>(Action<T1> handler)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        handler(p1);
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1, T2> safe_async_callback<T1, T2>(Action<T1, T2> handler)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        handler(p1, p2);
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1, T2, T3> safe_async_callback<T1, T2, T3>(Action<T1, T2, T3> handler)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        handler(p1, p2, p3);
                        no_check_next();
                    }
                });
            };
        }

        public Action async_result()
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return () => strand.distribute(beginQuit ? (Action)quit_next : no_quit_next);
        }

        private Action _async_result()
        {
            _pullTask.new_task();
            return _beginQuit ? (Action)quit_next : no_quit_next;
        }

        public Action<T1> async_result<T1>(async_result_wrap<T1> res)
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1)
            {
                res.value1 = p1;
                strand.distribute(beginQuit ? (Action)quit_next : no_quit_next);
            };
        }

        public Action<T1, T2> async_result<T1, T2>(async_result_wrap<T1, T2> res)
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2)
            {
                res.value1 = p1;
                res.value2 = p2;
                strand.distribute(beginQuit ? (Action)quit_next : no_quit_next);
            };
        }

        public Action<T1, T2, T3> async_result<T1, T2, T3>(async_result_wrap<T1, T2, T3> res)
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                res.value1 = p1;
                res.value2 = p2;
                res.value3 = p3;
                strand.distribute(beginQuit ? (Action)quit_next : no_quit_next);
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

        public Action safe_async_result()
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate ()
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check())
                    {
                        next(beginQuit);
                    }
                });
            };
        }

        public Action<T1> safe_async_result<T1>(async_result_wrap<T1> res)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        res.value1 = p1;
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1, T2> safe_async_result<T1, T2>(async_result_wrap<T1, T2> res)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        res.value1 = p1;
                        res.value2 = p2;
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1, T2, T3> safe_async_result<T1, T2, T3>(async_result_wrap<T1, T2, T3> res)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        res.value1 = p1;
                        res.value2 = p2;
                        res.value3 = p3;
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1> safe_async_ignore<T1>()
        {
            return safe_async_result(async_result_ignore_wrap<T1>.value);
        }

        public Action<T1, T2> safe_async_ignore<T1, T2>()
        {
            return safe_async_result(async_result_ignore_wrap<T1, T2>.value);
        }

        public Action<T1, T2, T3> safe_async_ignore<T1, T2, T3>()
        {
            return safe_async_result(async_result_ignore_wrap<T1, T2, T3>.value);
        }

        public Action timed_async_result(int ms, Action timedHandler = null)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                if (null != timedHandler)
                {
                    timedHandler();
                }
                else if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate ()
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1> timed_async_result<T1>(int ms, async_result_wrap<T1> res, Action timedHandler = null)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                if (null != timedHandler)
                {
                    timedHandler();
                }
                else if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate (T1 p1)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
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
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                if (null != timedHandler)
                {
                    timedHandler();
                }
                else if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate (T1 p1, T2 p2)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
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
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                if (null != timedHandler)
                {
                    timedHandler();
                }
                else if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
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
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                functional.catch_invoke(timedHandler);
                if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate ()
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        no_check_next();
                    }
                });
            };
        }

        public Action<T1> timed_async_result2<T1>(int ms, async_result_wrap<T1> res, Action timedHandler = null)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                functional.catch_invoke(timedHandler);
                if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate (T1 p1)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
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
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                functional.catch_invoke(timedHandler);
                if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate (T1 p1, T2 p2)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
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
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            _timer.timeout(ms, delegate ()
            {
                functional.catch_invoke(timedHandler);
                if (!multiCb.check())
                {
                    next(beginQuit);
                }
            });
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                if (multiCb.callbacked())
                {
                    return;
                }
                strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
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

        static public Task yield()
        {
            generator this_ = self;
            this_.strand.post(this_._async_result());
            return this_.async_wait();
        }

        static public generator self
        {
            get
            {
                shared_strand currStrand = shared_strand.work_strand();
                return null != currStrand ? currStrand.currSelf : null;
            }
        }

        static public shared_strand self_strand()
        {
            generator this_ = self;
            return null != this_ ? this_.strand : null;
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
            otherGen.suspend(this_.async_result());
            return this_.async_wait();
        }

        static public Task resume_other(generator otherGen)
        {
            generator this_ = self;
            otherGen.resume(this_.async_result());
            return this_.async_wait();
        }

        static public Task chan_clear<T>(channel<T> chan)
        {
            generator this_ = self;
            chan.clear(this_.async_result());
            return this_.async_wait();
        }

        static public Task chan_close<T>(channel<T> chan, bool isClear = false)
        {
            generator this_ = self;
            chan.close(this_.async_result(), isClear);
            return this_.async_wait();
        }

        static public Task chan_cancel<T>(channel<T> chan, bool isClear = false)
        {
            generator this_ = self;
            chan.cancel(this_.async_result(), isClear);
            return this_.async_wait();
        }

        static public async Task<bool> chan_is_closed<T>(channel<T> chan)
        {
            generator this_ = self;
            bool is_closed = chan.is_closed();
            if (!is_closed && chan.self_strand() != this_.strand)
            {
                Action continuation = this_.async_result();
                chan.self_strand().post(delegate ()
                {
                    is_closed = chan.is_closed();
                    continuation();
                });
                await this_.async_wait();
            }
            return is_closed;
        }

        static public async Task<chan_async_state> chan_send<T>(channel<T> chan, T msg)
        {
            generator this_ = self;
            chan_async_state result = chan_async_state.async_undefined;
            chan.push(this_.async_callback((chan_async_state state, object _) => result = state), msg);
            await this_.async_wait();
            return result;
        }

        static public Task<chan_async_state> chan_send(channel<void_type> chan)
        {
            return chan_send(chan, default(void_type));
        }

        static public async Task<chan_async_state> chan_force_send<T>(chan<T> chan, T msg, async_result_wrap<bool, T> outMsg = null)
        {
            generator this_ = self;
            chan_async_state result = chan_async_state.async_undefined;
            chan.force_push(this_.async_callback(delegate (chan_async_state state, bool hasOut, T freeMsg)
            {
                result = state;
                if (null != outMsg)
                {
                    outMsg.value1 = hasOut;
                    outMsg.value2 = freeMsg;
                }
            }), msg);
            await this_.async_wait();
            return result;
        }

        static public Task<chan_recv_wrap<T>> chan_receive<T>(channel<T> chan)
        {
            return chan_receive(chan, broadcast_chan_token._defToken);
        }

        static public async Task<chan_recv_wrap<T>> chan_receive<T>(channel<T> chan, broadcast_chan_token token)
        {
            generator this_ = self;
            chan_recv_wrap<T> result = default(chan_recv_wrap<T>);
            chan.pop(this_.async_callback(delegate (chan_async_state state, T msg, object _)
            {
                result.state = state;
                if (chan_async_state.async_ok == state)
                {
                    result.result = msg;
                }
            }), token);
            await this_.async_wait();
            return result;
        }

        static public async Task<chan_async_state> chan_try_send<T>(channel<T> chan, T msg)
        {
            generator this_ = self;
            chan_async_state result = chan_async_state.async_undefined;
            chan.try_push(this_.async_callback((chan_async_state state, object _) => result = state), msg);
            await this_.async_wait();
            return result;
        }

        static public Task<chan_async_state> chan_try_send(channel<void_type> chan)
        {
            return chan_try_send(chan, default(void_type));
        }

        static public Task<chan_recv_wrap<T>> chan_try_receive<T>(channel<T> chan)
        {
            return chan_try_receive(chan, broadcast_chan_token._defToken);
        }

        static public async Task<chan_recv_wrap<T>> chan_try_receive<T>(channel<T> chan, broadcast_chan_token token)
        {
            generator this_ = self;
            chan_recv_wrap<T> result = default(chan_recv_wrap<T>);
            chan.try_pop(this_.async_callback(delegate (chan_async_state state, T msg, object _)
            {
                result.state = state;
                if (chan_async_state.async_ok == state)
                {
                    result.result = msg;
                }
            }), token);
            await this_.async_wait();
            return result;
        }

        static public async Task<chan_async_state> chan_timed_send<T>(channel<T> chan, int ms, T msg)
        {
            generator this_ = self;
            chan_async_state result = chan_async_state.async_undefined;
            chan.timed_push(ms, this_.async_callback((chan_async_state state, object _) => result = state), msg);
            await this_.async_wait();
            return result;
        }

        static public Task<chan_async_state> chan_timed_send(channel<void_type> chan, int ms)
        {
            return chan_timed_send(chan, ms, default(void_type));
        }

        static public Task<chan_recv_wrap<T>> chan_timed_receive<T>(channel<T> chan, int ms)
        {
            return chan_timed_receive(chan, ms, broadcast_chan_token._defToken);
        }

        static public async Task<chan_recv_wrap<T>> chan_timed_receive<T>(channel<T> chan, int ms, broadcast_chan_token token)
        {
            generator this_ = self;
            chan_recv_wrap<T> result = default(chan_recv_wrap<T>);
            chan.timed_pop(ms, this_.async_callback(delegate (chan_async_state state, T msg, object _)
            {
                result.state = state;
                if (chan_async_state.async_ok == state)
                {
                    result.result = msg;
                }
            }), token);
            await this_.async_wait();
            return result;
        }

        static public async Task<csp_invoke_wrap<R>> csp_invoke<R, T>(csp_chan<R, T> chan, T msg)
        {
            generator this_ = self;
            csp_invoke_wrap<R> result = default(csp_invoke_wrap<R>);
            chan.push(this_.async_callback(delegate (chan_async_state state, object exObj)
            {
                result.state = state;
                if (chan_async_state.async_ok == state)
                {
                    result.result = (R)exObj;
                }
            }), msg);
            await this_.async_wait();
            return result;
        }

        static public Task<csp_invoke_wrap<R>> csp_invoke<R>(csp_chan<R, void_type> chan)
        {
            return csp_invoke(chan, default(void_type));
        }

        static public async Task<csp_wait_wrap<R, T>> csp_wait<R, T>(csp_chan<R, T> chan)
        {
            generator this_ = self;
            csp_wait_wrap<R, T> result = default(csp_wait_wrap<R, T>);
            chan.pop(this_.async_callback(delegate (chan_async_state state, T msg, object exObj)
            {
                result.state = state;
                if (chan_async_state.async_ok == state)
                {
                    result.msg = msg;
                    result.result = (csp_chan<R, T>.csp_result)exObj;
                }
            }));
            await this_.async_wait();
            return result;
        }

        static public async Task<chan_async_state> csp_wait<R, T>(csp_chan<R, T> chan, Func<T, Task<R>> handler)
        {
            csp_wait_wrap<R, T> result = await csp_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                result.complete(await handler(result.msg));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_wait<R, T1, T2>(csp_chan<R, tuple<T1, T2>> chan, Func<T1, T2, Task<R>> handler)
        {
            csp_wait_wrap<R, tuple<T1, T2>> result = await csp_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                result.complete(await handler(result.msg.value1, result.msg.value2));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_wait<R, T1, T2, T3>(csp_chan<R, tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task<R>> handler)
        {
            csp_wait_wrap<R, tuple<T1, T2, T3>> result = await csp_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                result.complete(await handler(result.msg.value1, result.msg.value2, result.msg.value3));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_wait<R>(csp_chan<R, void_type> chan, Func<Task<R>> handler)
        {
            csp_wait_wrap<R, void_type> result = await csp_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                result.complete(await handler());
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_wait<T>(csp_chan<void_type, T> chan, Func<T, Task> handler)
        {
            csp_wait_wrap<void_type, T> result = await csp_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                await handler(result.msg);
                result.complete(default(void_type));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_wait<T1, T2>(csp_chan<void_type, tuple<T1, T2>> chan, Func<T1, T2, Task> handler)
        {
            csp_wait_wrap<void_type, tuple<T1, T2>> result = await csp_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                await handler(result.msg.value1, result.msg.value2);
                result.complete(default(void_type));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_wait<T1, T2, T3>(csp_chan<void_type, tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler)
        {
            csp_wait_wrap<void_type, tuple<T1, T2, T3>> result = await csp_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                await handler(result.msg.value1, result.msg.value2, result.msg.value3);
                result.complete(default(void_type));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_wait(csp_chan<void_type, void_type> chan, Func<Task> handler)
        {
            csp_wait_wrap<void_type, void_type> result = await csp_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                await handler();
                result.complete(default(void_type));
            }
            return result.state;
        }

        static public async Task<csp_invoke_wrap<R>> csp_try_invoke<R, T>(csp_chan<R, T> chan, T msg)
        {
            generator this_ = self;
            csp_invoke_wrap<R> result = default(csp_invoke_wrap<R>);
            chan.try_push(this_.async_callback(delegate (chan_async_state state, object exObj)
            {
                result.state = state;
                if (chan_async_state.async_ok == state)
                {
                    result.result = (R)exObj;
                }
            }), msg);
            await this_.async_wait();
            return result;
        }

        static public Task<csp_invoke_wrap<R>> csp_try_invoke<R>(csp_chan<R, void_type> chan)
        {
            return csp_try_invoke(chan, default(void_type));
        }

        static public async Task<csp_wait_wrap<R, T>> csp_try_wait<R, T>(csp_chan<R, T> chan)
        {
            generator this_ = self;
            csp_wait_wrap<R, T> result = default(csp_wait_wrap<R, T>);
            chan.try_pop(this_.async_callback(delegate (chan_async_state state, T msg, object exObj)
            {
                result.state = state;
                if (chan_async_state.async_ok == state)
                {
                    result.msg = msg;
                    result.result = (csp_chan<R, T>.csp_result)exObj;
                }
            }));
            await this_.async_wait();
            return result;
        }

        static public async Task<chan_async_state> csp_try_wait<R, T>(csp_chan<R, T> chan, Func<T, Task<R>> handler)
        {
            csp_wait_wrap<R, T> result = await csp_try_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                result.complete(await handler(result.msg));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_try_wait<R, T1, T2>(csp_chan<R, tuple<T1, T2>> chan, Func<T1, T2, Task<R>> handler)
        {
            csp_wait_wrap<R, tuple<T1, T2>> result = await csp_try_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                result.complete(await handler(result.msg.value1, result.msg.value2));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_try_wait<R, T1, T2, T3>(csp_chan<R, tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task<R>> handler)
        {
            csp_wait_wrap<R, tuple<T1, T2, T3>> result = await csp_try_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                result.complete(await handler(result.msg.value1, result.msg.value2, result.msg.value3));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_try_wait<R>(csp_chan<R, void_type> chan, Func<Task> handler)
        {
            csp_wait_wrap<R, void_type> result = await csp_try_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                await handler();
                result.complete(default(R));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_try_wait<T>(csp_chan<void_type, T> chan, Func<T, Task> handler)
        {
            csp_wait_wrap<void_type, T> result = await csp_try_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                await handler(result.msg);
                result.complete(default(void_type));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_try_wait(csp_chan<void_type, void_type> chan, Func<Task> handler)
        {
            csp_wait_wrap<void_type, void_type> result = await csp_try_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                await handler();
                result.complete(default(void_type));
            }
            return result.state;
        }

        static public async Task<csp_invoke_wrap<R>> csp_timed_invoke<R, T>(csp_chan<R, T> chan, int ms, T msg)
        {
            generator this_ = self;
            csp_invoke_wrap<R> result = default(csp_invoke_wrap<R>);
            chan.timed_push(ms, this_.async_callback(delegate (chan_async_state state, object exObj)
            {
                result.state = state;
                if (chan_async_state.async_ok == state)
                {
                    result.result = (R)exObj;
                }
            }), msg);
            await this_.async_wait();
            return result;
        }

        static public Task<csp_invoke_wrap<R>> csp_timed_invoke<R>(csp_chan<R, void_type> chan, int ms)
        {
            return csp_timed_invoke(chan, ms, default(void_type));
        }

        static public async Task<csp_wait_wrap<R, T>> csp_timed_wait<R, T>(csp_chan<R, T> chan, int ms)
        {
            generator this_ = self;
            csp_wait_wrap<R, T> result = default(csp_wait_wrap<R, T>);
            chan.timed_pop(ms, this_.async_callback(delegate (chan_async_state state, T msg, object exObj)
            {
                result.state = state;
                if (chan_async_state.async_ok == state)
                {
                    result.msg = msg;
                    result.result = (csp_chan<R, T>.csp_result)exObj;
                }
            }));
            await this_.async_wait();
            return result;
        }

        static public async Task<chan_async_state> csp_timed_wait<R, T>(csp_chan<R, T> chan, int ms, Func<T, Task<R>> handler)
        {
            csp_wait_wrap<R, T> result = await csp_timed_wait(chan, ms);
            if (chan_async_state.async_ok == result.state)
            {
                result.complete(await handler(result.msg));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_timed_wait<R, T1, T2>(csp_chan<R, tuple<T1, T2>> chan, int ms, Func<T1, T2, Task<R>> handler)
        {
            csp_wait_wrap<R, tuple<T1, T2>> result = await csp_timed_wait(chan, ms);
            if (chan_async_state.async_ok == result.state)
            {
                result.complete(await handler(result.msg.value1, result.msg.value2));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_timed_wait<R, T1, T2, T3>(csp_chan<R, tuple<T1, T2, T3>> chan, int ms, Func<T1, T2, T3, Task<R>> handler)
        {
            csp_wait_wrap<R, tuple<T1, T2, T3>> result = await csp_timed_wait(chan, ms);
            if (chan_async_state.async_ok == result.state)
            {
                result.complete(await handler(result.msg.value1, result.msg.value2, result.msg.value3));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_timed_wait<R>(csp_chan<R, void_type> chan, int ms, Func<Task<R>> handler)
        {
            csp_wait_wrap<R, void_type> result = await csp_timed_wait(chan, ms);
            if (chan_async_state.async_ok == result.state)
            {
                result.complete(await handler());
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_timed_wait<T>(csp_chan<void_type, T> chan, int ms, Func<T, Task> handler)
        {
            csp_wait_wrap<void_type, T> result = await csp_timed_wait(chan, ms);
            if (chan_async_state.async_ok == result.state)
            {
                await handler(result.msg);
                result.complete(default(void_type));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_timed_wait<T1, T2>(csp_chan<void_type, tuple<T1, T2>> chan, int ms, Func<T1, T2, Task> handler)
        {
            csp_wait_wrap<void_type, tuple<T1, T2>> result = await csp_timed_wait(chan, ms);
            if (chan_async_state.async_ok == result.state)
            {
                await handler(result.msg.value1, result.msg.value2);
                result.complete(default(void_type));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_timed_wait<T1, T2, T3>(csp_chan<void_type, tuple<T1, T2, T3>> chan, int ms, Func<T1, T2, T3, Task> handler)
        {
            csp_wait_wrap<void_type, tuple<T1, T2, T3>> result = await csp_timed_wait(chan, ms);
            if (chan_async_state.async_ok == result.state)
            {
                await handler(result.msg.value1, result.msg.value2, result.msg.value3);
                result.complete(default(void_type));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_timed_wait(csp_chan<void_type, void_type> chan, int ms, Func<Task> handler)
        {
            csp_wait_wrap<void_type, void_type> result = await csp_timed_wait(chan, ms);
            if (chan_async_state.async_ok == result.state)
            {
                await handler();
                result.complete(default(void_type));
            }
            return result.state;
        }

        static public async Task<int> chans_broadcast<T>(T msg, params channel<T>[] chans)
        {
            generator this_ = self;
            int count = 0;
            wait_group wg = new wait_group(chans.Length);
            for (int i = 0; i < chans.Length; i++)
            {
                chans[i].push(delegate (chan_async_state state, object _)
                {
                    if (chan_async_state.async_ok == state)
                    {
                        Interlocked.Increment(ref count);
                    }
                    wg.done();
                }, msg);
            }
            wg.async_wait(this_.async_result());
            await this_.async_wait();
            return count;
        }

        static public async Task<int> chans_try_broadcast<T>(T msg, params channel<T>[] chans)
        {
            generator this_ = self;
            int count = 0;
            wait_group wg = new wait_group(chans.Length);
            for (int i = 0; i < chans.Length; i++)
            {
                chans[i].try_push(delegate (chan_async_state state, object _)
                {
                    if (chan_async_state.async_ok == state)
                    {
                        Interlocked.Increment(ref count);
                    }
                    wg.done();
                }, msg);
            }
            wg.async_wait(this_.async_result());
            await this_.async_wait();
            return count;
        }

        static public async Task<int> chans_timed_broadcast<T>(int ms, T msg, params channel<T>[] chans)
        {
            generator this_ = self;
            int count = 0;
            wait_group wg = new wait_group(chans.Length);
            for (int i = 0; i < chans.Length; i++)
            {
                chans[i].timed_push(ms, delegate (chan_async_state state, object _)
                {
                    if (chan_async_state.async_ok == state)
                    {
                        Interlocked.Increment(ref count);
                    }
                    wg.done();
                }, msg);
            }
            wg.async_wait(this_.async_result());
            await this_.async_wait();
            return count;
        }

        static public void check_chan(chan_async_state state, object obj = null)
        {
            if (chan_async_state.async_ok != state)
            {
                throw new chan_exception(state, obj);
            }
        }

        static public T check_chan<T>(chan_recv_wrap<T> wrap, object obj = null)
        {
            if (chan_async_state.async_ok != wrap.state)
            {
                throw new chan_exception(wrap.state, obj);
            }
            return wrap.result;
        }

        static public T check_chan<T>(csp_invoke_wrap<T> wrap, object obj = null)
        {
            if (chan_async_state.async_ok != wrap.state)
            {
                throw new chan_exception(wrap.state, obj);
            }
            return wrap.result;
        }

        static public Tuple<csp_chan<R, T>.csp_result, T> check_chan<R, T>(csp_wait_wrap<R, T> wrap, object obj = null)
        {
            if (chan_async_state.async_ok != wrap.state)
            {
                throw new chan_exception(wrap.state, obj);
            }
            return new Tuple<csp_chan<R, T>.csp_result, T>(wrap.result, wrap.msg);
        }

        static public Task nil_wait()
        {
            return _nilTask;
        }

        static public Task mutex_cancel(mutex mtx)
        {
            generator this_ = self;
            mtx.cancel(this_._id, this_.async_result());
            return this_.async_wait();
        }

        static public Task mutex_lock(mutex mtx)
        {
            generator this_ = self;
            mtx.Lock(this_._id, this_.async_result());
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

        static public async Task<bool> mutex_try_lock(mutex mtx)
        {
            generator this_ = self;
            async_result_wrap<chan_async_state> res = new async_result_wrap<chan_async_state>();
            mtx.try_lock(this_._id, this_.async_result(res));
            await this_.async_wait();
            return chan_async_state.async_ok == res.value1;
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

        static public async Task<bool> mutex_timed_lock(mutex mtx, int ms)
        {
            generator this_ = self;
            async_result_wrap<chan_async_state> res = new async_result_wrap<chan_async_state>();
            mtx.timed_lock(this_._id, ms, this_.async_result(res));
            await this_.async_wait();
            return chan_async_state.async_ok == res.value1;
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
            mtx.unlock(this_._id, this_.async_result());
            return this_.async_wait();
        }

        static public Task mutex_lock_shared(shared_mutex mtx)
        {
            generator this_ = self;
            mtx.lock_shared(this_._id, this_.async_result());
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
            mtx.lock_pess_shared(this_._id, this_.async_result());
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
            mtx.lock_upgrade(this_._id, this_.async_result());
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

        static public async Task<bool> mutex_try_lock_shared(shared_mutex mtx)
        {
            generator this_ = self;
            async_result_wrap<chan_async_state> res = new async_result_wrap<chan_async_state>();
            mtx.try_lock_shared(this_._id, this_.async_result(res));
            await this_.async_wait();
            return chan_async_state.async_ok == res.value1;
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

        static public async Task<bool> mutex_try_lock_upgrade(shared_mutex mtx)
        {
            generator this_ = self;
            async_result_wrap<chan_async_state> res = new async_result_wrap<chan_async_state>();
            mtx.try_lock_upgrade(this_._id, this_.async_result(res));
            await this_.async_wait();
            return chan_async_state.async_ok == res.value1;
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

        static public async Task<bool> mutex_timed_lock_shared(shared_mutex mtx, int ms)
        {
            generator this_ = self;
            async_result_wrap<chan_async_state> res = new async_result_wrap<chan_async_state>();
            mtx.timed_lock_shared(this_._id, ms, this_.async_result(res));
            await this_.async_wait();
            return chan_async_state.async_ok == res.value1;
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
            mtx.unlock_shared(this_._id, this_.async_result());
            return this_.async_wait();
        }

        static public Task mutex_unlock_upgrade(shared_mutex mtx)
        {
            generator this_ = self;
            mtx.unlock_upgrade(this_._id, this_.async_result());
            return this_.async_wait();
        }

        static public Task condition_wait(condition_variable conVar, mutex mutex)
        {
            generator this_ = self;
            conVar.wait(this_._id, mutex, this_.async_result());
            return this_.async_wait();
        }

        static public async Task<bool> condition_timed_wait(condition_variable conVar, mutex mutex, int ms)
        {
            generator this_ = self;
            async_result_wrap<bool> res = new async_result_wrap<bool>();
            conVar.timed_wait(this_._id, ms, mutex, this_.async_result(res));
            await this_.async_wait();
            return res.value1;
        }

        static public Task condition_cancel(condition_variable conVar)
        {
            generator this_ = self;
            conVar.cancel(this_.id, this_.async_result());
            return this_.async_wait();
        }

        static public async Task send_strand(shared_strand strand, Action handler)
        {
            generator this_ = self;
            if (this_.strand == strand)
            {
                handler();
            }
            else
            {
                System.Exception hasExcep = null;
                strand.post(this_.async_callback(delegate ()
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
        }

        static public async Task<R> send_strand<R>(shared_strand strand, Func<R> handler)
        {
            generator this_ = self;
            if (this_.strand == strand)
            {
                return handler();
            }
            else
            {
                R res = default(R);
                System.Exception hasExcep = null;
                strand.post(this_.async_callback(delegate ()
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
        }

        static public Func<Task> wrap_send_strand(shared_strand strand, Action handler)
        {
            return () => send_strand(strand, handler);
        }

        static public Func<T, Task> wrap_send_strand<T>(shared_strand strand, Action<T> handler)
        {
            return (T p) => send_strand(strand, () => handler(p));
        }

        static public Func<Task<R>> wrap_send_strand<R>(shared_strand strand, Func<R> handler)
        {
            return async delegate ()
            {
                R res = default(R);
                await send_strand(strand, () => res = handler());
                return res;
            };
        }

        static public Func<T, Task<R>> wrap_send_strand<R, T>(shared_strand strand, Func<T, R> handler)
        {
            return async delegate (T p)
            {
                R res = default(R);
                await send_strand(strand, () => res = handler(p));
                return res;
            };
        }

        static public void post_control(Control ctrl, Action handler)
        {
            try
            {
                ctrl.BeginInvoke((MethodInvoker)delegate ()
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

        static public async Task send_control(Control ctrl, Action handler)
        {
            if (!ctrl.InvokeRequired)
            {
                handler();
                return;
            }
            generator this_ = self;
            System.Exception hasExcep = null;
            post_control(ctrl, this_.async_callback(delegate ()
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

        static public async Task<R> send_control<R>(Control ctrl, Func<R> handler)
        {
            if (!ctrl.InvokeRequired)
            {
                return handler();
            }
            generator this_ = self;
            R res = default(R);
            System.Exception hasExcep = null;
            post_control(ctrl, this_.async_callback(delegate ()
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

        static public Action wrap_post_control(Control ctrl, Action handler)
        {
            return () => post_control(ctrl, handler);
        }

        static public Func<Task> wrap_send_control(Control ctrl, Action handler)
        {
            return () => send_control(ctrl, handler);
        }

        static public Func<T, Task> wrap_send_control<T>(Control ctrl, Action<T> handler)
        {
            return (T p) => send_control(ctrl, () => handler(p));
        }

        static public Func<Task<R>> wrap_send_control<R>(Control ctrl, Func<R> handler)
        {
            return async delegate ()
            {
                R res = default(R);
                await send_control(ctrl, () => res = handler());
                return res;
            };
        }

        static public Func<T, Task<R>> wrap_send_control<R, T>(Control ctrl, Func<T, R> handler)
        {
            return async delegate (T p)
            {
                R res = default(R);
                await send_control(ctrl, () => res = handler(p));
                return res;
            };
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
            }, this_.async_result());
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
            }, this_.async_result());
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
                task.GetAwaiter().OnCompleted(this_.async_result());
                return this_.async_wait();
            }
            return nil_wait();
        }

        static public async Task<R> wait_task<R>(Task<R> task)
        {
            if (!task.IsCompleted)
            {
                generator this_ = self;
                task.GetAwaiter().OnCompleted(this_.async_result());
                await this_.async_wait();
            }
            return task.Result;
        }

        static public async Task<bool> timed_wait_task(int ms, Task task)
        {
            if (!task.IsCompleted)
            {
                bool overtime = false;
                generator this_ = self;
                task.GetAwaiter().OnCompleted(this_.timed_async_result2(ms, () => overtime = true));
                await this_.async_wait();
                return !overtime;
            }
            return true;
        }

        static public async Task<tuple<bool, R>> timed_wait_task<R>(int ms, Task<R> task)
        {
            if (!task.IsCompleted)
            {
                bool overtime = false;
                generator this_ = self;
                task.GetAwaiter().OnCompleted(this_.timed_async_result2(ms, () => overtime = true));
                await this_.async_wait();
                return tuple.make(!overtime, overtime ? default(R) : task.Result);
            }
            return tuple.make(true, task.Result);
        }

        static public Task stop_other(generator otherGen)
        {
            generator this_ = self;
            otherGen.stop(this_.async_result());
            return this_.async_wait();
        }

        static public Task wait_other(generator otherGen)
        {
            generator this_ = self;
            otherGen.append_stop_callback(this_.async_result());
            return this_.async_wait();
        }

        static public async Task<bool> timed_wait_other(int ms, generator otherGen)
        {
            generator this_ = self;
            bool overtime = false;
            nil_chan<LinkedListNode<Action>> waitRemove = new nil_chan<LinkedListNode<Action>>();
            otherGen.append_stop_callback(this_.timed_async_result2(ms, () => overtime = true), waitRemove.wrap());
            await this_.async_wait();
            if (overtime)
            {
                LinkedListNode<Action> node = await chan_receive(waitRemove);
                if (null != node)
                {
                    otherGen.remove_stop_callback(node);
                }
            }
            return !overtime;
        }

        static public Task wait_group(wait_group wg)
        {
            generator this_ = self;
            wg.async_wait(this_.async_result());
            return this_.async_wait();
        }

        static public async Task<bool> timed_wait_group(int ms, wait_group wg)
        {
            generator this_ = self;
            bool overtime = false;
            wg.async_wait(this_.timed_async_result2(ms, () => overtime = true));
            await this_.async_wait();
            return !overtime;
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

        static public async Task<async_result_wrap<R1, R2>> async_call<R1, R2>(Action<Action<R1, R2>> handler)
        {
            generator this_ = self;
            async_result_wrap<R1, R2> res = new async_result_wrap<R1, R2>();
            handler(this_.async_result(res));
            await this_.async_wait();
            return res;
        }

        static public async Task<async_result_wrap<R1, R2, R3>> async_call<R1, R2, R3>(Action<Action<R1, R2, R3>> handler)
        {
            generator this_ = self;
            async_result_wrap<R1, R2, R3> res = new async_result_wrap<R1, R2, R3>();
            handler(this_.async_result(res));
            await this_.async_wait();
            return res;
        }

        static public Task safe_async_call(Action<Action> handler)
        {
            generator this_ = self;
            handler(this_.safe_async_result());
            return this_.async_wait();
        }

        static public async Task<R> safe_async_call<R>(Action<Action<R>> handler)
        {
            generator this_ = self;
            async_result_wrap<R> res = new async_result_wrap<R>();
            handler(this_.safe_async_result(res));
            await this_.async_wait();
            return res.value1;
        }

        static public async Task<async_result_wrap<R1, R2>> safe_async_call<R1, R2>(Action<Action<R1, R2>> handler)
        {
            generator this_ = self;
            async_result_wrap<R1, R2> res = new async_result_wrap<R1, R2>();
            handler(this_.safe_async_result(res));
            await this_.async_wait();
            return res;
        }

        static public async Task<async_result_wrap<R1, R2, R3>> safe_async_call<R1, R2, R3>(Action<Action<R1, R2, R3>> handler)
        {
            generator this_ = self;
            async_result_wrap<R1, R2, R3> res = new async_result_wrap<R1, R2, R3>();
            handler(this_.safe_async_result(res));
            await this_.async_wait();
            return res;
        }

        static public async Task<bool> timed_async_call(int ms, Action<Action> handler, Action timedHandler = null)
        {
            generator this_ = self;
            bool overtime = false;
            if (null == timedHandler)
            {
                handler(this_.timed_async_result2(ms, () => overtime = true));
            }
            else
            {
                handler(this_.timed_async_result(ms, delegate ()
                {
                    overtime = true;
                    timedHandler();
                }));
            }
            await this_.async_wait();
            return !overtime;
        }

        static public async Task<bool> timed_async_call<R>(int ms, async_result_wrap<R> res, Action<Action<R>> handler, Action timedHandler = null)
        {
            generator this_ = self;
            bool overtime = false;
            if (null == timedHandler)
            {
                handler(this_.timed_async_result2(ms, res, () => overtime = true));
            }
            else
            {
                handler(this_.timed_async_result(ms, res, delegate ()
                {
                    overtime = true;
                    timedHandler();
                }));
            }
            await this_.async_wait();
            return !overtime;
        }

        static public async Task<bool> timed_async_call<R1, R2>(int ms, async_result_wrap<R1, R2> res, Action<Action<R1, R2>> handler, Action timedHandler = null)
        {
            generator this_ = self;
            bool overtime = false;
            if (null == timedHandler)
            {
                handler(this_.timed_async_result2(ms, res, () => overtime = true));
            }
            else
            {
                handler(this_.timed_async_result(ms, res, delegate ()
                {
                    overtime = true;
                    timedHandler();
                }));
            }
            await this_.async_wait();
            return !overtime;
        }

        static public async Task<bool> timed_async_call<R1, R2, R3>(int ms, async_result_wrap<R1, R2, R3> res, Action<Action<R1, R2, R3>> handler, Action timedHandler = null)
        {
            generator this_ = self;
            bool overtime = false;
            if (null == timedHandler)
            {
                handler(this_.timed_async_result2(ms, res, () => overtime = true));
            }
            else
            {
                handler(this_.timed_async_result(ms, res, delegate ()
                {
                    overtime = true;
                    timedHandler();
                }));
            }
            await this_.async_wait();
            return !overtime;
        }
#if DEBUG
        static public async Task call(action handler)
        {
            generator this_ = self;
            up_stack_frame(this_._makeStack, 2);
            await handler();
            this_._makeStack.RemoveFirst();
        }

        static public async Task<R> call<R>(Func<Task<R>> handler)
        {
            generator this_ = self;
            up_stack_frame(this_._makeStack, 2);
            R res = await handler();
            this_._makeStack.RemoveFirst();
            return res;
        }

        static public async Task depth_call(shared_strand strand, action handler)
        {
            generator this_ = self;
            up_stack_frame(this_._makeStack, 2);
            (new generator()).init(strand, handler, this_.async_result(), null, this_._makeStack).run();
            await lock_stop(() => this_.async_wait());
            this_._makeStack.RemoveFirst();
        }

        static public async Task<R> depth_call<R>(shared_strand strand, Func<Task<R>> handler)
        {
            generator this_ = self;
            R res = default(R);
            up_stack_frame(this_._makeStack, 2);
            (new generator()).init(strand, async () => res = await handler(), this_.async_result(), null, this_._makeStack).run();
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
            go(strand, handler, this_.async_result());
            return lock_stop(() => this_.async_wait());
        }

        static public async Task<R> depth_call<R>(shared_strand strand, Func<Task<R>> handler)
        {
            generator this_ = self;
            R res = default(R);
            go(strand, async () => res = await handler(), this_.async_result(), null);
            await lock_stop(() => this_.async_wait());
            return res;
        }
#endif

#if DEBUG
        static public LinkedList<call_stack_info> call_stack
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

        static public channel<T> self_mailbox<T>(int id = 0)
        {
            generator this_ = self;
            if (null == this_._mailboxMap)
            {
                this_._mailboxMap = new Dictionary<long, mail_pck>();
            }
            mail_pck mb = null;
            if (!this_._mailboxMap.TryGetValue(calc_hash<T>(id), out mb))
            {
                mb = new mail_pck(new msg_buff<T>(this_.strand));
                this_._mailboxMap.Add(calc_hash<T>(id), mb);
            }
            return (channel<T>)mb.mailbox;
        }

        public Task<channel<T>> get_mailbox<T>(int id = 0)
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
                    mb = new mail_pck(new msg_buff<T>(strand));
                    _mailboxMap.Add(calc_hash<T>(id), mb);
                }
                return (channel<T>)mb.mailbox;
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
                mb = new mail_pck(new msg_buff<T>(this_.strand));
                this_._mailboxMap.Add(calc_hash<T>(id), mb);
            }
            else if (null != mb.agentAction)
            {
                await this_._agentMng.stop(mb.agentAction);
                mb.agentAction = null;
            }
            channel<T> agentMb = await agentGen.get_mailbox<T>();
            if (null == agentMb)
            {
                return false;
            }
            mb.agentAction = this_._agentMng.make(async delegate ()
            {
                channel<T> selfMb = (channel<T>)mb.mailbox;
                chan_notify_sign ntfSign = new chan_notify_sign();
                generator self = generator.self;
                try
                {
                    nil_chan<chan_async_state> waitHasChan = new nil_chan<chan_async_state>();
                    Action<chan_async_state> waitHasNtf = waitHasChan.wrap();
                    chan_recv_wrap<T> recvRes = default(chan_recv_wrap<T>);
                    Action<chan_async_state, T, object> tryPopHandler = delegate (chan_async_state state, T msg, object _)
                    {
                        recvRes.state = state;
                        if (chan_async_state.async_ok == state)
                        {
                            recvRes.result = msg;
                        }
                    };
                    selfMb.append_pop_notify(waitHasNtf, ntfSign);
                    while (true)
                    {
                        await chan_receive(waitHasChan);
                        try
                        {
                            lock_suspend_and_stop();
                            recvRes = default(chan_recv_wrap<T>);
                            selfMb.try_pop_and_append_notify(self.async_callback(tryPopHandler), waitHasNtf, ntfSign);
                            await self.async_wait();
                            if (chan_async_state.async_ok == recvRes.state)
                            {
                                recvRes.state = await chan_send(agentMb, recvRes.result);
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
                    selfMb.remove_pop_notify(self.async_ignore<chan_async_state>(), ntfSign);
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

        static public Task<chan_recv_wrap<T>> recv_msg<T>(int id = 0)
        {
            return chan_receive(self_mailbox<T>(id));
        }

        static public Task<chan_recv_wrap<T>> try_recv_msg<T>(int id = 0)
        {
            return chan_try_receive(self_mailbox<T>(id));
        }

        static public Task<chan_recv_wrap<T>> timed_recv_msg<T>(int ms, int id = 0)
        {
            return chan_timed_receive(self_mailbox<T>(id), ms);
        }

        public async Task<chan_async_state> send_msg<T>(int id, T msg)
        {
            channel<T> mb = await get_mailbox<T>(id);
            return null != mb ? await chan_send(mb, msg) : chan_async_state.async_fail;
        }

        public Task<chan_async_state> send_msg<T>(T msg)
        {
            return send_msg(0, msg);
        }

        public Task<chan_async_state> send_void_msg(int id)
        {
            return send_msg(id, default(void_type));
        }

        public Task<chan_async_state> send_void_msg()
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

            public receive_mail case_of(channel<void_type> chan, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(chan, (void_type _) => handler(), errHandler);
            }

            public receive_mail case_of<T>(channel<T> chan, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
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
                                chan_recv_wrap<T> res = await chan_receive(chan);
                                if (chan_async_state.async_ok == res.state)
                                {
                                    await handler(res.result);
                                }
                                else if (null == errHandler || await errHandler(res.state))
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
                            self._mailboxMap = _children.parent()._mailboxMap;
                            nil_chan<chan_async_state> waitHasChan = new nil_chan<chan_async_state>();
                            Action<chan_async_state> waitHasNtf = waitHasChan.wrap();
                            chan_recv_wrap<T> recvRes = default(chan_recv_wrap<T>);
                            Action<chan_async_state, T, object> tryPopHandler = delegate (chan_async_state state, T msg, object _)
                            {
                                recvRes.state = state;
                                if (chan_async_state.async_ok == state)
                                {
                                    recvRes.result = msg;
                                }
                            };
                            chan.append_pop_notify(waitHasNtf, ntfSign);
                            while (_run)
                            {
                                await chan_receive(waitHasChan);
                                await mutex_lock_shared(_mutex);
                                try
                                {
                                    recvRes = default(chan_recv_wrap<T>);
                                    chan.try_pop_and_append_notify(self.async_callback(tryPopHandler), waitHasNtf, ntfSign);
                                    await self.async_wait();
                                    if (chan_async_state.async_ok == recvRes.state)
                                    {
                                        await handler(recvRes.result);
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
                            self._mailboxMap = null;
                            lock_suspend_and_stop();
                            chan.remove_pop_notify(self.async_ignore<chan_async_state>(), ntfSign);
                            await self.async_wait();
                            await unlock_suspend_and_stop();
                        }
                    }
                });
                return this;
            }

            public receive_mail case_of<T1, T2>(channel<tuple<T1, T2>> chan, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(chan, (tuple<T1, T2> msg) => handler(msg.value1, msg.value2), errHandler);
            }

            public receive_mail case_of<T1, T2, T3>(channel<tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(chan, (tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), errHandler);
            }

            public receive_mail timed_case_of(channel<void_type> chan, int ms, Func<Task> timedHandler, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(chan, ms, timedHandler, (void_type _) => handler(), errHandler);
            }

            public receive_mail timed_case_of<T>(channel<T> chan, int ms, Func<Task> timedHandler, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _children.go(async delegate ()
                {
                    generator self = generator.self;
                    if (null == _mutex)
                    {
                        chan_recv_wrap<T> res = await chan_timed_receive(chan, ms);
                        try
                        {
                            self._mailboxMap = _children.parent()._mailboxMap;
                            if (chan_async_state.async_ok == res.state)
                            {
                                await handler(res.result);
                            }
                            else if (chan_async_state.async_overtime == res.state)
                            {
                                await timedHandler();
                            }
                            else if (null == errHandler || await errHandler(res.state)) { }
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
                            self._mailboxMap = _children.parent()._mailboxMap;
                            long endTick = system_tick.get_tick_ms() + ms;
                            while (_run)
                            {
                                bool overtime = false;
                                chan.append_pop_notify(self.timed_async_ignore2<chan_async_state>(ms, () => overtime = true), ntfSign);
                                await self.async_wait();
                                await mutex_lock_shared(_mutex);
                                try
                                {
                                    if (!overtime)
                                    {
                                        chan_recv_wrap<T> recvRes = await chan_try_receive(chan);
                                        if (chan_async_state.async_ok == recvRes.state)
                                        {
                                            await handler(recvRes.result); break;
                                        }
                                        else if ((null != errHandler && await errHandler(recvRes.state)) || chan_async_state.async_closed == recvRes.state) { break; }
                                        if (0 <= ms && 0 >= (ms = (int)(endTick - system_tick.get_tick_ms())))
                                        {
                                            await timedHandler(); break;
                                        }
                                    }
                                    else
                                    {
                                        await timedHandler(); break;
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
                            self._mailboxMap = null;
                            lock_suspend_and_stop();
                            chan.remove_pop_notify(self.async_ignore<chan_async_state>(), ntfSign);
                            await self.async_wait();
                            await unlock_suspend_and_stop();
                        }
                    }
                });
                return this;
            }

            public receive_mail timed_case_of<T1, T2>(channel<tuple<T1, T2>> chan, int ms, Func<Task> timedHandler, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(chan, ms, timedHandler, (tuple<T1, T2> msg) => handler(msg.value1, msg.value2), errHandler);
            }

            public receive_mail timed_case_of<T1, T2, T3>(channel<tuple<T1, T2, T3>> chan, int ms, Func<Task> timedHandler, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(chan, ms, timedHandler, (tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), errHandler);
            }

            public receive_mail try_case_of(channel<void_type> chan, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(chan, (void_type _) => handler(), errHandler);
            }

            public receive_mail try_case_of<T>(channel<T> chan, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
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
                        chan_recv_wrap<T> res = await chan_try_receive(chan);
                        if (chan_async_state.async_ok == res.state)
                        {
                            await handler(res.result);
                        }
                        else if (null == errHandler || await errHandler(res.state)) { }
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

            public receive_mail try_case_of<T1, T2>(channel<tuple<T1, T2>> chan, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(chan, (tuple<T1, T2> msg) => handler(msg.value1, msg.value2), errHandler);
            }

            public receive_mail try_case_of<T1, T2, T3>(channel<tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(chan, (tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), errHandler);
            }

            public receive_mail case_of(broadcast_chan<void_type> chan, Func<Task> handler, broadcast_chan_token token = null)
            {
                return case_of(chan, (void_type _) => handler(), token);
            }

            public receive_mail case_of<T>(broadcast_chan<T> chan, Func<T, Task> handler, broadcast_chan_token token = null)
            {
                return case_of(chan, handler, null, token);
            }

            public receive_mail case_of<T1, T2>(broadcast_chan<tuple<T1, T2>> chan, Func<T1, T2, Task> handler, broadcast_chan_token token = null)
            {
                return case_of(chan, handler, null, token);
            }

            public receive_mail case_of<T1, T2, T3>(broadcast_chan<tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, broadcast_chan_token token = null)
            {
                return case_of(chan, handler, null, token);
            }

            public receive_mail case_of(broadcast_chan<void_type> chan, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler, broadcast_chan_token token = null)
            {
                return case_of(chan, (void_type _) => handler(), errHandler, token);
            }

            public receive_mail case_of<T>(broadcast_chan<T> chan, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler, broadcast_chan_token token = null)
            {
                _children.go(async delegate ()
                {
                    generator self = generator.self;
                    token = null != token ? token : new broadcast_chan_token();
                    if (null == _mutex)
                    {
                        try
                        {
                            self._mailboxMap = _children.parent()._mailboxMap;
                            while (_run)
                            {
                                chan_recv_wrap<T> res = await chan_receive(chan, token);
                                if (chan_async_state.async_ok == res.state)
                                {
                                    await handler(res.result);
                                }
                                else if (null == errHandler || await errHandler(res.state))
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
                            self._mailboxMap = _children.parent()._mailboxMap;
                            nil_chan<chan_async_state> waitHasChan = new nil_chan<chan_async_state>();
                            Action<chan_async_state> waitHasNtf = waitHasChan.wrap();
                            chan_recv_wrap<T> recvRes = default(chan_recv_wrap<T>);
                            Action<chan_async_state, T, object> tryPopHandler = delegate (chan_async_state state, T msg, object _)
                            {
                                recvRes.state = state;
                                if (chan_async_state.async_ok == state)
                                {
                                    recvRes.result = msg;
                                }
                            };
                            chan.append_pop_notify(waitHasNtf, ntfSign);
                            while (_run)
                            {
                                await chan_receive(waitHasChan);
                                await mutex_lock_shared(_mutex);
                                try
                                {
                                    recvRes = default(chan_recv_wrap<T>);
                                    chan.try_pop_and_append_notify(self.async_callback(tryPopHandler), waitHasNtf, ntfSign);
                                    await self.async_wait();
                                    if (chan_async_state.async_ok == recvRes.state)
                                    {
                                        await handler(recvRes.result);
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
                            self._mailboxMap = null;
                            lock_suspend_and_stop();
                            chan.remove_pop_notify(self.async_ignore<chan_async_state>(), ntfSign);
                            await self.async_wait();
                            await unlock_suspend_and_stop();
                        }
                    }
                });
                return this;
            }

            public receive_mail case_of<T1, T2>(broadcast_chan<tuple<T1, T2>> chan, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler, broadcast_chan_token token = null)
            {
                return case_of(chan, (tuple<T1, T2> msg) => handler(msg.value1, msg.value2), errHandler);
            }

            public receive_mail case_of<T1, T2, T3>(broadcast_chan<tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler, broadcast_chan_token token = null)
            {
                return case_of(chan, (tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), errHandler);
            }

            public receive_mail timed_case_of(broadcast_chan<void_type> chan, int ms, Func<Task> timedHandler, Func<Task> handler, broadcast_chan_token token = null)
            {
                return timed_case_of(chan, ms, timedHandler, (void_type _) => handler(), token);
            }

            public receive_mail timed_case_of<T>(broadcast_chan<T> chan, int ms, Func<Task> timedHandler, Func<T, Task> handler, broadcast_chan_token token = null)
            {
                return timed_case_of(chan, ms, timedHandler, handler, null, token);
            }

            public receive_mail timed_case_of<T1, T2>(broadcast_chan<tuple<T1, T2>> chan, int ms, Func<Task> timedHandler, Func<T1, T2, Task> handler, broadcast_chan_token token = null)
            {
                return timed_case_of(chan, ms, timedHandler, handler, null, token);
            }

            public receive_mail timed_case_of<T1, T2, T3>(broadcast_chan<tuple<T1, T2, T3>> chan, int ms, Func<Task> timedHandler, Func<T1, T2, T3, Task> handler, broadcast_chan_token token = null)
            {
                return timed_case_of(chan, ms, timedHandler, handler, null, token);
            }

            public receive_mail timed_case_of(broadcast_chan<void_type> chan, int ms, Func<Task> timedHandler, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler, broadcast_chan_token token = null)
            {
                return timed_case_of(chan, ms, timedHandler, (void_type _) => handler(), errHandler, token);
            }

            public receive_mail timed_case_of<T>(broadcast_chan<T> chan, int ms, Func<Task> timedHandler, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler, broadcast_chan_token token = null)
            {
                _children.go(async delegate ()
                {
                    generator self = generator.self;
                    token = null != token ? token : new broadcast_chan_token();
                    if (null == _mutex)
                    {
                        chan_recv_wrap<T> res = await chan_timed_receive(chan, ms, token);
                        try
                        {
                            self._mailboxMap = _children.parent()._mailboxMap;
                            if (chan_async_state.async_ok == res.state)
                            {
                                await handler(res.result);
                            }
                            else if (chan_async_state.async_overtime == res.state)
                            {
                                await timedHandler();
                            }
                            else if (null == errHandler || await errHandler(res.state)) { }
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
                            self._mailboxMap = _children.parent()._mailboxMap;
                            long endTick = system_tick.get_tick_ms() + ms;
                            while (_run)
                            {
                                bool overtime = false;
                                chan.append_pop_notify(self.timed_async_ignore2<chan_async_state>(ms, () => overtime = true), ntfSign);
                                await self.async_wait();
                                await mutex_lock_shared(_mutex);
                                try
                                {
                                    if (!overtime)
                                    {
                                        chan_recv_wrap<T> recvRes = await chan_try_receive(chan);
                                        if (chan_async_state.async_ok == recvRes.state)
                                        {
                                            await handler(recvRes.result); break;
                                        }
                                        else if ((null != errHandler && await errHandler(recvRes.state)) || chan_async_state.async_closed == recvRes.state) { break; }
                                        if (0 <= ms && 0 >= (ms = (int)(endTick - system_tick.get_tick_ms())))
                                        {
                                            await timedHandler(); break;
                                        }
                                    }
                                    else
                                    {
                                        await timedHandler(); break;
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
                            self._mailboxMap = null;
                            lock_suspend_and_stop();
                            chan.remove_pop_notify(self.async_ignore<chan_async_state>(), ntfSign);
                            await self.async_wait();
                            await unlock_suspend_and_stop();
                        }
                    }
                });
                return this;
            }

            public receive_mail timed_case_of<T1, T2>(broadcast_chan<tuple<T1, T2>> chan, int ms, Func<Task> timedHandler, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler, broadcast_chan_token token = null)
            {
                return timed_case_of(chan, ms, timedHandler, (tuple<T1, T2> msg) => handler(msg.value1, msg.value2), errHandler);
            }

            public receive_mail timed_case_of<T1, T2, T3>(broadcast_chan<tuple<T1, T2, T3>> chan, int ms, Func<Task> timedHandler, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler, broadcast_chan_token token = null)
            {
                return timed_case_of(chan, ms, timedHandler, (tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), errHandler);
            }

            public receive_mail try_case_of(broadcast_chan<void_type> chan, Func<Task> handler, broadcast_chan_token token = null)
            {
                return try_case_of(chan, (void_type _) => handler(), token);
            }

            public receive_mail try_case_of<T>(broadcast_chan<T> chan, Func<T, Task> handler, broadcast_chan_token token = null)
            {
                return try_case_of(chan, handler, null, token);
            }

            public receive_mail try_case_of<T1, T2>(broadcast_chan<tuple<T1, T2>> chan, Func<T1, T2, Task> handler, broadcast_chan_token token = null)
            {
                return try_case_of(chan, handler, null, token);
            }

            public receive_mail try_case_of<T1, T2, T3>(broadcast_chan<tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, broadcast_chan_token token = null)
            {
                return try_case_of(chan, handler, null, token);
            }

            public receive_mail try_case_of(broadcast_chan<void_type> chan, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler, broadcast_chan_token token = null)
            {
                return try_case_of(chan, (void_type _) => handler(), errHandler, token);
            }

            public receive_mail try_case_of<T>(broadcast_chan<T> chan, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler, broadcast_chan_token token = null)
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
                        chan_recv_wrap<T> res = await chan_try_receive(chan, null != token ? token : new broadcast_chan_token());
                        if (chan_async_state.async_ok == res.state)
                        {
                            await handler(res.result);
                        }
                        else if (null == errHandler || await errHandler(res.state)) { }
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

            public receive_mail try_case_of<T1, T2>(broadcast_chan<tuple<T1, T2>> chan, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler, broadcast_chan_token token = null)
            {
                return try_case_of(chan, (tuple<T1, T2> msg) => handler(msg.value1, msg.value2), errHandler);
            }

            public receive_mail try_case_of<T1, T2, T3>(broadcast_chan<tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler, broadcast_chan_token token = null)
            {
                return try_case_of(chan, (tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), errHandler);
            }

            public receive_mail case_of<R>(csp_chan<R, void_type> chan, Func<Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(chan, async (csp_chan<R, void_type>.csp_result res, void_type _) => res.complete(await handler()), errHandler);
            }

            public receive_mail case_of<T>(csp_chan<void_type, T> chan, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(chan, async (csp_chan<void_type, T>.csp_result res, T msg) => { await handler(msg); res.complete(default(void_type)); }, errHandler);
            }

            public receive_mail case_of<T1, T2>(csp_chan<void_type, tuple<T1, T2>> chan, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(chan, async (csp_chan<void_type, tuple<T1, T2>>.csp_result res, tuple<T1, T2> msg) => { await handler(msg.value1, msg.value2); res.complete(default(void_type)); }, errHandler);
            }

            public receive_mail case_of<T1, T2, T3>(csp_chan<void_type, tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(chan, async (csp_chan<void_type, tuple<T1, T2, T3>>.csp_result res, tuple<T1, T2, T3> msg) => { await handler(msg.value1, msg.value2, msg.value3); res.complete(default(void_type)); }, errHandler);
            }

            public receive_mail case_of(csp_chan<void_type, void_type> chan, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(chan, async (csp_chan<void_type, void_type>.csp_result res, void_type _) => { await handler(); res.complete(default(void_type)); }, errHandler);
            }

            public receive_mail case_of<R, T>(csp_chan<R, T> chan, Func<T, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(chan, async (csp_chan<R, T>.csp_result res, T msg) => res.complete(await handler(msg)), errHandler);
            }

            public receive_mail case_of<R, T1, T2>(csp_chan<R, tuple<T1, T2>> chan, Func<T1, T2, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(chan, async (csp_chan<R, tuple<T1, T2>>.csp_result res, tuple<T1, T2> msg) => res.complete(await handler(msg.value1, msg.value2)), errHandler);
            }

            public receive_mail case_of<R, T1, T2, T3>(csp_chan<R, tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(chan, async (csp_chan<R, tuple<T1, T2, T3>>.csp_result res, tuple<T1, T2, T3> msg) => res.complete(await handler(msg.value1, msg.value2, msg.value3)), errHandler);
            }

            public receive_mail case_of<R>(csp_chan<R, void_type> chan, Func<csp_chan<R, void_type>.csp_result, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(chan, (csp_chan<R, void_type>.csp_result res, void_type _) => handler(res), errHandler);
            }

            public receive_mail case_of<R, T>(csp_chan<R, T> chan, Func<csp_chan<R, T>.csp_result, T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
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
                                csp_wait_wrap<R, T> res = await csp_wait(chan);
                                if (chan_async_state.async_ok == res.state)
                                {
                                    await handler(res.result, res.msg);
                                }
                                else if (null == errHandler || await errHandler(res.state))
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
                            self._mailboxMap = _children.parent()._mailboxMap;
                            nil_chan<chan_async_state> waitHasChan = new nil_chan<chan_async_state>();
                            Action<chan_async_state> waitHasNtf = waitHasChan.wrap();
                            csp_wait_wrap<R, T> recvRes = default(csp_wait_wrap<R, T>);
                            Action<chan_async_state, T, object> tryPopHandler = delegate (chan_async_state state, T msg, object exObj)
                            {
                                recvRes.state = state;
                                if (chan_async_state.async_ok == state)
                                {
                                    recvRes.msg = msg;
                                    recvRes.result = (csp_chan<R, T>.csp_result)exObj;
                                }
                            };
                            chan.append_pop_notify(waitHasNtf, ntfSign);
                            while (_run)
                            {
                                await chan_receive(waitHasChan);
                                await mutex_lock_shared(_mutex);
                                try
                                {
                                    recvRes = default(csp_wait_wrap<R, T>);
                                    chan.try_pop_and_append_notify(self.async_callback(tryPopHandler), waitHasNtf, ntfSign);
                                    await self.async_wait();
                                    if (chan_async_state.async_ok == recvRes.state)
                                    {
                                        await handler(recvRes.result, recvRes.msg);
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
                            self._mailboxMap = null;
                            lock_suspend_and_stop();
                            chan.remove_pop_notify(self.async_ignore<chan_async_state>(), ntfSign);
                            await self.async_wait();
                            await unlock_suspend_and_stop();
                        }
                    }
                });
                return this;
            }

            public receive_mail case_of<R, T1, T2>(csp_chan<R, tuple<T1, T2>> chan, Func<csp_chan<R, tuple<T1, T2>>.csp_result, T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(chan, (csp_chan<R, tuple<T1, T2>>.csp_result result, tuple<T1, T2> msg) => handler(result, msg.value1, msg.value2), errHandler);
            }

            public receive_mail case_of<R, T1, T2, T3>(csp_chan<R, tuple<T1, T2, T3>> chan, Func<csp_chan<R, tuple<T1, T2, T3>>.csp_result, T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(chan, (csp_chan<R, tuple<T1, T2, T3>>.csp_result result, tuple<T1, T2, T3> msg) => handler(result, msg.value1, msg.value2, msg.value3), errHandler);
            }

            public receive_mail timed_case_of<R>(csp_chan<R, void_type> chan, int ms, Func<Task> timedHandler, Func<Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(chan, ms, timedHandler, async (csp_chan<R, void_type>.csp_result res, void_type _) => res.complete(await handler()), errHandler);
            }

            public receive_mail timed_case_of<T>(csp_chan<void_type, T> chan, int ms, Func<Task> timedHandler, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(chan, ms, timedHandler, async (csp_chan<void_type, T>.csp_result res, T msg) => { await handler(msg); res.complete(default(void_type)); }, errHandler);
            }

            public receive_mail timed_case_of<T1, T2>(csp_chan<void_type, tuple<T1, T2>> chan, int ms, Func<Task> timedHandler, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(chan, ms, timedHandler, async (csp_chan<void_type, tuple<T1, T2>>.csp_result res, tuple<T1, T2> msg) => { await handler(msg.value1, msg.value2); res.complete(default(void_type)); }, errHandler);
            }

            public receive_mail timed_case_of<T1, T2, T3>(csp_chan<void_type, tuple<T1, T2, T3>> chan, int ms, Func<Task> timedHandler, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(chan, ms, timedHandler, async (csp_chan<void_type, tuple<T1, T2, T3>>.csp_result res, tuple<T1, T2, T3> msg) => { await handler(msg.value1, msg.value2, msg.value3); res.complete(default(void_type)); }, errHandler);
            }

            public receive_mail timed_case_of(csp_chan<void_type, void_type> chan, int ms, Func<Task> timedHandler, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(chan, ms, timedHandler, async (csp_chan<void_type, void_type>.csp_result res, void_type _) => { await handler(); res.complete(default(void_type)); }, errHandler);
            }

            public receive_mail timed_case_of<R, T>(csp_chan<R, T> chan, int ms, Func<Task> timedHandler, Func<T, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(chan, ms, timedHandler, async (csp_chan<R, T>.csp_result res, T msg) => res.complete(await handler(msg)), errHandler);
            }

            public receive_mail timed_case_of<R, T1, T2>(csp_chan<R, tuple<T1, T2>> chan, int ms, Func<Task> timedHandler, Func<T1, T2, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(chan, ms, timedHandler, async (csp_chan<R, tuple<T1, T2>>.csp_result res, tuple<T1, T2> msg) => res.complete(await handler(msg.value1, msg.value2)), errHandler);
            }

            public receive_mail timed_case_of<R, T1, T2, T3>(csp_chan<R, tuple<T1, T2, T3>> chan, int ms, Func<Task> timedHandler, Func<T1, T2, T3, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(chan, ms, timedHandler, async (csp_chan<R, tuple<T1, T2, T3>>.csp_result res, tuple<T1, T2, T3> msg) => res.complete(await handler(msg.value1, msg.value2, msg.value3)), errHandler);
            }

            public receive_mail timed_case_of<R>(csp_chan<R, void_type> chan, int ms, Func<Task> timedHandler, Func<csp_chan<R, void_type>.csp_result, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(chan, ms, timedHandler, (csp_chan<R, void_type>.csp_result res, void_type _) => handler(res), errHandler);
            }

            public receive_mail timed_case_of<R, T>(csp_chan<R, T> chan, int ms, Func<Task> timedHandler, Func<csp_chan<R, T>.csp_result, T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _children.go(async delegate ()
                {
                    generator self = generator.self;
                    if (null == _mutex)
                    {
                        csp_wait_wrap<R, T> res = await csp_timed_wait(chan, ms);
                        try
                        {
                            self._mailboxMap = _children.parent()._mailboxMap;
                            if (chan_async_state.async_ok == res.state)
                            {
                                await handler(res.result, res.msg);
                            }
                            else if (chan_async_state.async_overtime == res.state)
                            {
                                await timedHandler();
                            }
                            else if (null == errHandler || await errHandler(res.state)) { }
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
                            self._mailboxMap = _children.parent()._mailboxMap;
                            long endTick = system_tick.get_tick_ms() + ms;
                            while (_run)
                            {
                                bool overtime = false;
                                chan.append_pop_notify(self.timed_async_ignore2<chan_async_state>(ms, () => overtime = true), ntfSign);
                                await self.async_wait();
                                await mutex_lock_shared(_mutex);
                                try
                                {
                                    if (!overtime)
                                    {
                                        csp_wait_wrap<R, T> recvRes = await csp_try_wait(chan);
                                        if (chan_async_state.async_ok == recvRes.state)
                                        {
                                            await handler(recvRes.result, recvRes.msg); break;
                                        }
                                        else if ((null != errHandler && await errHandler(recvRes.state)) || chan_async_state.async_closed == recvRes.state) { break; }
                                        if (0 <= ms && 0 >= (ms = (int)(endTick - system_tick.get_tick_ms())))
                                        {
                                            await timedHandler(); break;
                                        }
                                    }
                                    else
                                    {
                                        await timedHandler(); break;
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
                            self._mailboxMap = null;
                            lock_suspend_and_stop();
                            chan.remove_pop_notify(self.async_ignore<chan_async_state>(), ntfSign);
                            await self.async_wait();
                            await unlock_suspend_and_stop();
                        }
                    }
                });
                return this;
            }

            public receive_mail timed_case_of<R, T1, T2>(csp_chan<R, tuple<T1, T2>> chan, int ms, Func<Task> timedHandler, Func<csp_chan<R, tuple<T1, T2>>.csp_result, T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(chan, ms, timedHandler, (csp_chan<R, tuple<T1, T2>>.csp_result result, tuple<T1, T2> msg) => handler(result, msg.value1, msg.value2), errHandler);
            }

            public receive_mail timed_case_of<R, T1, T2, T3>(csp_chan<R, tuple<T1, T2, T3>> chan, int ms, Func<Task> timedHandler, Func<csp_chan<R, tuple<T1, T2, T3>>.csp_result, T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(chan, ms, timedHandler, (csp_chan<R, tuple<T1, T2, T3>>.csp_result result, tuple<T1, T2, T3> msg) => handler(result, msg.value1, msg.value2, msg.value3), errHandler);
            }

            public receive_mail try_case_of<R>(csp_chan<R, void_type> chan, Func<Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(chan, async (csp_chan<R, void_type>.csp_result res, void_type _) => res.complete(await handler()), errHandler);
            }

            public receive_mail try_case_of<T>(csp_chan<void_type, T> chan, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(chan, async (csp_chan<void_type, T>.csp_result res, T msg) => { await handler(msg); res.complete(default(void_type)); }, errHandler);
            }

            public receive_mail try_case_of<T1, T2>(csp_chan<void_type, tuple<T1, T2>> chan, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(chan, async (csp_chan<void_type, tuple<T1, T2>>.csp_result res, tuple<T1, T2> msg) => { await handler(msg.value1, msg.value2); res.complete(default(void_type)); }, errHandler);
            }

            public receive_mail try_case_of<T1, T2, T3>(csp_chan<void_type, tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(chan, async (csp_chan<void_type, tuple<T1, T2, T3>>.csp_result res, tuple<T1, T2, T3> msg) => { await handler(msg.value1, msg.value2, msg.value3); res.complete(default(void_type)); }, errHandler);
            }

            public receive_mail try_case_of(csp_chan<void_type, void_type> chan, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(chan, async (csp_chan<void_type, void_type>.csp_result res, void_type _) => { await handler(); res.complete(default(void_type)); }, errHandler);
            }

            public receive_mail try_case_of<R, T>(csp_chan<R, T> chan, Func<T, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(chan, async (csp_chan<R, T>.csp_result res, T msg) => res.complete(await handler(msg)), errHandler);
            }

            public receive_mail try_case_of<R, T1, T2>(csp_chan<R, tuple<T1, T2>> chan, Func<T1, T2, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(chan, async (csp_chan<R, tuple<T1, T2>>.csp_result res, tuple<T1, T2> msg) => res.complete(await handler(msg.value1, msg.value2)), errHandler);
            }

            public receive_mail try_case_of<R, T1, T2, T3>(csp_chan<R, tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(chan, async (csp_chan<R, tuple<T1, T2, T3>>.csp_result res, tuple<T1, T2, T3> msg) => res.complete(await handler(msg.value1, msg.value2, msg.value3)), errHandler);
            }

            public receive_mail try_case_of<R>(csp_chan<R, void_type> chan, Func<csp_chan<R, void_type>.csp_result, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(chan, (csp_chan<R, void_type>.csp_result res, void_type _) => handler(res), errHandler);
            }

            public receive_mail try_case_of<R, T>(csp_chan<R, T> chan, Func<csp_chan<R, T>.csp_result, T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
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
                        csp_wait_wrap<R, T> res = await csp_try_wait(chan);
                        if (chan_async_state.async_ok == res.state)
                        {
                            await handler(res.result, res.msg);
                        }
                        else if (null == errHandler || await errHandler(res.state)) { }
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

            public receive_mail try_case_of<R, T1, T2>(csp_chan<R, tuple<T1, T2>> chan, Func<csp_chan<R, tuple<T1, T2>>.csp_result, T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(chan, (csp_chan<R, tuple<T1, T2>>.csp_result result, tuple<T1, T2> msg) => handler(result, msg.value1, msg.value2), errHandler);
            }

            public receive_mail try_case_of<R, T1, T2, T3>(csp_chan<R, tuple<T1, T2, T3>> chan, Func<csp_chan<R, tuple<T1, T2, T3>>.csp_result, T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(chan, (csp_chan<R, tuple<T1, T2, T3>>.csp_result result, tuple<T1, T2, T3> msg) => handler(result, msg.value1, msg.value2, msg.value3), errHandler);
            }

            public receive_mail case_of<T>(int id, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(self_mailbox<T>(id), handler, errHandler);
            }

            public receive_mail case_of<T1, T2>(int id, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(self_mailbox<tuple<T1, T2>>(id), handler, errHandler);
            }

            public receive_mail case_of<T1, T2, T3>(int id, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(self_mailbox<tuple<T1, T2, T3>>(id), handler, errHandler);
            }

            public receive_mail timed_case_of<T>(int id, int ms, Func<Task> timedHandler, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(self_mailbox<T>(id), ms, timedHandler, handler, errHandler);
            }

            public receive_mail timed_case_of<T1, T2>(int id, int ms, Func<Task> timedHandler, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(self_mailbox<tuple<T1, T2>>(id), ms, timedHandler, handler, errHandler);
            }

            public receive_mail timed_case_of<T1, T2, T3>(int id, int ms, Func<Task> timedHandler, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(self_mailbox<tuple<T1, T2, T3>>(id), ms, timedHandler, handler, errHandler);
            }

            public receive_mail try_case_of<T>(int id, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(self_mailbox<T>(id), handler, errHandler);
            }

            public receive_mail try_case_of<T1, T2>(int id, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(self_mailbox<tuple<T1, T2>>(id), handler, errHandler);
            }

            public receive_mail try_case_of<T1, T2, T3>(int id, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(self_mailbox<tuple<T1, T2, T3>>(id), handler, errHandler);
            }

            public receive_mail case_of<T>(Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(0, handler, errHandler);
            }

            public receive_mail case_of<T1, T2>(Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(0, handler, errHandler);
            }

            public receive_mail case_of<T1, T2, T3>(Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(0, handler, errHandler);
            }

            public receive_mail timed_case_of<T>(int ms, Func<Task> timedHandler, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(0, ms, timedHandler, handler, errHandler);
            }

            public receive_mail timed_case_of<T1, T2>(int ms, Func<Task> timedHandler, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(0, ms, timedHandler, handler, errHandler);
            }

            public receive_mail timed_case_of<T1, T2, T3>(int ms, Func<Task> timedHandler, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(0, ms, timedHandler, handler, errHandler);
            }

            public receive_mail try_case_of<T>(Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(0, handler, errHandler);
            }

            public receive_mail try_case_of<T1, T2>(Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(0, handler, errHandler);
            }

            public receive_mail try_case_of<T1, T2, T3>(Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(0, handler, errHandler);
            }

            public receive_mail case_of(int id, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(self_mailbox<void_type>(id), handler, errHandler);
            }

            public receive_mail timed_case_of(int id, int ms, Func<Task> timedHandler, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(self_mailbox<void_type>(id), ms, timedHandler, handler, errHandler);
            }

            public receive_mail try_case_of(int id, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(self_mailbox<void_type>(id), handler, errHandler);
            }

            public receive_mail case_of(Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_of(0, handler, errHandler);
            }

            public receive_mail timed_case_of(int ms, Func<Task> timedHandler, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return timed_case_of(0, ms, timedHandler, handler, errHandler);
            }

            public receive_mail try_case_of(Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return try_case_of(0, handler, errHandler);
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
            LinkedList<select_chan_base> _chans;

            internal select_chans(LinkedList<select_chan_base> chans)
            {
                _chans = chans;
            }

            public select_chans case_recv_mail<T>(Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_receive(self_mailbox<T>(), handler, errHandler);
            }

            public select_chans case_recv_mail<T1, T2>(Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_receive(self_mailbox<tuple<T1, T2>>(), handler, errHandler);
            }

            public select_chans case_recv_mail<T1, T2, T3>(Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_receive(self_mailbox<tuple<T1, T2, T3>>(), handler, errHandler);
            }

            public select_chans case_recv_mail<T>(int id, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_receive(self_mailbox<T>(id), handler, errHandler);
            }

            public select_chans case_recv_mail<T1, T2>(int id, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_receive(self_mailbox<tuple<T1, T2>>(id), handler, errHandler);
            }

            public select_chans case_recv_mail<T1, T2, T3>(int id, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                return case_receive(self_mailbox<tuple<T1, T2, T3>>(id), handler, errHandler);
            }

            public select_chans case_receive<T>(channel<T> chan, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_reader(handler, errHandler));
                return this;
            }

            public select_chans case_receive<T1, T2>(channel<tuple<T1, T2>> chan, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_reader((tuple<T1, T2> msg) => handler(msg.value1, msg.value2), errHandler));
                return this;
            }

            public select_chans case_receive<T1, T2, T3>(channel<tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_reader((tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), errHandler));
                return this;
            }

            public select_chans case_receive(channel<void_type> chan, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_reader((void_type _) => handler(), errHandler));
                return this;
            }

            public select_chans case_send<T>(channel<T> chan, Func<T> msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_writer(msg, handler, errHandler));
                return this;
            }

            public select_chans case_send<T>(channel<T> chan, async_result_wrap<T> msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_writer(msg, handler, errHandler));
                return this;
            }

            public select_chans case_send<T>(channel<T> chan, T msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_writer(msg, handler, errHandler));
                return this;
            }

            public select_chans case_send(channel<void_type> chan, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_writer(default(void_type), handler, errHandler));
                return this;
            }

            public select_chans case_receive<T>(broadcast_chan<T> chan, Func<T, Task> handler, broadcast_chan_token token)
            {
                _chans.AddLast(chan.make_select_reader(handler, token));
                return this;
            }

            public select_chans case_receive<T1, T2>(broadcast_chan<tuple<T1, T2>> chan, Func<T1, T2, Task> handler, broadcast_chan_token token)
            {
                _chans.AddLast(chan.make_select_reader((tuple<T1, T2> msg) => handler(msg.value1, msg.value2), token));
                return this;
            }

            public select_chans case_receive<T1, T2, T3>(broadcast_chan<tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, broadcast_chan_token token)
            {
                _chans.AddLast(chan.make_select_reader((tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), token));
                return this;
            }

            public select_chans case_receive<T>(broadcast_chan<T> chan, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler, broadcast_chan_token token)
            {
                _chans.AddLast(chan.make_select_reader(handler, errHandler, token));
                return this;
            }

            public select_chans case_receive<T1, T2>(broadcast_chan<tuple<T1, T2>> chan, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler, broadcast_chan_token token)
            {
                _chans.AddLast(chan.make_select_reader((tuple<T1, T2> msg) => handler(msg.value1, msg.value2), errHandler, token));
                return this;
            }

            public select_chans case_receive<T1, T2, T3>(broadcast_chan<tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler, broadcast_chan_token token)
            {
                _chans.AddLast(chan.make_select_reader((tuple<T1, T2, T3> msg) => handler(msg.value1, msg.value2, msg.value3), errHandler, token));
                return this;
            }

            public select_chans case_receive(broadcast_chan<void_type> chan, Func<Task> handler, broadcast_chan_token token)
            {
                _chans.AddLast(chan.make_select_reader((void_type _) => handler(), token));
                return this;
            }

            public select_chans case_receive(broadcast_chan<void_type> chan, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler, broadcast_chan_token token)
            {
                _chans.AddLast(chan.make_select_reader((void_type _) => handler(), errHandler, token));
                return this;
            }

            public select_chans case_receive<R, T>(csp_chan<R, T> chan, Func<csp_chan<R, T>.csp_result, T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_reader(handler, errHandler));
                return this;
            }

            public select_chans case_receive<R, T1, T2>(csp_chan<R, tuple<T1, T2>> chan, Func<csp_chan<R, tuple<T1, T2>>.csp_result, T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_reader((csp_chan<R, tuple<T1, T2>>.csp_result result, tuple<T1, T2> msg) => handler(result, msg.value1, msg.value2), errHandler));
                return this;
            }

            public select_chans case_receive<R, T1, T2, T3>(csp_chan<R, tuple<T1, T2, T3>> chan, Func<csp_chan<R, tuple<T1, T2, T3>>.csp_result, T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_reader((csp_chan<R, tuple<T1, T2, T3>>.csp_result result, tuple<T1, T2, T3> msg) => handler(result, msg.value1, msg.value2, msg.value3), errHandler));
                return this;
            }

            public select_chans case_receive<R, T>(csp_chan<R, T> chan, Func<T, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_reader(async (csp_chan<R, T>.csp_result res, T msg) => res.complete(await handler(msg)), errHandler));
                return this;
            }

            public select_chans case_receive<R, T1, T2>(csp_chan<R, tuple<T1, T2>> chan, Func<T1, T2, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_reader(async (csp_chan<R, tuple<T1, T2>>.csp_result res, tuple<T1, T2> msg) => res.complete(await handler(msg.value1, msg.value2)), errHandler));
                return this;
            }

            public select_chans case_receive<R, T1, T2, T3>(csp_chan<R, tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_reader(async (csp_chan<R, tuple<T1, T2, T3>>.csp_result res, tuple<T1, T2, T3> msg) => res.complete(await handler(msg.value1, msg.value2, msg.value3)), errHandler));
                return this;
            }

            public select_chans case_receive<R>(csp_chan<R, void_type> chan, Func<Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_reader(async (csp_chan<R, void_type>.csp_result res, void_type _) => res.complete(await handler()), errHandler));
                return this;
            }

            public select_chans case_receive<T>(csp_chan<void_type, T> chan, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_reader(async (csp_chan<void_type, T>.csp_result res, T msg) => { await handler(msg); res.complete(default(void_type)); }, errHandler));
                return this;
            }

            public select_chans case_receive<T1, T2>(csp_chan<void_type, tuple<T1, T2>> chan, Func<T1, T2, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_reader(async (csp_chan<void_type, tuple<T1, T2>>.csp_result res, tuple<T1, T2> msg) => { await handler(msg.value1, msg.value2); res.complete(default(void_type)); }, errHandler));
                return this;
            }

            public select_chans case_receive<T1, T2, T3>(csp_chan<void_type, tuple<T1, T2, T3>> chan, Func<T1, T2, T3, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_reader(async (csp_chan<void_type, tuple<T1, T2, T3>>.csp_result res, tuple<T1, T2, T3> msg) => { await handler(msg.value1, msg.value2, msg.value3); res.complete(default(void_type)); }, errHandler));
                return this;
            }

            public select_chans case_receive(csp_chan<void_type, void_type> chan, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_reader(async (csp_chan<void_type, void_type>.csp_result res, void_type _) => { await handler(); res.complete(default(void_type)); }, errHandler));
                return this;
            }

            public select_chans case_send<R, T>(csp_chan<R, T> chan, Func<T> msg, Func<R, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_writer(msg, handler, errHandler));
                return this;
            }

            public select_chans case_send<R, T>(csp_chan<R, T> chan, async_result_wrap<T> msg, Func<R, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_writer(msg, handler, errHandler));
                return this;
            }

            public select_chans case_send<R, T>(csp_chan<R, T> chan, T msg, Func<R, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_writer(msg, handler, errHandler));
                return this;
            }

            public select_chans case_send<R>(csp_chan<R, void_type> chan, Func<R, Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_writer(default(void_type), handler, errHandler));
                return this;
            }

            public select_chans case_send<T>(csp_chan<void_type, T> chan, Func<T> msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_writer(msg, (void_type _) => handler(), errHandler));
                return this;
            }

            public select_chans case_send<T>(csp_chan<void_type, T> chan, async_result_wrap<T> msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_writer(msg, (void_type _) => handler(), errHandler));
                return this;
            }

            public select_chans case_send<T>(csp_chan<void_type, T> chan, T msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_writer(msg, (void_type _) => handler(), errHandler));
                return this;
            }

            public select_chans case_send(csp_chan<void_type, void_type> chan, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler = null)
            {
                _chans.AddLast(chan.make_select_writer(default(void_type), (void_type _) => handler(), errHandler));
                return this;
            }

            public async Task<bool> loop(action eachAferDo = null)
            {
                generator this_ = self;
                LinkedList<select_chan_base> chans = _chans;
                msg_buff<select_chan_base> selectChans = new msg_buff<select_chan_base>(this_.strand);
                for (LinkedListNode<select_chan_base> it = chans.First; null != it; it = it.Next)
                {
                    select_chan_base chan = it.Value;
                    chan.ntfSign._selectOnce = false;
                    chan.nextSelect = (chan_async_state state) => selectChans.post(chan);
                    chan.begin();
                }
                try
                {
                    if (null == this_._selectChans)
                    {
                        this_._selectChans = new LinkedList<LinkedList<select_chan_base>>();
                    }
                    this_._selectChans.AddFirst(chans);
                    int count = chans.Count;
                    bool selected = false;
                    Func<Task> stepOne = null == eachAferDo ? (Func<Task>)null : delegate ()
                    {
                        selected = true;
                        return nil_wait();
                    };
                    while (0 != count)
                    {
                        select_chan_base selectedChan = (await chan_receive(selectChans)).result;
                        if (selectedChan.disabled())
                        {
                            continue;
                        }
                        try
                        {
                            select_chan_state selState = await selectedChan.invoke(stepOne);
                            if (!selState.nextRound)
                            {
                                count--;
                            }
                        }
                        catch (stop_this_case_exception)
                        {
                            count--;
                            await selectedChan.end();
                        }
                        if (selected)
                        {
                            selected = false;
                            await eachAferDo();
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
                    this_._selectChans.RemoveFirst();
                    lock_suspend_and_stop();
                    for (LinkedListNode<select_chan_base> it = chans.First; null != it; it = it.Next)
                    {
                        await it.Value.end();
                    }
                    await unlock_suspend_and_stop();
                }
            }

            public async Task<bool> end()
            {
                generator this_ = self;
                LinkedList<select_chan_base> chans = _chans;
                msg_buff<select_chan_base> selectChans = new msg_buff<select_chan_base>(this_.strand);
                for (LinkedListNode<select_chan_base> it = chans.First; null != it; it = it.Next)
                {
                    select_chan_base chan = it.Value;
                    chan.ntfSign._selectOnce = true;
                    chan.nextSelect = (chan_async_state state) => selectChans.post(chan);
                    chan.begin();
                }
                bool selected = false;
                try
                {
                    if (null == this_._selectChans)
                    {
                        this_._selectChans = new LinkedList<LinkedList<select_chan_base>>();
                    }
                    this_._selectChans.AddFirst(chans);
                    int count = chans.Count;
                    while (0 != count)
                    {
                        select_chan_base selectedChan = (await chan_receive(selectChans)).result;
                        if (selectedChan.disabled())
                        {
                            continue;
                        }
                        try
                        {
                            select_chan_state selState = await selectedChan.invoke(async delegate ()
                            {
                                for (LinkedListNode<select_chan_base> it = chans.First; null != it; it = it.Next)
                                {
                                    if (selectedChan != it.Value)
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
                                await selectedChan.end();
                            }
                        }
                    }
                }
                catch (stop_select_exception) { }
                finally
                {
                    this_._selectChans.RemoveFirst();
                    if (!selected)
                    {
                        lock_suspend_and_stop();
                        for (LinkedListNode<select_chan_base> it = chans.First; null != it; it = it.Next)
                        {
                            await it.Value.end();
                        }
                        await unlock_suspend_and_stop();
                    }
                }
                return selected;
            }

            public async Task<bool> timed(int ms)
            {
                generator this_ = self;
                LinkedList<select_chan_base> chans = _chans;
                msg_buff<select_chan_base> selectChans = new msg_buff<select_chan_base>(this_.strand);
                this_._timer.timeout(ms, selectChans.wrap_default());
                for (LinkedListNode<select_chan_base> it = chans.First; null != it; it = it.Next)
                {
                    select_chan_base chan = it.Value;
                    chan.ntfSign._selectOnce = true;
                    chan.nextSelect = (chan_async_state state) => selectChans.post(chan);
                    chan.begin();
                }
                bool selected = false;
                try
                {
                    if (null == this_._selectChans)
                    {
                        this_._selectChans = new LinkedList<LinkedList<select_chan_base>>();
                    }
                    this_._selectChans.AddFirst(chans);
                    int count = chans.Count;
                    while (0 != count)
                    {
                        select_chan_base selectedChan = (await chan_receive(selectChans)).result;
                        if (null != selectedChan)
                        {
                            if (selectedChan.disabled())
                            {
                                continue;
                            }
                            try
                            {
                                select_chan_state selState = await selectedChan.invoke(async delegate ()
                                {
                                    this_._timer.cancel();
                                    for (LinkedListNode<select_chan_base> it = chans.First; null != it; it = it.Next)
                                    {
                                        if (selectedChan != it.Value)
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
                                    await selectedChan.end();
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
                    this_._selectChans.RemoveFirst();
                    if (!selected)
                    {
                        this_._timer.cancel();
                        lock_suspend_and_stop();
                        for (LinkedListNode<select_chan_base> it = chans.First; null != it; it = it.Next)
                        {
                            await it.Value.end();
                        }
                        await unlock_suspend_and_stop();
                    }
                }
                return true;
            }
        }

        static public select_chans select()
        {
            return new select_chans(new LinkedList<select_chan_base>());
        }

        static public void stop_select()
        {
#if DEBUG
            generator this_ = self;
            Trace.Assert(null != this_ && null != this_._selectChans && 0 != this_._selectChans.Count, "不正确的 stop_select 调用!");
#endif
            throw stop_select_exception.val;
        }

        static public void stop_this_case()
        {
#if DEBUG
            generator this_ = self;
            Trace.Assert(null != this_ && null != this_._selectChans && 0 != this_._selectChans.Count, "不正确的 stop_this_case 调用!");
#endif
            throw stop_this_case_exception.val;
        }

        static public async Task disable_other_case(channel_base otherChan, bool disable = true)
        {
            generator this_ = self;
            if (null != this_._selectChans && 0 != this_._selectChans.Count)
            {
                LinkedList<select_chan_base> currSelect = this_._selectChans.First.Value;
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
                            chan.begin();
                        }
                    }
                }
            }
        }

        static public async Task disable_other_case_receive(channel_base otherChan, bool disable = true)
        {
            generator this_ = self;
            if (null != this_._selectChans && 0 != this_._selectChans.Count)
            {
                LinkedList<select_chan_base> currSelect = this_._selectChans.First.Value;
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
                            chan.begin();
                        }
                    }
                }
            }
        }

        static public async Task disable_other_case_send(channel_base otherChan, bool disable = true)
        {
            generator this_ = self;
            if (null != this_._selectChans && 0 != this_._selectChans.Count)
            {
                LinkedList<select_chan_base> currSelect = this_._selectChans.First.Value;
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
                            chan.begin();
                        }
                    }
                }
            }
        }

        static public Task enable_other_case(channel_base otherChan)
        {
            return disable_other_case(otherChan, false);
        }

        static public Task enable_other_case_receive(channel_base otherChan)
        {
            return disable_other_case_receive(otherChan, false);
        }

        static public Task enable_other_case_send(channel_base otherChan)
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
                    gen.stop(_parent.async_result());
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
                    msg_buff<child> waitStop = new msg_buff<child>(_parent.strand);
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
                        child gen = (await chan_receive(waitStop)).result;
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
                    gen.append_stop_callback(_parent.async_result());
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
                    msg_buff<child> waitStop = new msg_buff<child>(_parent.strand);
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
                        child gen = (await chan_receive(waitStop)).result;
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
                    msg_buff<child> waitStop = new msg_buff<child>(_parent.strand);
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
                        child gen = (await chan_receive(waitStop)).result;
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
                    msg_buff<Tuple<children, child>> waitStop = new msg_buff<Tuple<children, child>>(self.strand);
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
                        Tuple<children, child> oneRes = (await chan_receive(waitStop)).result;
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
                    msg_buff<tuple<child, LinkedListNode<Action>>> waitRemove = new msg_buff<tuple<child, LinkedListNode<Action>>>(_parent.strand);
                    async_result_wrap<child> res = new async_result_wrap<child>();
                    Action<child> ntf = _parent.safe_async_result(res);
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
                            tuple<child, LinkedListNode<Action>> node = (await chan_receive(waitRemove)).result;
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
                    msg_buff<tuple<child, LinkedListNode<Action>>> waitRemove = new msg_buff<tuple<child, LinkedListNode<Action>>>(_parent.strand);
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
                            tuple<child, LinkedListNode<Action>> node = (await chan_receive(waitRemove)).result;
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
                    msg_buff<child> waitStop = new msg_buff<child>(_parent.strand);
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
                        child gen = (await chan_receive(waitStop)).result;
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

        msg_buff<gen_pck> _queue;
        generator _runGen;

        public async_queue()
        {
            shared_strand strand = new shared_strand();
            _queue = new msg_buff<gen_pck>(strand);
            _runGen = generator.make(strand, async delegate ()
            {
                while (true)
                {
                    chan_recv_wrap<gen_pck> pck = await generator.chan_receive(_queue);
                    await generator.depth_call(pck.result.strand, pck.result.action);
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
        msg_buff<generator.action> _queue;
        generator _runGen;

        public async_strand(shared_strand strand)
        {
            _queue = new msg_buff<generator.action>(strand);
            _runGen = generator.make(strand, async delegate ()
            {
                while (true)
                {
                    chan_recv_wrap<generator.action> pck = await generator.chan_receive(_queue);
                    generator.lock_stop();
                    try
                    {
                        await pck.result();
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
        LinkedList<Action> _waitList;

        public wait_group(int initTasks = 0)
        {
            _tasks = initTasks;
            _waitList = new LinkedList<Action>();
        }

        public void reset()
        {
#if DEBUG
            Trace.Assert(0 == _tasks, "不正确的 reset 调用!");
#endif
            Monitor.Enter(this);
            if (null != _waitList)
            {
                _waitList.AddLast(delegate ()
                {
                    Monitor.Enter(this);
                    Monitor.Pulse(this);
                    Monitor.Exit(this);
                });
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
                LinkedListNode<Action> newNode = new LinkedListNode<Action>(delegate ()
                {
                    Monitor.Enter(this);
                    Monitor.Pulse(this);
                    Monitor.Exit(this);
                });
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
                LinkedListNode<Action> newNode = new LinkedListNode<Action>(delegate ()
                {
                    Monitor.Enter(this);
                    Monitor.Pulse(this);
                    Monitor.Exit(this);
                });
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
