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
        public bool _selectOnce = false;
        public bool _disable = false;
    }

    public struct select_chan_state
    {
        public bool failed;
        public bool nextRound;
    }

    public abstract class select_chan_base
    {
        public chan_notify_sign ntfSign = new chan_notify_sign();
        public msg_buff<select_chan_base> nextSelect;
        public bool disabled() { return ntfSign._disable; }
        public abstract void begin();
        public abstract Task<select_chan_state> invoke(functional.func_res<Task> stepOne = null);
        public abstract Task end();
        public abstract bool is_read();
        public abstract channel_base channel();
    }

    public abstract class channel_base
    {
        public abstract chan_type type();
        public abstract void clear(functional.same_func ntf);
        public abstract void close(functional.same_func ntf, bool isClear = false);
        public abstract void cancel(functional.same_func ntf, bool isClear = false);
        public abstract shared_strand self_strand();
        public abstract bool is_closed();
        public void clear() { clear(functional.any_handler); }
        public void close(bool isClear = false) { close(functional.any_handler, isClear); }
        public void cancel(bool isClear = false) { cancel(functional.any_handler, isClear); }

        static protected void safe_callback(ref LinkedList<functional.func<chan_async_state>> callback, chan_async_state state)
        {
            if (0 != callback.Count)
            {
                LinkedList<functional.func<chan_async_state>> tempCb = callback;
                callback = new LinkedList<functional.func<chan_async_state>>();
                for (LinkedListNode<functional.func<chan_async_state>> it = tempCb.First; null != it; it = it.Next)
                {
                    it.Value.Invoke(state);
                }
            }
        }

        static protected void safe_callback(ref LinkedList<functional.func<chan_async_state>> callback1, ref LinkedList<functional.func<chan_async_state>> callback2, chan_async_state state)
        {
            LinkedList<functional.func<chan_async_state>> tempCb1 = null;
            LinkedList<functional.func<chan_async_state>> tempCb2 = null;
            if (0 != callback1.Count)
            {
                tempCb1 = callback1;
                callback1 = new LinkedList<functional.func<chan_async_state>>();
            }
            if (0 != callback2.Count)
            {
                tempCb2 = callback2;
                callback2 = new LinkedList<functional.func<chan_async_state>>();
            }
            if (null != tempCb1)
            {
                for (LinkedListNode<functional.func<chan_async_state>> it = tempCb1.First; null != it; it = it.Next)
                {
                    it.Value.Invoke(state);
                }
            }
            if (null != tempCb2)
            {
                for (LinkedListNode<functional.func<chan_async_state>> it = tempCb2.First; null != it; it = it.Next)
                {
                    it.Value.Invoke(state);
                }
            }
        }
    }

    public abstract class channel<T> : channel_base
    {
        public class select_chan_reader : select_chan_base
        {
            public broadcast_chan_token _token = broadcast_chan_token._defToken;
            public channel<T> _chan;
            public functional.func_res<Task, T> _handler;
            public functional.func_res<Task<bool>, chan_async_state> _errHandler;

            public override void begin()
            {
                ntfSign._disable = false;
                _chan.append_pop_notify(delegate (object[] args)
                {
                    nextSelect.post(this);
                }, ntfSign);
            }

            public override async Task<select_chan_state> invoke(functional.func_res<Task> stepOne)
            {
                generator self = generator.self;
                chan_pop_wrap<T> result = default(chan_pop_wrap<T>);
                _chan.try_pop_and_append_notify(self.async_same_callback(delegate (object[] args)
                {
                    result.state = (chan_async_state)args[0];
                    if (chan_async_state.async_ok == result.state)
                    {
                        result.result = (T)args[1];
                    }
                }), delegate (object[] args)
                {
                    nextSelect.post(this);
                }, ntfSign, _token);
                await self.async_wait();
                select_chan_state chanState;
                chanState.failed = false;
                chanState.nextRound = true;
                if (chan_async_state.async_ok == result.state)
                {
                    if (null != stepOne)
                    {
                        await stepOne();
                    }
                    await _handler(result.result);
                }
                else if (null != _errHandler)
                {
                    if (await _errHandler(result.state))
                    {
                        await end();
                        chanState.failed = true;
                    }
                }
                else if (chan_async_state.async_closed == result.state)
                {
                    await end();
                    chanState.failed = true;
                }
                else
                {
                    chanState.failed = true;
                }
                chanState.nextRound = !ntfSign._disable;
                return chanState;
            }

            public override Task end()
            {
                generator self = generator.self;
                ntfSign._disable = true;
                _chan.remove_pop_notify(self.async_same_callback(), ntfSign);
                return self.async_wait();
            }

            public override bool is_read()
            {
                return true;
            }

            public override channel_base channel()
            {
                return _chan;
            }
        }

        public class select_chan_writer : select_chan_base
        {
            public channel<T> _chan;
            public functional.func_res<T> _msg;
            public functional.func_res<Task> _handler;
            public functional.func_res<Task<bool>, chan_async_state> _errHandler;

            public override void begin()
            {
                ntfSign._disable = false;
                _chan.append_push_notify(delegate (object[] args)
                {
                    nextSelect.post(this);
                }, ntfSign);
            }

            public override async Task<select_chan_state> invoke(functional.func_res<Task> stepOne)
            {
                generator self = generator.self;
                chan_async_state result = chan_async_state.async_undefined;
                _chan.try_push_and_append_notify(self.async_same_callback(delegate (object[] args)
                {
                    result = (chan_async_state)args[0];
                }), delegate (object[] args)
                {
                    nextSelect.post(this);
                }, ntfSign, _msg());
                await self.async_wait();
                select_chan_state chanState;
                chanState.failed = false;
                chanState.nextRound = true;
                if (chan_async_state.async_ok == result)
                {
                    if (null != stepOne)
                    {
                        await stepOne();
                    }
                    await _handler();
                }
                else if (null != _errHandler)
                {
                    if (await _errHandler(result))
                    {
                        await end();
                        chanState.failed = true;
                    }
                }
                else if (chan_async_state.async_closed == result)
                {
                    await end();
                    chanState.failed = true;
                }
                else
                {
                    chanState.failed = true;
                }
                chanState.nextRound = !ntfSign._disable;
                return chanState;
            }

            public override Task end()
            {
                generator self = generator.self;
                ntfSign._disable = true;
                _chan.remove_push_notify(self.async_same_callback(), ntfSign);
                return self.async_wait();
            }

            public override bool is_read()
            {
                return false;
            }

            public override channel_base channel()
            {
                return _chan;
            }
        }

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

        static public channel<T> make(shared_strand strand, int len)
        {
            if (0 == len)
            {
                return new nil_chan<T>(strand);
            }
            else if (0 < len)
            {
                return new chan<T>(strand, len);
            }
            return new msg_buff<T>(strand);
        }

        static public channel<T> make(int len)
        {
            shared_strand strand = generator.self_strand();
            return make(null != strand ? strand : new shared_strand(), len);
        }

        public void post(T msg)
        {
            push(functional.any_handler, msg);
        }

        public void try_post(T msg)
        {
            try_push(functional.any_handler, msg);
        }

        public void timed_post(int ms, T msg)
        {
            timed_push(ms, functional.any_handler, msg);
        }

        public functional.func<T> wrap()
        {
            return (T p) => post(p);
        }

        public functional.func<T> wrap_try()
        {
            return (T p) => try_post(p);
        }

        public functional.func<int, T> wrap_timed()
        {
            return (int ms, T p) => timed_post(ms, p);
        }

        public functional.func<T> wrap_timed(int ms)
        {
            return (T p) => timed_post(ms, p);
        }

        public functional.func wrap_default()
        {
            return () => post(default(T));
        }

        public functional.func wrap_try_default()
        {
            return () => try_post(default(T));
        }

        public functional.func<int> wrap_timed_default()
        {
            return (int ms) => timed_post(ms, default(T));
        }

        public functional.func wrap_timed_default(int ms)
        {
            return () => timed_post(ms, default(T));
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

        public select_chan_base make_select_writer(functional.func_res<T> msg, functional.func_res<Task> handler)
        {
            select_chan_writer sel = new select_chan_writer();
            sel._chan = this;
            sel._msg = msg;
            sel._handler = handler;
            return sel;
        }

        public select_chan_base make_select_writer(functional.func_res<T> msg, functional.func_res<Task> handler, functional.func_res<Task<bool>, chan_async_state> errHandler)
        {
            select_chan_writer sel = new select_chan_writer();
            sel._chan = this;
            sel._msg = msg;
            sel._handler = handler;
            sel._errHandler = errHandler;
            return sel;
        }

        public select_chan_base make_select_writer(async_result_wrap<T> msg, functional.func_res<Task> handler)
        {
            return make_select_writer(() => msg.value_1, handler);
        }

        public select_chan_base make_select_writer(async_result_wrap<T> msg, functional.func_res<Task> handler, functional.func_res<Task<bool>, chan_async_state> errHandler)
        {
            return make_select_writer(() => msg.value_1, handler, errHandler);
        }

        public select_chan_base make_select_writer(T msg, functional.func_res<Task> handler)
        {
            return make_select_writer(() => msg, handler);
        }

        public select_chan_base make_select_writer(T msg, functional.func_res<Task> handler, functional.func_res<Task<bool>, chan_async_state> errHandler)
        {
            return make_select_writer(() => msg, handler, errHandler);
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
        public abstract int Count { get; }
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

        public override int Count
        {
            get
            {
                return _msgBuff.Count;
            }
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
            return default(T);
        }

        public override void RemoveFirst()
        {
            _count--;
        }

        public override int Count
        {
            get
            {
                return _count;
            }
        }

        public override void Clear()
        {
            _count = 0;
        }
    }

    public class msg_buff<T> : channel<T>
    {
        shared_strand _strand;
        MsgQueue_<T> _buffer;
        LinkedList<functional.func<chan_async_state>> _waitQueue;
        bool _closed;

        public msg_buff(shared_strand strand)
        {
            init(strand);
        }

        public msg_buff()
        {
            shared_strand strand = generator.self_strand();
            init(null != strand ? strand : new shared_strand());
        }

        private void init(shared_strand strand)
        {
            _strand = strand;
            _closed = false;
            _buffer = typeof(T) == typeof(void_type) ? (MsgQueue_<T>)new VoidMsgQueue_<T>() : new NoVoidMsgQueue_<T>();
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
                _buffer.AddLast(msg);
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
                if (0 != _buffer.Count)
                {
                    T msg = _buffer.First();
                    _buffer.RemoveFirst();
                    ntf(chan_async_state.async_ok, msg);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
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
                if (0 != _buffer.Count)
                {
                    T msg = _buffer.First();
                    _buffer.RemoveFirst();
                    ntf(chan_async_state.async_ok, msg);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
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
                if (0 != _buffer.Count)
                {
                    T msg = _buffer.First();
                    _buffer.RemoveFirst();
                    ntf(chan_async_state.async_ok, msg);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
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
                if (0 != _buffer.Count)
                {
                    ntf(chan_async_state.async_ok);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                }
                else
                {
                    ntfSign._ntfNode = _waitQueue.AddLast(delegate (chan_async_state state)
                    {
                        ntfSign._ntfNode = null;
                        ntf(state);
                    });
                }
            });
        }

        public override void try_pop_and_append_notify(functional.same_func cb, functional.same_func msgNtf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (0 != _buffer.Count)
                {
                    T msg = _buffer.First();
                    _buffer.RemoveFirst();
                    if (!ntfSign._selectOnce)
                    {
                        append_pop_notify(msgNtf, ntfSign);
                    }
                    cb(chan_async_state.async_ok, msg);
                }
                else if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed);
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
                bool effect = null != ntfSign._ntfNode;
                if (effect)
                {
                    _waitQueue.Remove(ntfSign._ntfNode);
                    ntfSign._ntfNode = null;
                }
                if (0 != _buffer.Count && 0 != _waitQueue.Count)
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
                _buffer.AddLast(msg);
                if (0 != _waitQueue.Count)
                {
                    _waitQueue.RemoveFirst();
                }
                if (!ntfSign._selectOnce)
                {
                    append_push_notify(msgNtf, ntfSign);
                }
                cb(chan_async_state.async_ok);
            });
        }

        public override void remove_push_notify(functional.same_func ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntf(chan_async_state.async_fail);
            });
        }

        public override void clear(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                _buffer.Clear();
                ntf();
            });
        }

        public override void close(functional.same_func ntf, bool isClear = false)
        {
            _strand.distribute(delegate ()
            {
                _closed = true;
                if (isClear)
                {
                    _buffer.Clear();
                }
                safe_callback(ref _waitQueue, chan_async_state.async_closed);
                ntf();
            });
        }

        public override void cancel(functional.same_func ntf, bool isClear = false)
        {
            _strand.distribute(delegate ()
            {
                if (isClear)
                {
                    _buffer.Clear();
                }
                safe_callback(ref _waitQueue, chan_async_state.async_cancel);
                ntf();
            });
        }

        public override shared_strand self_strand()
        {
            return _strand;
        }

        public override bool is_closed()
        {
            return _closed;
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
            init(strand, len);
        }

        public chan(int len)
        {
            shared_strand strand = generator.self_strand();
            init(null != strand ? strand : new shared_strand(), len);
        }

        private void init(shared_strand strand, int len)
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
                if (_buffer.Count == _length)
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
                if (0 != _buffer.Count)
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
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
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
                if (_buffer.Count == _length)
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
                if (0 != _buffer.Count)
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
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
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
                if (_buffer.Count == _length)
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
                if (0 != _buffer.Count)
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
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
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
                if (0 != _buffer.Count)
                {
                    ntf(chan_async_state.async_ok);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                }
                else
                {
                    ntfSign._ntfNode = _popWait.AddLast(delegate (chan_async_state state)
                    {
                        ntfSign._ntfNode = null;
                        ntf(state);
                    });
                }
            });
        }

        public override void try_pop_and_append_notify(functional.same_func cb, functional.same_func msgNtf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (0 != _buffer.Count)
                {
                    T msg = _buffer.First();
                    _buffer.RemoveFirst();
                    if (0 != _pushWait.Count)
                    {
                        functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                        _pushWait.RemoveFirst();
                        pushNtf(chan_async_state.async_ok);
                    }
                    if (!ntfSign._selectOnce)
                    {
                        append_pop_notify(msgNtf, ntfSign);
                    }
                    cb(chan_async_state.async_ok, msg);
                }
                else if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed);
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
                bool effect = null != ntfSign._ntfNode;
                if (effect)
                {
                    _popWait.Remove(ntfSign._ntfNode);
                    ntfSign._ntfNode = null;
                }
                if (0 != _buffer.Count && 0 != _popWait.Count)
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
                if (_buffer.Count != _length)
                {
                    ntf(chan_async_state.async_ok);
                }
                else
                {
                    ntfSign._ntfNode = _pushWait.AddLast(delegate (chan_async_state state)
                    {
                        ntfSign._ntfNode = null;
                        ntf(state);
                    });
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
                if (_buffer.Count != _length)
                {
                    _buffer.AddLast(msg);
                    if (0 != _popWait.Count)
                    {
                        functional.func<chan_async_state> popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf(chan_async_state.async_ok);
                    }
                    if (!ntfSign._selectOnce)
                    {
                        append_push_notify(msgNtf, ntfSign);
                    }
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
                bool effect = null != ntfSign._ntfNode;
                if (effect)
                {
                    _pushWait.Remove(ntfSign._ntfNode);
                    ntfSign._ntfNode = null;
                }
                if (_buffer.Count != _length && 0 != _pushWait.Count)
                {
                    functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                    _pushWait.RemoveFirst();
                    pushNtf(chan_async_state.async_ok);
                }
                ntf(effect ? chan_async_state.async_ok : chan_async_state.async_fail);
            });
        }

        public override void clear(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                _buffer.Clear();
                safe_callback(ref _pushWait, chan_async_state.async_fail);
                ntf();
            });
        }

        public override void close(functional.same_func ntf, bool isClear = false)
        {
            _strand.distribute(delegate ()
            {
                _closed = true;
                if (isClear)
                {
                    _buffer.Clear();
                }
                safe_callback(ref _popWait, ref _pushWait, chan_async_state.async_closed);
                ntf();
            });
        }

        public override void cancel(functional.same_func ntf, bool isClear = false)
        {
            _strand.distribute(delegate ()
            {
                if (isClear)
                {
                    _buffer.Clear();
                }
                safe_callback(ref _popWait, ref _pushWait, chan_async_state.async_cancel);
                ntf();
            });
        }

        public override shared_strand self_strand()
        {
            return _strand;
        }

        public override bool is_closed()
        {
            return _closed;
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
            init(strand);
        }

        public nil_chan()
        {
            shared_strand strand = generator.self_strand();
            init(null != strand ? strand : new shared_strand());
        }

        private void init(shared_strand strand)
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
                if (_has)
                {
                    T msg = _msg;
                    _has = false;
                    functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                    _pushWait.RemoveFirst();
                    pushNtf(chan_async_state.async_ok);
                    ntf(chan_async_state.async_ok, msg);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
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
                if (_has)
                {
                    T msg = _msg;
                    _has = false;
                    functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                    _pushWait.RemoveFirst();
                    pushNtf(chan_async_state.async_ok);
                    ntf(chan_async_state.async_ok, msg);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
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
                if (_has)
                {
                    T msg = _msg;
                    _has = false;
                    functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                    _pushWait.RemoveFirst();
                    pushNtf(chan_async_state.async_ok);
                    ntf(chan_async_state.async_ok, msg);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
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
                if (_has)
                {
                    ntf(chan_async_state.async_ok);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                }
                else
                {
                    ntfSign._ntfNode = _popWait.AddLast(delegate (chan_async_state state)
                    {
                        ntfSign._ntfNode = null;
                        ntf(state);
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

        public override void try_pop_and_append_notify(functional.same_func cb, functional.same_func msgNtf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_has)
                {
                    T msg = _msg;
                    _has = false;
                    functional.func<chan_async_state> pushNtf = _pushWait.First.Value;
                    _pushWait.RemoveFirst();
                    pushNtf(chan_async_state.async_ok);
                    if (!ntfSign._selectOnce)
                    {
                        append_pop_notify(msgNtf, ntfSign);
                    }
                    cb(chan_async_state.async_ok, msg);
                }
                else if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed);
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
                bool effect = null != ntfSign._ntfNode;
                if (effect)
                {
                    _popWait.Remove(ntfSign._ntfNode);
                    ntfSign._ntfNode = null;
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
                        ntfSign._ntfNode = null;
                        ntf(state);
                    });
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
                            if (!ntfSign._selectOnce)
                            {
                                append_push_notify(msgNtf, ntfSign);
                            }
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
                bool effect = null != ntfSign._ntfNode;
                if (effect)
                {
                    _pushWait.Remove(ntfSign._ntfNode);
                    ntfSign._ntfNode = null;
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

        public override void clear(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                _has = false;
                safe_callback(ref _pushWait, chan_async_state.async_fail);
                ntf();
            });
        }

        public override void close(functional.same_func ntf, bool isClear = false)
        {
            _strand.distribute(delegate ()
            {
                _closed = true;
                _has = false;
                safe_callback(ref _popWait, ref _pushWait, chan_async_state.async_closed);
                ntf();
            });
        }

        public override void cancel(functional.same_func ntf, bool isClear = false)
        {
            _strand.distribute(delegate ()
            {
                _has = false;
                safe_callback(ref _popWait, ref _pushWait, chan_async_state.async_cancel);
                ntf();
            });
        }

        public override shared_strand self_strand()
        {
            return _strand;
        }

        public override bool is_closed()
        {
            return _closed;
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
            init(strand);
        }

        public broadcast_chan()
        {
            shared_strand strand = generator.self_strand();
            init(null != strand ? strand : new shared_strand());
        }

        private void init(shared_strand strand)
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
                if (_has && token._lastId != _pushCount)
                {
                    if (!token.is_default())
                    {
                        token._lastId = _pushCount;
                    }
                    ntf(chan_async_state.async_ok, _msg);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
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
                if (_has && token._lastId != _pushCount)
                {
                    if (!token.is_default())
                    {
                        token._lastId = _pushCount;
                    }
                    ntf(chan_async_state.async_ok, _msg);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
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
                if (_has && token._lastId != _pushCount)
                {
                    if (!token.is_default())
                    {
                        token._lastId = _pushCount;
                    }
                    ntf(chan_async_state.async_ok, _msg);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
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
            if (_has && token._lastId != _pushCount)
            {
                if (!token.is_default())
                {
                    token._lastId = _pushCount;
                }
                ntf(chan_async_state.async_ok);
                return true;
            }
            else if (_closed)
            {
                return false;
            }
            else
            {
                ntfSign._ntfNode = _popWait.AddLast(delegate (chan_async_state state)
                {
                    ntfSign._ntfNode = null;
                    ntf(state);
                });
                return false;
            }
        }

        public override void try_pop_and_append_notify(functional.same_func cb, functional.same_func msgNtf, chan_notify_sign ntfSign)
        {
            try_pop_and_append_notify(cb, msgNtf, ntfSign, broadcast_chan_token._defToken);
        }

        public override void try_pop_and_append_notify(functional.same_func cb, functional.same_func msgNtf, chan_notify_sign ntfSign, broadcast_chan_token token)
        {
            _strand.distribute(delegate ()
            {
                if (_append_pop_notify(msgNtf, ntfSign, token))
                {
                    cb(chan_async_state.async_ok, _msg);
                }
                else if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed);
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
                bool effect = null != ntfSign._ntfNode;
                if (effect)
                {
                    _popWait.Remove(ntfSign._ntfNode);
                    ntfSign._ntfNode = null;
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
                ntf(chan_async_state.async_fail);
            });
        }

        public override void clear(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                _has = false;
                ntf();
            });
        }

        public override void close(functional.same_func ntf, bool isClear = false)
        {
            _strand.distribute(delegate ()
            {
                _closed = true;
                _has &= !isClear;
                safe_callback(ref _popWait, chan_async_state.async_closed);
                ntf();
            });
        }

        public override void cancel(functional.same_func ntf, bool isClear = false)
        {
            _strand.distribute(delegate ()
            {
                _has &= !isClear;
                safe_callback(ref _popWait, chan_async_state.async_cancel);
                ntf();
            });
        }

        public override shared_strand self_strand()
        {
            return _strand;
        }

        public override bool is_closed()
        {
            return _closed;
        }
    }

    public class csp_chan<R, T> : channel<T>
    {
        struct send_pck
        {
            public functional.same_func _ntf;
            public T _msg;
            async_timer _timer;

            public void set(functional.same_func ntf, T msg, async_timer timer)
            {
                _ntf = ntf;
                _msg = msg;
                _timer = timer;
            }

            public void set(functional.same_func ntf, T msg)
            {
                _ntf = ntf;
                _msg = msg;
                _timer = null;
            }

            public void cancel_timer()
            {
                if (null != _timer)
                {
                    _timer.cancel();
                }
            }
        }

        public struct csp_result
        {
            functional.same_func _notify;

            public csp_result(functional.same_func notify)
            {
                _notify = notify;
            }

            public void complete(R res)
            {
                _notify(chan_async_state.async_ok, res);
                _notify = null;
            }
        }

        public class select_csp_reader : select_chan_base
        {
            public csp_chan<R, T> _chan;
            public functional.func_res<Task, csp_result, T> _handler;
            public functional.func_res<Task<bool>, chan_async_state> _errHandler;

            public override void begin()
            {
                ntfSign._disable = false;
                _chan.append_pop_notify(delegate (object[] args)
                {
                    nextSelect.post(this);
                }, ntfSign);
            }

            public override async Task<select_chan_state> invoke(functional.func_res<Task> stepOne)
            {
                generator self = generator.self;
                csp_wait_wrap<R, T> result = default(csp_wait_wrap<R, T>);
                _chan.try_pop_and_append_notify(self.async_same_callback(delegate (object[] args)
                {
                    result.state = (chan_async_state)args[0];
                    if (chan_async_state.async_ok == result.state)
                    {
                        result.result = (csp_chan<R, T>.csp_result)(args[1]);
                        result.msg = (T)args[2];
                    }
                }), delegate (object[] args)
                {
                    nextSelect.post(this);
                }, ntfSign);
                await self.async_wait();
                select_chan_state chanState;
                chanState.failed = false;
                chanState.nextRound = true;
                if (chan_async_state.async_ok == result.state)
                {
                    if (null != stepOne)
                    {
                        await stepOne();
                    }
                    await _handler(result.result, result.msg);
                }
                else if (null != _errHandler)
                {
                    if (await _errHandler(result.state))
                    {
                        await end();
                        chanState.failed = true;
                    }
                }
                else if (chan_async_state.async_closed == result.state)
                {
                    await end();
                    chanState.failed = true;
                }
                else
                {
                    chanState.failed = true;
                }
                chanState.nextRound = !ntfSign._disable;
                return chanState;
            }

            public override Task end()
            {
                generator self = generator.self;
                ntfSign._disable = true;
                _chan.remove_pop_notify(self.async_same_callback(), ntfSign);
                return self.async_wait();
            }

            public override bool is_read()
            {
                return true;
            }

            public override channel_base channel()
            {
                return _chan;
            }
        }

        public class select_csp_writer : select_chan_base
        {
            public csp_chan<R, T> _chan;
            public functional.func_res<T> _msg;
            public functional.func_res<Task, R> _handler;
            public functional.func_res<Task<bool>, chan_async_state> _errHandler;

            public override void begin()
            {
                ntfSign._disable = false;
                _chan.append_push_notify(delegate (object[] args)
                {
                    nextSelect.post(this);
                }, ntfSign);
            }

            public override async Task<select_chan_state> invoke(functional.func_res<Task> stepOne)
            {
                generator self = generator.self;
                csp_invoke_wrap<R> result = default(csp_invoke_wrap<R>);
                _chan.try_push_and_append_notify(self.async_same_callback(delegate (object[] args)
                {
                    result.state = (chan_async_state)args[0];
                    if (chan_async_state.async_ok == result.state)
                    {
                        result.result = (R)args[1];
                    }
                }), delegate (object[] args)
                {
                    nextSelect.post(this);
                }, ntfSign, _msg());
                await self.async_wait();
                select_chan_state chanState;
                chanState.failed = false;
                chanState.nextRound = true;
                if (chan_async_state.async_ok == result.state)
                {
                    if (null != stepOne)
                    {
                        await stepOne();
                    }
                    await _handler(result.result);
                }
                else if (null != _errHandler)
                {
                    if (await _errHandler(result.state))
                    {
                        await end();
                        chanState.failed = true;
                    }
                }
                else if (chan_async_state.async_closed == result.state)
                {
                    await end();
                    chanState.failed = true;
                }
                else
                {
                    chanState.failed = true;
                }
                chanState.nextRound = !ntfSign._disable;
                return chanState;
            }

            public override Task end()
            {
                generator self = generator.self;
                ntfSign._disable = true;
                _chan.remove_push_notify(self.async_same_callback(), ntfSign);
                return self.async_wait();
            }

            public override bool is_read()
            {
                return false;
            }

            public override channel_base channel()
            {
                return _chan;
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
            init(strand);
        }

        public csp_chan()
        {
            shared_strand strand = generator.self_strand();
            init(null != strand ? strand : new shared_strand());
        }

        private void init(shared_strand strand)
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

        public select_chan_base make_select_writer(functional.func_res<T> msg, functional.func_res<Task, R> handler)
        {
            select_csp_writer sel = new select_csp_writer();
            sel._chan = this;
            sel._msg = msg;
            sel._handler = handler;
            return sel;
        }

        public select_chan_base make_select_writer(functional.func_res<T> msg, functional.func_res<Task, R> handler, functional.func_res<Task<bool>, chan_async_state> errHandler)
        {
            select_csp_writer sel = new select_csp_writer();
            sel._chan = this;
            sel._msg = msg;
            sel._handler = handler;
            sel._errHandler = errHandler;
            return sel;
        }

        public select_chan_base make_select_writer(async_result_wrap<T> msg, functional.func_res<Task, R> handler)
        {
            return make_select_writer(() => msg.value_1, handler);
        }

        public select_chan_base make_select_writer(async_result_wrap<T> msg, functional.func_res<Task, R> handler, functional.func_res<Task<bool>, chan_async_state> errHandler)
        {
            return make_select_writer(() => msg.value_1, handler, errHandler);
        }

        public select_chan_base make_select_writer(T msg, functional.func_res<Task, R> handler)
        {
            return make_select_writer(() => msg, handler);
        }

        public select_chan_base make_select_writer(T msg, functional.func_res<Task, R> handler, functional.func_res<Task<bool>, chan_async_state> errHandler)
        {
            return make_select_writer(() => msg, handler, errHandler);
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
                    _msg.set(ntf, msg);
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
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
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
                        _msg.set(ntf, msg);
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
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
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
                    _msg.set(ntf, msg, timer);
                    timer.timeout(ms, delegate ()
                    {
                        _msg.cancel_timer();
                        functional.same_func ntf_ = _msg._ntf;
                        _has = false;
                        ntf_(chan_async_state.async_overtime);
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
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
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
                if (_has)
                {
                    ntf(chan_async_state.async_ok);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                }
                else
                {
                    ntfSign._ntfNode = _waitQueue.AddLast(delegate (chan_async_state state)
                    {
                        ntfSign._ntfNode = null;
                        ntf(state);
                    });
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
                    if (!ntfSign._selectOnce)
                    {
                        append_pop_notify(msgNtf, ntfSign);
                    }
                    cb(chan_async_state.async_ok, new csp_result(pck._ntf), pck._msg);
                }
                else if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed);
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
                bool effect = null != ntfSign._ntfNode;
                if (effect)
                {
                    _waitQueue.Remove(ntfSign._ntfNode);
                    ntfSign._ntfNode = null;
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
                        functional.same_func ntf_ = _msg._ntf;
                        _has = false;
                        ntf_(chan_async_state.async_fail);
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
                        ntfSign._ntfNode = null;
                        ntf(state);
                    });
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
                        _msg.set(cb, msg);
                        if (!ntfSign._selectOnce)
                        {
                            append_push_notify(msgNtf, ntfSign);
                        }
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
                bool effect = null != ntfSign._ntfNode;
                if (effect)
                {
                    _sendQueue.Remove(ntfSign._ntfNode);
                    ntfSign._ntfNode = null;
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

        public override void clear(functional.same_func ntf)
        {
            _strand.distribute(delegate ()
            {
                _has = false;
                safe_callback(ref _sendQueue, chan_async_state.async_fail);
                ntf();
            });
        }

        public override void close(functional.same_func ntf, bool isClear = false)
        {
            _strand.distribute(delegate ()
            {
                _closed = true;
                functional.same_func hasMsg = null;
                if (_has)
                {
                    _msg.cancel_timer();
                    hasMsg = _msg._ntf;
                    _has = false;
                }
                safe_callback(ref _sendQueue, ref _waitQueue, chan_async_state.async_closed);
                hasMsg?.Invoke(chan_async_state.async_closed);
                ntf();
            });
        }

        public override void cancel(functional.same_func ntf, bool isClear = false)
        {
            _strand.distribute(delegate ()
            {
                functional.same_func hasMsg = null;
                if (_has)
                {
                    _msg.cancel_timer();
                    hasMsg = _msg._ntf;
                    _has = false;
                }
                safe_callback(ref _sendQueue, ref _waitQueue, chan_async_state.async_cancel);
                hasMsg?.Invoke(chan_async_state.async_cancel);
                ntf();
            });
        }

        public override shared_strand self_strand()
        {
            return _strand;
        }

        public override bool is_closed()
        {
            return _closed;
        }
    }
}
