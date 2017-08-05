using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Go
{
    public enum chan_async_state
    {
        async_undefined = -1,
        async_ok = 0,
        async_fail,
        async_cancel,
        async_closed,
        async_overtime
    }

    public enum chan_type
    {
        nolimit,
        limit,
        nil,
        broadcast,
        csp
    }

    public class chan_notify_sign
    {
        public LinkedListNode<functional.func<chan_async_state>> _ntfNode;
        public bool _nodeEffect = false;
        public bool _disable = false;
    }

    public class select_chan_state
    {
        public bool failed = false;
        public bool nextRound = true;
    }

    public abstract class select_chan_base
    {
        public chan_notify_sign ntfSign = new chan_notify_sign();
        public bool disabled() { return ntfSign._disable; }
        public abstract void begin(functional.func<int, chan_async_state> cb, int id);
        public abstract Task<select_chan_state> invoke(functional.func<int, chan_async_state> cb, int id);
        public abstract Task end();
    }

    public abstract class channel<T>
    {
        public class select_chan_reader : select_chan_base
        {
            public broadcast_chan_token _token = broadcast_chan_token._defToken;
            public channel<T> _chan;
            public functional.func_res<Task, T> _handler;
            public functional.func_res<Task<bool>, chan_async_state> _errHandler;

            public override void begin(functional.func<int, chan_async_state> cb, int id)
            {
                _chan.append_pop_notify(delegate (object[] args)
                {
                    chan_async_state state = (chan_async_state)args[0];
                    if (chan_async_state.async_fail != state)
                    {
                        cb(id, state);
                    }
                }, ntfSign);
            }

            public override async Task<select_chan_state> invoke(functional.func<int, chan_async_state> cb, int id)
            {
                generator self = generator.self;
                chan_pop_wrap<T> result = null;
                await self.async_wait(delegate ()
                {
                    _chan.try_pop_and_append_notify(self.async_callback(delegate (object[] args)
                    {
                        chan_async_state state = (chan_async_state)args[0];
                        result = chan_async_state.async_ok == state ? new chan_pop_wrap<T>(state, (T)args[1]) : new chan_pop_wrap<T>(state);
                    }), delegate (object[] args)
                    {
                        chan_async_state state = (chan_async_state)args[0];
                        if (chan_async_state.async_fail != state)
                        {
                            cb(id, state);
                        }
                    }, ntfSign, _token);
                });
                select_chan_state chanState = new select_chan_state();
                if (chan_async_state.async_ok == result.state)
                {
                    await _handler(result.result);
                }
                else if (null != _errHandler)
                {
                    if (await _errHandler(result.state))
                    {
                        await end();
                        chanState.nextRound = false;
                    }
                }
                else if (chan_async_state.async_closed == result.state)
                {
                    await end();
                    chanState.nextRound = false;
                }
                else
                {
                    chanState.failed = true;
                }
                return chanState;
            }

            public override async Task end()
            {
                generator self = generator.self;
                ntfSign._disable = true;
                await self.async_wait(() => _chan.remove_pop_notify(self.async_callback(), ntfSign));
            }
        }

        public class select_chan_writer : select_chan_base
        {
            public channel<T> _chan;
            public T _msg;
            public functional.func_res<Task> _handler;
            public functional.func_res<Task<bool>, chan_async_state> _errHandler;

            public override void begin(functional.func<int, chan_async_state> cb, int id)
            {
                _chan.append_push_notify(delegate (object[] args)
                {
                    chan_async_state state = (chan_async_state)args[0];
                    if (chan_async_state.async_fail != state)
                    {
                        cb(id, state);
                    }
                }, ntfSign);
            }

            public override async Task<select_chan_state> invoke(functional.func<int, chan_async_state> cb, int id)
            {
                generator self = generator.self;
                chan_async_state result = chan_async_state.async_undefined;
                await self.async_wait(delegate ()
                {
                    _chan.try_push_and_append_notify(self.async_callback(delegate (object[] args)
                    {
                        result = (chan_async_state)args[0];
                    }), delegate (object[] args)
                    {
                        chan_async_state state = (chan_async_state)args[0];
                        if (chan_async_state.async_fail != state)
                        {
                            cb(id, state);
                        }
                    }, ntfSign, _msg);
                });
                select_chan_state chanState = new select_chan_state();
                if (chan_async_state.async_ok == result)
                {
                    await _handler();
                }
                else if (null != _errHandler)
                {
                    if (await _errHandler(result))
                    {
                        await end();
                        chanState.nextRound = false;
                    }
                }
                else if (chan_async_state.async_closed == result)
                {
                    await end();
                    chanState.nextRound = false;
                }
                else
                {
                    chanState.failed = true;
                }
                return chanState;
            }

            public override async Task end()
            {
                generator self = generator.self;
                ntfSign._disable = true;
                await self.async_wait(() => _chan.remove_push_notify(self.async_callback(), ntfSign));
            }
        }

        public abstract chan_type type();
        public abstract void push(functional.same_func ntf, T msg);
        public abstract void pop(functional.same_func ntf);
        public abstract void try_push(functional.same_func ntf, T msg);
        public abstract void try_pop(functional.same_func ntf);
        public abstract void timed_push(int ms, functional.same_func ntf, T msg);
        public abstract void timed_pop(int ms, functional.same_func ntf);
        public abstract void append_pop_notify(functional.same_func ntf, chan_notify_sign ntfSign);
        public abstract void try_pop_and_append_notify(functional.same_func cb, functional.same_func msgNtf, chan_notify_sign ntfSign);
        public abstract void remove_pop_notify(functional.same_func ntf, chan_notify_sign ntfSign);
        public abstract void append_push_notify(functional.same_func ntf, chan_notify_sign ntfSign);
        public abstract void try_push_and_append_notify(functional.same_func cb, functional.same_func msgNtf, chan_notify_sign ntfSign, T msg);
        public abstract void remove_push_notify(functional.same_func ntf, chan_notify_sign ntfSign);
        public abstract void close(functional.same_func ntf);
        public abstract void cancel(functional.same_func ntf);
        public abstract void reset();
        public abstract shared_strand self_strand();

        public void post(T msg)
        {
            push(functional.any_handler, msg);
        }

        public functional.func<T> wrap()
        {
            return (T p) => post(p);
        }

        public functional.func wrap_default()
        {
            return () => post(default_value<T>.value);
        }

        public select_chan_base make_select_reader(functional.func_res<Task, T> handler)
        {
            select_chan_reader sel = new select_chan_reader();
            sel._chan = this;
            sel._handler = handler;
            return sel;
        }

        public select_chan_base make_select_reader(functional.func_res<Task, T> handler, functional.func_res<Task<bool>, chan_async_state> errHandler)
        {
            select_chan_reader sel = new select_chan_reader();
            sel._chan = this;
            sel._handler = handler;
            sel._errHandler = errHandler;
            return sel;
        }

        public select_chan_base make_select_writer(T msg, functional.func_res<Task> handler)
        {
            select_chan_writer sel = new select_chan_writer();
            sel._chan = this;
            sel._msg = msg;
            sel._handler = handler;
            return sel;
        }

        public select_chan_base make_select_writer(T msg, functional.func_res<Task> handler, functional.func_res<Task<bool>, chan_async_state> errHandler)
        {
            select_chan_writer sel = new select_chan_writer();
            sel._chan = this;
            sel._msg = msg;
            sel._handler = handler;
            sel._errHandler = errHandler;
            return sel;
        }

        public virtual void pop(functional.same_func ntf, broadcast_chan_token token)
        {
            pop(ntf);
        }

        public virtual void try_pop(functional.same_func ntf, broadcast_chan_token token)
        {
            try_pop(ntf);
        }

        public virtual void timed_pop(int ms, functional.same_func ntf, broadcast_chan_token token)
        {
            timed_pop(ms, ntf);
        }

        public virtual void append_pop_notify(functional.same_func ntf, chan_notify_sign ntfSign, broadcast_chan_token token)
        {
            append_pop_notify(ntf, ntfSign);
        }

        public virtual void try_pop_and_append_notify(functional.same_func cb, functional.same_func msgNtf, chan_notify_sign ntfSign, broadcast_chan_token token)
        {
            try_pop_and_append_notify(cb, msgNtf, ntfSign);
        }

        public virtual select_chan_base make_select_reader(functional.func_res<Task, T> handler, broadcast_chan_token token)
        {
            return make_select_reader(handler);
        }

        public virtual select_chan_base make_select_reader(functional.func_res<Task, T> handler, functional.func_res<Task<bool>, chan_async_state> errHandler, broadcast_chan_token token)
        {
            return make_select_reader(handler, errHandler);
        }
    }

    public abstract class MsgQueue_<T>
    {
        public abstract void AddLast(T msg);
        public abstract T First();
        public abstract void RemoveFirst();
        public abstract int Count();
        public abstract void Clear();
    }

    public class NoVoidMsgQueue_<T> : MsgQueue_<T>
    {
        LinkedList<T> _msgBuff;

        public NoVoidMsgQueue_()
        {
            _msgBuff = new LinkedList<T>();
        }

        public override void AddLast(T msg)
        {
            _msgBuff.AddLast(msg);
        }

        public override T First()
        {
            return _msgBuff.First();
        }

        public override void RemoveFirst()
        {
            _msgBuff.RemoveFirst();
        }

        public override int Count()
        {
            return _msgBuff.Count;
        }

        public override void Clear()
        {
            _msgBuff.Clear();
        }
    }

    public class VoidMsgQueue_<T> : MsgQueue_<T>
    {
        int _count;

        public VoidMsgQueue_()
        {
            _count = 0;
        }

        public override void AddLast(T msg)
        {
            _count++;
        }

        public override T First()
        {
            return default_value<T>.value;
        }

        public override void RemoveFirst()
        {
            _count--;
        }

        public override int Count()
        {
            return _count;
        }

        public override void Clear()
        {
            _count = 0;
        }
    }

    public class msg_buff<T> : channel<T>
    {
        shared_strand _strand;
        MsgQueue_<T> _msgBuff;
        LinkedList<functional.func<chan_async_state>> _waitQueue;
        bool _closed;

        public msg_buff(shared_strand strand)
        {
            _strand = strand;
            _closed = false;
            _msgBuff = typeof(T) == typeof(void_type) ? (MsgQueue_<T>)new VoidMsgQueue_<T>() : new NoVoidMsgQueue_<T>();
            _waitQueue = new LinkedList<functional.func<chan_async_state>>();
        }

        public override chan_type type()
        {
            return chan_type.nolimit;
        }

        public override void push(functional.same_func ntf, T msg)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                _msgBuff.AddLast(msg);
                if (0 != _waitQueue.Count)
                {
                    functional.func<chan_async_state> wtNtf = _waitQueue.First();
                    _waitQueue.RemoveFirst();
                    wtNtf(chan_async_state.async_ok);
                }
                ntf(chan_async_state.async_ok);
            });
        }

        public override void pop(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (0 != _msgBuff.Count())
                {
                    T msg = _msgBuff.First();
                    _msgBuff.RemoveFirst();
                    ntf(chan_async_state.async_ok, msg);
                }
                else
                {
                    _waitQueue.AddLast(delegate (chan_async_state state)
                    {
                        if (chan_async_state.async_ok == state)
                        {
                            pop(ntf);
                        }
                        else
                        {
                            ntf(state);
                        }
                    });
                }
            });
        }

        public override void try_push(functional.same_func ntf, T msg)
        {
            push(ntf, msg);
        }

        public override void try_pop(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (0 != _msgBuff.Count())
                {
                    T msg = _msgBuff.First();
                    _msgBuff.RemoveFirst();
                    ntf(chan_async_state.async_ok, msg);
                }
                else
                {
                    ntf(chan_async_state.async_fail);
                }
            });
        }

        public override void timed_push(int ms, functional.same_func ntf, T msg)
        {
            push(ntf, msg);
        }

        public override void timed_pop(int ms, functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (0 != _msgBuff.Count())
                {
                    T msg = _msgBuff.First();
                    _msgBuff.RemoveFirst();
                    ntf(chan_async_state.async_ok, msg);
                }
                else if (ms > 0)
                {
                    async_timer timer = new async_timer(_strand);
                    LinkedListNode<functional.func<chan_async_state>> it = _waitQueue.AddLast(delegate (chan_async_state state)
                    {
                        timer.cancel();
                        if (chan_async_state.async_ok == state)
                        {
                            pop(ntf);
                        }
                        else
                        {
                            ntf(state);
                        }
                    });
                    timer.timeout(ms, delegate ()
                    {
                        functional.func<chan_async_state> popNtf = it.Value;
                        _waitQueue.Remove(it);
                        popNtf(chan_async_state.async_overtime);
                    });
                }
                else
                {
                    ntf(chan_async_state.async_overtime);
                }
            });
        }

        public override void append_pop_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (0 != _msgBuff.Count())
                {
                    ntf(chan_async_state.async_ok);
                }
                else
                {
                    ntfSign._ntfNode = _waitQueue.AddLast(delegate (chan_async_state state)
                    {
                        ntfSign._nodeEffect = false;
                        ntf(state);
                    });
                    ntfSign._nodeEffect = true;
                }
            });
        }

        public override void try_pop_and_append_notify(functional.same_func cb, functional.same_func msgNtf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed);
                    return;
                }
                if (0 != _msgBuff.Count())
                {
                    T msg = _msgBuff.First();
                    _msgBuff.RemoveFirst();
                    append_pop_notify(msgNtf, ntfSign);
                    cb(chan_async_state.async_ok, msg);
                }
                else
                {
                    append_pop_notify(msgNtf, ntfSign);
                    cb(chan_async_state.async_fail);
                }
            });
        }

        public override void remove_pop_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                bool effect = ntfSign._nodeEffect;
                ntfSign._nodeEffect = false;
                if (effect)
                {
                    _waitQueue.Remove(ntfSign._ntfNode);
                }
                if (0 != _msgBuff.Count() && 0 != _waitQueue.Count())
                {
                    functional.func<chan_async_state> wtNtf = _waitQueue.First();
                    _waitQueue.RemoveFirst();
                    wtNtf(chan_async_state.async_ok);
                }
                ntf(effect ? chan_async_state.async_ok : chan_async_state.async_fail);
            });
        }

        public override void append_push_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                ntf(chan_async_state.async_ok);
            });
        }

        public override void try_push_and_append_notify(functional.same_func cb, functional.same_func msgNtf, chan_notify_sign ntfSign, T msg)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed);
                    return;
                }
                _msgBuff.AddLast(msg);
                if (0 != _waitQueue.Count)
                {
                    _waitQueue.RemoveFirst();
                }
                append_push_notify(msgNtf, ntfSign);
                cb(chan_async_state.async_ok);
            });
        }

        public override void remove_push_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                ntf(chan_async_state.async_ok);
            });
        }

        public override void close(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                _closed = true;
                _msgBuff.Clear();
                LinkedList<functional.func<chan_async_state>> ntfs = _waitQueue;
                _waitQueue = new LinkedList<functional.func<chan_async_state>>();
                while (0 != ntfs.Count)
                {
                    ntfs.First()(chan_async_state.async_closed);
                    ntfs.RemoveFirst();
                }
                ntf();
            });
        }

        public override void cancel(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                _closed = true;
                LinkedList<functional.func<chan_async_state>> ntfs = _waitQueue;
                _waitQueue = new LinkedList<functional.func<chan_async_state>>();
                while (0 != ntfs.Count)
                {
                    ntfs.First()(chan_async_state.async_cancel);
                    ntfs.RemoveFirst();
                }
                ntf();
            });
        }

        public override void reset()
        {
            _closed = false;
        }

        public override shared_strand self_strand()
        {
            return _strand;
        }
    }

    public class chan<T> : channel<T>
    {
        shared_strand _strand;
        MsgQueue_<T> _buffer;
        LinkedList<functional.func<chan_async_state>> _pushWait;
        LinkedList<functional.func<chan_async_state>> _popWait;
        int _length;
        bool _closed;

        public chan(shared_strand strand, int len)
        {
            _strand = strand;
            _buffer = typeof(T) == typeof(void_type) ? (MsgQueue_<T>)new VoidMsgQueue_<T>() : new NoVoidMsgQueue_<T>();
            _pushWait = new LinkedList<functional.func<chan_async_state>>();
            _popWait = new LinkedList<functional.func<chan_async_state>>();
            _length = len;
            _closed = false;
        }

        public override chan_type type()
        {
            return chan_type.limit;
        }

        public override void push(functional.same_func ntf, T msg)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_buffer.Count() == _length)
                {
                    _pushWait.AddLast(delegate (chan_async_state state)
                    {
                        if (chan_async_state.async_ok == state)
                        {
                            push(ntf, msg);
                        }
                        else
                        {
                            ntf(state);
                        }
                    });
                }
                else
                {
                    _buffer.AddLast(msg);
                    if (0 != _popWait.Count)
                    {
                        functional.func<chan_async_state> popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf(chan_async_state.async_ok);
                    }
                    ntf(chan_async_state.async_ok);
                }
            });
        }

        public override void pop(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (0 != _buffer.Count())
                {
                    T msg = _buffer.First();
                    _buffer.RemoveFirst();
                    if (0 != _pushWait.Count)
                    {
                        functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                        _pushWait.RemoveFirst();
                        pushNtf(chan_async_state.async_ok);
                    }
                    ntf(chan_async_state.async_ok, msg);
                }
                else
                {
                    _popWait.AddLast(delegate (chan_async_state state)
                    {
                        if (chan_async_state.async_ok == state)
                        {
                            pop(ntf);
                        }
                        else
                        {
                            ntf(state);
                        }
                    });
                }
            });
        }

        public override void try_push(functional.same_func ntf, T msg)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_buffer.Count() == _length)
                {
                    ntf(chan_async_state.async_fail);
                }
                else
                {
                    _buffer.AddLast(msg);
                    if (0 != _popWait.Count)
                    {
                        functional.func<chan_async_state> popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf(chan_async_state.async_ok);
                    }
                    ntf(chan_async_state.async_ok);
                }
            });
        }

        public override void try_pop(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (0 != _buffer.Count())
                {
                    T msg = _buffer.First();
                    _buffer.RemoveFirst();
                    if (0 != _pushWait.Count)
                    {
                        functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                        _pushWait.RemoveFirst();
                        pushNtf(chan_async_state.async_ok);
                    }
                    ntf(chan_async_state.async_ok, msg);
                }
                else
                {
                    ntf(chan_async_state.async_fail);
                }
            });
        }

        public override void timed_push(int ms, functional.same_func ntf, T msg)
        {
            _strand.distribute(delegate ()
            {
                if (_buffer.Count() == _length)
                {
                    if (ms > 0)
                    {
                        async_timer timer = new async_timer(_strand);
                        LinkedListNode<functional.func<chan_async_state>> it = _pushWait.AddLast(delegate (chan_async_state state)
                        {
                            timer.cancel();
                            if (chan_async_state.async_ok == state)
                            {
                                push(ntf, msg);
                            }
                            else
                            {
                                ntf(state);
                            }
                        });
                        timer.timeout(ms, delegate ()
                        {
                            functional.func<chan_async_state> pushWait = it.Value;
                            _pushWait.Remove(it);
                            pushWait(chan_async_state.async_overtime);
                        });
                    }
                    else
                    {
                        ntf(chan_async_state.async_overtime);
                    }
                }
                else
                {
                    _buffer.AddLast(msg);
                    if (0 != _popWait.Count)
                    {
                        functional.func<chan_async_state> popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf(chan_async_state.async_ok);
                    }
                    ntf(chan_async_state.async_ok);
                }
            });
        }

        public override void timed_pop(int ms, functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (0 != _buffer.Count())
                {
                    T msg = _buffer.First();
                    _buffer.RemoveFirst();
                    if (0 != _pushWait.Count)
                    {
                        functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                        _pushWait.RemoveFirst();
                        pushNtf(chan_async_state.async_ok);
                    }
                    ntf(chan_async_state.async_ok, msg);
                }
                else if (ms > 0)
                {
                    async_timer timer = new async_timer(_strand);
                    LinkedListNode<functional.func<chan_async_state>> it = _popWait.AddLast(delegate (chan_async_state state)
                    {
                        timer.cancel();
                        if (chan_async_state.async_ok == state)
                        {
                            pop(ntf);
                        }
                        else
                        {
                            ntf(state);
                        }
                    });
                    timer.timeout(ms, delegate ()
                    {
                        functional.func<chan_async_state> popNtf = it.Value;
                        _popWait.Remove(it);
                        popNtf(chan_async_state.async_overtime);
                    });
                }
                else
                {
                    ntf(chan_async_state.async_overtime);
                }
            });
        }

        public override void append_pop_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (0 != _buffer.Count())
                {
                    ntf(chan_async_state.async_ok);
                }
                else
                {
                    ntfSign._ntfNode = _popWait.AddLast(delegate (chan_async_state state)
                    {
                        ntfSign._nodeEffect = false;
                        ntf(state);
                    });
                    ntfSign._nodeEffect = true;
                }
            });
        }

        public override void try_pop_and_append_notify(functional.same_func cb, functional.same_func msgNtf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed);
                    return;
                }
                if (0 != _buffer.Count())
                {
                    T msg = _buffer.First();
                    _buffer.RemoveFirst();
                    if (0 != _pushWait.Count)
                    {
                        functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                        _pushWait.RemoveFirst();
                        pushNtf(chan_async_state.async_ok);
                    }
                    append_pop_notify(msgNtf, ntfSign);
                    cb(chan_async_state.async_ok, msg);
                }
                else
                {
                    append_pop_notify(msgNtf, ntfSign);
                    cb(chan_async_state.async_fail);
                }
            });
        }
        public override void remove_pop_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                bool effect = ntfSign._nodeEffect;
                ntfSign._nodeEffect = false;
                if (effect)
                {
                    _popWait.Remove(ntfSign._ntfNode);
                }
                if (0 != _buffer.Count() && 0 != _popWait.Count())
                {
                    functional.func<chan_async_state> popNtf = _popWait.First();
                    _popWait.RemoveFirst();
                    popNtf(chan_async_state.async_ok);
                }
                ntf(effect ? chan_async_state.async_ok : chan_async_state.async_fail);
            });
        }

        public override void append_push_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_buffer.Count() != _length)
                {
                    ntf(chan_async_state.async_ok);
                }
                else
                {
                    ntfSign._ntfNode = _pushWait.AddLast(delegate (chan_async_state state)
                    {
                        ntfSign._nodeEffect = false;
                        ntf(state);
                    });
                    ntfSign._nodeEffect = true;
                }
            });
        }

        public override void try_push_and_append_notify(functional.same_func cb, functional.same_func msgNtf, chan_notify_sign ntfSign, T msg)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed);
                    return;
                }
                if (_buffer.Count() != _length)
                {
                    _buffer.AddLast(msg);
                    if (0 != _popWait.Count)
                    {
                        functional.func<chan_async_state> popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf(chan_async_state.async_ok);
                    }
                    append_push_notify(msgNtf, ntfSign);
                    cb(chan_async_state.async_ok);
                }
                else
                {
                    append_push_notify(msgNtf, ntfSign);
                    cb(chan_async_state.async_fail);
                }
            });
        }

        public override void remove_push_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                bool effect = ntfSign._nodeEffect;
                ntfSign._nodeEffect = false;
                if (effect)
                {
                    _pushWait.Remove(ntfSign._ntfNode);
                }
                if (_buffer.Count() != _length && 0 != _pushWait.Count)
                {
                    functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                    _pushWait.RemoveFirst();
                    pushNtf(chan_async_state.async_ok);
                }
                ntf(effect ? chan_async_state.async_ok : chan_async_state.async_fail);
            });
        }

        public override void close(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                _closed = true;
                _buffer.Clear();
                LinkedList<functional.func<chan_async_state>> ntfs1 = _popWait;
                LinkedList<functional.func<chan_async_state>> ntfs2 = _pushWait;
                _popWait = new LinkedList<functional.func<chan_async_state>>();
                _pushWait = new LinkedList<functional.func<chan_async_state>>();
                while (0 != ntfs1.Count)
                {
                    ntfs1.First()(chan_async_state.async_closed);
                    ntfs1.RemoveFirst();
                }
                while (0 != ntfs2.Count)
                {
                    ntfs2.First()(chan_async_state.async_closed);
                    ntfs2.RemoveFirst();
                }
                ntf();
            });
        }
        public override void cancel(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                LinkedList<functional.func<chan_async_state>> ntfs1 = _popWait;
                LinkedList<functional.func<chan_async_state>> ntfs2 = _pushWait;
                _popWait = new LinkedList<functional.func<chan_async_state>>();
                _pushWait = new LinkedList<functional.func<chan_async_state>>();
                while (0 != ntfs1.Count)
                {
                    ntfs1.First()(chan_async_state.async_cancel);
                    ntfs1.RemoveFirst();
                }
                while (0 != ntfs2.Count)
                {
                    ntfs2.First()(chan_async_state.async_cancel);
                    ntfs2.RemoveFirst();
                }
                ntf();
            });
        }

        public override void reset()
        {
            _closed = false;
        }

        public override shared_strand self_strand()
        {
            return _strand;
        }
    }

    public class nil_chan<T> : channel<T>
    {
        shared_strand _strand;
        LinkedList<functional.func<chan_async_state>> _pushWait;
        LinkedList<functional.func<chan_async_state>> _popWait;
        T _msg;
        bool _has;
        bool _msgIsTryPush;
        bool _closed;

        public nil_chan(shared_strand strand)
        {
            _strand = strand;
            _pushWait = new LinkedList<functional.func<chan_async_state>>();
            _popWait = new LinkedList<functional.func<chan_async_state>>();
            _has = false;
            _msgIsTryPush = false;
            _closed = false;
        }

        public override chan_type type()
        {
            return chan_type.nil;
        }

        public override void push(functional.same_func ntf, T msg)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_has)
                {
                    _pushWait.AddLast(delegate (chan_async_state state)
                    {
                        if (chan_async_state.async_ok == state)
                        {
                            push(ntf, msg);
                        }
                        else
                        {
                            ntf(state);
                        }
                    });
                }
                else
                {
                    _msgIsTryPush = false;
                    _msg = msg;
                    _has = true;
                    _pushWait.AddLast(delegate (chan_async_state state)
                    {
                        if (chan_async_state.async_closed != state && chan_async_state.async_cancel != state && 0 != _pushWait.Count)
                        {
                            functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                            _pushWait.RemoveFirst();
                            pushNtf(chan_async_state.async_ok);
                        }
                        ntf(state);
                    });
                    if (0 != _popWait.Count)
                    {
                        functional.func<chan_async_state> popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf(chan_async_state.async_ok);
                    }
                }
            });
        }

        public override void pop(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_has)
                {
                    T msg = _msg;
                    _has = false;
                    functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                    _pushWait.RemoveFirst();
                    pushNtf(chan_async_state.async_ok);
                    ntf(chan_async_state.async_ok, msg);
                }
                else
                {
                    _popWait.AddLast(delegate (chan_async_state state)
                    {
                        if (chan_async_state.async_ok == state)
                        {
                            pop(ntf);
                        }
                        else
                        {
                            ntf(state);
                        }
                    });
                    if (0 != _pushWait.Count)
                    {
                        functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                        _pushWait.RemoveFirst();
                        pushNtf(chan_async_state.async_ok);
                    }
                }
            });
        }

        public override void try_push(functional.same_func ntf, T msg)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_has)
                {
                    ntf(chan_async_state.async_fail);
                }
                else
                {
                    if (0 != _popWait.Count)
                    {
                        _msgIsTryPush = true;
                        _msg = msg;
                        _has = true;
                        _pushWait.AddLast(delegate (chan_async_state state)
                        {
                            if (chan_async_state.async_closed != state && chan_async_state.async_cancel != state && 0 != _pushWait.Count)
                            {
                                functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                                _pushWait.RemoveFirst();
                                pushNtf(chan_async_state.async_ok);
                            }
                            ntf(state);
                        });
                        functional.func<chan_async_state> popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf(chan_async_state.async_ok);
                    }
                    else
                    {
                        ntf(chan_async_state.async_fail);
                    }
                }
            });
        }

        public override void try_pop(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_has)
                {
                    T msg = _msg;
                    _has = false;
                    functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                    _pushWait.RemoveFirst();
                    pushNtf(chan_async_state.async_ok);
                    ntf(chan_async_state.async_ok, msg);
                }
                else
                {
                    ntf(chan_async_state.async_fail);
                }
            });
        }

        public override void timed_push(int ms, functional.same_func ntf, T msg)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_has)
                {
                    if (ms > 0)
                    {
                        async_timer timer = new async_timer(_strand);
                        LinkedListNode<functional.func<chan_async_state>> it = _pushWait.AddLast(delegate (chan_async_state state)
                        {
                            timer.cancel();
                            if (chan_async_state.async_ok == state)
                            {
                                push(ntf, msg);
                            }
                            else
                            {
                                ntf(state);
                            }
                        });
                        timer.timeout(ms, delegate ()
                        {
                            functional.func<chan_async_state> pushWait = it.Value;
                            _pushWait.Remove(it);
                            pushWait(chan_async_state.async_overtime);
                        });
                    }
                    else
                    {
                        ntf(chan_async_state.async_overtime);
                    }
                }
                else
                {
                    _msgIsTryPush = false;
                    _msg = msg;
                    _has = true;
                    async_timer timer = new async_timer(_strand);
                    LinkedListNode<functional.func<chan_async_state>> it = _pushWait.AddLast(delegate (chan_async_state state)
                    {
                        timer.cancel();
                        if (chan_async_state.async_closed != state && chan_async_state.async_cancel != state && 0 != _pushWait.Count)
                        {
                            functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                            _pushWait.RemoveFirst();
                            pushNtf(chan_async_state.async_ok);
                        }
                        ntf(state);
                    });
                    timer.timeout(ms, delegate ()
                    {
                        functional.func<chan_async_state> pushWait = it.Value;
                        _pushWait.Remove(it);
                        pushWait(chan_async_state.async_overtime);
                    });
                    if (0 != _popWait.Count)
                    {
                        functional.func<chan_async_state> popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf(chan_async_state.async_ok);
                    }
                }
            });
        }

        public override void timed_pop(int ms, functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_has)
                {
                    T msg = _msg;
                    _has = false;
                    functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                    _pushWait.RemoveFirst();
                    pushNtf(chan_async_state.async_ok);
                    ntf(chan_async_state.async_ok, msg);
                }
                else if (ms > 0)
                {
                    async_timer timer = new async_timer(_strand);
                    LinkedListNode<functional.func<chan_async_state>> it = _popWait.AddLast(delegate (chan_async_state state)
                    {
                        timer.cancel();
                        if (chan_async_state.async_ok == state)
                        {
                            pop(ntf);
                        }
                        else
                        {
                            ntf(state);
                        }
                    });
                    timer.timeout(ms, delegate ()
                    {
                        functional.func<chan_async_state> popNtf = it.Value;
                        _popWait.Remove(it);
                        popNtf(chan_async_state.async_overtime);
                    });
                    if (0 != _pushWait.Count)
                    {
                        functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                        _pushWait.RemoveFirst();
                        pushNtf(chan_async_state.async_ok);
                    }
                }
                else
                {
                    ntf(chan_async_state.async_overtime);
                }
            });
        }

        public override void append_pop_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_has)
                {
                    ntf(chan_async_state.async_ok);
                }
                else
                {
                    ntfSign._ntfNode = _popWait.AddLast(delegate (chan_async_state state)
                    {
                        ntfSign._nodeEffect = false;
                        ntf(state);
                    });
                    ntfSign._nodeEffect = true;
                    if (0 != _pushWait.Count)
                    {
                        functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                        _pushWait.RemoveFirst();
                        pushNtf(chan_async_state.async_ok);
                    }
                }
            });
        }

        public override void try_pop_and_append_notify(functional.same_func cb, functional.same_func msgNtf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed);
                    return;
                }
                if (_has)
                {
                    T msg = _msg;
                    _has = false;
                    functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                    _pushWait.RemoveFirst();
                    pushNtf(chan_async_state.async_ok);
                    append_pop_notify(msgNtf, ntfSign);
                    cb(chan_async_state.async_ok, msg);
                }
                else
                {
                    append_pop_notify(msgNtf, ntfSign);
                    cb(chan_async_state.async_fail);
                }
            });
        }

        public override void remove_pop_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                bool effect = ntfSign._nodeEffect;
                ntfSign._nodeEffect = false;
                if (effect)
                {
                    _popWait.Remove(ntfSign._ntfNode);
                }
                if (_has)
                {
                    if (0 != _popWait.Count)
                    {
                        functional.func<chan_async_state> popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf(chan_async_state.async_ok);
                    }
                    else if (_msgIsTryPush)
                    {
                        _has = false;
                        functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                        _pushWait.RemoveFirst();
                        pushNtf(chan_async_state.async_fail);
                    }
                }
                ntf(effect ? chan_async_state.async_ok : chan_async_state.async_fail);
            });
        }

        public override void append_push_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (0 != _popWait.Count)
                {
                    ntf(chan_async_state.async_ok);
                }
                else
                {
                    ntfSign._ntfNode = _pushWait.AddLast(delegate (chan_async_state state)
                    {
                        ntfSign._nodeEffect = false;
                        ntf(state);
                    });
                    ntfSign._nodeEffect = true;
                }
            });
        }

        public override void try_push_and_append_notify(functional.same_func cb, functional.same_func msgNtf, chan_notify_sign ntfSign, T msg)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed);
                    return;
                }
                if (!_has)
                {
                    if (0 != _popWait.Count)
                    {
                        _msgIsTryPush = true;
                        _has = true;
                        _msg = msg;
                        _pushWait.AddLast(delegate (chan_async_state state)
                        {
                            if (chan_async_state.async_closed != state && chan_async_state.async_cancel != state && 0 != _pushWait.Count)
                            {
                                functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                                _pushWait.RemoveFirst();
                                pushNtf(chan_async_state.async_ok);
                            }
                            append_push_notify(msgNtf, ntfSign);
                            cb(state);
                        });
                        functional.func<chan_async_state> popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf(chan_async_state.async_ok);
                        return;
                    }
                }
                append_push_notify(msgNtf, ntfSign);
                cb(chan_async_state.async_fail);
            });
        }

        public override void remove_push_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                bool effect = ntfSign._nodeEffect;
                ntfSign._nodeEffect = false;
                if (effect)
                {
                    _pushWait.Remove(ntfSign._ntfNode);
                }
                if (!_has && 0 != _pushWait.Count)
                {
                    functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                    _pushWait.RemoveFirst();
                    pushNtf(chan_async_state.async_ok);
                }
                ntf(effect ? chan_async_state.async_ok : chan_async_state.async_fail);
            });
        }

        public override void close(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                _closed = true;
                _has = false;
                LinkedList<functional.func<chan_async_state>> ntfs1 = _popWait;
                LinkedList<functional.func<chan_async_state>> ntfs2 = _pushWait;
                _popWait = new LinkedList<functional.func<chan_async_state>>();
                _pushWait = new LinkedList<functional.func<chan_async_state>>();
                while (0 != ntfs1.Count)
                {
                    ntfs1.First()(chan_async_state.async_closed);
                    ntfs1.RemoveFirst();
                }
                while (0 != ntfs2.Count)
                {
                    ntfs2.First()(chan_async_state.async_closed);
                    ntfs2.RemoveFirst();
                }
                ntf();
            });
        }

        public override void cancel(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                _has = false;
                LinkedList<functional.func<chan_async_state>> ntfs1 = _popWait;
                LinkedList<functional.func<chan_async_state>> ntfs2 = _pushWait;
                _popWait = new LinkedList<functional.func<chan_async_state>>();
                _pushWait = new LinkedList<functional.func<chan_async_state>>();
                while (0 != ntfs1.Count)
                {
                    ntfs1.First()(chan_async_state.async_cancel);
                    ntfs1.RemoveFirst();
                }
                while (0 != ntfs2.Count)
                {
                    ntfs2.First()(chan_async_state.async_cancel);
                    ntfs2.RemoveFirst();
                }
                ntf();
            });
        }

        public override void reset()
        {
            _closed = false;
        }

        public override shared_strand self_strand()
        {
            return _strand;
        }
    }

    public class broadcast_chan_token
    {
        public long _lastId = -1;
        public static broadcast_chan_token _defToken = new broadcast_chan_token();

        public void reset()
        {
            _lastId = -1;
        }

	    public bool is_default()
        {
            return this == _defToken;
        }
    }

    public class broadcast_chan<T> : channel<T>
    {
        shared_strand _strand;
        LinkedList<functional.func<chan_async_state>> _popWait;
        T _msg;
        bool _has;
        long _pushCount;
        bool _closed;

        public broadcast_chan(shared_strand strand)
        {
            _strand = strand;
            _popWait = new LinkedList<functional.func<chan_async_state>>();
            _has = false;
            _pushCount = 0;
            _closed = false;
        }

        public override select_chan_base make_select_reader(functional.func_res<Task, T> handler, broadcast_chan_token token)
        {
            select_chan_reader sel = new select_chan_reader();
            sel._token = token;
            sel._chan = this;
            sel._handler = handler;
            return sel;
        }

        public override chan_type type()
        {
            return chan_type.broadcast;
        }

        public override void push(functional.same_func ntf, T msg)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                _pushCount++;
                _msg = msg;
                _has = true;
                LinkedList<functional.func<chan_async_state>> ntfs = _popWait;
                _popWait = new LinkedList<functional.func<chan_async_state>>();
                while (0 != ntfs.Count)
                {
                    ntfs.First()(chan_async_state.async_ok);
                    ntfs.RemoveFirst();
                }
                ntf(chan_async_state.async_ok);
            });
        }

        public override void pop(functional.same_func ntf)
        {
            pop(ntf, broadcast_chan_token._defToken);
        }

        public override void pop(functional.same_func ntf, broadcast_chan_token token)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_has && token._lastId != _pushCount)
                {
                    if (!token.is_default())
                    {
                        token._lastId = _pushCount;
                    }
                    ntf(chan_async_state.async_ok, _msg);
                }
                else
                {
                    _popWait.AddLast(delegate (chan_async_state state)
                    {
                        if (chan_async_state.async_ok == state)
                        {
                            pop(ntf, token);
                        }
                        else
                        {
                            ntf(state);
                        }
                    });
                }
            });
        }

        public override void try_push(functional.same_func ntf, T msg)
        {
            push(ntf, msg);
        }

        public override void try_pop(functional.same_func ntf)
        {
            try_pop(ntf, broadcast_chan_token._defToken);
        }

        public override void try_pop(functional.same_func ntf, broadcast_chan_token token)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_has && token._lastId != _pushCount)
                {
                    if (!token.is_default())
                    {
                        token._lastId = _pushCount;
                    }
                    ntf(chan_async_state.async_ok, _msg);
                }
                else
                {
                    ntf(chan_async_state.async_fail);
                }
            });
        }

        public override void timed_push(int ms, functional.same_func ntf, T msg)
        {
            push(ntf, msg);
        }

        public override void timed_pop(int ms, functional.same_func ntf)
        {
            timed_pop(ms, ntf, broadcast_chan_token._defToken);
        }

        public override void timed_pop(int ms, functional.same_func ntf, broadcast_chan_token token)
        {
            _timed_check_pop(system_tick.get_tick_ms() + ms, ntf, token);
        }

        void _timed_check_pop(long deadms, functional.same_func ntf, broadcast_chan_token token)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_has && token._lastId != _pushCount)
                {
                    if (!token.is_default())
                    {
                        token._lastId = _pushCount;
                    }
                    ntf(chan_async_state.async_ok, _msg);
                }
                else
                {
                    async_timer timer = new async_timer(_strand);
                    LinkedListNode<functional.func<chan_async_state>> it = _popWait.AddLast(delegate (chan_async_state state)
                    {
                        timer.cancel();
                        if (chan_async_state.async_ok == state)
                        {
                            _timed_check_pop(deadms, ntf, token);
                        }
                        else
                        {
                            ntf(state);
                        }
                    });
                    timer.deadline(deadms, delegate ()
                    {
                        functional.func<chan_async_state> popWait = it.Value;
                        _popWait.Remove(it);
                        popWait(chan_async_state.async_overtime);
                    });
                }
            });
        }

        public override void append_pop_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            append_pop_notify(ntf, ntfSign, broadcast_chan_token._defToken);
        }

        public override void append_pop_notify(functional.same_func ntf, chan_notify_sign ntfSign, broadcast_chan_token token)
        {
            _strand.distribute(delegate ()
            {
                _append_pop_notify(ntf, ntfSign, token);
            });
        }

        bool _append_pop_notify(functional.same_func ntf, chan_notify_sign ntfSign, broadcast_chan_token token)
        {
            if (_closed)
            {
                ntf(chan_async_state.async_closed);
                return true;
            }
            if (_has && token._lastId != _pushCount)
            {
                if (!token.is_default())
                {
                    token._lastId = _pushCount;
                }
                ntf(chan_async_state.async_ok);
                return true;
            }
            ntfSign._ntfNode = _popWait.AddLast(delegate (chan_async_state state)
            {
                ntfSign._nodeEffect = false;
                ntf(state);
            });
            ntfSign._nodeEffect = true;
            return false;
        }

        public override void try_pop_and_append_notify(functional.same_func cb, functional.same_func msgNtf, chan_notify_sign ntfSign)
        {
            try_pop_and_append_notify(cb, msgNtf, ntfSign, broadcast_chan_token._defToken);
        }

        public override void try_pop_and_append_notify(functional.same_func cb, functional.same_func msgNtf, chan_notify_sign ntfSign, broadcast_chan_token token)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed);
                    return;
                }
                if (_append_pop_notify(msgNtf, ntfSign, token))
                {
                    cb(chan_async_state.async_ok, _msg);
                }
                else
                {
                    cb(chan_async_state.async_fail);
                }
            });
        }

        public override void remove_pop_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                bool effect = ntfSign._nodeEffect;
                ntfSign._nodeEffect = false;
                if (effect)
                {
                    _popWait.Remove(ntfSign._ntfNode);
                }
                if (_has)
                {
                    if (0 != _popWait.Count)
                    {
                        functional.func<chan_async_state> popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf(chan_async_state.async_ok);
                    }
                }
                ntf(effect ? chan_async_state.async_ok : chan_async_state.async_fail);
            });
        }

        public override void append_push_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                ntf(chan_async_state.async_ok);
            });
        }

        public override void try_push_and_append_notify(functional.same_func cb, functional.same_func msgNtf, chan_notify_sign ntfSign, T msg)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed);
                    return;
                }
                _pushCount++;
                _msg = msg;
                _has = true;
                msgNtf(chan_async_state.async_ok);
                cb(chan_async_state.async_ok);
            });
        }

        public override void remove_push_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                ntf(chan_async_state.async_fail);
            });
        }

        public override void close(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                _closed = true;
                _has = false;
                LinkedList<functional.func<chan_async_state>> ntfs = _popWait;
                _popWait = new LinkedList<functional.func<chan_async_state>>();
                while (0 != ntfs.Count)
                {
                    ntfs.First()(chan_async_state.async_closed);
                    ntfs.RemoveFirst();
                }
                ntf();
            });
        }

        public override void cancel(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                _has = false;
                LinkedList<functional.func<chan_async_state>> ntfs = _popWait;
                _popWait = new LinkedList<functional.func<chan_async_state>>();
                while (0 != ntfs.Count)
                {
                    ntfs.First()(chan_async_state.async_cancel);
                    ntfs.RemoveFirst();
                }
                ntf();
            });
        }

        public override void reset()
        {
            _closed = false;
        }

        public override shared_strand self_strand()
        {
            return _strand;
        }
    }

    public class csp_chan<R, T> : channel<T>
    {
        class send_pck
        {
            public result_notify _ntf;
            public T _msg;
            async_timer _timer;

            public send_pck(result_notify ntf, T msg, async_timer timer)
            {
                _ntf = ntf;
                _msg = msg;
                _timer = timer;
            }

            public send_pck(result_notify ntf, T msg)
            {
                _ntf = ntf;
                _msg = msg;
            }

            public void cancel_timer()
            {
                if (null != _timer)
                {
                    _timer.cancel();
                }
            }
        }

        public class result_notify
        {
            public result_notify(functional.same_func ntf)
            {
                _ntf = ntf;
            }

            public void invoke(chan_async_state state, R res)
            {
                _ntf(state, res);
            }

            public void invoke(chan_async_state state)
            {
                _ntf(state);
            }

            functional.same_func _ntf;
        }

        public class csp_result
        {
            result_notify _notify;

            public csp_result()
            {
                _notify = null;
            }

            public csp_result(result_notify notify)
            {
                _notify = notify;
            }

            public void return_(R res)
            {
                _notify.invoke(chan_async_state.async_ok, res);
                _notify = null;
            }

            public void return_()
            {
                _notify.invoke(chan_async_state.async_ok);
                _notify = null;
            }
        }

        public class select_csp_reader : select_chan_base
        {
            public csp_chan<R, T> _chan;
            public functional.func_res<Task, csp_result, T> _handler;
            public functional.func_res<Task<bool>, chan_async_state> _errHandler;

            public override void begin(functional.func<int, chan_async_state> cb, int id)
            {
                _chan.append_pop_notify(delegate (object[] args)
                {
                    chan_async_state state = (chan_async_state)args[0];
                    if (chan_async_state.async_fail != state)
                    {
                        cb(id, state);
                    }
                }, ntfSign);
            }

            public override async Task<select_chan_state> invoke(functional.func<int, chan_async_state> cb, int id)
            {
                generator self = generator.self;
                csp_wait_wrap<R, T> result = null;
                await self.async_wait(delegate ()
                {
                    _chan.try_pop_and_append_notify(self.async_callback(delegate (object[] args)
                    {
                        chan_async_state state = (chan_async_state)args[0];
                        result = chan_async_state.async_ok == state ? new csp_wait_wrap<R, T>(state, (csp_chan<R, T>.csp_result)(args[1]), (T)args[2]) : new csp_wait_wrap<R, T>(state);
                    }), delegate (object[] args)
                    {
                        chan_async_state state = (chan_async_state)args[0];
                        if (chan_async_state.async_fail != state)
                        {
                            cb(id, state);
                        }
                    }, ntfSign);
                });
                select_chan_state chanState = new select_chan_state();
                if (chan_async_state.async_ok == result.state)
                {
                    await _handler(result.result, result.msg);
                }
                else if (null != _errHandler)
                {
                    if (await _errHandler(result.state))
                    {
                        await end();
                        chanState.nextRound = false;
                    }
                }
                else if (chan_async_state.async_closed == result.state)
                {
                    await end();
                    chanState.nextRound = false;
                }
                else
                {
                    chanState.failed = true;
                }
                return chanState;
            }

            public override async Task end()
            {
                generator self = generator.self;
                ntfSign._disable = true;
                await self.async_wait(() => _chan.remove_pop_notify(self.async_callback(), ntfSign));
            }
        }

        public class select_csp_writer : select_chan_base
        {
            public csp_chan<R, T> _chan;
            public T _msg;
            public functional.func_res<Task, R> _handler;
            public functional.func_res<Task<bool>, chan_async_state> _errHandler;

            public override void begin(functional.func<int, chan_async_state> cb, int id)
            {
                _chan.append_push_notify(delegate (object[] args)
                {
                    chan_async_state state = (chan_async_state)args[0];
                    if (chan_async_state.async_fail != state)
                    {
                        cb(id, state);
                    }
                }, ntfSign);
            }

            public override async Task<select_chan_state> invoke(functional.func<int, chan_async_state> cb, int id)
            {
                generator self = generator.self;
                csp_invoke_wrap<R> result = null;
                await self.async_wait(delegate ()
                {
                    _chan.try_push_and_append_notify(self.async_callback(delegate (object[] args)
                    {
                        chan_async_state state = (chan_async_state)args[0];
                        result = chan_async_state.async_ok == state ? new csp_invoke_wrap<R>(state, (R)args[1]) : new csp_invoke_wrap<R>(state);
                    }), delegate (object[] args)
                    {
                        chan_async_state state = (chan_async_state)args[0];
                        if (chan_async_state.async_fail != state)
                        {
                            cb(id, state);
                        }
                    }, ntfSign, _msg);
                });
                select_chan_state chanState = new select_chan_state();
                if (chan_async_state.async_ok == result.state)
                {
                    await _handler(result.result);
                }
                else if (null != _errHandler)
                {
                    if (await _errHandler(result.state))
                    {
                        await end();
                        chanState.nextRound = false;
                    }
                }
                else if (chan_async_state.async_closed == result.state)
                {
                    await end();
                    chanState.nextRound = false;
                }
                else
                {
                    chanState.failed = true;
                }
                return chanState;
            }

            public override async Task end()
            {
                generator self = generator.self;
                ntfSign._disable = true;
                await self.async_wait(() => _chan.remove_push_notify(self.async_callback(), ntfSign));
            }
        }

        shared_strand _strand;
        send_pck _msg;
        bool _has;
        LinkedList<functional.func<chan_async_state>> _sendQueue;
        LinkedList<functional.func<chan_async_state>> _waitQueue;
        bool _msgIsTryPush;
        bool _closed;

        public csp_chan(shared_strand strand)
        {
            _strand = strand;
            _has = false;
            _sendQueue = new LinkedList<functional.func<chan_async_state>>();
            _waitQueue = new LinkedList<functional.func<chan_async_state>>();
            _msgIsTryPush = false;
            _closed = false;
        }

        public select_chan_base make_select_reader(functional.func_res<Task, csp_result, T> handler)
        {
            select_csp_reader sel = new select_csp_reader();
            sel._chan = this;
            sel._handler = handler;
            return sel;
        }

        public select_chan_base make_select_reader(functional.func_res<Task, csp_result, T> handler, functional.func_res<Task<bool>, chan_async_state> errHandler)
        {
            select_csp_reader sel = new select_csp_reader();
            sel._chan = this;
            sel._handler = handler;
            sel._errHandler = errHandler;
            return sel;
        }

        public select_chan_base make_select_writer(T msg, functional.func_res<Task, R> handler)
        {
            select_csp_writer sel = new select_csp_writer();
            sel._chan = this;
            sel._msg = msg;
            sel._handler = handler;
            return sel;
        }

        public select_chan_base make_select_writer(T msg, functional.func_res<Task, R> handler, functional.func_res<Task<bool>, chan_async_state> errHandler)
        {
            select_csp_writer sel = new select_csp_writer();
            sel._chan = this;
            sel._msg = msg;
            sel._handler = handler;
            sel._errHandler = errHandler;
            return sel;
        }

        public override chan_type type()
        {
            return chan_type.csp;
        }

        public override void push(functional.same_func ntf, T msg)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_has)
                {
                    _sendQueue.AddLast(delegate (chan_async_state state)
                    {
                        if (chan_async_state.async_ok == state)
                        {
                            push(ntf, msg);
                        }
                        else
                        {
                            ntf(state);
                        }
                    });
                }
                else
                {
                    _msgIsTryPush = false;
                    _has = true;
                    _msg = new send_pck(new result_notify(ntf), msg);
                    if (0 != _waitQueue.Count)
                    {
                        functional.func<chan_async_state> handler = _waitQueue.First.Value;
                        _waitQueue.RemoveFirst();
                        handler(chan_async_state.async_ok);
                    }
                }
            });
        }

        public override void pop(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_has)
                {
                    send_pck pck = _msg;
                    pck.cancel_timer();
                    _has = false;
                    if (0 != _sendQueue.Count)
                    {
                        functional.func<chan_async_state> sendWait = _sendQueue.First.Value;
                        _sendQueue.RemoveFirst();
                        sendWait(chan_async_state.async_ok);
                    }
                    ntf(chan_async_state.async_ok, new csp_result(pck._ntf), pck._msg);
                }
                else
                {
                    _waitQueue.AddLast(delegate (chan_async_state state)
                    {
                        if (chan_async_state.async_ok == state)
                        {
                            pop(ntf);
                        }
                        else
                        {
                            ntf(state);
                        }
                    });
                    if (0 != _sendQueue.Count)
                    {
                        functional.func<chan_async_state> sendWait = _sendQueue.First.Value;
                        _sendQueue.RemoveFirst();
                        sendWait(chan_async_state.async_ok);
                    }
                }
            });
        }

        public override void try_push(functional.same_func ntf, T msg)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_has)
                {
                    ntf(chan_async_state.async_fail);
                }
                else
                {
                    if (0 != _waitQueue.Count)
                    {
                        _msgIsTryPush = true;
                        _has = true;
                        _msg = new send_pck(new result_notify(ntf), msg);
                        functional.func<chan_async_state> handler = _waitQueue.First.Value;
                        _waitQueue.RemoveFirst();
                        handler(chan_async_state.async_ok);
                    }
                    else
                    {
                        ntf(chan_async_state.async_fail);
                    }
                }
            });
        }

        public override void try_pop(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_has)
                {
                    send_pck pck = _msg;
                    pck.cancel_timer();
                    _has = false;
                    if (0 != _sendQueue.Count)
                    {
                        functional.func<chan_async_state> sendWait = _sendQueue.First.Value;
                        _sendQueue.RemoveFirst();
                        sendWait(chan_async_state.async_ok);
                    }
                    ntf(chan_async_state.async_ok, new csp_result(pck._ntf), pck._msg);
                }
                else
                {
                    ntf(chan_async_state.async_fail);
                }
            });
        }

        public override void timed_push(int ms, functional.same_func ntf, T msg)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_has)
                {
                    if (ms > 0)
                    {
                        async_timer timer = new async_timer(_strand);
                        LinkedListNode<functional.func<chan_async_state>> it = _sendQueue.AddLast(delegate (chan_async_state state)
                        {
                            timer.cancel();
                            if (chan_async_state.async_ok == state)
                            {
                                push(ntf, msg);
                            }
                            else
                            {
                                ntf(state);
                            }
                        });
                        timer.timeout(ms, delegate ()
                        {
                            functional.func<chan_async_state> sendWait = it.Value;
                            _sendQueue.Remove(it);
                            sendWait(chan_async_state.async_overtime);
                        });
                    }
                    else
                    {
                        ntf(chan_async_state.async_overtime);
                    }
                }
                else
                {
                    _msgIsTryPush = false;
                    _has = true;
                    async_timer timer = new async_timer(_strand);
                    _msg = new send_pck(new result_notify(ntf), msg, timer);
                    timer.timeout(ms, delegate ()
                    {
                        _msg.cancel_timer();
                        result_notify ntf_ = _msg._ntf;
                        _has = false;
                        ntf_.invoke(chan_async_state.async_overtime);
                    });
                    if (0 != _waitQueue.Count)
                    {
                        functional.func<chan_async_state> handler = _waitQueue.First.Value;
                        _waitQueue.RemoveFirst();
                        handler(chan_async_state.async_ok);
                    }
                }
            });
        }

        public override void timed_pop(int ms, functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_has)
                {
                    send_pck pck = _msg;
                    pck.cancel_timer();
                    _has = false;
                    if (0 != _sendQueue.Count)
                    {
                        functional.func<chan_async_state> sendWait = _sendQueue.First.Value;
                        _sendQueue.RemoveFirst();
                        sendWait(chan_async_state.async_ok);
                    }
                    ntf(chan_async_state.async_ok, new csp_result(pck._ntf), pck._msg);
                }
                else
                {
                    if (ms > 0)
                    {
                        async_timer timer = new async_timer(_strand);
                        LinkedListNode<functional.func<chan_async_state>> it = _waitQueue.AddLast(delegate (chan_async_state state)
                        {
                            timer.cancel();
                            if (chan_async_state.async_ok == state)
                            {
                                pop(ntf);
                            }
                            else
                            {
                                ntf(state);
                            }
                        });
                        timer.timeout(ms, delegate ()
                        {
                            functional.func<chan_async_state> waitNtf = it.Value;
                            _waitQueue.Remove(it);
                            waitNtf(chan_async_state.async_overtime);
                        });
                    }
                    else
                    {
                        ntf(chan_async_state.async_overtime);
                    }
                }
            });
        }

        public override void append_pop_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (_has)
                {
                    ntf(chan_async_state.async_ok);
                }
                else
                {
                    ntfSign._ntfNode = _waitQueue.AddLast(delegate (chan_async_state state)
                    {
                        ntfSign._nodeEffect = false;
                        ntf(state);
                    });
                    ntfSign._nodeEffect = true;
                    if (0 != _sendQueue.Count)
                    {
                        functional.func<chan_async_state> sendNtf = _sendQueue.First.Value;
                        _sendQueue.RemoveFirst();
                        sendNtf(chan_async_state.async_ok);
                    }
                }
            });
        }

        public override void try_pop_and_append_notify(functional.same_func cb, functional.same_func msgNtf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed);
                    return;
                }
                if (_has)
                {
                    send_pck pck = _msg;
                    pck.cancel_timer();
                    _has = false;
                    if (0 != _sendQueue.Count)
                    {
                        functional.func<chan_async_state> sendNtf = _sendQueue.First.Value;
                        _sendQueue.RemoveFirst();
                        sendNtf(chan_async_state.async_ok);
                    }
                    append_pop_notify(msgNtf, ntfSign);
                    cb(chan_async_state.async_ok, new csp_result(pck._ntf), pck._msg);
                }
                else
                {
                    append_pop_notify(msgNtf, ntfSign);
                    cb(chan_async_state.async_fail);
                }
            });
        }

        public override void remove_pop_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                bool effect = ntfSign._nodeEffect;
                ntfSign._nodeEffect = false;
                if (effect)
                {
                    _waitQueue.Remove(ntfSign._ntfNode);
                }
                if (_has)
                {
                    if (0 != _waitQueue.Count)
                    {
                        functional.func<chan_async_state> handler = _waitQueue.First.Value;
                        _waitQueue.RemoveFirst();
                        handler(chan_async_state.async_ok);
                    }
                    else if (_msgIsTryPush)
                    {
                        _msg.cancel_timer();
                        result_notify ntf_ = _msg._ntf;
                        _has = false;
                        ntf_.invoke(chan_async_state.async_fail);
                        if (0 != _sendQueue.Count)
                        {
                            functional.func<chan_async_state> sendNtf = _sendQueue.First.Value;
                            _sendQueue.RemoveFirst();
                            sendNtf(chan_async_state.async_ok);
                        }
                    }
                }
                ntf(effect ? chan_async_state.async_ok : chan_async_state.async_fail);
            });
        }

        public override void append_push_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                if (0 != _waitQueue.Count)
                {
                    ntf(chan_async_state.async_ok);
                }
                else
                {
                    ntfSign._ntfNode = _sendQueue.AddLast(delegate (chan_async_state state)
                    {
                        ntfSign._nodeEffect = false;
                        ntf(state);
                    });
                    ntfSign._nodeEffect = true;
                }
            });
        }

        public override void try_push_and_append_notify(functional.same_func cb, functional.same_func msgNtf, chan_notify_sign ntfSign, T msg)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed);
                    return;
                }
                if (!_has)
                {
                    if (0 != _waitQueue.Count)
                    {
                        _msgIsTryPush = true;
                        _has = true;
                        _msg = new send_pck(new result_notify(cb), msg);
                        append_push_notify(msgNtf, ntfSign);
                        functional.func<chan_async_state> handler = _waitQueue.First.Value;
                        _waitQueue.RemoveFirst();
                        handler(chan_async_state.async_ok);
                        return;
                    }
                }
                append_push_notify(msgNtf, ntfSign);
                cb(chan_async_state.async_fail);
            });
        }

        public override void remove_push_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                    return;
                }
                bool effect = ntfSign._nodeEffect;
                ntfSign._nodeEffect = false;
                if (effect)
                {
                    _sendQueue.Remove(ntfSign._ntfNode);
                }
                if (!_has && 0 != _sendQueue.Count)
                {
                    functional.func<chan_async_state> sendNtf = _sendQueue.First.Value;
                    _sendQueue.RemoveFirst();
                    sendNtf(chan_async_state.async_ok);
                }
                ntf(effect ? chan_async_state.async_ok : chan_async_state.async_fail);
            });
        }

        public override void close(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                _closed = true;
                if (_has)
                {
                    _msg.cancel_timer();
                    result_notify ntf_ = _msg._ntf;
                    _has = false;
                    ntf_.invoke(chan_async_state.async_closed);
                }
                LinkedList<functional.func<chan_async_state>> ntfs1 = _sendQueue;
                LinkedList<functional.func<chan_async_state>> ntfs2 = _waitQueue;
                _sendQueue = new LinkedList<functional.func<chan_async_state>>();
                _waitQueue = new LinkedList<functional.func<chan_async_state>>();
                while (0 != ntfs1.Count)
                {
                    ntfs1.First.Value(chan_async_state.async_closed);
                    ntfs1.RemoveFirst();
                }
                while (0 != ntfs2.Count)
                {
                    ntfs2.First.Value(chan_async_state.async_closed);
                    ntfs2.RemoveFirst();
                }
                ntf();
            });
        }

        public override void cancel(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                if (_has)
                {
                    _msg.cancel_timer();
                    result_notify ntf_ = _msg._ntf;
                    _has = false;
                    ntf_.invoke(chan_async_state.async_cancel);
                }
                LinkedList<functional.func<chan_async_state>> ntfs1 = _sendQueue;
                LinkedList<functional.func<chan_async_state>> ntfs2 = _waitQueue;
                _sendQueue = new LinkedList<functional.func<chan_async_state>>();
                _waitQueue = new LinkedList<functional.func<chan_async_state>>();
                while (0 != ntfs1.Count)
                {
                    ntfs1.First.Value(chan_async_state.async_cancel);
                    ntfs1.RemoveFirst();
                }
                while (0 != ntfs2.Count)
                {
                    ntfs2.First.Value(chan_async_state.async_cancel);
                    ntfs2.RemoveFirst();
                }
                ntf();
            });
        }

        public override void reset()
        {
            _closed = false;
        }

        public override shared_strand self_strand()
        {
            return _strand;
        }
    }
}
