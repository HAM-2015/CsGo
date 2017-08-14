using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            p1 = default_value<T1>.value;
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
            p1 = default_value<T1>.value;
            p2 = default_value<T2>.value;
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
            p1 = default_value<T1>.value;
            p2 = default_value<T2>.value;
            p3 = default_value<T3>.value;
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

    public class chan_pop_wrap<T>
    {
        public readonly chan_async_state state;
        public readonly T result;

        public chan_pop_wrap(chan_async_state st)
        {
            state = st;
        }

        public chan_pop_wrap(chan_async_state st, T res)
        {
            state = st;
            result = res;
        }
    }

    public class csp_invoke_wrap<T>
    {
        public readonly chan_async_state state;
        public readonly T result;

        public csp_invoke_wrap(chan_async_state st)
        {
            state = st;
        }

        public csp_invoke_wrap(chan_async_state st, T res)
        {
            state = st;
            result = res;
        }
    }

    public class csp_wait_wrap<R, T>
    {
        public readonly csp_chan<R, T>.csp_result result;
        public readonly chan_async_state state;
        public readonly T msg;

        public csp_wait_wrap(chan_async_state st)
        {
            state = st;
        }

        public csp_wait_wrap(chan_async_state st, csp_chan<R, T>.csp_result res, T p)
        {
            state = st;
            result = res;
            msg = p;
        }

        public void return_(R res)
        {
            result.return_(res);
        }

        public void return_()
        {
            result.return_();
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

        class mail_pck
        {
            public object mailbox;
            public child agentAction;

            public mail_pck(object mb)
            {
                mailbox = mb;
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

        static long _idCount = 0;
        static Task _nilTask = new Task(functional.nil_action);
        static ReaderWriterLockSlim _nameMutex = new ReaderWriterLockSlim();
        static SortedList<string, generator> _nameGens = new SortedList<string, generator>();
        static static_init _init = new static_init();

        SortedList<Type, mail_pck> _chanMap;
        functional.func<bool> _suspendCb;
        LinkedList<children> _children;
        mutli_callback _multiCb;
        shared_strand _strand;
        children _agentMap;
        async_timer _timer;
        object _selfValue;
        Task _pullTask;
        Task _syncNtf;
        string _name;
        long _id;
        int _lockCount;
        int _lastTm;
        bool _beginQuit;
        bool _isSuspend;
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

        static public action wrap_children(functional.func_res<Task, children> handler)
        {
            return async delegate ()
            {
                children children = new children();
                try
                {
                    await handler(children);
                }
                catch (stop_exception)
                {
                    await children.stop();
                    throw;
                }
            };
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
            _hasBlock = false;
            _beginQuit = false;
            _lockCount = -1;
            _lastTm = 0;
            _strand = strand;
            _suspendCb = suspendCb;
            _syncNtf = new Task(functional.nil_action);
            _pullTask = new Task(functional.nil_action);
            _timer = new async_timer(_strand);
            _strand.hold_work();
            _strand.distribute(async delegate ()
            {
                try
                {
                    _lockCount = 0;
                    await async_wait();
                    await handler();
                }
                catch (stop_exception)
                {
                    _timer.cancel();
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
            else
            {
                tls_values tlsVal = shared_strand._runningTls.Value;
                generator oldGen = tlsVal.self;
                tlsVal.self = this;
                _pullTask.RunSynchronously();
                tlsVal.self = oldGen;
            }
        }

        void next(bool beginQuit)
        {
            if (!_isStop && (!_beginQuit || beginQuit))
            {
                no_check_next();
            }
        }

        void newCbPuller()
        {
#if DEBUG
            Trace.Assert(this == self, "running is not self!!!");
#endif
            _pullTask = new Task(functional.nil_action);
        }

        mutli_callback newMultiCbPuller()
        {
#if DEBUG
            Trace.Assert(this == self, "running is not self!!!");
#endif
            if (null == _pullTask)
            {
                _pullTask = new Task(functional.nil_action);
                _multiCb = new mutli_callback();
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

        private void _suspend_cb(functional.func cb = null)
        {
            if (null != _children && 0 != _children.Count)
            {
                int count = _children.Count;
                functional.func suspendCb = delegate ()
                {
                    if (0 == --count)
                    {
                        functional.catch_invoke(_suspendCb, true);
                        functional.catch_invoke(cb);
                    }
                };
                children[] tempChildren = new children[_children.Count];
                _children.CopyTo(tempChildren, 0);
                foreach (children ele in tempChildren)
                {
                    ele.suspend(true, suspendCb);
                }
            }
            else
            {
                functional.catch_invoke(_suspendCb, true);
                functional.catch_invoke(cb);
            }
        }

        private void _suspend(functional.func cb = null)
        {
            if (!_isStop && !_beginQuit && !_isSuspend)
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
                _suspend_cb(cb);
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

        private void _resume_cb(functional.func cb = null)
        {
            if (null != _children && 0 != _children.Count)
            {
                int count = _children.Count;
                functional.func resumeCb = delegate ()
                {
                    if (0 == --count)
                    {
                        functional.catch_invoke(_suspendCb, false);
                        functional.catch_invoke(cb);
                    }
                };
                children[] tempChildren = new children[_children.Count];
                _children.CopyTo(tempChildren, 0);
                foreach (children ele in tempChildren)
                {
                    ele.suspend(false, resumeCb);
                }
            }
            else
            {
                functional.catch_invoke(_suspendCb, false);
                functional.catch_invoke(cb);
            }
        }

        private void _resume(functional.func cb = null)
        {
            if (!_isStop && !_beginQuit && _isSuspend)
            {
                _isSuspend = false;
                _resume_cb(cb);
                if (!_isStop && !_beginQuit && !_isSuspend)
                {
                    if (_hasBlock)
                    {
                        _hasBlock = false;
                        next(false);
                    }
                    else if (0 != _lastTm)
                    {
                        _timer.timeout(_lastTm, () => next(false));
                    }
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

        protected void _stop()
        {
            _isForce = true;
            if (0 == _lockCount)
            {
                _isSuspend = false;
                if (null == _pullTask)
                {
                    _beginQuit = true;
                    _suspendCb = null;
                    _timer.cancel();
                    throw stop_exception.val;
                }
                else
                {
                    next(!_beginQuit);
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
            _strand.distribute(delegate ()
            {
                if (-1 == _lockCount)
                {
                    delay_stop();
                }
                else if (!_isStop)
                {
                    _stop();
                }
            });
        }

        public bool is_force()
        {
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

        static public async Task hold()
        {
            generator this_ = self;
            this_.newCbPuller();
            await this_.async_wait();
        }

        static public async Task pause_self()
        {
            generator this_ = self;
            if (!this_._beginQuit)
            {
                this_._isSuspend = true;
                this_._suspend_cb();
                if (this_._isSuspend)
                {
                    this_._hasBlock = true;
                    this_._pullTask = new Task(functional.nil_action);
                    await this_.async_wait();
                }
            }
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
                this_._beginQuit = true;
                this_._suspendCb = null;
                this_._timer.cancel();
                throw stop_exception.val;
            }
        }

        static public async Task lock_stop(functional.func_res<Task> handler)
        {
            lock_stop();
            await handler();
            unlock_stop();
        }

        public async Task async_wait()
        {
            await _pullTask;
            _pullTask = null;
            _multiCb = null;
            if (!_beginQuit && 0 == _lockCount && _isForce)
            {
                _beginQuit = true;
                _suspendCb = null;
                _timer.cancel();
                throw stop_exception.val;
            }
        }

        public async Task async_wait(functional.func handler)
        {
            handler();
            await async_wait();
        }

        public async Task<R> wait_result<R>(functional.func<async_result_wrap<R>> handler)
        {
            async_result_wrap<R> res = new async_result_wrap<R>();
            await async_wait(() => handler(res));
            return res.value_1;
        }

        public functional.same_func async_callback()
        {
            newCbPuller();
            bool beginQuit = _beginQuit;
            return delegate (object[] args)
            {
                _strand.distribute(() => next(beginQuit));
            };
        }

        public functional.same_func async_callback(functional.same_func handler)
        {
            newCbPuller();
            bool beginQuit = _beginQuit;
            return delegate (object[] args)
            {
                handler(args);
                _strand.distribute(() => next(beginQuit));
            };
        }

        public functional.func async_callback2(functional.func handler)
        {
            newCbPuller();
            bool beginQuit = _beginQuit;
            return delegate ()
            {
                handler();
                _strand.distribute(() => next(beginQuit));
            };
        }

        public functional.func<T1> async_callback2<T1>(functional.func<T1> handler)
        {
            newCbPuller();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1)
            {
                handler(p1);
                _strand.distribute(() => next(beginQuit));
            };
        }

        public functional.func<T1, T2> async_callback2<T1, T2>(functional.func<T1, T2> handler)
        {
            newCbPuller();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2)
            {
                handler(p1, p2);
                _strand.distribute(() => next(beginQuit));
            };
        }

        public functional.func<T1, T2, T3> async_callback2<T1, T2, T3>(functional.func<T1, T2, T3> handler)
        {
            newCbPuller();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                handler(p1, p2, p3);
                _strand.distribute(() => next(beginQuit));
            };
        }

        public functional.same_func safe_async_callback()
        {
            mutli_callback multiCb = newMultiCbPuller();
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

        public functional.same_func safe_async_callback(functional.same_func handler)
        {
            mutli_callback multiCb = newMultiCbPuller();
            bool beginQuit = _beginQuit;
            return delegate (object[] args)
            {
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && (!_beginQuit || beginQuit))
                    {
                        handler(args);
                        no_check_next();
                    }
                });
            };
        }

        public functional.func safe_async_callback2(functional.func handler)
        {
            mutli_callback multiCb = newMultiCbPuller();
            bool beginQuit = _beginQuit;
            return delegate ()
            {
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && (!_beginQuit || beginQuit))
                    {
                        handler();
                        no_check_next();
                    }
                });
            };
        }

        public functional.func<T1> safe_async_callback2<T1>(functional.func<T1> handler)
        {
            mutli_callback multiCb = newMultiCbPuller();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1)
            {
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && (!_beginQuit || beginQuit))
                    {
                        handler(p1);
                        no_check_next();
                    }
                });
            };
        }

        public functional.func<T1, T2> safe_async_callback2<T1, T2>(functional.func<T1, T2> handler)
        {
            mutli_callback multiCb = newMultiCbPuller();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2)
            {
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && (!_beginQuit || beginQuit))
                    {
                        handler(p1, p2);
                        no_check_next();
                    }
                });
            };
        }

        public functional.func<T1, T2, T3> safe_async_callback2<T1, T2, T3>(functional.func<T1, T2, T3> handler)
        {
            mutli_callback multiCb = newMultiCbPuller();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && (!_beginQuit || beginQuit))
                    {
                        handler(p1, p2, p3);
                        no_check_next();
                    }
                });
            };
        }

        public functional.func async_result()
        {
            newCbPuller();
            bool beginQuit = _beginQuit;
            return () => _strand.distribute(() => next(beginQuit));
        }

        public functional.func<T1> async_result<T1>(async_result_wrap<T1> res)
        {
            newCbPuller();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1)
            {
                res.value_1 = p1;
                _strand.distribute(() => next(beginQuit));
            };
        }

        public functional.func<T1, T2> async_result<T1, T2>(async_result_wrap<T1, T2> res)
        {
            newCbPuller();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2)
            {
                res.value_1 = p1;
                res.value_2 = p2;
                _strand.distribute(() => next(beginQuit));
            };
        }

        public functional.func<T1, T2, T3> async_result<T1, T2, T3>(async_result_wrap<T1, T2, T3> res)
        {
            newCbPuller();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                res.value_1 = p1;
                res.value_2 = p2;
                res.value_3 = p3;
                _strand.distribute(() => next(beginQuit));
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
            mutli_callback multiCb = newMultiCbPuller();
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
            mutli_callback multiCb = newMultiCbPuller();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1)
            {
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && (!_beginQuit || beginQuit))
                    {
                        res.value_1 = p1;
                        no_check_next();
                    }
                });
            };
        }

        public functional.func<T1, T2> safe_async_result<T1, T2>(async_result_wrap<T1, T2> res)
        {
            mutli_callback multiCb = newMultiCbPuller();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2)
            {
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && (!_beginQuit || beginQuit))
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
            mutli_callback multiCb = newMultiCbPuller();
            bool beginQuit = _beginQuit;
            return delegate (T1 p1, T2 p2, T3 p3)
            {
                _strand.distribute(delegate ()
                {
                    if (!multiCb.check() && !_isStop && (!_beginQuit || beginQuit))
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
            mutli_callback multiCb = newMultiCbPuller();
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
                    if (!multiCb.check() && !_isStop && (!_beginQuit || beginQuit))
                    {
                        _timer.cancel();
                        no_check_next();
                    }
                });
            };
        }

        public functional.func<T1> timed_async_result<T1>(int ms, async_result_wrap<T1> res, functional.func timedHandler = null)
        {
            mutli_callback multiCb = newMultiCbPuller();
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
                    if (!multiCb.check() && !_isStop && (!_beginQuit || beginQuit))
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
            mutli_callback multiCb = newMultiCbPuller();
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
                    if (!multiCb.check() && !_isStop && (!_beginQuit || beginQuit))
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
            mutli_callback multiCb = newMultiCbPuller();
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
                    if (!multiCb.check() && !_isStop && (!_beginQuit || beginQuit))
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
            mutli_callback multiCb = newMultiCbPuller();
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
                    if (!multiCb.check() && !_isStop && (!_beginQuit || beginQuit))
                    {
                        _timer.cancel();
                        no_check_next();
                    }
                });
            };
        }

        public functional.func<T1> timed_async_result2<T1>(int ms, async_result_wrap<T1> res, functional.func timedHandler = null)
        {
            mutli_callback multiCb = newMultiCbPuller();
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
                    if (!multiCb.check() && !_isStop && (!_beginQuit || beginQuit))
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
            mutli_callback multiCb = newMultiCbPuller();
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
                    if (!multiCb.check() && !_isStop && (!_beginQuit || beginQuit))
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
            mutli_callback multiCb = newMultiCbPuller();
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
                    if (!multiCb.check() && !_isStop && (!_beginQuit || beginQuit))
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

        static public async Task sleep(int ms)
        {
            if (ms > 0)
            {
                generator this_ = self;
                this_._lastTm = ms;
                await this_.async_wait(() => this_._timer.timeout(ms, this_.async_result()));
                this_._lastTm = 0;
            }
            else if (ms < 0)
            {
                await hold();
            }
            else
            {
                await yield();
            }
        }

        static public async Task deadline(long ms)
        {
            generator this_ = self;
            await this_.async_wait(() => this_._timer.deadline(ms, this_.async_result()));
        }

        static public async Task yield()
        {
            generator this_ = self;
            await this_.async_wait(() => this_._strand.post(this_.async_result()));
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
            return self._strand;
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
            return self._id;
        }

        public long id
        {
            get
            {
                return _id;
            }
        }

        static public async Task suspend_other(generator otherGen)
        {
            generator this_ = self;
            await this_.async_wait(() => otherGen.suspend(this_.async_result()));
        }

        static public async Task resume_other(generator otherGen)
        {
            generator this_ = self;
            await this_.async_wait(() => otherGen.resume(this_.async_result()));
        }

        static public async Task<chan_async_state> chan_push<T>(channel<T> chan, T p)
        {
            generator this_ = self;
            chan_async_state result = chan_async_state.async_undefined;
            await this_.async_wait(delegate ()
            {
                chan.push(this_.async_callback(delegate (object[] args)
                {
                    result = (chan_async_state)args[0];
                }), p);
            });
            return result;
        }

        static public async Task<chan_pop_wrap<T>> chan_pop<T>(channel<T> chan)
        {
            return await chan_pop(chan, broadcast_chan_token._defToken);
        }

        static public async Task<chan_pop_wrap<T>> chan_pop<T>(channel<T> chan, broadcast_chan_token token)
        {
            generator this_ = self;
            chan_pop_wrap<T> result = null;
            await this_.async_wait(delegate ()
            {
                chan.pop(this_.async_callback(delegate (object[] args)
                {
                    chan_async_state state = (chan_async_state)args[0];
                    result = chan_async_state.async_ok == state ? new chan_pop_wrap<T>(state, (T)args[1]) : new chan_pop_wrap<T>(state);
                }), token);
            });
            return result;
        }

        static public async Task<chan_async_state> chan_try_push<T>(channel<T> chan, T p)
        {
            generator this_ = self;
            chan_async_state result = chan_async_state.async_undefined;
            await this_.async_wait(delegate ()
            {
                chan.try_push(this_.async_callback(delegate (object[] args)
                {
                    result = (chan_async_state)args[0];
                }), p);
            });
            return result;
        }

        static public async Task<chan_pop_wrap<T>> chan_try_pop<T>(channel<T> chan)
        {
            return await chan_try_pop(chan, broadcast_chan_token._defToken);
        }

        static public async Task<chan_pop_wrap<T>> chan_try_pop<T>(channel<T> chan, broadcast_chan_token token)
        {
            generator this_ = self;
            chan_pop_wrap<T> result = null;
            await this_.async_wait(delegate ()
            {
                chan.try_pop(this_.async_callback(delegate (object[] args)
                {
                    chan_async_state state = (chan_async_state)args[0];
                    result = chan_async_state.async_ok == state ? new chan_pop_wrap<T>(state, (T)args[1]) : new chan_pop_wrap<T>(state);
                }), token);
            });
            return result;
        }

        static public async Task<chan_async_state> chan_timed_push<T>(int ms, channel<T> chan, T p)
        {
            generator this_ = self;
            chan_async_state result = chan_async_state.async_undefined;
            await this_.async_wait(delegate ()
            {
                chan.timed_push(ms, this_.async_callback(delegate (object[] args)
                {
                    result = (chan_async_state)args[0];
                }), p);
            });
            return result;
        }

        static public async Task<chan_pop_wrap<T>> chan_timed_pop<T>(int ms, channel<T> chan)
        {
            return await chan_timed_pop(ms, chan, broadcast_chan_token._defToken);
        }

        static public async Task<chan_pop_wrap<T>> chan_timed_pop<T>(int ms, channel<T> chan, broadcast_chan_token token)
        {
            generator this_ = self;
            chan_pop_wrap<T> result = null;
            await this_.async_wait(delegate ()
            {
                chan.timed_pop(ms, this_.async_callback(delegate (object[] args)
                {
                    chan_async_state state = (chan_async_state)args[0];
                    result = chan_async_state.async_ok == state ? new chan_pop_wrap<T>(state, (T)args[1]) : new chan_pop_wrap<T>(state);
                }), token);
            });
            return result;
        }

        static public async Task<csp_invoke_wrap<R>> csp_invoke<R, T>(csp_chan<R, T> chan, T p)
        {
            generator this_ = self;
            csp_invoke_wrap<R> result = null;
            await this_.async_wait(delegate ()
            {
                chan.push(this_.async_callback(delegate(object[] args)
                {
                    chan_async_state state = (chan_async_state)args[0];
                    result = chan_async_state.async_ok == state ? new csp_invoke_wrap<R>(state, (R)args[1]) : new csp_invoke_wrap<R>(state);
                }), p);
            });
            return result;
        }

        static public async Task<csp_wait_wrap<R, T>> csp_wait<R, T>(csp_chan<R, T> chan)
        {
            generator this_ = self;
            csp_wait_wrap<R, T> result = null;
            await this_.async_wait(delegate ()
            {
                chan.pop(this_.async_callback(delegate(object[] args)
                {
                    chan_async_state state = (chan_async_state)args[0];
                    result = chan_async_state.async_ok == state ? new csp_wait_wrap<R, T>(state, (csp_chan<R, T>.csp_result)(args[1]), (T)args[2]) : new csp_wait_wrap<R, T>(state);
                }));
            });
            return result;
        }

        static public async Task<chan_async_state> csp_wait<R, T>(csp_chan<R, T> chan, functional.func_res<Task<R>, T> handler)
        {
            csp_wait_wrap<R, T> result = await csp_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                result.result.return_(await handler(result.msg));
            }
            return result.state;
        }

        static public async Task<csp_invoke_wrap<R>> csp_try_invoke<R, T>(csp_chan<R, T> chan, T p)
        {
            generator this_ = self;
            csp_invoke_wrap<R> result = null;
            await this_.async_wait(delegate ()
            {
                chan.try_push(this_.async_callback(delegate (object[] args)
                {
                    chan_async_state state = (chan_async_state)args[0];
                    result = chan_async_state.async_ok == state ? new csp_invoke_wrap<R>(state, (R)args[1]) : new csp_invoke_wrap<R>(state);
                }), p);
            });
            return result;
        }

        static public async Task<csp_wait_wrap<R, T>> csp_try_wait<R, T>(csp_chan<R, T> chan)
        {
            generator this_ = self;
            csp_wait_wrap<R, T> result = null;
            await this_.async_wait(delegate ()
            {
                chan.try_pop(this_.async_callback(delegate (object[] args)
                {
                    chan_async_state state = (chan_async_state)args[0];
                    result = chan_async_state.async_ok == state ? new csp_wait_wrap<R, T>(state, (csp_chan<R, T>.csp_result)(args[1]), (T)args[2]) : new csp_wait_wrap<R, T>(state);
                }));
            });
            return result;
        }

        static public async Task<chan_async_state> csp_try_wait<R, T>(csp_chan<R, T> chan, functional.func_res<Task<R>, T> handler)
        {
            csp_wait_wrap<R, T> result = await csp_try_wait(chan);
            if (chan_async_state.async_ok == result.state)
            {
                result.result.return_(await handler(result.msg));
            }
            return result.state;
        }

        static public async Task<csp_invoke_wrap<R>> csp_timed_invoke<R, T>(int ms, csp_chan<R, T> chan, T p)
        {
            generator this_ = self;
            csp_invoke_wrap<R> result = null;
            await this_.async_wait(delegate ()
            {
                chan.timed_push(ms, this_.async_callback(delegate (object[] args)
                {
                    chan_async_state state = (chan_async_state)args[0];
                    result = chan_async_state.async_ok == state ? new csp_invoke_wrap<R>(state, (R)args[1]) : new csp_invoke_wrap<R>(state);
                }), p);
            });
            return result;
        }

        static public async Task<csp_wait_wrap<R, T>> csp_timed_wait<R, T>(int ms, csp_chan<R, T> chan)
        {
            generator this_ = self;
            csp_wait_wrap<R, T> result = null;
            await this_.async_wait(delegate ()
            {
                chan.timed_pop(ms, this_.async_callback(delegate (object[] args)
                {
                    chan_async_state state = (chan_async_state)args[0];
                    result = chan_async_state.async_ok == state ? new csp_wait_wrap<R, T>(state, (csp_chan<R, T>.csp_result)(args[1]), (T)args[2]) : new csp_wait_wrap<R, T>(state);
                }));
            });
            return result;
        }

        static public async Task<chan_async_state> csp_timed_wait<R, T>(int ms, csp_chan<R, T> chan, functional.func_res<Task<R>, T> handler)
        {
            csp_wait_wrap<R, T> result = await csp_timed_wait(ms, chan);
            if (chan_async_state.async_ok == result.state)
            {
                result.result.return_(await handler(result.msg));
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
            msg_buff<int> selectId = new msg_buff<int>(this_._strand);
            int count = chans.Count();
            for (int i = 0; i < count; i++)
            {
                int id = i;
                chans[i].ntfSign._selectOnce = false;
                chans[i].begin(() => selectId.post(id));
            }
            try
            {
                while (true)
                {
                    chan_pop_wrap<int> sel = await chan_pop(selectId);
                    if (chans[sel.result].disabled())
                    {
                        continue;
                    }
                    count--;
                    select_chan_state selState = await chans[sel.result].invoke(() => selectId.post(sel.result));
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
                for (int i = 0; i < chans.Count(); i++)
                {
                    await chans[i].end();
                }
                unlock_stop();
            }
        }

        static public async Task select_chans_once(params select_chan_base[] chans)
        {
            generator this_ = self;
            msg_buff<int> selectId = new msg_buff<int>(this_._strand);
            for (int i = 0; i < chans.Count(); i++)
            {
                int id = i;
                chans[i].ntfSign._selectOnce = true;
                chans[i].begin(() => selectId.post(id));
            }
            bool selected = false;
            try
            {
                select_chan_state selState = null;
                do
                {
                    chan_pop_wrap<int> sel = await chan_pop(selectId);
                    if (chans[sel.result].disabled())
                    {
                        continue;
                    }
                    selState = await chans[sel.result].invoke(() => selectId.post(sel.result), async delegate(select_chan_base selectedChan)
                    {
                        for (int i = 0; i < chans.Count(); i++)
                        {
                            if (selectedChan != chans[i])
                            {
                                await chans[i].end();
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
                    for (int i = 0; i < chans.Count(); i++)
                    {
                        await chans[i].end();
                    }
                    unlock_stop();
                }
            }
        }

        static public async Task timed_select_chans(int ms, functional.func_res<Task> timedHandler, params select_chan_base[] chans)
        {
            generator this_ = self;
            msg_buff<int> selectId = new msg_buff<int>(this_._strand);
            this_._timer.timeout(ms, () => selectId.push(functional.any_handler, -1));
            for (int i = 0; i < chans.Count(); i++)
            {
                int id = i;
                chans[i].ntfSign._selectOnce = true;
                chans[i].begin(() => selectId.post(id));
            }
            bool selected = false;
            try
            {
                while (true)
                {
                    chan_pop_wrap<int> sel = await chan_pop(selectId);
                    if (-1 != sel.result)
                    {
                        if (chans[sel.result].disabled())
                        {
                            continue;
                        }
                        select_chan_state selState = await chans[sel.result].invoke(() => selectId.post(sel.result), async delegate (select_chan_base selectedChan)
                        {
                            this_._timer.cancel();
                            for (int i = 0; i < chans.Count(); i++)
                            {
                                if (selectedChan != chans[i])
                                {
                                    await chans[i].end();
                                }
                            }
                            selected = true;
                        });
                        if (!selState.failed)
                        {
                            break;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < chans.Count(); i++)
                        {
                            await chans[i].end();
                        }
                        selected = true;
                        await timedHandler();
                        break;
                    }
                }
            }
            catch (stop_select_exception) { }
            finally
            {
                if (!selected)
                {
                    this_._timer.cancel();
                    lock_stop();
                    for (int i = 0; i < chans.Count(); i++)
                    {
                        await chans[i].end();
                    }
                    unlock_stop();
                }
            }
        }

        static public select_chan_base case_read<T>(channel<T> chan, functional.func_res<Task, T> handler, functional.func_res<Task<bool>, chan_async_state> errHandler = null)
        {
            return chan.make_select_reader(handler, errHandler);
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

        static public select_chan_base case_read<T>(broadcast_chan<T> chan, functional.func_res<Task, T> handler, broadcast_chan_token token)
        {
            return chan.make_select_reader(handler, token);
        }

        static public select_chan_base case_read<T>(broadcast_chan<T> chan, functional.func_res<Task, T> handler, functional.func_res<Task<bool>, chan_async_state> errHandler, broadcast_chan_token token)
        {
            return chan.make_select_reader(handler, errHandler, token);
        }

        static public select_chan_base case_read<R, T>(csp_chan<R, T> chan, functional.func_res<Task, csp_chan<R, T>.csp_result, T> handler, functional.func_res<Task<bool>, chan_async_state> errHandler)
        {
            return chan.make_select_reader(handler, errHandler);
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

        static public async Task close_chan<T>(channel<T> chan)
        {
            generator this_ = self;
            await this_.async_wait(() => chan.close(this_.async_callback()));
        }

        static public async Task cancel_chan<T>(channel<T> chan)
        {
            generator this_ = self;
            await this_.async_wait(() => chan.cancel(this_.async_callback()));
        }

        static public async Task mutex_lock(mutex_base mtx)
        {
            generator this_ = self;
            await this_.async_wait(() => mtx.Lock(this_._id, this_.async_result()));
        }

        static public async Task mutex_lock(mutex_base mtx, functional.func_res<Task> handler)
        {
            await mutex_lock(mtx);
            await handler();
            await mutex_unlock(mtx);
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
                await handler();
                await mutex_unlock(mtx);
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
                await handler();
                await mutex_unlock(mtx);
                return true;
            }
            return false;
        }

        static public async Task mutex_unlock(mutex_base mtx)
        {
            generator this_ = self;
            await this_.async_wait(() => mtx.unlock(this_._id, this_.async_result()));
        }

        static public async Task mutex_lock_shared(shared_mutex mtx)
        {
            generator this_ = self;
            await this_.async_wait(() => mtx.lock_shared(this_._id, this_.async_result()));
        }

        static public async Task mutex_lock_shared(shared_mutex mtx, functional.func_res<Task> handler)
        {
            await mutex_lock_shared(mtx);
            await handler();
            await mutex_unlock_shared(mtx);
        }

        static public async Task mutex_lock_pess_shared(shared_mutex mtx)
        {
            generator this_ = self;
            await this_.async_wait(() => mtx.lock_pess_shared(this_._id, this_.async_result()));
        }

        static public async Task mutex_lock_pess_shared(shared_mutex mtx, functional.func_res<Task> handler)
        {
            await mutex_lock_pess_shared(mtx);
            await handler();
            await mutex_unlock_shared(mtx);
        }

        static public async Task mutex_lock_upgrade(shared_mutex mtx)
        {
            generator this_ = self;
            await this_.async_wait(() => mtx.lock_upgrade(this_._id, this_.async_result()));
        }

        static public async Task mutex_lock_upgrade(shared_mutex mtx, functional.func_res<Task> handler)
        {
            await mutex_lock_upgrade(mtx);
            await handler();
            await mutex_unlock_upgrade(mtx);
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
                await handler();
                await mutex_unlock_shared(mtx);
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
                await handler();
                await mutex_unlock_upgrade(mtx);
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
                await handler();
                await mutex_unlock_shared(mtx);
                return true;
            }
            return false;
        }

        static public async Task mutex_unlock_shared(shared_mutex mtx)
        {
            generator this_ = self;
            await this_.async_wait(() => mtx.unlock_shared(this_._id, this_.async_result()));
        }

        static public async Task mutex_unlock_upgrade(shared_mutex mtx)
        {
            generator this_ = self;
            await this_.async_wait(() => mtx.unlock_upgrade(this_._id, this_.async_result()));
        }

        static public async Task condition_wait(condition_variable conVar, mutex_base mutex)
        {
            generator this_ = self;
            await this_.async_wait(() => conVar.wait(this_._id, mutex, this_.async_result()));
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
                await this_.async_wait(() => strand.post(this_.async_callback2(delegate ()
                {
                    try
                    {
                        handler();
                    }
                    catch (System.Exception ec)
                    {
                        hasExcep = ec;
                    }
                })));
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
                delay_result<R> res = null;
                System.Exception hasExcep = null;
                await this_.async_wait(() => strand.post(this_.async_callback2(delegate ()
                {
                    try
                    {
                        res = new delay_result<R>(handler());
                    }
                    catch (System.Exception ec)
                    {
                        hasExcep = ec;
                    }
                })));
                if (null != hasExcep)
                {
                    throw hasExcep;
                }
                return res.value;
            }
        }

        static public functional.func_res<Task> wrap_send_strand(shared_strand strand, functional.func handler)
        {
            return async () => await send_strand(strand, handler);
        }

        static public functional.func_res<Task, T> wrap_send_strand<T>(shared_strand strand, functional.func<T> handler)
        {
            return async (T p) => await send_strand(strand, () => handler(p));
        }

        static public functional.func_res<Task<R>> wrap_send_strand<R>(shared_strand strand, functional.func_res<R> handler)
        {
            return async delegate ()
            {
                delay_result<R> res = null;
                await send_strand(strand, () => res = new delay_result<R>(handler()));
                return res.value;
            };
        }

        static public functional.func_res<Task<R>, T> wrap_send_strand<R, T>(shared_strand strand, functional.func_res<R, T> handler)
        {
            return async delegate (T p)
            {
                delay_result<R> res = null;
                await send_strand(strand, () => res = new delay_result<R>(handler(p)));
                return res.value;
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
            await this_.async_wait(() => post_control(ctrl, this_.async_callback2(delegate ()
            {
                try
                {
                    handler();
                }
                catch (System.Exception ec)
                {
                    hasExcep = ec;
                }
            })));
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
            delay_result<R> res = null;
            System.Exception hasExcep = null;
            await this_.async_wait(() => post_control(ctrl, this_.async_callback2(delegate ()
            {
                try
                {
                    res = new delay_result<R>(handler());
                }
                catch (System.Exception ec)
                {
                    hasExcep = ec;
                }
            })));
            if (null != hasExcep)
            {
                throw hasExcep;
            }
            return res.value;
        }

        static public functional.func wrap_post_control(Control ctrl, functional.func handler)
        {
            return () => post_control(ctrl, handler);
        }

        static public functional.func_res<Task> wrap_send_control(Control ctrl, functional.func handler)
        {
            return async () => await send_control(ctrl, handler);
        }

        static public functional.func_res<Task, T> wrap_send_control<T>(Control ctrl, functional.func<T> handler)
        {
            return async (T p) => await send_control(ctrl, () => handler(p));
        }

        static public functional.func_res<Task<R>> wrap_send_control<R>(Control ctrl, functional.func_res<R> handler)
        {
            return async delegate ()
            {
                delay_result<R> res = null;
                await send_control(ctrl, () => res = new delay_result<R>(handler()));
                return res.value;
            };
        }

        static public functional.func_res<Task<R>, T> wrap_send_control<R, T>(Control ctrl, functional.func_res<R, T> handler)
        {
            return async delegate (T p)
            {
                delay_result<R> res = null;
                await send_control(ctrl, () => res = new delay_result<R>(handler(p)));
                return res.value;
            };
        }

        static public async Task send_task(functional.func handler)
        {
            generator this_ = self;
            System.Exception hasExcep = null;
            functional.func cb = this_.async_callback2(delegate ()
            {
                try
                {
                    handler();
                }
                catch (System.Exception ec)
                {
                    hasExcep = ec;
                }
            });
            await this_.async_wait(() => Task.Run(() => cb()));
            if (null != hasExcep)
            {
                throw hasExcep;
            }
        }

        static public async Task<R> send_task<R>(functional.func_res<R> handler)
        {
            generator this_ = self;
            delay_result<R> res = null;
            System.Exception hasExcep = null;
            functional.func cb = this_.async_callback2(delegate ()
            {
                try
                {
                    res = new delay_result<R>(handler());
                }
                catch (System.Exception ec)
                {
                    hasExcep = ec;
                }
            });
            await this_.async_wait(() => Task.Run(() => cb()));
            if (null != hasExcep)
            {
                throw hasExcep;
            }
            return res.value;
        }

        static public functional.func_res<Task> wrap_send_task(functional.func handler)
        {
            return async () => await send_task(handler);
        }

        static public functional.func_res<Task, T> wrap_send_task<T>(functional.func<T> handler)
        {
            return async (T p) => await send_task(() => handler(p));
        }

        static public functional.func_res<Task<R>> wrap_send_task<R>(functional.func_res<R> handler)
        {
            return async delegate ()
            {
                delay_result<R> res = null;
                await send_task(() => res = new delay_result<R>(handler()));
                return res.value;
            };
        }

        static public functional.func_res<Task<R>, T> wrap_send_task<R, T>(functional.func_res<R, T> handler)
        {
            return async delegate (T p)
            {
                delay_result<R> res = null;
                await send_task(() => res = new delay_result<R>(handler(p)));
                return res.value;
            };
        }

        static public async Task send_async_queue(async_queue queue, shared_strand strand, generator.action action)
        {
            generator this_ = self;
            System.Exception hasExcep = null;
            await self.async_wait(() => queue.post(strand, async delegate ()
            {
                try
                {
                    await action();
                }
                catch (System.Exception ec)
                {
                    hasExcep = ec;
                }
            }, this_.async_result()));
            if (null != hasExcep)
            {
                throw hasExcep;
            }
        }

        static public functional.func_res<Task> wrap_send_async_queue(async_queue queue, shared_strand strand, generator.action action)
        {
            return async () => await send_async_queue(queue, strand, action);
        }

        static public functional.func_res<Task, T> wrap_send_async_queue<T>(async_queue queue, shared_strand strand, functional.func_res<Task, T> action)
        {
            return async (T p) => await send_async_queue(queue, strand, async () => await action(p));
        }

        static public functional.func_res<Task<R>> wrap_send_async_queue<R>(async_queue queue, shared_strand strand, functional.func_res<Task<R>> action)
        {
            return async delegate ()
            {
                delay_result<R> res = null;
                await send_async_queue(queue, strand, async () => res = new delay_result<R>(await action()));
                return res.value;
            };
        }

        static public functional.func_res<Task<R>, T> wrap_send_async_queue<R, T>(async_queue queue, shared_strand strand, functional.func_res<Task<R>, T> action)
        {
            return async delegate (T p)
            {
                delay_result<R> res = null;
                await send_async_queue(queue, strand, async () => res = new delay_result<R>(await action(p)));
                return res.value;
            };
        }

        static public async Task send_async_strand(async_strand queue, generator.action action)
        {
            generator this_ = self;
            System.Exception hasExcep = null;
            await self.async_wait(() => queue.post(async delegate ()
            {
                try
                {
                    await action();
                }
                catch (System.Exception ec)
                {
                    hasExcep = ec;
                }
            }, this_.async_result()));
            if (null != hasExcep)
            {
                throw hasExcep;
            }
        }

        static public functional.func_res<Task> wrap_send_async_strand(async_strand queue, generator.action action)
        {
            return async () => await send_async_strand(queue, action);
        }

        static public functional.func_res<Task, T> wrap_send_async_strand<T>(async_strand queue, functional.func_res<Task, T> action)
        {
            return async (T p) => await send_async_strand(queue, async () => await action(p));
        }

        static public functional.func_res<Task<R>> wrap_send_async_strand<R>(async_strand queue, functional.func_res<Task<R>> action)
        {
            return async delegate ()
            {
                delay_result<R> res = null;
                await send_async_strand(queue, async () => res = new delay_result<R>(await action()));
                return res.value;
            };
        }

        static public functional.func_res<Task<R>, T> wrap_send_async_strand<R, T>(async_strand queue, functional.func_res<Task<R>, T> action)
        {
            return async delegate (T p)
            {
                delay_result<R> res = null;
                await send_async_strand(queue, async () => res = new delay_result<R>(await action(p)));
                return res.value;
            };
        }

        static public async Task call(action handler)
        {
#if DEBUG
            generator this_ = self;
            up_stack_frame(this_._callStack, 2);
            await handler();
            this_._callStack.RemoveFirst();
#else
            await handler();
#endif
        }

        static public async Task<R> call<R>(functional.func_res<Task<R>> handler)
        {
#if DEBUG
            generator this_ = self;
            up_stack_frame(this_._callStack, 2);
            R res = await handler();
            this_._callStack.RemoveFirst();
            return res;
#else
            return await handler();
#endif
        }

        static public async Task depth_call(shared_strand strand, action handler)
        {
            generator this_ = self;
#if DEBUG
            up_stack_frame(this_._callStack, 2);
            await lock_stop(async () => await this_.async_wait(() => (new generator()).init(strand, handler, this_.async_result(), null, this_._callStack).run()));
            this_._callStack.RemoveFirst();
#else
            await lock_stop(async () => await this_.async_wait(() => go(strand, handler, this_.async_result())));
#endif
        }

        static public async Task<R> depth_call<R>(shared_strand strand, functional.func_res<Task<R>> handler)
        {
            generator this_ = self;
            delay_result<R> res = null;
#if DEBUG
            up_stack_frame(this_._callStack, 2);
            await lock_stop(async () => await this_.async_wait(() => (new generator()).init(strand, async () => res = new delay_result<R>(await handler()), this_.async_result(), null, this_._callStack).run()));
            this_._callStack.RemoveFirst();
#else
            await lock_stop(async () => await this_.async_wait(() => go(strand, async () => res = new delay_result<R>(await handler()), this_.async_result(), null)));
#endif
            return res.value;
        }

#if DEBUG
        static public LinkedList<call_stack_info> call_stack
        {
            get
            {
                return self._callStack;
            }
        }
#endif

        static public msg_buff<T> self_mailbox<T>()
        {
            generator this_ = self;
            if (null == this_._chanMap)
            {
                this_._chanMap = new SortedList<Type, mail_pck>();
            }
            mail_pck mb = null;
            if (!this_._chanMap.TryGetValue(typeof(msg_buff<T>), out mb))
            {
                mb = new mail_pck(new msg_buff<T>(this_._strand));
                this_._chanMap.Add(typeof(msg_buff<T>), mb);
            }
            return (msg_buff<T>)mb.mailbox;
        }

        public void get_mailbox<T>(functional.func<msg_buff<T>> cb)
        {
            _strand.distribute(delegate ()
            {
                if (null == _chanMap)
                {
                    _chanMap = new SortedList<Type, mail_pck>();
                }
                mail_pck mb = null;
                if (!_chanMap.TryGetValue(typeof(msg_buff<T>), out mb))
                {
                    mb = new mail_pck(new msg_buff<T>(_strand));
                    _chanMap.Add(typeof(msg_buff<T>), mb);
                }
                functional.catch_invoke(cb, (msg_buff<T>)mb.mailbox);
            });
        }

        public async Task<msg_buff<T>> get_mailbox<T>()
        {
            generator host_ = self;
            async_result_wrap<msg_buff<T>> res = new async_result_wrap<msg_buff<T>>();
            await host_.async_wait(() => get_mailbox(host_.async_result(res)));
            return res.value_1;
        }

        static public async Task agent_mail<T>(child agentChild, bool selectMode = true)
        {
            generator this_ = self;
            if (null == this_._agentMap)
            {
                this_._agentMap = new children();
            }
            if (null == this_._chanMap)
            {
                this_._chanMap = new SortedList<Type, mail_pck>();
            }
            mail_pck mb = null;
            if (!this_._chanMap.TryGetValue(typeof(msg_buff<T>), out mb))
            {
                mb = new mail_pck(new msg_buff<T>(this_._strand));
                this_._chanMap.Add(typeof(msg_buff<T>), mb);
            }
            else if (null != mb.agentAction)
            {
                await mb.agentAction.stop();
            }
            mb.agentAction = this_._agentMap.go(async delegate ()
            {
                msg_buff<T> childMb = await agentChild.get_mailbox<T>();
                msg_buff<T> selfMb = (msg_buff<T>)mb.mailbox;
                if (selectMode)
                {
                    await select_chans(case_read(selfMb, delegate (T msg)
                    {
                        childMb.post(msg);
                        return nil_wait();
                    }));
                }
                else
                {
                    while (true)
                    {
                        chan_pop_wrap<T> res = await chan_pop(selfMb);
                        if (chan_async_state.async_ok == res.state)
                        {
                            childMb.post(res.result);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            });
        }

        static public async Task agent_stop<T>()
        {
            generator this_ = self;
            mail_pck mb = null;
            if (null != this_._agentMap && null != this_._chanMap &&
                this_._chanMap.TryGetValue(typeof(msg_buff<T>), out mb) && null != mb.agentAction)
            {
                await mb.agentAction.stop();
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
                _strand.distribute(delegate ()
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
                });
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

            public new async Task stop()
            {
                generator parent = self;
                await parent.async_wait(() => stop(parent.async_result()));
            }

            public async Task wait()
            {
                generator parent = self;
                await parent.async_wait(() => append_stop_callback(parent.async_result()));
            }

            public async Task<bool> timed_wait(int ms)
            {
                generator parent = self;
                bool overtime = false;
                await parent.async_wait(() => append_stop_callback(parent.timed_async_result2(ms, () => overtime = true)));
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
                    functional.func suspendCb = delegate ()
                    {
                        if (0 == --count)
                        {
                            cb();
                        }
                    };
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

            public async Task stop(child gen)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                if (null != gen.node)
                {
                    await _parent.async_wait(() => gen.stop(_parent.async_result()));
                    _children.Remove(gen.node);
                    gen.node = null;
                    check_remove_node();
                }
            }

            public async Task wait(child gen)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                if (null != gen.node)
                {
                    await _parent.async_wait(() => gen.append_stop_callback(_parent.async_result()));
                    _children.Remove(gen.node);
                    gen.node = null;
                    check_remove_node();
                }
            }

            public async Task<bool> timed_wait(child gen, int ms)
            {
#if DEBUG
                Trace.Assert(self == _parent, "此 children 不属于当前 generator");
#endif
                bool overtime = false;
                if (null != gen.node)
                {
                    await _parent.async_wait(() => gen.append_stop_callback(_parent.timed_async_result2(ms, () => overtime = true)));
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
                    bool overtime = false;
                    async_result_wrap<child> res = new async_result_wrap<child>();
                    functional.func<child> ntf = _parent.timed_async_result2(ms, res, () => overtime = true);
                    foreach (child ele in _children)
                    {
                        ele.append_stop_callback(() => ntf(ele));
                    }
                    await _parent.async_wait();
                    if (!overtime)
                    {
                        await _parent.async_wait();
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
