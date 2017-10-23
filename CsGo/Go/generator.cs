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
    }

    public class async_result_wrap<T1>
    {
        T1 p1;

        public virtual T1 value_1
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

        public virtual T1 value_1
        {
            get { return p1; }
            set { p1 = value; }
        }

        public virtual T2 value_2
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

        public virtual T1 value_1
        {
            get { return p1; }
            set { p1 = value; }
        }

        public virtual T2 value_2
        {
            get { return p2; }
            set { p2 = value; }
        }

        public virtual T3 value_3
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

        public override T1 value_1
        {
            set {  }
        }
    }

    public class async_result_ignore_wrap<T1, T2> : async_result_wrap<T1, T2>
    {
        static public async_result_ignore_wrap<T1, T2> value = new async_result_ignore_wrap<T1, T2>();

        public override T1 value_1
        {
            set { }
        }

        public override T2 value_2
        {
            set { }
        }
    }

    public class async_result_ignore_wrap<T1, T2, T3> : async_result_wrap<T1, T2, T3>
    {
        static public async_result_ignore_wrap<T1, T2, T3> value = new async_result_ignore_wrap<T1, T2, T3>();

        public override T1 value_1
        {
            set { }
        }

        public override T2 value_2
        {
            set { }
        }

        public override T3 value_3
        {
            set { }
        }
    }

    public struct chan_pop_wrap<T>
    {
        public chan_async_state state;
        public T result;
    }

    public struct csp_invoke_wrap<T>
    {
        public chan_async_state state;
        public T result;
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

        public class stop_select_exception : System.Exception
        {
            public static readonly stop_select_exception val = new stop_select_exception();
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
            public object mailbox;
            public child agentAction;

            public mail_pck(object mb)
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
                Trace.Assert(_completed, "不对称的推入操作!");
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
        }
        LinkedList<call_stack_info> _callStack;
#endif

        static int _hashCount = 0;
        static long _idCount = 0;
        static Task _nilTask = new Task(functional.nil_action);
        static ReaderWriterLockSlim _nameMutex = new ReaderWriterLockSlim();
        static SortedList<string, generator> _nameGens = new SortedList<string, generator>();
        static static_init _init = new static_init();

        SortedList<long, mail_pck> _chanMap;
        functional.func<bool> _suspendCb;
        LinkedList<children> _children;
        mutli_callback _multiCb;
        shared_strand _strand;
        pull_task _pullTask;
        children _agentMap;
        async_timer _timer;
        object _selfValue;
        Task _syncNtf;
        string _name;
        long _yieldCount;
        long _id;
        int _lockCount;
        int _lockSuspendCount;
        int _lastTm;
        bool _beginQuit;
        bool _isSuspend;
        bool _holdSuspend;
        bool _hasBlock;
        bool _isForce;
        bool _isStop;
        bool _isRun;

        public delegate Task action();

        generator() { }

        static public generator make(shared_strand strand, action handler, functional.func callback = null, functional.func<bool> suspendCb = null)
        {
            return (new generator()).init(strand, handler, callback, suspendCb);
        }

        static public generator go(shared_strand strand, action handler, functional.func callback = null, functional.func<bool> suspendCb = null)
        {
            return (new generator()).init(strand, handler, callback, suspendCb).run();
        }

        static public generator tick_go(shared_strand strand, action handler, functional.func callback = null, functional.func<bool> suspendCb = null)
        {
            return (new generator()).init(strand, handler, callback, suspendCb).tick_run();
        }

        static public generator make(string name, shared_strand strand, action handler, functional.func callback = null, functional.func<bool> suspendCb = null)
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

        static public generator go(string name, shared_strand strand, action handler, functional.func callback = null, functional.func<bool> suspendCb = null)
        {
            return make(name, strand, handler, callback, suspendCb).run();
        }

        static public generator tick_go(string name, shared_strand strand, action handler, functional.func callback = null, functional.func<bool> suspendCb = null)
        {
            return make(name, strand, handler, callback, suspendCb).tick_run();
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
            string time = string.Format("{0}.{1}", DateTime.Now.ToLocalTime(), DateTime.Now.Millisecond);
            for (int i = 0; i < count; ++i, ++offset)
            {
                if (offset < sts.Count())
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
        generator init(shared_strand strand, action handler, functional.func callback, functional.func<bool> suspendCb, LinkedList<call_stack_info> callStack = null)
        {
            if (null != callStack)
            {
                _callStack = callStack;
            }
            else
            {
                _callStack = new LinkedList<call_stack_info>();
                up_stack_frame(_callStack, 1, 6);
            }
#else
        generator init(shared_strand strand, action handler, functional.func callback, functional.func<bool> suspendCb)
        {
#endif
            _id = Interlocked.Increment(ref _idCount);
            _isForce = false;
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
            _strand = strand;
            _suspendCb = suspendCb;
            _pullTask = new pull_task();
            _syncNtf = new Task(functional.nil_action);
            _timer = new async_timer(_strand);
            _strand.hold_work();
            _strand.distribute(async delegate ()
            {
                try
                {
                    _lockCount = 0;
                    await async_wait();
                    await handler();
                    if (null != _children)
                    {
                        if (null != _agentMap)
                        {
                            await _agentMap.stop();
                        }
                        while (0 != _children.Count)
                        {
                            await _children.First.Value.wait_all();
                        }
                    }
                }
                catch (stop_exception)
                {
                    _timer.cancel();
                    if (null != _children && 0 != _children.Count)
                    {
                        children[] childs = new children[_children.Count];
                        _children.CopyTo(childs, 0);
                        await children.stop(childs);
                    }
                }
                catch (System.Exception ec)
                {
                    _timer.cancel();
                    MessageBox.Show(String.Format("Message:\n{0}\n{1}", ec.Message, ec.StackTrace), "generator 内部未捕获的异常!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                if (null != _name)
                {
                    _nameMutex.EnterWriteLock();
                    _nameGens.Remove(_name);
                    _nameMutex.ExitWriteLock();
                }
                if (null != _agentMap)
                {
                    await _agentMap.stop();
                }
                _isStop = true;
                _suspendCb = null;
                functional.catch_invoke(callback);
                _syncNtf.RunSynchronously();
                _strand.release_work();
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
                tls_values tlsVal = shared_strand._runningTls.Value;
                generator oldGen = tlsVal.self;
                if (null == oldGen || !oldGen._pullTask.activated)
                {
                    tlsVal.self = this;
                    _pullTask.complete();
                    tlsVal.self = oldGen;
                }
                else
                {
                    oldGen._pullTask.activated = false;
                    tlsVal.self = this;
                    _pullTask.complete();
                    tlsVal.self = oldGen;
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

        public generator run()
        {
            _strand.distribute(delegate ()
            {
                if (-1 == _lockCount)
                {
                    tick_run();
                }
                else if (!_isRun && !_isStop)
                {
                    _isRun = true;
                    no_check_next();
                }
            });
            return this;
        }

        public generator tick_run()
        {
            _strand.post(delegate ()
            {
                if (!_isRun && !_isStop)
                {
                    _isRun = true;
                    no_check_next();
                }
            });
            return this;
        }

        private void _suspend_cb(bool isSuspend, functional.func cb = null, bool canSuspendCb = true)
        {
            if (null != _children && 0 != _children.Count)
            {
                int count = _children.Count;
                functional.func suspendCb = delegate ()
                {
                    if (0 == --count)
                    {
                        functional.catch_invoke(canSuspendCb ? _suspendCb : null, isSuspend);
                        functional.catch_invoke(cb);
                    }
                };
                children[] tempChildren = new children[_children.Count];
                _children.CopyTo(tempChildren, 0);
                foreach (children ele in tempChildren)
                {
                    ele.suspend(isSuspend, suspendCb);
                }
            }
            else
            {
                functional.catch_invoke(canSuspendCb ? _suspendCb : null, isSuspend);
                functional.catch_invoke(cb);
            }
        }

        private void _suspend(functional.func cb = null)
        {
            if (!_isStop && !_beginQuit && !_isSuspend)
            {
                if (0 == _lockSuspendCount)
                {
                    _isSuspend = true;
                    if (0 != _lastTm)
                    {
                        _lastTm -= (int)(system_tick.get_tick_ms() - _timer.cancel());
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

        public void tick_suspend(functional.func cb = null)
        {
            _strand.post(() => _suspend(cb));
        }

        public void suspend(functional.func cb = null)
        {
            _strand.distribute(delegate ()
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

        private void _resume(functional.func cb = null)
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
                            _timer.timeout(_lastTm, no_check_next);
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

        public void tick_resume(functional.func cb = null)
        {
            _strand.post(() => _resume(cb));
        }

        public void resume(functional.func cb = null)
        {
            _strand.distribute(delegate ()
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
            _strand.post(delegate ()
            {
                if (!_isStop)
                {
                    _stop();
                }
            });
        }

        public void stop()
        {
            if (_strand.running_in_this_thread())
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

        public bool is_force()
        {
#if DEBUG
            Trace.Assert(_isStop, "不正确的 is_force 调用，generator 还没有结束");
#endif
            return _isForce;
        }

        public void sync_wait()
        {
#if DEBUG
            Trace.Assert(_strand.wait_safe(), "不安全的 sync_wait 调用");
#endif
            _syncNtf.Wait();
        }

        public bool is_completed()
        {
            return _syncNtf.IsCompleted;
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

        static public async Task lock_stop(functional.func_res<Task> handler)
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

        static public async Task<R> lock_stop<R>(functional.func_res<Task<R>> handler)
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

        static public async Task lock_suspend(functional.func_res<Task> handler)
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

        static public async Task<R> lock_suspend<R>(functional.func_res<Task<R>> handler)
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

        public async Task async_wait()
        {
            await _pullTask;
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

        public Task async_wait(functional.func handler)
        {
            handler();
            return async_wait();
        }

        public async Task<R> wait_result<R>(functional.func<async_result_wrap<R>> handler)
        {
            async_result_wrap<R> res = new async_result_wrap<R>();
            handler(res);
            await async_wait();
            return res.value_1;
        }

        public functional.same_func async_same_callback()
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return delegate (object[] args)
            {
                _strand.distribute(beginQuit ? (functional.func)quit_next : no_quit_next);
            };
        }

        public functional.same_func async_same_callback(functional.same_func handler)
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return delegate (object[] args)
            {
                handler(args);
                _strand.distribute(beginQuit ? (functional.func)quit_next : no_quit_next);
            };
        }

        public functional.func async_callback(functional.func handler)
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return delegate ()
            {
                handler();
                _strand.distribute(beginQuit ? (functional.func)quit_next : no_quit_next);
            };
        }

        public functional.func<T1> async_callback<T1>(functional.func<T1> handler)
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1)
            {
                handler(p1);
                _strand.distribute(beginQuit ? (functional.func)quit_next : no_quit_next);
            };
        }

        public functional.func<T1, T2> async_callback<T1, T2>(functional.func<T1, T2> handler)
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2)
            {
                handler(p1, p2);
                _strand.distribute(beginQuit ? (functional.func)quit_next : no_quit_next);
            };
        }

        public functional.func<T1, T2, T3> async_callback<T1, T2, T3>(functional.func<T1, T2, T3> handler)
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                handler(p1, p2, p3);
                _strand.distribute(beginQuit ? (functional.func)quit_next : no_quit_next);
            };
        }

        public functional.same_func safe_async_same_callback()
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate (object[] args)
            {
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check())
                    {
                        next(beginQuit);
                    }
                });
            };
        }

        public functional.same_func safe_async_same_callback(functional.same_func handler)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate (object[] args)
            {
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        handler(args);
                        no_check_next();
                    }
                });
            };
        }

        public functional.func safe_async_callback(functional.func handler)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate ()
            {
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        handler();
                        no_check_next();
                    }
                });
            };
        }

        public functional.func<T1> safe_async_callback<T1>(functional.func<T1> handler)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1)
            {
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        handler(p1);
                        no_check_next();
                    }
                });
            };
        }

        public functional.func<T1, T2> safe_async_callback<T1, T2>(functional.func<T1, T2> handler)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2)
            {
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        handler(p1, p2);
                        no_check_next();
                    }
                });
            };
        }

        public functional.func<T1, T2, T3> safe_async_callback<T1, T2, T3>(functional.func<T1, T2, T3> handler)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        handler(p1, p2, p3);
                        no_check_next();
                    }
                });
            };
        }

        public functional.func async_result()
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return () => _strand.distribute(beginQuit ? (functional.func)quit_next : no_quit_next);
        }

        private functional.func _async_result()
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return () => (beginQuit ? (functional.func)quit_next : no_quit_next)();
        }

        public functional.func<T1> async_result<T1>(async_result_wrap<T1> res)
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1)
            {
                res.value_1 = p1;
                _strand.distribute(beginQuit ? (functional.func)quit_next : no_quit_next);
            };
        }

        public functional.func<T1, T2> async_result<T1, T2>(async_result_wrap<T1, T2> res)
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2)
            {
                res.value_1 = p1;
                res.value_2 = p2;
                _strand.distribute(beginQuit ? (functional.func)quit_next : no_quit_next);
            };
        }

        public functional.func<T1, T2, T3> async_result<T1, T2, T3>(async_result_wrap<T1, T2, T3> res)
        {
            _pullTask.new_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                res.value_1 = p1;
                res.value_2 = p2;
                res.value_3 = p3;
                _strand.distribute(beginQuit ? (functional.func)quit_next : no_quit_next);
            };
        }

        public functional.func<T1> async_ignore<T1>()
        {
            return async_result(async_result_ignore_wrap<T1>.value);
        }

        public functional.func<T1, T2> async_ignore<T1, T2>()
        {
            return async_result(async_result_ignore_wrap<T1, T2>.value);
        }

        public functional.func<T1, T2, T3> async_ignore<T1, T2, T3>()
        {
            return async_result(async_result_ignore_wrap<T1, T2, T3>.value);
        }

        public functional.func safe_async_result()
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate ()
            {
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check())
                    {
                        next(beginQuit);
                    }
                });
            };
        }

        public functional.func<T1> safe_async_result<T1>(async_result_wrap<T1> res)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1)
            {
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        res.value_1 = p1;
                        no_check_next();
                    }
                });
            };
        }

        public functional.func<T1, T2> safe_async_result<T1, T2>(async_result_wrap<T1, T2> res)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2)
            {
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        res.value_1 = p1;
                        res.value_2 = p2;
                        no_check_next();
                    }
                });
            };
        }

        public functional.func<T1, T2, T3> safe_async_result<T1, T2, T3>(async_result_wrap<T1, T2, T3> res)
        {
            mutli_callback multiCb = new_multi_task();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        res.value_1 = p1;
                        res.value_2 = p2;
                        res.value_3 = p3;
                        no_check_next();
                    }
                });
            };
        }

        public functional.func<T1> safe_async_ignore<T1>()
        {
            return safe_async_result(async_result_ignore_wrap<T1>.value);
        }

        public functional.func<T1, T2> safe_async_ignore<T1, T2>()
        {
            return safe_async_result(async_result_ignore_wrap<T1, T2>.value);
        }

        public functional.func<T1, T2, T3> safe_async_ignore<T1, T2, T3>()
        {
            return safe_async_result(async_result_ignore_wrap<T1, T2, T3>.value);
        }

        public functional.func timed_async_result(int ms, functional.func timedHandler = null)
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
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        no_check_next();
                    }
                });
            };
        }

        public functional.func<T1> timed_async_result<T1>(int ms, async_result_wrap<T1> res, functional.func timedHandler = null)
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
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        res.value_1 = p1;
                        no_check_next();
                    }
                });
            };
        }

        public functional.func<T1, T2> timed_async_result<T1, T2>(int ms, async_result_wrap<T1, T2> res, functional.func timedHandler = null)
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
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        res.value_1 = p1;
                        res.value_2 = p2;
                        no_check_next();
                    }
                });
            };
        }

        public functional.func<T1, T2, T3> timed_async_result<T1, T2, T3>(int ms, async_result_wrap<T1, T2, T3> res, functional.func timedHandler = null)
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
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        res.value_1 = p1;
                        res.value_2 = p2;
                        res.value_3 = p3;
                        no_check_next();
                    }
                });
            };
        }
        
        public functional.func timed_async_result2(int ms, functional.func timedHandler = null)
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
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        no_check_next();
                    }
                });
            };
        }

        public functional.func<T1> timed_async_result2<T1>(int ms, async_result_wrap<T1> res, functional.func timedHandler = null)
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
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        res.value_1 = p1;
                        no_check_next();
                    }
                });
            };
        }

        public functional.func<T1, T2> timed_async_result2<T1, T2>(int ms, async_result_wrap<T1, T2> res, functional.func timedHandler = null)
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
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        res.value_1 = p1;
                        res.value_2 = p2;
                        no_check_next();
                    }
                });
            };
        }

        public functional.func<T1, T2, T3> timed_async_result2<T1, T2, T3>(int ms, async_result_wrap<T1, T2, T3> res, functional.func timedHandler = null)
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
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && _beginQuit == beginQuit)
                    {
                        _timer.cancel();
                        res.value_1 = p1;
                        res.value_2 = p2;
                        res.value_3 = p3;
                        no_check_next();
                    }
                });
            };
        }

        public functional.func<T1> timed_async_ignore<T1>(int ms, functional.func timedHandler = null)
        {
            return timed_async_result(ms, async_result_ignore_wrap<T1>.value, timedHandler);
        }

        public functional.func<T1, T2> timed_async_ignore<T1, T2>(int ms, functional.func timedHandler = null)
        {
            return timed_async_result(ms, async_result_ignore_wrap<T1, T2>.value, timedHandler);
        }

        public functional.func<T1, T2, T3> timed_async_ignore<T1, T2, T3>(int ms, functional.func timedHandler = null)
        {
            return timed_async_result(ms, async_result_ignore_wrap<T1, T2, T3>.value, timedHandler);
        }

        public functional.func<T1> timed_async_ignore2<T1>(int ms, functional.func timedHandler = null)
        {
            return timed_async_result2(ms, async_result_ignore_wrap<T1>.value, timedHandler);
        }

        public functional.func<T1, T2> timed_async_ignore2<T1, T2>(int ms, functional.func timedHandler = null)
        {
            return timed_async_result2(ms, async_result_ignore_wrap<T1, T2>.value, timedHandler);
        }

        public functional.func<T1, T2, T3> timed_async_ignore2<T1, T2, T3>(int ms, functional.func timedHandler = null)
        {
            return timed_async_result2(ms, async_result_ignore_wrap<T1, T2, T3>.value, timedHandler);
        }

        static public Task sleep(int ms)
        {
            if (ms > 0)
            {
                generator this_ = self;
                this_._lastTm = ms;
                this_._timer.timeout(ms, this_._async_result());
                return this_.async_wait();
            }
            else if (ms < 0)
            {
                return hold();
            }
            else
            {
                return yield();
            }
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
            this_._strand.post(this_._async_result());
            return this_.async_wait();
        }

        static public generator self
        {
            get
            {
                tls_values tlsVal = shared_strand._runningTls.Value;
                if (null != tlsVal)
                {
                    return tlsVal.self;
                }
                return null;
            }
        }

        static public shared_strand self_strand()
        {
            generator this_ = self;
            return null != this_ ? this_._strand : null;
        }

        public shared_strand strand
        {
            get
            {
                return _strand;
            }
        }

        static public long self_id()
        {
            generator this_ = self;
            return null != this_ ? this_._id : 0;
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

        static public async Task<chan_async_state> chan_push<T>(channel<T> chan, T p)
        {
            generator this_ = self;
            chan_async_state result = chan_async_state.async_undefined;
            chan.push(this_.async_same_callback(delegate (object[] args)
            {
                result = (chan_async_state)args[0];
            }), p);
            await this_.async_wait();
            return result;
        }

        static public Task<chan_async_state> chan_push(channel<void_type> chan)
        {
            return chan_push(chan, default(void_type));
        }

        static public Task<chan_pop_wrap<T>> chan_pop<T>(channel<T> chan)
        {
            return chan_pop(chan, broadcast_chan_token._defToken);
        }

        static public async Task<chan_pop_wrap<T>> chan_pop<T>(channel<T> chan, broadcast_chan_token token)
        {
            generator this_ = self;
            chan_pop_wrap<T> result = default(chan_pop_wrap<T>);
            chan.pop(this_.async_same_callback(delegate (object[] args)
            {
                result.state = (chan_async_state)args[0];
                if (chan_async_state.async_ok == result.state)
                {
                    result.result = (T)args[1];
                }
            }), token);
            await this_.async_wait();
            return result;
        }

        static public async Task<chan_async_state> chan_try_push<T>(channel<T> chan, T p)
        {
            generator this_ = self;
            chan_async_state result = chan_async_state.async_undefined;
            chan.try_push(this_.async_same_callback(delegate (object[] args)
            {
                result = (chan_async_state)args[0];
            }), p);
            await this_.async_wait();
            return result;
        }

        static public Task<chan_async_state> chan_try_push(channel<void_type> chan)
        {
            return chan_try_push(chan, default(void_type));
        }

        static public Task<chan_pop_wrap<T>> chan_try_pop<T>(channel<T> chan)
        {
            return chan_try_pop(chan, broadcast_chan_token._defToken);
        }

        static public async Task<chan_pop_wrap<T>> chan_try_pop<T>(channel<T> chan, broadcast_chan_token token)
        {
            generator this_ = self;
            chan_pop_wrap<T> result = default(chan_pop_wrap<T>);
            chan.try_pop(this_.async_same_callback(delegate (object[] args)
            {
                result.state = (chan_async_state)args[0];
                if (chan_async_state.async_ok == result.state)
                {
                    result.result = (T)args[1];
                }
            }), token);
            await this_.async_wait();
            return result;
        }

        static public async Task<chan_async_state> chan_timed_push<T>(int ms, channel<T> chan, T p)
        {
            generator this_ = self;
            chan_async_state result = chan_async_state.async_undefined;
            chan.timed_push(ms, this_.async_same_callback(delegate (object[] args)
            {
                result = (chan_async_state)args[0];
            }), p);
            await this_.async_wait();
            return result;
        }

        static public Task<chan_async_state> chan_timed_push(int ms, channel<void_type> chan)
        {
            return chan_timed_push(ms, chan, default(void_type));
        }

        static public Task<chan_pop_wrap<T>> chan_timed_pop<T>(int ms, channel<T> chan)
        {
            return chan_timed_pop(ms, chan, broadcast_chan_token._defToken);
        }

        static public async Task<chan_pop_wrap<T>> chan_timed_pop<T>(int ms, channel<T> chan, broadcast_chan_token token)
        {
            generator this_ = self;
            chan_pop_wrap<T> result = default(chan_pop_wrap<T>);
            chan.timed_pop(ms, this_.async_same_callback(delegate (object[] args)
            {
                result.state = (chan_async_state)args[0];
                if (chan_async_state.async_ok == result.state)
                {
                    result.result = (T)args[1];
                }
            }), token);
            await this_.async_wait();
            return result;
        }

        static public async Task<csp_invoke_wrap<R>> csp_invoke<R, T>(csp_chan<R, T> chan, T p)
        {
            generator this_ = self;
            csp_invoke_wrap<R> result = default(csp_invoke_wrap<R>);
            chan.push(this_.async_same_callback(delegate (object[] args)
            {
                result.state = (chan_async_state)args[0];
                if (chan_async_state.async_ok == result.state)
                {
                    result.result = (R)args[1];
                }
            }), p);
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
            chan.pop(this_.async_same_callback(delegate (object[] args)
            {
                result.state = (chan_async_state)args[0];
                if (chan_async_state.async_ok == result.state)
                {
                    result.result = (csp_chan<R, T>.csp_result)(args[1]);
                    result.msg = (T)args[2];
                }
            }));
            await this_.async_wait();
            return result;
        }

        static public async Task<chan_async_state> csp_wait<R, T>(csp_chan<R, T> chan, functional.func_res<Task<R>, T> handler)
        {
            csp_wait_wrap<R, T> result = await csp_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                result.complete(await handler(result.msg));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_wait<R>(csp_chan<R, void_type> chan, functional.func_res<Task<R>> handler)
        {
            csp_wait_wrap<R, void_type> result = await csp_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                result.complete(await handler());
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_wait<T>(csp_chan<void_type, T> chan, functional.func_res<Task, T> handler)
        {
            csp_wait_wrap<void_type, T> result = await csp_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                await handler(result.msg);
                result.complete(default(void_type));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_wait(csp_chan<void_type, void_type> chan, functional.func_res<Task> handler)
        {
            csp_wait_wrap<void_type, void_type> result = await csp_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                await handler();
                result.complete(default(void_type));
            }
            return result.state;
        }

        static public async Task<csp_invoke_wrap<R>> csp_try_invoke<R, T>(csp_chan<R, T> chan, T p)
        {
            generator this_ = self;
            csp_invoke_wrap<R> result = default(csp_invoke_wrap<R>);
            chan.try_push(this_.async_same_callback(delegate (object[] args)
            {
                result.state = (chan_async_state)args[0];
                if (chan_async_state.async_ok == result.state)
                {
                    result.result = (R)args[1];
                }
            }), p);
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
            chan.try_pop(this_.async_same_callback(delegate (object[] args)
            {
                result.state = (chan_async_state)args[0];
                if (chan_async_state.async_ok == result.state)
                {
                    result.result = (csp_chan<R, T>.csp_result)(args[1]);
                    result.msg = (T)args[2];
                }
            }));
            await this_.async_wait();
            return result;
        }

        static public async Task<chan_async_state> csp_try_wait<R, T>(csp_chan<R, T> chan, functional.func_res<Task<R>, T> handler)
        {
            csp_wait_wrap<R, T> result = await csp_try_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                result.complete(await handler(result.msg));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_try_wait<R>(csp_chan<R, void_type> chan, functional.func_res<Task> handler)
        {
            csp_wait_wrap<R, void_type> result = await csp_try_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                await handler();
                result.complete(default(R));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_try_wait<T>(csp_chan<void_type, T> chan, functional.func_res<Task, T> handler)
        {
            csp_wait_wrap<void_type, T> result = await csp_try_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                await handler(result.msg);
                result.complete(default(void_type));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_try_wait(csp_chan<void_type, void_type> chan, functional.func_res<Task> handler)
        {
            csp_wait_wrap<void_type, void_type> result = await csp_try_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                await handler();
                result.complete(default(void_type));
            }
            return result.state;
        }

        static public async Task<csp_invoke_wrap<R>> csp_timed_invoke<R, T>(int ms, csp_chan<R, T> chan, T p)
        {
            generator this_ = self;
            csp_invoke_wrap<R> result = default(csp_invoke_wrap<R>);
            chan.timed_push(ms, this_.async_same_callback(delegate (object[] args)
            {
                result.state = (chan_async_state)args[0];
                if (chan_async_state.async_ok == result.state)
                {
                    result.result = (R)args[1];
                }
            }), p);
            await this_.async_wait();
            return result;
        }

        static public Task<csp_invoke_wrap<R>> csp_timed_invoke<R>(int ms, csp_chan<R, void_type> chan)
        {
            return csp_timed_invoke(ms, chan, default(void_type));
        }

        static public async Task<csp_wait_wrap<R, T>> csp_timed_wait<R, T>(int ms, csp_chan<R, T> chan)
        {
            generator this_ = self;
            csp_wait_wrap<R, T> result = default(csp_wait_wrap<R, T>);
            chan.timed_pop(ms, this_.async_same_callback(delegate (object[] args)
            {
                result.state = (chan_async_state)args[0];
                if (chan_async_state.async_ok == result.state)
                {
                    result.result = (csp_chan<R, T>.csp_result)(args[1]);
                    result.msg = (T)args[2];
                }
            }));
            await this_.async_wait();
            return result;
        }

        static public async Task<chan_async_state> csp_timed_wait<R, T>(int ms, csp_chan<R, T> chan, functional.func_res<Task<R>, T> handler)
        {
            csp_wait_wrap<R, T> result = await csp_timed_wait(ms, chan);
            if (chan_async_state.async_ok == result.state)
            {
                result.complete(await handler(result.msg));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_timed_wait<R>(int ms, csp_chan<R, void_type> chan, functional.func_res<Task<R>> handler)
        {
            csp_wait_wrap<R, void_type> result = await csp_timed_wait(ms, chan);
            if (chan_async_state.async_ok == result.state)
            {
                result.complete(await handler());
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_timed_wait<T>(int ms, csp_chan<void_type, T> chan, functional.func_res<Task, T> handler)
        {
            csp_wait_wrap<void_type, T> result = await csp_timed_wait(ms, chan);
            if (chan_async_state.async_ok == result.state)
            {
                await handler(result.msg);
                result.complete(default(void_type));
            }
            return result.state;
        }

        static public async Task<chan_async_state> csp_timed_wait(int ms, csp_chan<void_type, void_type> chan, functional.func_res<Task> handler)
        {
            csp_wait_wrap<void_type, void_type> result = await csp_timed_wait(ms, chan);
            if (chan_async_state.async_ok == result.state)
            {
                await handler();
                result.complete(default(void_type));
            }
            return result.state;
        }

        static public void stop_select()
        {
            throw stop_select_exception.val;
        }

        static public Task nil_wait()
        {
            return _nilTask;
        }

        static public async Task select_chans(params select_chan_base[] chans)
        {
            generator this_ = self;
            msg_buff<select_chan_base> selectChans = new msg_buff<select_chan_base>(this_._strand);
            foreach (select_chan_base chan in chans)
            {
                chan.ntfSign._selectOnce = false;
                chan.nextSelect = selectChans;
                chan.begin();
            }
            try
            {
                int count = chans.Count();
                while (true)
                {
                    select_chan_base selectedChan = (await chan_pop(selectChans)).result;
                    if (selectedChan.disabled())
                    {
                        continue;
                    }
                    count--;
                    select_chan_state selState = await selectedChan.invoke();
                    if (selState.nextRound)
                    {
                        count++;
                    }
                    if (0 == count)
                    {
                        break;
                    }
                }
            }
            catch (stop_select_exception) { }
            finally
            {
                lock_stop();
                foreach (select_chan_base chan in chans)
                {
                    await chan.end();
                }
                unlock_stop();
            }
        }

        static public async Task select_chans_once(params select_chan_base[] chans)
        {
            generator this_ = self;
            msg_buff<select_chan_base> selectChans = new msg_buff<select_chan_base>(this_._strand);
            foreach (select_chan_base chan in chans)
            {
                chan.ntfSign._selectOnce = true;
                chan.nextSelect = selectChans;
                chan.begin();
            }
            bool selected = false;
            try
            {
                select_chan_state selState = default(select_chan_state);
                do
                {
                    select_chan_base selectedChan = (await chan_pop(selectChans)).result;
                    if (selectedChan.disabled())
                    {
                        continue;
                    }
                    selState = await selectedChan.invoke(async delegate()
                    {
                        foreach (select_chan_base chan in chans)
                        {
                            if (selectedChan != chan)
                            {
                                await chan.end();
                            }
                        }
                        selected = true;
                    });
                } while (selState.failed);
            }
            catch (stop_select_exception) { }
            finally
            {
                if (!selected)
                {
                    lock_stop();
                    foreach (select_chan_base chan in chans)
                    {
                        await chan.end();
                    }
                    unlock_stop();
                }
            }
        }

        static public async Task timed_select_chans(int ms, functional.func_res<Task> timedHandler, params select_chan_base[] chans)
        {
            generator this_ = self;
            msg_buff<select_chan_base> selectChans = new msg_buff<select_chan_base>(this_._strand);
            this_._timer.timeout(ms, () => selectChans.post(null));
            foreach (select_chan_base chan in chans)
            {
                chan.ntfSign._selectOnce = true;
                chan.nextSelect = selectChans;
                chan.begin();
            }
            bool selected = false;
            try
            {
                select_chan_state selState = default(select_chan_state);
                do
                {
                    select_chan_base selectedChan = (await chan_pop(selectChans)).result;
                    if (null != selectedChan)
                    {
                        if (selectedChan.disabled())
                        {
                            continue;
                        }
                        selState = await selectedChan.invoke(async delegate ()
                        {
                            this_._timer.cancel();
                            foreach (select_chan_base chan in chans)
                            {
                                if (selectedChan != chan)
                                {
                                    await chan.end();
                                }
                            }
                            selected = true;
                        });
                    }
                    else
                    {
                        foreach (select_chan_base chan in chans)
                        {
                            await chan.end();
                        }
                        selected = true;
                        await timedHandler();
                        break;
                    }
                } while (selState.failed);
            }
            catch (stop_select_exception) { }
            finally
            {
                if (!selected)
                {
                    this_._timer.cancel();
                    lock_stop();
                    foreach (select_chan_base chan in chans)
                    {
                        await chan.end();
                    }
                    unlock_stop();
                }
            }
        }

        static public select_chan_base case_read<T>(channel<T> chan, functional.func_res<Task, T> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_reader(handler, errHandler);
        }

        static public select_chan_base case_read(channel<void_type> chan, functional.func_res<Task> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_reader((void_type _) => handler(), errHandler);
        }

        static public select_chan_base case_write<T>(channel<T> chan, functional.func_res<T> msg, functional.func_res<Task> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_writer(msg, handler, errHandler);
        }

        static public select_chan_base case_write<T>(channel<T> chan, async_result_wrap<T> msg, functional.func_res<Task> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_writer(msg, handler, errHandler);
        }

        static public select_chan_base case_write<T>(channel<T> chan, T msg, functional.func_res<Task> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_writer(msg, handler, errHandler);
        }

        static public select_chan_base case_write(channel<void_type> chan, functional.func_res<Task> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_writer(default(void_type), handler, errHandler);
        }

        static public select_chan_base case_read<T>(broadcast_chan<T> chan, functional.func_res<Task, T> handler, broadcast_chan_token token)
        {
            return chan.make_select_reader(handler, token);
        }

        static public select_chan_base case_read<T>(broadcast_chan<T> chan, functional.func_res<Task, T> handler, functional.func_res<Task<bool>, chan_async_state> errHandler, broadcast_chan_token token)
        {
            return chan.make_select_reader(handler, errHandler, token);
        }

        static public select_chan_base case_read(broadcast_chan<void_type> chan, functional.func_res<Task> handler, broadcast_chan_token token)
        {
            return chan.make_select_reader((void_type _) => handler(), token);
        }

        static public select_chan_base case_read(broadcast_chan<void_type> chan, functional.func_res<Task> handler, functional.func_res<Task<bool>, chan_async_state> errHandler, broadcast_chan_token token)
        {
            return chan.make_select_reader((void_type _) => handler(), errHandler, token);
        }

        static public select_chan_base case_read<R, T>(csp_chan<R, T> chan, functional.func_res<Task, csp_chan<R, T>.csp_result, T> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_reader(handler, errHandler);
        }

        static public select_chan_base case_read<R, T>(csp_chan<R, T> chan, functional.func_res<Task<R>, T> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_reader(async (csp_chan<R, T>.csp_result res, T msg) => res.complete(await handler(msg)), errHandler);
        }

        static public select_chan_base case_read<R>(csp_chan<R, void_type> chan, functional.func_res<Task<R>> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_reader(async (csp_chan<R, void_type>.csp_result res, void_type _) => res.complete(await handler()), errHandler);
        }

        static public select_chan_base case_read<T>(csp_chan<void_type, T> chan, functional.func_res<Task, T> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_reader(async (csp_chan<void_type, T>.csp_result res, T msg) => { await handler(msg); res.complete(default(void_type)); }, errHandler);
        }

        static public select_chan_base case_read(csp_chan<void_type, void_type> chan, functional.func_res<Task> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_reader(async (csp_chan<void_type, void_type>.csp_result res, void_type _) => { await handler(); res.complete(default(void_type)); }, errHandler);
        }

        static public select_chan_base case_write<R, T>(csp_chan<R, T> chan, functional.func_res<T> msg, functional.func_res<Task, R> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_writer(msg, handler, errHandler);
        }

        static public select_chan_base case_write<R, T>(csp_chan<R, T> chan, async_result_wrap<T> msg, functional.func_res<Task, R> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_writer(msg, handler, errHandler);
        }

        static public select_chan_base case_write<R, T>(csp_chan<R, T> chan, T msg, functional.func_res<Task, R> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_writer(msg, handler, errHandler);
        }

        static public select_chan_base case_write<R>(csp_chan<R, void_type> chan, functional.func_res<Task, R> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_writer(default(void_type), handler, errHandler);
        }

        static public select_chan_base case_write<T>(csp_chan<void_type, T> chan, functional.func_res<T> msg, functional.func_res<Task> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_writer(msg, (void_type _) => handler(), errHandler);
        }

        static public select_chan_base case_write<T>(csp_chan<void_type, T> chan, async_result_wrap<T> msg, functional.func_res<Task> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_writer(msg, (void_type _) => handler(), errHandler);
        }

        static public select_chan_base case_write<T>(csp_chan<void_type, T> chan, T msg, functional.func_res<Task> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_writer(msg, (void_type _) => handler(), errHandler);
        }

        static public select_chan_base case_write(csp_chan<void_type, void_type> chan, functional.func_res<Task> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_writer(default(void_type), (void_type _) => handler(), errHandler);
        }

        static public Task close_chan<T>(channel<T> chan)
        {
            generator this_ = self;
            chan.close(this_.async_same_callback());
            return this_.async_wait();
        }

        static public Task cancel_chan<T>(channel<T> chan)
        {
            generator this_ = self;
            chan.cancel(this_.async_same_callback());
            return this_.async_wait();
        }

        static public Task mutex_lock(mutex_base mtx)
        {
            generator this_ = self;
            mtx.Lock(this_._id, this_.async_result());
            return this_.async_wait();
        }

        static public async Task mutex_lock(mutex_base mtx, functional.func_res<Task> handler)
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

        static public async Task<R> mutex_lock<R>(mutex_base mtx, functional.func_res<Task<R>> handler)
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

        static public async Task<bool> mutex_try_lock(mutex_base mtx)
        {
            generator this_ = self;
            return chan_async_state.async_ok == await this_.wait_result(delegate (async_result_wrap<chan_async_state> res)
            {
                mtx.try_lock(this_._id, this_.async_result(res));
            });
        }

        static public async Task<bool> mutex_try_lock(mutex_base mtx, functional.func_res<Task> handler)
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

        static public async Task<bool> mutex_timed_lock(int ms, mutex_base mtx)
        {
            generator this_ = self;
            return chan_async_state.async_ok == await this_.wait_result(delegate (async_result_wrap<chan_async_state> res)
            {
                mtx.timed_lock(this_._id, ms, this_.async_result(res));
            });
        }

        static public async Task<bool> mutex_timed_lock(int ms, mutex_base mtx, functional.func_res<Task> handler)
        {
            if (await mutex_timed_lock(ms, mtx))
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

        static public Task mutex_unlock(mutex_base mtx)
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

        static public async Task mutex_lock_shared(shared_mutex mtx, functional.func_res<Task> handler)
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

        static public async Task<R> mutex_lock_shared<R>(shared_mutex mtx, functional.func_res<Task<R>> handler)
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

        static public async Task mutex_lock_pess_shared(shared_mutex mtx, functional.func_res<Task> handler)
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

        static public async Task<R> mutex_lock_pess_shared<R>(shared_mutex mtx, functional.func_res<Task<R>> handler)
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

        static public async Task mutex_lock_upgrade(shared_mutex mtx, functional.func_res<Task> handler)
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

        static public async Task<R> mutex_lock_upgrade<R>(shared_mutex mtx, functional.func_res<Task<R>> handler)
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
            return chan_async_state.async_ok == await this_.wait_result(delegate (async_result_wrap<chan_async_state> res)
            {
                mtx.try_lock_shared(this_._id, this_.async_result(res));
            });
        }

        static public async Task<bool> mutex_try_lock_shared(shared_mutex mtx, functional.func_res<Task> handler)
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
            return chan_async_state.async_ok == await this_.wait_result(delegate (async_result_wrap<chan_async_state> res)
            {
                mtx.try_lock_upgrade(this_._id, this_.async_result(res));
            });
        }

        static public async Task<bool> mutex_try_lock_upgrade(shared_mutex mtx, functional.func_res<Task> handler)
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

        static public async Task<bool> mutex_timed_lock_shared(int ms, shared_mutex mtx)
        {
            generator this_ = self;
            return chan_async_state.async_ok == await this_.wait_result(delegate (async_result_wrap<chan_async_state> res)
            {
                mtx.timed_lock_shared(this_._id, ms, this_.async_result(res));
            });
        }

        static public async Task<bool> mutex_timed_lock_shared(int ms, shared_mutex mtx, functional.func_res<Task> handler)
        {
            if (await mutex_timed_lock_shared(ms, mtx))
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

        static public Task condition_wait(condition_variable conVar, mutex_base mutex)
        {
            generator this_ = self;
            conVar.wait(this_._id, mutex, this_.async_result());
            return this_.async_wait();
        }

        static public async Task<bool> condition_timed_wait(int ms, condition_variable conVar, mutex_base mutex)
        {
            generator this_ = self;
            return chan_async_state.async_ok == await this_.wait_result(delegate (async_result_wrap<chan_async_state> res)
            {
                conVar.timed_wait(this_._id, ms, mutex, this_.async_result(res));
            });
        }

        static public async Task send_strand(shared_strand strand, functional.func handler)
        {
            generator this_ = self;
            if (this_._strand == strand)
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

        static public async Task<R> send_strand<R>(shared_strand strand, functional.func_res<R> handler)
        {
            generator this_ = self;
            if (this_._strand == strand)
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

        static public functional.func_res<Task> wrap_send_strand(shared_strand strand, functional.func handler)
        {
            return () => send_strand(strand, handler);
        }

        static public functional.func_res<Task, T> wrap_send_strand<T>(shared_strand strand, functional.func<T> handler)
        {
            return (T p) => send_strand(strand, () => handler(p));
        }

        static public functional.func_res<Task<R>> wrap_send_strand<R>(shared_strand strand, functional.func_res<R> handler)
        {
            return async delegate ()
            {
                R res = default(R);
                await send_strand(strand, () => res = handler());
                return res;
            };
        }

        static public functional.func_res<Task<R>, T> wrap_send_strand<R, T>(shared_strand strand, functional.func_res<R, T> handler)
        {
            return async delegate (T p)
            {
                R res = default(R);
                await send_strand(strand, () => res = handler(p));
                return res;
            };
        }
        
        static public void post_control(Control ctrl, functional.func handler)
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

        static public async Task send_control(Control ctrl, functional.func handler)
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

        static public async Task<R> send_control<R>(Control ctrl, functional.func_res<R> handler)
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

        static public functional.func wrap_post_control(Control ctrl, functional.func handler)
        {
            return () => post_control(ctrl, handler);
        }

        static public functional.func_res<Task> wrap_send_control(Control ctrl, functional.func handler)
        {
            return () => send_control(ctrl, handler);
        }

        static public functional.func_res<Task, T> wrap_send_control<T>(Control ctrl, functional.func<T> handler)
        {
            return (T p) => send_control(ctrl, () => handler(p));
        }

        static public functional.func_res<Task<R>> wrap_send_control<R>(Control ctrl, functional.func_res<R> handler)
        {
            return async delegate ()
            {
                R res = default(R);
                await send_control(ctrl, () => res = handler());
                return res;
            };
        }

        static public functional.func_res<Task<R>, T> wrap_send_control<R, T>(Control ctrl, functional.func_res<R, T> handler)
        {
            return async delegate (T p)
            {
                R res = default(R);
                await send_control(ctrl, () => res = handler(p));
                return res;
            };
        }

        static public async Task send_task(functional.func handler)
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
                this_._strand.post(beginQuit ? (functional.func)this_.quit_next : this_.no_quit_next);
            });
            await this_.async_wait();
            if (null != hasExcep)
            {
                throw hasExcep;
            }
        }

        static public async Task<R> send_task<R>(functional.func_res<R> handler)
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
                this_._strand.post(beginQuit ? (functional.func)this_.quit_next : this_.no_quit_next);
            });
            await this_.async_wait();
            if (null != hasExcep)
            {
                throw hasExcep;
            }
            return res;
        }

        static public functional.func_res<Task> wrap_send_task(functional.func handler)
        {
            return () => send_task(handler);
        }

        static public functional.func_res<Task, T> wrap_send_task<T>(functional.func<T> handler)
        {
            return (T p) => send_task(() => handler(p));
        }

        static public functional.func_res<Task<R>> wrap_send_task<R>(functional.func_res<R> handler)
        {
            return async delegate ()
            {
                R res = default(R);
                await send_task(() => res = handler());
                return res;
            };
        }

        static public functional.func_res<Task<R>, T> wrap_send_task<R, T>(functional.func_res<R, T> handler)
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

        static public functional.func_res<Task> wrap_send_async_queue(async_queue queue, shared_strand strand, generator.action action)
        {
            return () => send_async_queue(queue, strand, action);
        }

        static public functional.func_res<Task, T> wrap_send_async_queue<T>(async_queue queue, shared_strand strand, functional.func_res<Task, T> action)
        {
            return (T p) => send_async_queue(queue, strand, () => action(p));
        }

        static public functional.func_res<Task<R>> wrap_send_async_queue<R>(async_queue queue, shared_strand strand, functional.func_res<Task<R>> action)
        {
            return async delegate ()
            {
                R res = default(R);
                await send_async_queue(queue, strand, async () => res = await action());
                return res;
            };
        }

        static public functional.func_res<Task<R>, T> wrap_send_async_queue<R, T>(async_queue queue, shared_strand strand, functional.func_res<Task<R>, T> action)
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

        static public functional.func_res<Task> wrap_send_async_strand(async_strand queue, generator.action action)
        {
            return () => send_async_strand(queue, action);
        }

        static public functional.func_res<Task, T> wrap_send_async_strand<T>(async_strand queue, functional.func_res<Task, T> action)
        {
            return (T p) => send_async_strand(queue, () => action(p));
        }

        static public functional.func_res<Task<R>> wrap_send_async_strand<R>(async_strand queue, functional.func_res<Task<R>> action)
        {
            return async delegate ()
            {
                R res = default(R);
                await send_async_strand(queue, async () => res = await action());
                return res;
            };
        }

        static public functional.func_res<Task<R>, T> wrap_send_async_strand<R, T>(async_strand queue, functional.func_res<Task<R>, T> action)
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
                functional.func continuation = this_.async_result();
                task.GetAwaiter().OnCompleted(() => continuation());
                return this_.async_wait();
            }
            return nil_wait();
        }

        static public async Task<bool> timed_wait_task(int ms, Task task)
        {
            if (!task.IsCompleted)
            {
                bool overtime = false;
                generator this_ = self;
                functional.func continuation = this_.timed_async_result2(ms, () => overtime = true);
                task.GetAwaiter().OnCompleted(() => continuation());
                await this_.async_wait();
                return !overtime;
            }
            return true;
        }

        static public Task wait_other(generator otherGen)
        {
            return wait_task(otherGen._syncNtf);
        }

        static public Task<bool> timed_wait_other(int ms, generator otherGen)
        {
            return timed_wait_task(ms, otherGen._syncNtf);
        }

        static public Task async_call(functional.func<functional.func> handler)
        {
            generator this_ = self;
            handler(this_.async_result());
            return this_.async_wait();
        }

        static public async Task<R> async_call<R>(functional.func<functional.func<R>> handler)
        {
            generator this_ = self;
            async_result_wrap<R> res = new async_result_wrap<R>();
            handler(this_.async_result(res));
            await this_.async_wait();
            return res.value_1;
        }

        static public async Task<async_result_wrap<R1, R2>> async_call<R1, R2>(functional.func<functional.func<R1, R2>> handler)
        {
            generator this_ = self;
            async_result_wrap<R1, R2> res = new async_result_wrap<R1, R2>();
            handler(this_.async_result(res));
            await this_.async_wait();
            return res;
        }

        static public async Task<async_result_wrap<R1, R2, R3>> async_call<R1, R2, R3>(functional.func<functional.func<R1, R2, R3>> handler)
        {
            generator this_ = self;
            async_result_wrap<R1, R2, R3> res = new async_result_wrap<R1, R2, R3>();
            handler(this_.async_result(res));
            await this_.async_wait();
            return res;
        }

        static public Task safe_async_call(functional.func<functional.func> handler)
        {
            generator this_ = self;
            handler(this_.safe_async_result());
            return this_.async_wait();
        }

        static public async Task<R> safe_async_call<R>(functional.func<functional.func<R>> handler)
        {
            generator this_ = self;
            async_result_wrap<R> res = new async_result_wrap<R>();
            handler(this_.safe_async_result(res));
            await this_.async_wait();
            return res.value_1;
        }

        static public async Task<async_result_wrap<R1, R2>> safe_async_call<R1, R2>(functional.func<functional.func<R1, R2>> handler)
        {
            generator this_ = self;
            async_result_wrap<R1, R2> res = new async_result_wrap<R1, R2>();
            handler(this_.safe_async_result(res));
            await this_.async_wait();
            return res;
        }

        static public async Task<async_result_wrap<R1, R2, R3>> safe_async_call<R1, R2, R3>(functional.func<functional.func<R1, R2, R3>> handler)
        {
            generator this_ = self;
            async_result_wrap<R1, R2, R3> res = new async_result_wrap<R1, R2, R3>();
            handler(this_.safe_async_result(res));
            await this_.async_wait();
            return res;
        }

        static public async Task<bool> timed_async_call(int ms, functional.func<functional.func> handler, functional.func timedHandler = null)
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

        static public async Task<bool> timed_async_call<R>(int ms, async_result_wrap<R> res, functional.func<functional.func<R>> handler, functional.func timedHandler = null)
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

        static public async Task<bool> timed_async_call<R1, R2>(int ms, async_result_wrap<R1, R2> res, functional.func<functional.func<R1, R2>> handler, functional.func timedHandler = null)
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

        static public async Task<bool> timed_async_call<R1, R2, R3>(int ms, async_result_wrap<R1, R2, R3> res, functional.func<functional.func<R1, R2, R3>> handler, functional.func timedHandler = null)
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
            up_stack_frame(this_._callStack, 2);
            await handler();
            this_._callStack.RemoveFirst();
        }

        static public async Task<R> call<R>(functional.func_res<Task<R>> handler)
        {
            generator this_ = self;
            up_stack_frame(this_._callStack, 2);
            R res = await handler();
            this_._callStack.RemoveFirst();
            return res;
        }

        static public async Task depth_call(shared_strand strand, action handler)
        {
            generator this_ = self;
            up_stack_frame(this_._callStack, 2);
            (new generator()).init(strand, handler, this_.async_result(), null, this_._callStack).run();
            await lock_stop(() => this_.async_wait());
            this_._callStack.RemoveFirst();
        }

        static public async Task<R> depth_call<R>(shared_strand strand, functional.func_res<Task<R>> handler)
        {
            generator this_ = self;
            R res = default(R);
            up_stack_frame(this_._callStack, 2);
            (new generator()).init(strand, async () => res = await handler(), this_.async_result(), null, this_._callStack).run();
            await lock_stop(() => this_.async_wait());
            this_._callStack.RemoveFirst();
            return res;
        }
#else
        static public Task call(action handler)
        {
            return handler();
        }

        static public Task<R> call<R>(functional.func_res<Task<R>> handler)
        {
            return handler();
        }

        static public Task depth_call(shared_strand strand, action handler)
        {
            generator this_ = self;
            go(strand, handler, this_.async_result());
            return lock_stop(() => this_.async_wait());
        }

        static public async Task<R> depth_call<R>(shared_strand strand, functional.func_res<Task<R>> handler)
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
                return self._callStack;
            }
        }
#endif

        static long calc_hash<T>(int id)
        {
            return (long)id << 32 | (uint)type_hash<T>.code;
        }

        static public msg_buff<T> self_mailbox<T>(int id = 0)
        {
            generator this_ = self;
            if (null == this_._chanMap)
            {
                this_._chanMap = new SortedList<long, mail_pck>();
            }
            mail_pck mb = null;
            if (!this_._chanMap.TryGetValue(calc_hash<T>(id), out mb))
            {
                mb = new mail_pck(new msg_buff<T>(this_._strand));
                this_._chanMap.Add(calc_hash<T>(id), mb);
            }
            return (msg_buff<T>)mb.mailbox;
        }

        public void get_mailbox<T>(functional.func<msg_buff<T>> cb, int id = 0)
        {
            _strand.distribute(delegate ()
            {
                if (null == _chanMap)
                {
                    _chanMap = new SortedList<long, mail_pck>();
                }
                mail_pck mb = null;
                if (!_chanMap.TryGetValue(calc_hash<T>(id), out mb))
                {
                    mb = new mail_pck(new msg_buff<T>(_strand));
                    _chanMap.Add(calc_hash<T>(id), mb);
                }
                functional.catch_invoke(cb, (msg_buff<T>)mb.mailbox);
            });
        }

        public async Task<msg_buff<T>> get_mailbox<T>(int id = 0)
        {
            generator host_ = self;
            async_result_wrap<msg_buff<T>> res = new async_result_wrap<msg_buff<T>>();
            get_mailbox(host_.async_result(res), id);
            await host_.async_wait();
            return res.value_1;
        }

        static public async Task agent_mail<T>(child agentChild, int id = 0)
        {
            generator this_ = self;
            if (null == this_._agentMap)
            {
                this_._agentMap = new children();
            }
            if (null == this_._chanMap)
            {
                this_._chanMap = new SortedList<long, mail_pck>();
            }
            mail_pck mb = null;
            if (!this_._chanMap.TryGetValue(calc_hash<T>(id), out mb))
            {
                mb = new mail_pck(new msg_buff<T>(this_._strand));
                this_._chanMap.Add(calc_hash<T>(id), mb);
            }
            else if (null != mb.agentAction)
            {
                await this_._agentMap.stop(mb.agentAction);
            }
            mb.agentAction = this_._agentMap.go(async delegate ()
            {
                msg_buff<T> childMb = await agentChild.get_mailbox<T>();
                msg_buff<T> selfMb = (msg_buff<T>)mb.mailbox;
                chan_notify_sign ntfSign = new chan_notify_sign();
                generator self = generator.self;
                try
                {
                    while (true)
                    {
                        selfMb.append_pop_notify(self.async_same_callback(), ntfSign);
                        await self.async_wait();
                        try
                        {
                            lock_suspend();
                            lock_stop();
                            chan_pop_wrap<T> popRes = await chan_try_pop(selfMb);
                            if (chan_async_state.async_ok == popRes.state)
                            {
                                popRes.state = await chan_push(childMb, popRes.result);
                            }
                            if (chan_async_state.async_closed == popRes.state)
                            {
                                break;
                            }
                        }
                        finally
                        {
                            unlock_stop();
                            await unlock_suspend();
                        }
                    }
                }
                catch (stop_exception)
                {
                    selfMb.remove_pop_notify(self.async_same_callback(), ntfSign);
                    await self.async_wait();
                }
            });
        }

        static public async Task agent_stop<T>(int id = 0)
        {
            generator this_ = self;
            mail_pck mb = null;
            if (null != this_._agentMap && null != this_._chanMap &&
                this_._chanMap.TryGetValue(calc_hash<T>(id), out mb) && null != mb.agentAction)
            {
                await this_._agentMap.stop(mb.agentAction);
                mb.agentAction = null;
            }
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
            LinkedList<functional.func> _callbacks;
            LinkedListNode<child> _childNode;

            public child() : base()
            {
                _callbacks = new LinkedList<functional.func>();
            }

            static new public child make(shared_strand strand, action handler, functional.func callback = null, functional.func<bool> suspendCb = null)
            {
                child newGen = new child();
                newGen._callbacks.AddFirst(callback);
                newGen.init(strand, handler, delegate ()
                {
                    while (0 != newGen._callbacks.Count)
                    {
                        functional.catch_invoke(newGen._callbacks.First.Value);
                        newGen._callbacks.RemoveFirst();
                    }
                }, suspendCb);
                return newGen;
            }

            static new public child go(shared_strand strand, action handler, functional.func callback = null, functional.func<bool> suspendCb = null)
            {
                return make(strand, handler, callback, suspendCb).run();
            }

            static new public child tick_go(shared_strand strand, action handler, functional.func callback = null, functional.func<bool> suspendCb = null)
            {
                return make(strand, handler, callback, suspendCb).tick_run();
            }

            public LinkedListNode<child> node
            {
                get
                {
                    return _childNode;
                }
                set
                {
                    _childNode = value;
                }
            }

            public new child run()
            {
                return (child)base.run();
            }

            public new child tick_run()
            {
                return (child)base.tick_run();
            }

            public void delay_stop(functional.func cb)
            {
                _strand.post(delegate ()
                {
                    if (!_isStop)
                    {
                        _callbacks.AddLast(cb);
                        _stop();
                    }
                    else
                    {
                        cb();
                    }
                });
            }

            public void stop(functional.func cb)
            {
                if (_strand.running_in_this_thread())
                {
                    if (-1 == _lockCount)
                    {
                        delay_stop(cb);
                    }
                    else if (!_isStop)
                    {
                        _callbacks.AddLast(cb);
                        _stop();
                    }
                    else
                    {
                        cb();
                    }
                }
                else
                {
                    delay_stop(cb);
                }
            }

            public void append_stop_callback(functional.func cb)
            {
                _strand.distribute(delegate ()
                {
                    if (!_isStop)
                    {
                        _callbacks.AddLast(cb);
                    }
                    else
                    {
                        cb();
                    }
                });
            }

            public new Task stop()
            {
                generator parent = self;
                stop(parent.async_result());
                return parent.async_wait();
            }

            public Task wait()
            {
                generator parent = self;
                append_stop_callback(parent.async_result());
                return parent.async_wait();
            }

            public async Task<bool> timed_wait(int ms)
            {
                generator parent = self;
                bool overtime = false;
                append_stop_callback(parent.timed_async_result2(ms, () => overtime = true));
                await parent.async_wait();
                return !overtime;
            }
        }

        public class children
        {
            bool _ignoreSuspend;
            generator _parent;
            LinkedList<child> _children;
            LinkedListNode<children> _node;

            public children()
            {
                _parent = self;
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
                if (0 == _children.Count)
                {
                    _parent._children.Remove(_node);
                    _node = null;
                }
            }

            public child make(shared_strand strand, action handler, functional.func callback = null, functional.func<bool> suspendCb = null)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                check_append_node();
                child newGen = child.make(strand, handler, callback, suspendCb);
                newGen.node = _children.AddLast(newGen);
                return newGen;
            }

            public child go(shared_strand strand, action handler, functional.func callback = null, functional.func<bool> suspendCb = null)
            {
                return make(strand, handler, callback, suspendCb).run();
            }

            public child tick_go(shared_strand strand, action handler, functional.func callback = null, functional.func<bool> suspendCb = null)
            {
                return make(strand, handler, callback, suspendCb).tick_run();
            }

            public child make(action handler, functional.func callback = null, functional.func<bool> suspendCb = null)
            {
                return make(_parent._strand, handler, callback, suspendCb);
            }

            public child go(action handler, functional.func callback = null, functional.func<bool> suspendCb = null)
            {
                return go(_parent._strand, handler, callback, suspendCb);
            }

            public child tick_go(action handler, functional.func callback = null, functional.func<bool> suspendCb = null)
            {
                return tick_go(_parent._strand, handler, callback, suspendCb);
            }

            public void ignore_suspend(bool igonre = true)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                _ignoreSuspend = igonre;
            }

            public void suspend(bool isSuspend, functional.func cb)
            {
                if (!_ignoreSuspend && 0 != _children.Count)
                {
                    int count = _children.Count;
                    functional.func suspendCb = _parent._strand.wrap(delegate ()
                    {
                        if (0 == --count)
                        {
                            cb();
                        }
                    });
                    child[] tempChildren = new child[_children.Count];
                    _children.CopyTo(tempChildren, 0);
                    foreach (child ele in tempChildren)
                    {
                        (isSuspend ? (Action<functional.func>)ele.suspend : ele.resume)(suspendCb);
                    }
                }
                else
                {
                    cb();
                }
            }

            public int count()
            {
                return _children.Count;
            }

            public int discard(params child[] gens)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                int count = 0;
                if (0 != gens.Count())
                {
                    foreach (child ele in gens)
                    {
                        if (null != ele.node)
                        {
#if DEBUG
                            Trace.Assert(ele.node.List == _children, "此 child 不属于当前 children");
#endif
                            count++;
                            _children.Remove(ele.node);
                            ele.node = null;
                        }
                    }
                    check_remove_node();
                }
                return count;
            }

            static public int discard(params children[] childrens)
            {
                int count = 0;
                if (0 != childrens.Count())
                {
#if DEBUG
                    generator self = generator.self;
#endif
                    foreach (children childs in childrens)
                    {
#if DEBUG
                        Trace.Assert(self == childs._parent, "此 children 不属于当前 generator");
#endif
                        foreach (child ele in childs._children)
                        {
                            count++;
                            childs._children.Remove(ele.node);
                            ele.node = null;
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
                Trace.Assert(null == gen.node || gen.node.List == _children, "此 child 不属于当前 children");
#endif
                if (null != gen.node)
                {
                    gen.stop(_parent.async_result());
                    await _parent.async_wait();
                    _children.Remove(gen.node);
                    gen.node = null;
                    check_remove_node();
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
                if (0 != gens.Count())
                {
                    msg_buff<child> waitStop = new msg_buff<child>(_parent._strand);
                    foreach (child ele in gens)
                    {
                        if (null != ele.node)
                        {
#if DEBUG
                            Trace.Assert(ele.node.List == _children, "此 child 不属于当前 children");
#endif
                            count++;
                            ele.stop(() => waitStop.post(ele));
                        }
                    }
                    for (int i = 0; i < count; i++)
                    {
                        child gen = (await chan_pop(waitStop)).result;
                        _children.Remove(gen.node);
                        gen.node = null;
                    }
                    check_remove_node();
                }
                return count;
            }

            public async Task<bool> wait(child gen)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
                Trace.Assert(null == gen.node || gen.node.List == _children, "此 child 不属于当前 children");
#endif
                if (null != gen.node)
                {
                    gen.append_stop_callback(_parent.async_result());
                    await _parent.async_wait();
                    _children.Remove(gen.node);
                    gen.node = null;
                    check_remove_node();
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
                if (0 != gens.Count())
                {
                    msg_buff<child> waitStop = new msg_buff<child>(_parent._strand);
                    foreach (child ele in gens)
                    {
                        if (null != ele.node)
                        {
#if DEBUG
                            Trace.Assert(ele.node.List == _children, "此 child 不属于当前 children");
#endif
                            count++;
                            ele.append_stop_callback(() => waitStop.post(ele));
                        }
                    }
                    for (int i = 0; i < count; i++)
                    {
                        child gen = (await chan_pop(waitStop)).result;
                        _children.Remove(gen.node);
                        gen.node = null;
                    }
                    check_remove_node();
                }
                return count;
            }

            public async Task<bool> timed_wait(child gen, int ms)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
                Trace.Assert(null == gen.node || gen.node.List == _children, "此 child 不属于当前 children");
#endif
                bool overtime = false;
                if (null != gen.node)
                {
                    gen.append_stop_callback(_parent.timed_async_result2(ms, () => overtime = true));
                    await _parent.async_wait();
                    if (!overtime)
                    {
                        _children.Remove(gen.node);
                        gen.node = null;
                        check_remove_node();
                    }
                }
                return !overtime;
            }

            public async Task stop()
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                if (0 != _children.Count)
                {
                    msg_buff<child> waitStop = new msg_buff<child>(_parent._strand);
                    foreach (child ele in _children)
                    {
                        ele.stop(() => waitStop.post(ele));
                    }
                    while (0 != _children.Count)
                    {
                        child gen = (await chan_pop(waitStop)).result;
                        _children.Remove(gen.node);
                        gen.node = null;
                    }
                    check_remove_node();
                }
            }

            static public async Task stop(params children[] childrens)
            {
                if (0 != childrens.Count())
                {
                    generator self = generator.self;
                    msg_buff<Tuple<children, child>> waitStop = new msg_buff<Tuple<children, child>>(self._strand);
                    int count = 0;
                    foreach (children childs in childrens)
                    {
#if DEBUG
                        Trace.Assert(self == childs._parent, "此 children 不属于当前 generator");
#endif
                        foreach (child ele in childs._children)
                        {
                            count++;
                            ele.stop(() => waitStop.post(new Tuple<children, child>(childs, ele)));
                        }
                    }
                    for (int i = 0; i < count; i++)
                    {
                        Tuple<children, child> oneRes = (await chan_pop(waitStop)).result;
                        oneRes.Item1._children.Remove(oneRes.Item2.node);
                        oneRes.Item2.node = null;
                        oneRes.Item1.check_remove_node();
                    }
                }
            }

            public async Task<child> wait_one()
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                if (0 != _children.Count)
                {
                    async_result_wrap<child> res = new async_result_wrap<child>();
                    functional.func<child> ntf = _parent.safe_async_result(res);
                    foreach (child ele in _children)
                    {
                        ele.append_stop_callback(() => ntf(ele));
                    }
                    await _parent.async_wait();
                    _children.Remove(res.value_1.node);
                    res.value_1.node = null;
                    check_remove_node();
                    return res.value_1;
                }
                return null;
            }

            public async Task<child> timed_wait_one(int ms)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                if (0 != _children.Count)
                {
                    async_result_wrap<child> res = new async_result_wrap<child>();
                    functional.func<child> ntf = _parent.timed_async_result(ms, res);
                    foreach (child ele in _children)
                    {
                        ele.append_stop_callback(() => ntf(ele));
                    }
                    await _parent.async_wait();
                    if (null != res.value_1)
                    {
                        _children.Remove(res.value_1.node);
                        res.value_1.node = null;
                        check_remove_node();
                        return res.value_1;
                    }
                }
                return null;
            }

            public async Task wait_all()
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                if (0 != _children.Count)
                {
                    msg_buff<child> waitStop = new msg_buff<child>(_parent._strand);
                    foreach (child ele in _children)
                    {
                        ele.append_stop_callback(() => waitStop.post(ele));
                    }
                    while (0 != _children.Count)
                    {
                        child gen = (await chan_pop(waitStop)).result;
                        _children.Remove(gen.node);
                        gen.node = null;
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
            _runGen = generator.go(strand, async delegate ()
            {
                while (true)
                {
                    chan_pop_wrap<gen_pck> pck = await generator.chan_pop(_queue);
                    await generator.depth_call(pck.result.strand, pck.result.action);
                }
            });
        }

        ~async_queue()
        {
            _runGen.stop();
        }

        public void post(shared_strand strand, generator.action action)
        {
            _queue.post(new gen_pck(strand, action));
        }

        public void post(shared_strand strand, generator.action action, functional.func cb)
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
        shared_strand _strand;
        msg_buff<generator.action> _queue;
        generator _runGen;

        public async_strand(shared_strand strand)
        {
            _strand = strand;
            _queue = new msg_buff<generator.action>(_strand);
            _runGen = generator.go(_strand, async delegate ()
            {
                while (true)
                {
                    chan_pop_wrap<generator.action> pck = await generator.chan_pop(_queue);
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
        }

        ~async_strand()
        {
            _runGen.stop();
        }

        public void post(generator.action action)
        {
            _queue.post(action);
        }

        public void post(generator.action action, functional.func cb)
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

    public class check_tasks
    {
        int _count;
        shared_strand _strand;
        functional.func _callback;

        public check_tasks(shared_strand strand, functional.func cb)
        {
            _count = 0;
            _strand = strand;
            _callback = cb;
        }

        public functional.func wrap_check()
        {
#if DEBUG
            Trace.Assert(_strand.thread_safe(), "check_tasks::wrap_check");
#endif
            _count++;
            mutli_callback multiCb = new mutli_callback();
            return _strand.wrap(delegate ()
            {
                if (!multiCb.check() && 0 == --_count)
                {
                    _callback();
                }
            });
        }

        public check_tasks make_child()
        {
            return make_child(_strand);
        }

        public check_tasks make_child(shared_strand strand)
        {
#if DEBUG
            Trace.Assert(_strand.thread_safe(), "check_tasks::make_child");
#endif
            return new check_tasks(strand, wrap_check());
        }
    }
}
