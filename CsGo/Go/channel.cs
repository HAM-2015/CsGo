using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Go
{
    using option_node = LinkedListNode<notify_pck>;

    public enum chan_async_state
    {
        async_undefined = -1,
        async_ok = 0,
        async_fail,
        async_csp_fail,
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

    struct notify_pck
    {
        public Action<chan_async_state> ntf;

        public void Invoke(chan_async_state state)
        {
            ntf(state);
        }
    }

    internal class chan_notify_sign
    {
        public option_node _ntfNode;
        public bool _selectOnce = false;
        public bool _disable = false;
        public bool _success = false;

        public void set(option_node node)
        {
            _ntfNode = node;
        }

        public void clear()
        {
            _ntfNode = null;
        }

        public void reset_success()
        {
            _success = false;
        }

        static public void set_node(chan_notify_sign sign, option_node node)
        {
            if (null != sign)
            {
                sign._ntfNode = node;
            }
        }
    }

    public struct select_chan_state
    {
        public bool failed;
        public bool nextRound;
    }

    internal abstract class select_chan_base
    {
        public chan_notify_sign ntfSign = new chan_notify_sign();
        public Action<chan_async_state> nextSelect;
        public bool disabled() { return ntfSign._disable; }
        public abstract void begin();
        public abstract Task<select_chan_state> invoke(Func<Task> stepOne = null);
        public abstract Task end();
        public abstract bool is_read();
        public abstract channel_base channel();
    }

    public abstract class channel_base
    {
        public abstract chan_type type();
        internal abstract void clear(Action ntf);
        internal abstract void close(Action ntf, bool isClear = false);
        internal abstract void cancel(Action ntf, bool isClear = false);
        public abstract shared_strand self_strand();
        public abstract bool is_closed();
        internal abstract void append_pop_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign);
        internal abstract void remove_pop_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign);
        internal abstract void append_push_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign);
        internal abstract void remove_push_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign);
        public void clear() { clear(nil_action.action); }
        public void close(bool isClear = false) { close(nil_action.action, isClear); }
        public void cancel(bool isClear = false) { cancel(nil_action.action, isClear); }

        static internal void safe_callback(ref LinkedList<notify_pck> callback, chan_async_state state)
        {
            if (null != callback && 0 != callback.Count)
            {
                LinkedList<notify_pck> tempCb = callback;
                callback = chan_async_state.async_closed == state ? null : new LinkedList<notify_pck>();
                for (option_node it = tempCb.First; null != it; it = it.Next)
                {
                    it.Value.Invoke(state);
                }
            }
        }

        static internal void safe_callback(ref LinkedList<notify_pck> callback1, ref LinkedList<notify_pck> callback2, chan_async_state state)
        {
            LinkedList<notify_pck> tempCb1 = null;
            LinkedList<notify_pck> tempCb2 = null;
            if (null != callback1 && 0 != callback1.Count)
            {
                tempCb1 = callback1;
                callback1 = chan_async_state.async_closed == state ? null : new LinkedList<notify_pck>();
            }
            if (null != callback2 && 0 != callback2.Count)
            {
                tempCb2 = callback2;
                callback2 = chan_async_state.async_closed == state ? null : new LinkedList<notify_pck>();
            }
            if (null != tempCb1)
            {
                for (option_node it = tempCb1.First; null != it; it = it.Next)
                {
                    it.Value.Invoke(state);
                }
            }
            if (null != tempCb2)
            {
                for (option_node it = tempCb2.First; null != it; it = it.Next)
                {
                    it.Value.Invoke(state);
                }
            }
        }
    }

    public abstract class channel<T> : channel_base
    {
        internal class select_chan_reader : select_chan_base
        {
            public broadcast_chan_token _token = broadcast_chan_token._defToken;
            public channel<T> _chan;
            public Func<T, Task> _handler;
            public Func<chan_async_state, Task<bool>> _errHandler;
            public Func<T, Task> _lostHandler;
            chan_recv_wrap<T> _tempResult = default(chan_recv_wrap<T>);
            Action<chan_async_state, T, object> _tryPushHandler;
            generator _host;

            public override void begin()
            {
                ntfSign._disable = false;
                _host = generator.self;
                _chan.append_pop_notify(nextSelect, ntfSign);
            }

            public override async Task<select_chan_state> invoke(Func<Task> stepOne)
            {
                if (null == _tryPushHandler)
                {
                    _tryPushHandler = delegate (chan_async_state state, T msg, object _)
                    {
                        _tempResult.state = state;
                        if (chan_async_state.async_ok == state)
                        {
                            _tempResult.msg = msg;
                        }
                    };
                }
                try
                {
                    _tempResult = chan_recv_wrap<T>.undefined();
                    _chan.try_pop_and_append_notify(_host.async_callback(_tryPushHandler), nextSelect, ntfSign, _token);
                    await _host.async_wait();
                }
                catch (generator.stop_exception)
                {
                    _chan.remove_pop_notify(_host.async_ignore<chan_async_state>(), ntfSign);
                    await _host.async_wait();
                    if (chan_async_state.async_ok == _tempResult.state && null != _lostHandler)
                    {
                        await _lostHandler(_tempResult.msg);
                    }
                    throw;
                }
                select_chan_state chanState = new select_chan_state() { failed = false, nextRound = true };
                if (chan_async_state.async_ok == _tempResult.state)
                {
                    if (null != stepOne)
                    {
                        await stepOne();
                    }
                    try
                    {
                        await generator.unlock_suspend();
                        await _handler(_tempResult.msg);
                    }
                    finally
                    {
                        generator.lock_suspend();
                    }
                }
                else if (null != _errHandler)
                {
                    try
                    {
                        await generator.unlock_suspend();
                        chanState.failed = await _errHandler(_tempResult.state);
                    }
                    finally
                    {
                        generator.lock_suspend();
                        if (chanState.failed)
                        {
                            await end();
                        }
                    }
                }
                else if (chan_async_state.async_closed == _tempResult.state)
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
                ntfSign._disable = true;
                _chan.remove_pop_notify(_host.async_ignore<chan_async_state>(), ntfSign);
                return _host.async_wait();
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

        internal class select_chan_writer : select_chan_base
        {
            public channel<T> _chan;
            public Func<T> _msg;
            public Func<Task> _handler;
            public Func<chan_async_state, Task<bool>> _errHandler;
            chan_async_state _tempResult = chan_async_state.async_undefined;
            Action<chan_async_state, object> _tryPushHandler;
            generator _host;

            public override void begin()
            {
                ntfSign._disable = false;
                _host = generator.self;
                _chan.append_push_notify(nextSelect, ntfSign);
            }

            public override async Task<select_chan_state> invoke(Func<Task> stepOne)
            {
                if (null == _tryPushHandler)
                {
                    _tryPushHandler = (chan_async_state state, object _) => _tempResult = state;
                }
                _tempResult = chan_async_state.async_undefined;
                _chan.try_push_and_append_notify(_host.async_callback(_tryPushHandler), nextSelect, ntfSign, _msg());
                await _host.async_wait();
                select_chan_state chanState = new select_chan_state() { failed = false, nextRound = true };
                if (chan_async_state.async_ok == _tempResult)
                {
                    if (null != stepOne)
                    {
                        await stepOne();
                    }
                    try
                    {
                        await generator.unlock_suspend();
                        await _handler();
                    }
                    finally
                    {
                        generator.lock_suspend();
                    }
                }
                else if (null != _errHandler)
                {
                    try
                    {
                        await generator.unlock_suspend();
                        chanState.failed = await _errHandler(_tempResult);
                    }
                    finally
                    {
                        generator.lock_suspend();
                        if (chanState.failed)
                        {
                            await end();
                        }
                    }
                }
                else if (chan_async_state.async_closed == _tempResult)
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
                ntfSign._disable = true;
                _chan.remove_push_notify(_host.async_ignore<chan_async_state>(), ntfSign);
                return _host.async_wait();
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

        internal abstract void push(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign);
        internal abstract void pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign);
        internal abstract void try_push(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign);
        internal abstract void try_pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign);
        internal abstract void timed_push(int ms, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign);
        internal abstract void timed_pop(int ms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign);
        internal abstract void try_pop_and_append_notify(Action<chan_async_state, T, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign);
        internal abstract void try_push_and_append_notify(Action<chan_async_state, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, T msg);

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
            push(nil_action<chan_async_state, object>.action, msg, null);
        }

        public void try_post(T msg)
        {
            try_push(nil_action<chan_async_state, object>.action, msg, null);
        }

        public void timed_post(int ms, T msg)
        {
            timed_push(ms, nil_action<chan_async_state, object>.action, msg, null);
        }

        public void discard()
        {
            pop(nil_action<chan_async_state, T, object>.action, null);
        }

        public void try_discard()
        {
            try_pop(nil_action<chan_async_state, T, object>.action, null);
        }

        public void timed_discard(int ms)
        {
            timed_pop(ms, nil_action<chan_async_state, T, object>.action, null);
        }

        public Action<T> wrap()
        {
            return post;
        }

        public Action<T> wrap_try()
        {
            return try_post;
        }

        public Action<int, T> wrap_timed()
        {
            return timed_post;
        }

        public Action<T> wrap_timed(int ms)
        {
            return (T p) => timed_post(ms, p);
        }

        public Action wrap_default()
        {
            return () => post(default(T));
        }

        public Action wrap_try_default()
        {
            return () => try_post(default(T));
        }

        public Action<int> wrap_timed_default()
        {
            return (int ms) => timed_post(ms, default(T));
        }

        public Action wrap_timed_default(int ms)
        {
            return () => timed_post(ms, default(T));
        }

        public Action wrap_discard()
        {
            return discard;
        }

        public Action wrap_try_discard()
        {
            return try_discard;
        }

        public Action<int> wrap_timed_discard()
        {
            return timed_discard;
        }

        public Action wrap_timed_discard(int ms)
        {
            return () => timed_discard(ms);
        }

        internal select_chan_base make_select_reader(Func<T, Task> handler, Func<T, Task> lostHandler = null)
        {
            return new select_chan_reader() { _chan = this, _handler = handler, _lostHandler = lostHandler };
        }

        internal select_chan_base make_select_reader(Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler, Func<T, Task> lostHandler = null)
        {
            return new select_chan_reader() { _chan = this, _handler = handler, _errHandler = errHandler, _lostHandler = lostHandler };
        }

        internal select_chan_base make_select_writer(Func<T> msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler)
        {
            return new select_chan_writer() { _chan = this, _msg = msg, _handler = handler, _errHandler = errHandler };
        }

        internal select_chan_base make_select_writer(async_result_wrap<T> msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler)
        {
            return make_select_writer(() => msg.value1, handler, errHandler);
        }

        internal select_chan_base make_select_writer(T msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler)
        {
            return make_select_writer(() => msg, handler, errHandler);
        }

        internal virtual void pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign, broadcast_chan_token token)
        {
            pop(ntf, ntfSign);
        }

        internal virtual void try_pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign, broadcast_chan_token token)
        {
            try_pop(ntf, ntfSign);
        }

        internal virtual void timed_pop(int ms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign, broadcast_chan_token token)
        {
            timed_pop(ms, ntf, ntfSign);
        }

        internal virtual void append_pop_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign, broadcast_chan_token token)
        {
            append_pop_notify(ntf, ntfSign);
        }

        internal virtual void try_pop_and_append_notify(Action<chan_async_state, T, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, broadcast_chan_token token)
        {
            try_pop_and_append_notify(cb, msgNtf, ntfSign);
        }

        internal virtual select_chan_base make_select_reader(Func<T, Task> handler, broadcast_chan_token token)
        {
            return make_select_reader(handler);
        }

        internal virtual select_chan_base make_select_reader(Func<T, Task> handler, broadcast_chan_token token, Func<chan_async_state, Task<bool>> errHandler)
        {
            return make_select_reader(handler, errHandler);
        }
    }

    abstract class MsgQueue_<T>
    {
        public abstract void AddLast(T msg);
        public abstract T First();
        public abstract void RemoveFirst();
        public abstract int Count { get; }
        public abstract void Clear();
    }

    class NoVoidMsgQueue_<T> : MsgQueue_<T>
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

    class VoidMsgQueue_<T> : MsgQueue_<T>
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
        LinkedList<notify_pck> _waitQueue;
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
            _waitQueue = new LinkedList<notify_pck>();
        }

        public override chan_type type()
        {
            return chan_type.nolimit;
        }

        internal override void push(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (_closed)
                {
                    ntf(chan_async_state.async_closed, null);
                    return;
                }
                _buffer.AddLast(msg);
                if (0 != _waitQueue.Count)
                {
                    notify_pck wtNtf = _waitQueue.First.Value;
                    _waitQueue.RemoveFirst();
                    wtNtf.Invoke(chan_async_state.async_ok);
                }
                ntf(chan_async_state.async_ok, null);
            });
        }

        internal override void pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (0 != _buffer.Count)
                {
                    T msg = _buffer.First();
                    _buffer.RemoveFirst();
                    ntf(chan_async_state.async_ok, msg, null);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed, default(T), null);
                }
                else
                {
                    chan_notify_sign.set_node(ntfSign, _waitQueue.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            if (chan_async_state.async_ok == state)
                            {
                                pop(ntf, ntfSign);
                            }
                            else
                            {
                                ntf(state, default(T), null);
                            }
                        }
                    }));
                }
            });
        }

        internal override void try_push(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            push(ntf, msg, ntfSign);
        }

        internal override void try_pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (0 != _buffer.Count)
                {
                    T msg = _buffer.First();
                    _buffer.RemoveFirst();
                    ntf(chan_async_state.async_ok, msg, null);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed, default(T), null);
                }
                else
                {
                    ntf(chan_async_state.async_fail, default(T), null);
                }
            });
        }

        internal override void timed_push(int ms, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            push(ntf, msg, ntfSign);
        }

        internal override void timed_pop(int ms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (0 != _buffer.Count)
                {
                    T msg = _buffer.First();
                    _buffer.RemoveFirst();
                    ntf(chan_async_state.async_ok, msg, null);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed, default(T), null);
                }
                else if (ms >= 0)
                {
                    async_timer timer = new async_timer(_strand);
                    option_node node = _waitQueue.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            timer.cancel();
                            if (chan_async_state.async_ok == state)
                            {
                                pop(ntf, ntfSign);
                            }
                            else
                            {
                                ntf(state, default(T), null);
                            }
                        }
                    });
                    ntfSign?.set(node);
                    timer.timeout(ms, delegate ()
                    {
                        ntfSign?.clear();
                        notify_pck popNtf = node.Value;
                        _waitQueue.Remove(node);
                        popNtf.Invoke(chan_async_state.async_overtime);
                    });
                }
                else
                {
                    chan_notify_sign.set_node(ntfSign, _waitQueue.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            if (chan_async_state.async_ok == state)
                            {
                                pop(ntf, ntfSign);
                            }
                            else
                            {
                                ntf(state, default(T), null);
                            }
                        }
                    }));
                }
            });
        }

        internal override void append_pop_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
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
                    ntfSign._ntfNode = _waitQueue.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign._ntfNode = null;
                            ntfSign._success = chan_async_state.async_ok == state;
                            ntf(state);
                        }
                    });
                }
            });
        }

        internal override void try_pop_and_append_notify(Action<chan_async_state, T, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign.reset_success();
                if (0 != _buffer.Count)
                {
                    T msg = _buffer.First();
                    _buffer.RemoveFirst();
                    if (!ntfSign._selectOnce)
                    {
                        append_pop_notify(msgNtf, ntfSign);
                    }
                    cb(chan_async_state.async_ok, msg, null);
                }
                else if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed, default(T), null);
                }
                else
                {
                    append_pop_notify(msgNtf, ntfSign);
                    cb(chan_async_state.async_fail, default(T), null);
                }
            });
        }

        internal override void remove_pop_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                bool effect = null != ntfSign._ntfNode;
                bool success = ntfSign._success;
                ntfSign._success = false;
                if (effect)
                {
                    _waitQueue.Remove(ntfSign._ntfNode);
                    ntfSign._ntfNode = null;
                }
                if (success && 0 != _buffer.Count && 0 != _waitQueue.Count)
                {
                    notify_pck wtNtf = _waitQueue.First.Value;
                    _waitQueue.RemoveFirst();
                    wtNtf.Invoke(chan_async_state.async_ok);
                }
                ntf(effect || success ? chan_async_state.async_ok : chan_async_state.async_fail);
            });
        }

        internal override void append_push_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
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

        internal override void try_push_and_append_notify(Action<chan_async_state, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, T msg)
        {
            _strand.distribute(delegate ()
            {
                ntfSign.reset_success();
                if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed, null);
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
                cb(chan_async_state.async_ok, null);
            });
        }

        internal override void remove_push_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntf(chan_async_state.async_fail);
            });
        }

        internal override void clear(Action ntf)
        {
            _strand.distribute(delegate ()
            {
                _buffer.Clear();
                ntf();
            });
        }

        internal override void close(Action ntf, bool isClear = false)
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

        internal override void cancel(Action ntf, bool isClear = false)
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
        LinkedList<notify_pck> _pushWait;
        LinkedList<notify_pck> _popWait;
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
            _pushWait = new LinkedList<notify_pck>();
            _popWait = new LinkedList<notify_pck>();
            _length = len;
            _closed = false;
        }

        public override chan_type type()
        {
            return chan_type.limit;
        }

        internal override void push(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (_closed)
                {
                    ntf(chan_async_state.async_closed, null);
                    return;
                }
                if (_buffer.Count == _length)
                {
                    chan_notify_sign.set_node(ntfSign, _pushWait.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            if (chan_async_state.async_ok == state)
                            {
                                push(ntf, msg, ntfSign);
                            }
                            else
                            {
                                ntf(state, null);
                            }
                        }
                    }));
                }
                else
                {
                    _buffer.AddLast(msg);
                    if (0 != _popWait.Count)
                    {
                        notify_pck popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf.Invoke(chan_async_state.async_ok);
                    }
                    ntf(chan_async_state.async_ok, null);
                }
            });
        }

        internal void force_push(Action<chan_async_state, bool, T> ntf, T msg)
        {
            _strand.distribute(delegate ()
            {
                if (_closed)
                {
                    ntf(chan_async_state.async_closed, false, default(T));
                    return;
                }
                bool hasOut = false;
                T outMsg = default(T);
                if (_buffer.Count == _length)
                {
                    hasOut = true;
                    outMsg = _buffer.First();
                    _buffer.RemoveFirst();
                }
                _buffer.AddLast(msg);
                if (0 != _popWait.Count)
                {
                    notify_pck popNtf = _popWait.First.Value;
                    _popWait.RemoveFirst();
                    popNtf.Invoke(chan_async_state.async_ok);
                }
                if (hasOut)
                {
                    ntf(chan_async_state.async_ok, true, outMsg);
                }
                else
                {
                    ntf(chan_async_state.async_ok, false, default(T));
                }
            });
        }

        internal override void pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (0 != _buffer.Count)
                {
                    T msg = _buffer.First();
                    _buffer.RemoveFirst();
                    if (0 != _pushWait.Count)
                    {
                        notify_pck pushNtf = _pushWait.First.Value;
                        _pushWait.RemoveFirst();
                        pushNtf.Invoke(chan_async_state.async_ok);
                    }
                    ntf(chan_async_state.async_ok, msg, null);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed, default(T), null);
                }
                else
                {
                    chan_notify_sign.set_node(ntfSign, _popWait.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            if (chan_async_state.async_ok == state)
                            {
                                pop(ntf, ntfSign);
                            }
                            else
                            {
                                ntf(state, default(T), null);
                            }
                        }
                    }));
                }
            });
        }

        internal override void try_push(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (_closed)
                {
                    ntf(chan_async_state.async_closed, null);
                    return;
                }
                if (_buffer.Count == _length)
                {
                    ntf(chan_async_state.async_fail, null);
                }
                else
                {
                    _buffer.AddLast(msg);
                    if (0 != _popWait.Count)
                    {
                        notify_pck popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf.Invoke(chan_async_state.async_ok);
                    }
                    ntf(chan_async_state.async_ok, null);
                }
            });
        }

        internal override void try_pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (0 != _buffer.Count)
                {
                    T msg = _buffer.First();
                    _buffer.RemoveFirst();
                    if (0 != _pushWait.Count)
                    {
                        notify_pck pushNtf = _pushWait.First.Value;
                        _pushWait.RemoveFirst();
                        pushNtf.Invoke(chan_async_state.async_ok);
                    }
                    ntf(chan_async_state.async_ok, msg, null);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed, default(T), null);
                }
                else
                {
                    ntf(chan_async_state.async_fail, default(T), null);
                }
            });
        }

        internal override void timed_push(int ms, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (_buffer.Count == _length)
                {
                    if (ms >= 0)
                    {
                        async_timer timer = new async_timer(_strand);
                        option_node node = _pushWait.AddLast(new notify_pck()
                        {
                            ntf = delegate (chan_async_state state)
                            {
                                ntfSign?.clear();
                                timer.cancel();
                                if (chan_async_state.async_ok == state)
                                {
                                    push(ntf, msg, ntfSign);
                                }
                                else
                                {
                                    ntf(state, null);
                                }
                            }
                        });
                        ntfSign?.set(node);
                        timer.timeout(ms, delegate ()
                        {
                            ntfSign?.clear();
                            notify_pck pushWait = node.Value;
                            _pushWait.Remove(node);
                            pushWait.Invoke(chan_async_state.async_overtime);
                        });
                    }
                    else
                    {
                        chan_notify_sign.set_node(ntfSign, _pushWait.AddLast(new notify_pck()
                        {
                            ntf = delegate (chan_async_state state)
                            {
                                ntfSign?.clear();
                                if (chan_async_state.async_ok == state)
                                {
                                    push(ntf, msg, ntfSign);
                                }
                                else
                                {
                                    ntf(state, null);
                                }
                            }
                        }));
                    }
                }
                else
                {
                    _buffer.AddLast(msg);
                    if (0 != _popWait.Count)
                    {
                        notify_pck popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf.Invoke(chan_async_state.async_ok);
                    }
                    ntf(chan_async_state.async_ok, null);
                }
            });
        }

        internal override void timed_pop(int ms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (0 != _buffer.Count)
                {
                    T msg = _buffer.First();
                    _buffer.RemoveFirst();
                    if (0 != _pushWait.Count)
                    {
                        notify_pck pushNtf = _pushWait.First.Value;
                        _pushWait.RemoveFirst();
                        pushNtf.Invoke(chan_async_state.async_ok);
                    }
                    ntf(chan_async_state.async_ok, msg, null);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed, default(T), null);
                }
                else if (ms >= 0)
                {
                    async_timer timer = new async_timer(_strand);
                    option_node node = _popWait.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            timer.cancel();
                            if (chan_async_state.async_ok == state)
                            {
                                pop(ntf, ntfSign);
                            }
                            else
                            {
                                ntf(state, default(T), null);
                            }
                        }
                    });
                    ntfSign?.set(node);
                    timer.timeout(ms, delegate ()
                    {
                        ntfSign?.clear();
                        notify_pck popNtf = node.Value;
                        _popWait.Remove(node);
                        popNtf.Invoke(chan_async_state.async_overtime);
                    });
                }
                else
                {
                    chan_notify_sign.set_node(ntfSign, _popWait.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            if (chan_async_state.async_ok == state)
                            {
                                pop(ntf, ntfSign);
                            }
                            else
                            {
                                ntf(state, default(T), null);
                            }
                        }
                    }));
                }
            });
        }

        internal override void append_pop_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
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
                    ntfSign._ntfNode = _popWait.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign._ntfNode = null;
                            ntfSign._success = chan_async_state.async_ok == state;
                            ntf(state);
                        }
                    });
                }
            });
        }

        internal override void try_pop_and_append_notify(Action<chan_async_state, T, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign.reset_success();
                if (0 != _buffer.Count)
                {
                    T msg = _buffer.First();
                    _buffer.RemoveFirst();
                    if (0 != _pushWait.Count)
                    {
                        notify_pck pushNtf = _pushWait.First.Value;
                        _pushWait.RemoveFirst();
                        pushNtf.Invoke(chan_async_state.async_ok);
                    }
                    if (!ntfSign._selectOnce)
                    {
                        append_pop_notify(msgNtf, ntfSign);
                    }
                    cb(chan_async_state.async_ok, msg, null);
                }
                else if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed, default(T), null);
                }
                else
                {
                    append_pop_notify(msgNtf, ntfSign);
                    cb(chan_async_state.async_fail, default(T), null);
                }
            });
        }
        internal override void remove_pop_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                bool effect = null != ntfSign._ntfNode;
                bool success = ntfSign._success;
                ntfSign._success = false;
                if (effect)
                {
                    _popWait.Remove(ntfSign._ntfNode);
                    ntfSign._ntfNode = null;
                }
                if (success && 0 != _buffer.Count && 0 != _popWait.Count)
                {
                    notify_pck popNtf = _popWait.First.Value;
                    _popWait.RemoveFirst();
                    popNtf.Invoke(chan_async_state.async_ok);
                }
                ntf(effect || success ? chan_async_state.async_ok : chan_async_state.async_fail);
            });
        }

        internal override void append_push_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
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
                    ntfSign._ntfNode = _pushWait.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign._ntfNode = null;
                            ntfSign._success = chan_async_state.async_ok == state;
                            ntf(state);
                        }
                    });
                }
            });
        }

        internal override void try_push_and_append_notify(Action<chan_async_state, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, T msg)
        {
            _strand.distribute(delegate ()
            {
                ntfSign.reset_success();
                if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed, null);
                    return;
                }
                if (_buffer.Count != _length)
                {
                    _buffer.AddLast(msg);
                    if (0 != _popWait.Count)
                    {
                        notify_pck popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf.Invoke(chan_async_state.async_ok);
                    }
                    if (!ntfSign._selectOnce)
                    {
                        append_push_notify(msgNtf, ntfSign);
                    }
                    cb(chan_async_state.async_ok, null);
                }
                else
                {
                    append_push_notify(msgNtf, ntfSign);
                    cb(chan_async_state.async_fail, null);
                }
            });
        }

        internal override void remove_push_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                bool effect = null != ntfSign._ntfNode;
                bool success = ntfSign._success;
                ntfSign._success = false;
                if (effect)
                {
                    _pushWait.Remove(ntfSign._ntfNode);
                    ntfSign._ntfNode = null;
                }
                if (success && _buffer.Count != _length && 0 != _pushWait.Count)
                {
                    notify_pck pushNtf = _pushWait.First.Value;
                    _pushWait.RemoveFirst();
                    pushNtf.Invoke(chan_async_state.async_ok);
                }
                ntf(effect || success ? chan_async_state.async_ok : chan_async_state.async_fail);
            });
        }

        internal override void clear(Action ntf)
        {
            _strand.distribute(delegate ()
            {
                _buffer.Clear();
                safe_callback(ref _pushWait, chan_async_state.async_fail);
                ntf();
            });
        }

        internal override void close(Action ntf, bool isClear = false)
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

        internal override void cancel(Action ntf, bool isClear = false)
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
        LinkedList<notify_pck> _pushWait;
        LinkedList<notify_pck> _popWait;
        T _msg;
        bool _isTryPush;
        bool _isTryPop;
        bool _has;
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
            _pushWait = new LinkedList<notify_pck>();
            _popWait = new LinkedList<notify_pck>();
            _isTryPush = false;
            _isTryPop = false;
            _has = false;
            _closed = false;
        }

        public override chan_type type()
        {
            return chan_type.nil;
        }

        internal override void push(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (_closed)
                {
                    ntf(chan_async_state.async_closed, null);
                    return;
                }
                if (_has)
                {
                    chan_notify_sign.set_node(ntfSign, _pushWait.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            if (chan_async_state.async_ok == state)
                            {
                                push(ntf, msg, ntfSign);
                            }
                            else
                            {
                                ntf(state, null);
                            }
                        }
                    }));
                }
                else
                {
                    _msg = msg;
                    _has = true;
                    chan_notify_sign.set_node(ntfSign, _pushWait.AddFirst(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            if (chan_async_state.async_closed != state && chan_async_state.async_cancel != state && 0 != _pushWait.Count)
                            {
                                notify_pck pushNtf = _pushWait.First.Value;
                                _pushWait.RemoveFirst();
                                pushNtf.Invoke(chan_async_state.async_ok);
                            }
                            ntf(state, null);
                        }
                    }));
                    if (0 != _popWait.Count)
                    {
                        notify_pck popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf.Invoke(chan_async_state.async_ok);
                    }
                }
            });
        }

        internal override void pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (_has)
                {
                    T msg = _msg;
                    _has = false;
                    notify_pck pushNtf = _pushWait.First.Value;
                    _pushWait.RemoveFirst();
                    pushNtf.Invoke(chan_async_state.async_ok);
                    ntf(chan_async_state.async_ok, msg, default(T));
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed, default(T), null);
                }
                else
                {
                    chan_notify_sign.set_node(ntfSign, _popWait.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            if (chan_async_state.async_ok == state)
                            {
                                pop(ntf, ntfSign);
                            }
                            else
                            {
                                ntf(state, default(T), null);
                            }
                        }
                    }));
                    if (0 != _pushWait.Count)
                    {
                        notify_pck pushNtf = _pushWait.First.Value;
                        _pushWait.RemoveFirst();
                        pushNtf.Invoke(chan_async_state.async_ok);
                    }
                }
            });
        }

        internal override void try_push(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (_closed)
                {
                    ntf(chan_async_state.async_closed, null);
                    return;
                }
                if (_has)
                {
                    ntf(chan_async_state.async_fail, null);
                }
                else if (0 != _popWait.Count)
                {
                    _msg = msg;
                    _has = true;
                    _isTryPush = true;
                    chan_notify_sign.set_node(ntfSign, _pushWait.AddFirst(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            _isTryPush = false;
                            ntfSign?.clear();
                            if (chan_async_state.async_closed != state && chan_async_state.async_cancel != state && 0 != _pushWait.Count)
                            {
                                notify_pck pushNtf = _pushWait.First.Value;
                                _pushWait.RemoveFirst();
                                pushNtf.Invoke(chan_async_state.async_ok);
                            }
                            ntf(state, null);
                        }
                    }));
                    notify_pck popNtf = _popWait.First.Value;
                    _popWait.RemoveFirst();
                    popNtf.Invoke(chan_async_state.async_ok);
                }
                else
                {
                    ntf(chan_async_state.async_fail, null);
                }
            });
        }

        internal override void try_pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (_has)
                {
                    T msg = _msg;
                    _has = false;
                    notify_pck pushNtf = _pushWait.First.Value;
                    _pushWait.RemoveFirst();
                    pushNtf.Invoke(chan_async_state.async_ok);
                    ntf(chan_async_state.async_ok, msg, null);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed, default(T), null);
                }
                else if (0 != _pushWait.Count && 0 == _popWait.Count)
                {
                    _isTryPop = true;
                    chan_notify_sign.set_node(ntfSign, _popWait.AddFirst(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            _isTryPop = false;
                            ntfSign?.clear();
                            if (chan_async_state.async_ok == state)
                            {
                                pop(ntf, ntfSign);
                            }
                            else
                            {
                                ntf(state, default(T), null);
                            }
                        }
                    }));
                    notify_pck pushNtf = _pushWait.First.Value;
                    _pushWait.RemoveFirst();
                    pushNtf.Invoke(chan_async_state.async_ok);
                }
                else
                {
                    ntf(chan_async_state.async_fail, default(T), null);
                }
            });
        }

        internal override void timed_push(int ms, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (_closed)
                {
                    ntf(chan_async_state.async_closed, null);
                    return;
                }
                if (_has)
                {
                    if (ms >= 0)
                    {
                        async_timer timer = new async_timer(_strand);
                        option_node node = _pushWait.AddLast(new notify_pck()
                        {
                            ntf = delegate (chan_async_state state)
                            {
                                ntfSign?.clear();
                                timer.cancel();
                                if (chan_async_state.async_ok == state)
                                {
                                    push(ntf, msg, ntfSign);
                                }
                                else
                                {
                                    ntf(state, null);
                                }
                            }
                        });
                        ntfSign?.set(node);
                        timer.timeout(ms, delegate ()
                        {
                            ntfSign?.clear();
                            notify_pck pushWait = node.Value;
                            _pushWait.Remove(node);
                            pushWait.Invoke(chan_async_state.async_overtime);
                        });
                    }
                    else
                    {
                        chan_notify_sign.set_node(ntfSign, _pushWait.AddLast(new notify_pck()
                        {
                            ntf = delegate (chan_async_state state)
                            {
                                ntfSign?.clear();
                                if (chan_async_state.async_ok == state)
                                {
                                    push(ntf, msg, ntfSign);
                                }
                                else
                                {
                                    ntf(state, null);
                                }
                            }
                        }));
                    }
                }
                else if (ms >= 0)
                {
                    _msg = msg;
                    _has = true;
                    async_timer timer = new async_timer(_strand);
                    option_node node = _pushWait.AddFirst(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            timer.cancel();
                            if (chan_async_state.async_closed != state && chan_async_state.async_cancel != state && 0 != _pushWait.Count)
                            {
                                notify_pck pushNtf = _pushWait.First.Value;
                                _pushWait.RemoveFirst();
                                pushNtf.Invoke(chan_async_state.async_ok);
                            }
                            ntf(state, null);
                        }
                    });
                    ntfSign?.set(node);
                    timer.timeout(ms, delegate ()
                    {
                        ntfSign?.clear();
                        _has = false;
                        notify_pck pushWait = node.Value;
                        _pushWait.Remove(node);
                        pushWait.Invoke(chan_async_state.async_overtime);
                    });
                    if (0 != _popWait.Count)
                    {
                        notify_pck popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf.Invoke(chan_async_state.async_ok);
                    }
                }
                else
                {
                    _msg = msg;
                    _has = true;
                    chan_notify_sign.set_node(ntfSign, _pushWait.AddFirst(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            if (chan_async_state.async_closed != state && chan_async_state.async_cancel != state && 0 != _pushWait.Count)
                            {
                                notify_pck pushNtf = _pushWait.First.Value;
                                _pushWait.RemoveFirst();
                                pushNtf.Invoke(chan_async_state.async_ok);
                            }
                            ntf(state, null);
                        }
                    }));
                    if (0 != _popWait.Count)
                    {
                        notify_pck popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf.Invoke(chan_async_state.async_ok);
                    }
                }
            });
        }

        internal override void timed_pop(int ms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (_has)
                {
                    T msg = _msg;
                    _has = false;
                    notify_pck pushNtf = _pushWait.First.Value;
                    _pushWait.RemoveFirst();
                    pushNtf.Invoke(chan_async_state.async_ok);
                    ntf(chan_async_state.async_ok, msg, null);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed, default(T), null);
                }
                else if (ms >= 0)
                {
                    async_timer timer = new async_timer(_strand);
                    option_node node = _popWait.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            timer.cancel();
                            if (chan_async_state.async_ok == state)
                            {
                                pop(ntf, ntfSign);
                            }
                            else
                            {
                                ntf(state, default(T), null);
                            }
                        }
                    });
                    ntfSign?.set(node);
                    timer.timeout(ms, delegate ()
                    {
                        ntfSign?.clear();
                        notify_pck popNtf = node.Value;
                        _popWait.Remove(node);
                        popNtf.Invoke(chan_async_state.async_overtime);
                    });
                    if (0 != _pushWait.Count)
                    {
                        notify_pck pushNtf = _pushWait.First.Value;
                        _pushWait.RemoveFirst();
                        pushNtf.Invoke(chan_async_state.async_ok);
                    }
                }
                else
                {
                    chan_notify_sign.set_node(ntfSign, _popWait.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            if (chan_async_state.async_ok == state)
                            {
                                pop(ntf, ntfSign);
                            }
                            else
                            {
                                ntf(state, default(T), null);
                            }
                        }
                    }));
                    if (0 != _pushWait.Count)
                    {
                        notify_pck pushNtf = _pushWait.First.Value;
                        _pushWait.RemoveFirst();
                        pushNtf.Invoke(chan_async_state.async_ok);
                    }
                }
            });
        }

        internal override void append_pop_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
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
                    ntfSign._ntfNode = _popWait.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign._ntfNode = null;
                            ntfSign._success = chan_async_state.async_ok == state;
                            ntf(state);
                        }
                    });
                    if (0 != _pushWait.Count)
                    {
                        notify_pck pushNtf = _pushWait.First.Value;
                        _pushWait.RemoveFirst();
                        pushNtf.Invoke(chan_async_state.async_ok);
                    }
                }
            });
        }

        internal override void try_pop_and_append_notify(Action<chan_async_state, T, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign.reset_success();
                if (_has)
                {
                    T msg = _msg;
                    _has = false;
                    notify_pck pushNtf = _pushWait.First.Value;
                    _pushWait.RemoveFirst();
                    pushNtf.Invoke(chan_async_state.async_ok);
                    if (!ntfSign._selectOnce)
                    {
                        append_pop_notify(msgNtf, ntfSign);
                    }
                    cb(chan_async_state.async_ok, msg, null);
                }
                else if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed, default(T), null);
                }
                else if (0 != _pushWait.Count && 0 == _popWait.Count)
                {
                    _isTryPop = true;
                    chan_notify_sign.set_node(ntfSign, _popWait.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            _isTryPop = false;
                            ntfSign?.clear();
                            if (chan_async_state.async_ok == state)
                            {
                                pop(cb, ntfSign);
                            }
                            else
                            {
                                cb(state, default(T), null);
                            }
                        }
                    }));
                    notify_pck pushNtf = _pushWait.First.Value;
                    _pushWait.RemoveFirst();
                    pushNtf.Invoke(chan_async_state.async_ok);
                }
                else
                {
                    append_pop_notify(msgNtf, ntfSign);
                    cb(chan_async_state.async_fail, default(T), null);
                }
            });
        }

        internal override void remove_pop_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                bool effect = null != ntfSign._ntfNode;
                bool success = ntfSign._success;
                ntfSign._success = false;
                if (effect)
                {
                    _isTryPop &= _popWait.First != ntfSign._ntfNode;
                    _popWait.Remove(ntfSign._ntfNode);
                    ntfSign._ntfNode = null;
                }
                if (success && _has)
                {
                    if (0 != _popWait.Count)
                    {
                        notify_pck popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf.Invoke(chan_async_state.async_ok);
                    }
                    else if (0 != _pushWait.Count && _isTryPush)
                    {
                        _has = false;
                        notify_pck pushNtf = _pushWait.First.Value;
                        _pushWait.RemoveFirst();
                        pushNtf.Invoke(chan_async_state.async_fail);
                    }
                }
                ntf(effect || success ? chan_async_state.async_ok : chan_async_state.async_fail);
            });
        }

        internal override void append_push_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
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
                    ntfSign._ntfNode = _pushWait.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign._ntfNode = null;
                            ntfSign._success = chan_async_state.async_ok == state;
                            ntf(state);
                        }
                    });
                }
            });
        }

        internal override void try_push_and_append_notify(Action<chan_async_state, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, T msg)
        {
            _strand.distribute(delegate ()
            {
                ntfSign.reset_success();
                if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed, null);
                    return;
                }
                if (!_has && 0 != _popWait.Count)
                {
                    _has = true;
                    _msg = msg;
                    _isTryPop = true;
                    ntfSign._ntfNode = _pushWait.AddFirst(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            _isTryPop = false;
                            ntfSign._ntfNode = null;
                            if (chan_async_state.async_closed != state && chan_async_state.async_cancel != state && 0 != _pushWait.Count)
                            {
                                notify_pck pushNtf = _pushWait.First.Value;
                                _pushWait.RemoveFirst();
                                pushNtf.Invoke(chan_async_state.async_ok);
                            }
                            if (!ntfSign._selectOnce)
                            {
                                append_push_notify(msgNtf, ntfSign);
                            }
                            cb(state, null);
                        }
                    });
                    notify_pck popNtf = _popWait.First.Value;
                    _popWait.RemoveFirst();
                    popNtf.Invoke(chan_async_state.async_ok);
                }
                else
                {
                    append_push_notify(msgNtf, ntfSign);
                    cb(chan_async_state.async_fail, null);
                }
            });
        }

        internal override void remove_push_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                bool effect = null != ntfSign._ntfNode;
                bool success = ntfSign._success;
                ntfSign._success = false;
                if (effect)
                {
                    _isTryPush &= _pushWait.First != ntfSign._ntfNode;
                    _pushWait.Remove(ntfSign._ntfNode);
                    ntfSign._ntfNode = null;
                }
                if (success && !_has)
                {
                    if (0 != _pushWait.Count)
                    {
                        notify_pck pushNtf = _pushWait.First.Value;
                        _pushWait.RemoveFirst();
                        pushNtf.Invoke(chan_async_state.async_ok);
                    }
                    else if (0 != _popWait.Count && _isTryPop)
                    {
                        notify_pck popNtf = _popWait.First.Value;
                        _popWait.RemoveFirst();
                        popNtf.Invoke(chan_async_state.async_fail);
                    }
                }
                ntf(effect || success ? chan_async_state.async_ok : chan_async_state.async_fail);
            });
        }

        internal override void clear(Action ntf)
        {
            _strand.distribute(delegate ()
            {
                _has = false;
                safe_callback(ref _pushWait, chan_async_state.async_fail);
                ntf();
            });
        }

        internal override void close(Action ntf, bool isClear = false)
        {
            _strand.distribute(delegate ()
            {
                _closed = true;
                _has = false;
                safe_callback(ref _popWait, ref _pushWait, chan_async_state.async_closed);
                ntf();
            });
        }

        internal override void cancel(Action ntf, bool isClear = false)
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
        LinkedList<notify_pck> _popWait;
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
            _popWait = new LinkedList<notify_pck>();
            _has = false;
            _pushCount = 0;
            _closed = false;
        }

        internal override select_chan_base make_select_reader(Func<T, Task> handler, broadcast_chan_token token)
        {
            return new select_chan_reader() { _token = token, _chan = this, _handler = handler };
        }

        internal override select_chan_base make_select_reader(Func<T, Task> handler, broadcast_chan_token token, Func<chan_async_state, Task<bool>> errHandler)
        {
            return new select_chan_reader() { _token = token, _chan = this, _handler = handler, _errHandler = errHandler };
        }

        public override chan_type type()
        {
            return chan_type.broadcast;
        }

        internal override void push(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (_closed)
                {
                    ntf(chan_async_state.async_closed, null);
                    return;
                }
                _pushCount++;
                _msg = msg;
                _has = true;
                LinkedList<notify_pck> ntfs = _popWait;
                _popWait = new LinkedList<notify_pck>();
                while (0 != ntfs.Count)
                {
                    ntfs.First.Value.Invoke(chan_async_state.async_ok);
                    ntfs.RemoveFirst();
                }
                ntf(chan_async_state.async_ok, null);
            });
        }

        internal override void pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            pop(ntf, ntfSign, broadcast_chan_token._defToken);
        }

        internal override void pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign, broadcast_chan_token token)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (_has && token._lastId != _pushCount)
                {
                    if (!token.is_default())
                    {
                        token._lastId = _pushCount;
                    }
                    ntf(chan_async_state.async_ok, _msg, null);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed, default(T), null);
                }
                else
                {
                    chan_notify_sign.set_node(ntfSign, _popWait.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            if (chan_async_state.async_ok == state)
                            {
                                pop(ntf, ntfSign, token);
                            }
                            else
                            {
                                ntf(state, default(T), null);
                            }
                        }
                    }));
                }
            });
        }

        internal override void try_push(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            push(ntf, msg, ntfSign);
        }

        internal override void try_pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            try_pop(ntf, ntfSign, broadcast_chan_token._defToken);
        }

        internal override void try_pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign, broadcast_chan_token token)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (_has && token._lastId != _pushCount)
                {
                    if (!token.is_default())
                    {
                        token._lastId = _pushCount;
                    }
                    ntf(chan_async_state.async_ok, _msg, null);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed, default(T), null);
                }
                else
                {
                    ntf(chan_async_state.async_fail, default(T), null);
                }
            });
        }

        internal override void timed_push(int ms, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            push(ntf, msg, ntfSign);
        }

        internal override void timed_pop(int ms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            timed_pop(ms, ntf, ntfSign, broadcast_chan_token._defToken);
        }

        internal override void timed_pop(int ms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign, broadcast_chan_token token)
        {
            ntfSign?.reset_success();
            _timed_check_pop(system_tick.get_tick_ms() + ms, ntf, ntfSign, token);
        }

        void _timed_check_pop(long deadms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign, broadcast_chan_token token)
        {
            _strand.distribute(delegate ()
            {
                if (_has && token._lastId != _pushCount)
                {
                    if (!token.is_default())
                    {
                        token._lastId = _pushCount;
                    }
                    ntf(chan_async_state.async_ok, _msg, null);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed, default(T), null);
                }
                else
                {
                    async_timer timer = new async_timer(_strand);
                    option_node node = _popWait.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            timer.cancel();
                            if (chan_async_state.async_ok == state)
                            {
                                _timed_check_pop(deadms, ntf, ntfSign, token);
                            }
                            else
                            {
                                ntf(state, default(T), null);
                            }
                        }
                    });
                    ntfSign?.set(node);
                    timer.deadline(deadms, delegate ()
                    {
                        ntfSign?.clear();
                        notify_pck popWait = node.Value;
                        _popWait.Remove(node);
                        popWait.Invoke(chan_async_state.async_overtime);
                    });
                }
            });
        }

        internal override void append_pop_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            append_pop_notify(ntf, ntfSign, broadcast_chan_token._defToken);
        }

        internal override void append_pop_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign, broadcast_chan_token token)
        {
            _strand.distribute(delegate ()
            {
                _append_pop_notify(ntf, ntfSign, token);
            });
        }

        bool _append_pop_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign, broadcast_chan_token token)
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
                ntfSign._ntfNode = _popWait.AddLast(new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign._ntfNode = null;
                        ntfSign._success = chan_async_state.async_ok == state;
                        ntf(state);
                    }
                });
                return false;
            }
        }

        internal override void try_pop_and_append_notify(Action<chan_async_state, T, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign)
        {
            try_pop_and_append_notify(cb, msgNtf, ntfSign, broadcast_chan_token._defToken);
        }

        internal override void try_pop_and_append_notify(Action<chan_async_state, T, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, broadcast_chan_token token)
        {
            _strand.distribute(delegate ()
            {
                ntfSign.reset_success();
                if (_append_pop_notify(msgNtf, ntfSign, token))
                {
                    cb(chan_async_state.async_ok, _msg, null);
                }
                else if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed, default(T), null);
                }
                else
                {
                    cb(chan_async_state.async_fail, default(T), null);
                }
            });
        }

        internal override void remove_pop_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                bool effect = null != ntfSign._ntfNode;
                bool success = ntfSign._success;
                ntfSign._success = false;
                if (effect)
                {
                    _popWait.Remove(ntfSign._ntfNode);
                    ntfSign._ntfNode = null;
                }
                if (success && _has && 0 != _popWait.Count)
                {
                    notify_pck popNtf = _popWait.First.Value;
                    _popWait.RemoveFirst();
                    popNtf.Invoke(chan_async_state.async_ok);
                }
                ntf(effect || success ? chan_async_state.async_ok : chan_async_state.async_fail);
            });
        }

        internal override void append_push_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
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

        internal override void try_push_and_append_notify(Action<chan_async_state, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, T msg)
        {
            _strand.distribute(delegate ()
            {
                ntfSign.reset_success();
                if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed, null);
                    return;
                }
                _pushCount++;
                _msg = msg;
                _has = true;
                msgNtf(chan_async_state.async_ok);
                cb(chan_async_state.async_ok, null);
            });
        }

        internal override void remove_push_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntf(chan_async_state.async_fail);
            });
        }

        internal override void clear(Action ntf)
        {
            _strand.distribute(delegate ()
            {
                _has = false;
                ntf();
            });
        }

        internal override void close(Action ntf, bool isClear = false)
        {
            _strand.distribute(delegate ()
            {
                _closed = true;
                _has &= !isClear;
                safe_callback(ref _popWait, chan_async_state.async_closed);
                ntf();
            });
        }

        internal override void cancel(Action ntf, bool isClear = false)
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
            public Action<chan_async_state, object> _notify;
            public T _msg;
            public bool _has;
            public int _invokeMs;
            async_timer _timer;

            public void set(Action<chan_async_state, object> ntf, T msg, async_timer timer, int ms = -1)
            {
                _notify = ntf;
                _msg = msg;
                _has = true;
                _invokeMs = ms;
                _timer = timer;
            }

            public void set(Action<chan_async_state, object> ntf, T msg, int ms = -1)
            {
                _notify = ntf;
                _msg = msg;
                _has = true;
                _invokeMs = ms;
                _timer = null;
            }

            public void cancel_timer()
            {
                _has = false;
                _timer?.cancel();
            }
        }

        public class csp_result
        {
            internal int _invokeMs;
            internal Action<chan_async_state, object> _notify;
            async_timer _invokeTimer;

            internal csp_result(int ms, Action<chan_async_state, object> notify)
            {
                _invokeMs = ms;
                _notify = notify;
                _invokeTimer = null;
            }

            internal void start_invoke_timer(generator host)
            {
                if (_invokeMs >= 0)
                {
                    _invokeTimer = new async_timer(host.strand);
                    _invokeTimer.timeout(_invokeMs, fail);
                }
            }

            public bool complete(R res)
            {
                _invokeTimer?.cancel();
                _invokeTimer = null;
                if (null != _notify)
                {
                    Action<chan_async_state, object> ntf = _notify;
                    _notify = null;
                    ntf.Invoke(chan_async_state.async_ok, res);
                    return true;
                }
                return false;
            }

            public void fail()
            {
                _invokeTimer?.cancel();
                _invokeTimer = null;
                Action<chan_async_state, object> ntf = _notify;
                _notify = null;
                ntf?.Invoke(chan_async_state.async_csp_fail, default(T));
            }
        }

        internal class select_csp_reader : select_chan_base
        {
            public csp_chan<R, T> _chan;
            public Func<csp_result, T, Task> _handler;
            public Func<chan_async_state, Task<bool>> _errHandler;
            public Func<T, Task> _lostHandler;
            csp_wait_wrap<R, T> _tempResult = default(csp_wait_wrap<R, T>);
            Action<chan_async_state, T, object> _tryPopHandler;
            generator _host;

            public override void begin()
            {
                ntfSign._disable = false;
                _host = generator.self;
                _chan.append_pop_notify(nextSelect, ntfSign);
            }

            public override async Task<select_chan_state> invoke(Func<Task> stepOne)
            {
                if (null == _tryPopHandler)
                {
                    _tryPopHandler = delegate (chan_async_state state, T msg, object exObj)
                    {
                        _tempResult.state = state;
                        if (chan_async_state.async_ok == state)
                        {
                            _tempResult.msg = msg;
                            _tempResult.result = (csp_chan<R, T>.csp_result)exObj;
                        }
                    };
                }
                _tempResult = csp_wait_wrap<R, T>.undefined();
                try
                {
                    _chan.try_pop_and_append_notify(_host.async_callback(_tryPopHandler), nextSelect, ntfSign);
                    await _host.async_wait();
                }
                catch (generator.stop_exception)
                {
                    _chan.remove_pop_notify(_host.async_ignore<chan_async_state>(), ntfSign);
                    await _host.async_wait();
                    if (chan_async_state.async_ok == _tempResult.state)
                    {
                        if (null != _lostHandler)
                        {
                            await _lostHandler(_tempResult.msg);
                        }
                        _tempResult.fail();
                    }
                    throw;
                }
                select_chan_state chanState = new select_chan_state() { failed = false, nextRound = true };
                if (chan_async_state.async_ok == _tempResult.state)
                {
                    if (null != stepOne)
                    {
                        await stepOne();
                    }
                    try
                    {
                        _tempResult.result.start_invoke_timer(_host);
                        await generator.unlock_suspend();
                        await _handler(_tempResult.result, _tempResult.msg);
                    }
                    catch (csp_fail_exception)
                    {
                        _tempResult.fail();
                    }
                    catch (generator.stop_exception)
                    {
                        _tempResult.fail();
                        throw;
                    }
                    finally
                    {
                        generator.lock_suspend();
                    }
                }
                else if (null != _errHandler)
                {
                    try
                    {
                        await generator.unlock_suspend();
                        chanState.failed = await _errHandler(_tempResult.state);
                    }
                    finally
                    {
                        generator.lock_suspend();
                        if (chanState.failed)
                        {
                            await end();
                        }
                    }
                }
                else if (chan_async_state.async_closed == _tempResult.state)
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
                ntfSign._disable = true;
                _chan.remove_pop_notify(_host.async_ignore<chan_async_state>(), ntfSign);
                return _host.async_wait();
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

        internal class select_csp_writer : select_chan_base
        {
            public csp_chan<R, T> _chan;
            public Func<T> _msg;
            public Func<R, Task> _handler;
            public Func<chan_async_state, Task<bool>> _errHandler;
            public Action<chan_async_state, object> _lostHandler;
            csp_invoke_wrap<R> _tempResult = default(csp_invoke_wrap<R>);
            Action<chan_async_state, object> _tryPushHandler;
            generator _host;

            public override void begin()
            {
                ntfSign._disable = false;
                _host = generator.self;
                _chan.append_push_notify(nextSelect, ntfSign);
            }

            public override async Task<select_chan_state> invoke(Func<Task> stepOne)
            {
                if (null == _tryPushHandler)
                {
                    _tryPushHandler = delegate (chan_async_state state, object exObj)
                    {
                        _tempResult.state = state;
                        if (chan_async_state.async_ok == state)
                        {
                            _tempResult.result = (R)exObj;
                        }
                    };
                }
                _tempResult = csp_invoke_wrap<R>.undefined();
                _chan.try_push_and_append_notify(null == _lostHandler ? _host.async_callback(_tryPushHandler) : _host.safe_async_callback(_tryPushHandler, _lostHandler), nextSelect, ntfSign, _msg());
                await _host.async_wait();
                select_chan_state chanState = new select_chan_state() { failed = false, nextRound = true };
                if (chan_async_state.async_ok == _tempResult.state)
                {
                    if (null != stepOne)
                    {
                        await stepOne();
                    }
                    try
                    {
                        await generator.unlock_suspend();
                        await _handler(_tempResult.result);
                    }
                    finally
                    {
                        generator.lock_suspend();
                    }
                }
                else if (null != _errHandler)
                {
                    try
                    {
                        await generator.unlock_suspend();
                        chanState.failed = await _errHandler(_tempResult.state);
                    }
                    finally
                    {
                        generator.lock_suspend();
                        if (chanState.failed)
                        {
                            await end();
                        }
                    }
                }
                else if (chan_async_state.async_closed == _tempResult.state)
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
                ntfSign._disable = true;
                _chan.remove_push_notify(_host.async_ignore<chan_async_state>(), ntfSign);
                return _host.async_wait();
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
        LinkedList<notify_pck> _sendQueue;
        LinkedList<notify_pck> _waitQueue;
        send_pck _msg;
        bool _isTryMsg;
        bool _isTryPop;
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
            _sendQueue = new LinkedList<notify_pck>();
            _waitQueue = new LinkedList<notify_pck>();
            _msg.cancel_timer();
            _isTryMsg = false;
            _isTryPop = false;
            _closed = false;
        }

        internal select_chan_base make_select_reader(Func<csp_result, T, Task> handler, Func<T, Task> lostHandler = null)
        {
            return new select_csp_reader() { _chan = this, _handler = handler, _lostHandler = lostHandler };
        }

        internal select_chan_base make_select_reader(Func<csp_result, T, Task> handler, Func<chan_async_state, Task<bool>> errHandler, Func<T, Task> lostHandler = null)
        {
            return new select_csp_reader() { _chan = this, _handler = handler, _errHandler = errHandler, _lostHandler = lostHandler };
        }

        internal select_chan_base make_select_writer(Func<T> msg, Func<R, Task> handler, Func<chan_async_state, Task<bool>> errHandler, Action<R> lostHandler = null)
        {
            return new select_csp_writer()
            {
                _chan = this,
                _msg = msg,
                _handler = handler,
                _errHandler = errHandler,
                _lostHandler = null == lostHandler ? (Action<chan_async_state, object>)null : delegate (chan_async_state state, object exObj)
                {
                    if (chan_async_state.async_ok == state)
                    {
                        lostHandler((R)exObj);
                    }
                }
            };
        }

        internal select_chan_base make_select_writer(async_result_wrap<T> msg, Func<R, Task> handler, Func<chan_async_state, Task<bool>> errHandler, Action<R> lostHandler = null)
        {
            return make_select_writer(() => msg.value1, handler, errHandler, lostHandler);
        }

        internal select_chan_base make_select_writer(T msg, Func<R, Task> handler, Func<chan_async_state, Task<bool>> errHandler, Action<R> lostHandler = null)
        {
            return make_select_writer(() => msg, handler, errHandler, lostHandler);
        }

        public override chan_type type()
        {
            return chan_type.csp;
        }

        internal override void push(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            push(-1, ntf, msg, ntfSign);
        }

        internal void push(int invokeMs, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (_closed)
                {
                    ntf(chan_async_state.async_closed, null);
                    return;
                }
                if (_msg._has)
                {
                    chan_notify_sign.set_node(ntfSign, _sendQueue.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            if (chan_async_state.async_ok == state)
                            {
                                push(ntf, msg, ntfSign);
                            }
                            else
                            {
                                ntf(state, null);
                            }
                        }
                    }));
                }
                else
                {
                    _msg.set(ntf, msg, invokeMs);
                    if (0 != _waitQueue.Count)
                    {
                        notify_pck handler = _waitQueue.First.Value;
                        _waitQueue.RemoveFirst();
                        handler.Invoke(chan_async_state.async_ok);
                    }
                }
            });
        }

        internal override void pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (_msg._has)
                {
                    send_pck pck = _msg;
                    _msg.cancel_timer();
                    _isTryMsg = false;
                    if (0 != _sendQueue.Count)
                    {
                        notify_pck sendWait = _sendQueue.First.Value;
                        _sendQueue.RemoveFirst();
                        sendWait.Invoke(chan_async_state.async_ok);
                    }
                    ntf(chan_async_state.async_ok, pck._msg, new csp_result(pck._invokeMs, pck._notify));
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed, default(T), null);
                }
                else
                {
                    chan_notify_sign.set_node(ntfSign, _waitQueue.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            if (chan_async_state.async_ok == state)
                            {
                                pop(ntf, ntfSign);
                            }
                            else
                            {
                                ntf(state, default(T), null);
                            }
                        }
                    }));
                    if (0 != _sendQueue.Count)
                    {
                        notify_pck sendWait = _sendQueue.First.Value;
                        _sendQueue.RemoveFirst();
                        sendWait.Invoke(chan_async_state.async_ok);
                    }
                }
            });
        }

        internal override void try_push(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            try_push(-1, ntf, msg, ntfSign);
        }

        internal void try_push(int invokeMs, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (_closed)
                {
                    ntf(chan_async_state.async_closed, null);
                    return;
                }
                if (_msg._has)
                {
                    ntf(chan_async_state.async_fail, null);
                }
                else if (0 != _waitQueue.Count)
                {
                    _isTryMsg = true;
                    _msg.set(ntf, msg, invokeMs);
                    notify_pck handler = _waitQueue.First.Value;
                    _waitQueue.RemoveFirst();
                    handler.Invoke(chan_async_state.async_ok);
                }
                else
                {
                    ntf(chan_async_state.async_fail, null);
                }
            });
        }

        internal override void try_pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (_msg._has)
                {
                    send_pck pck = _msg;
                    _msg.cancel_timer();
                    _isTryMsg = false;
                    if (0 != _sendQueue.Count)
                    {
                        notify_pck sendWait = _sendQueue.First.Value;
                        _sendQueue.RemoveFirst();
                        sendWait.Invoke(chan_async_state.async_ok);
                    }
                    ntf(chan_async_state.async_ok, pck._msg, new csp_result(pck._invokeMs, pck._notify));
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed, default(T), null);
                }
                else if (0 != _sendQueue.Count && 0 == _waitQueue.Count)
                {
                    _isTryPop = true;
                    chan_notify_sign.set_node(ntfSign, _waitQueue.AddFirst(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            _isTryPop = false;
                            ntfSign?.clear();
                            if (chan_async_state.async_ok == state)
                            {
                                pop(ntf, ntfSign);
                            }
                            else
                            {
                                ntf(state, default(T), null);
                            }
                        }
                    }));
                    notify_pck sendNtf = _sendQueue.First.Value;
                    _sendQueue.RemoveFirst();
                    sendNtf.Invoke(chan_async_state.async_ok);
                }
                else
                {
                    ntf(chan_async_state.async_fail, default(T), null);
                }
            });
        }

        internal override void timed_push(int ms, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            timed_push(ms, -1, ntf, msg, ntfSign);
        }

        internal void timed_push(int ms, int invokeMs, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (_closed)
                {
                    ntf(chan_async_state.async_closed, null);
                    return;
                }
                if (_msg._has)
                {
                    if (ms >= 0)
                    {
                        async_timer timer = new async_timer(_strand);
                        option_node node = _sendQueue.AddLast(new notify_pck()
                        {
                            ntf = delegate (chan_async_state state)
                            {
                                ntfSign?.clear();
                                timer.cancel();
                                if (chan_async_state.async_ok == state)
                                {
                                    push(invokeMs, ntf, msg, ntfSign);
                                }
                                else
                                {
                                    ntf(state, null);
                                }
                            }
                        });
                        ntfSign?.set(node);
                        timer.timeout(ms, delegate ()
                        {
                            ntfSign?.clear();
                            notify_pck sendWait = node.Value;
                            _sendQueue.Remove(node);
                            sendWait.Invoke(chan_async_state.async_overtime);
                        });
                    }
                    else
                    {
                        chan_notify_sign.set_node(ntfSign, _sendQueue.AddLast(new notify_pck()
                        {
                            ntf = delegate (chan_async_state state)
                            {
                                ntfSign?.clear();
                                if (chan_async_state.async_ok == state)
                                {
                                    push(invokeMs, ntf, msg, ntfSign);
                                }
                                else
                                {
                                    ntf(state, null);
                                }
                            }
                        }));
                    }
                }
                else if (ms >= 0)
                {
                    async_timer timer = new async_timer(_strand);
                    _msg.set(ntf, msg, timer, invokeMs);
                    timer.timeout(ms, delegate ()
                    {
                        _msg.cancel_timer();
                        Action<chan_async_state, object> ntf_ = _msg._notify;
                        ntf_(chan_async_state.async_overtime, null);
                    });
                    if (0 != _waitQueue.Count)
                    {
                        notify_pck handler = _waitQueue.First.Value;
                        _waitQueue.RemoveFirst();
                        handler.Invoke(chan_async_state.async_ok);
                    }
                }
                else
                {
                    _msg.set(ntf, msg, invokeMs);
                    if (0 != _waitQueue.Count)
                    {
                        notify_pck handler = _waitQueue.First.Value;
                        _waitQueue.RemoveFirst();
                        handler.Invoke(chan_async_state.async_ok);
                    }
                }
            });
        }

        internal override void timed_pop(int ms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign?.reset_success();
                if (_msg._has)
                {
                    send_pck pck = _msg;
                    _msg.cancel_timer();
                    _isTryMsg = false;
                    if (0 != _sendQueue.Count)
                    {
                        notify_pck sendWait = _sendQueue.First.Value;
                        _sendQueue.RemoveFirst();
                        sendWait.Invoke(chan_async_state.async_ok);
                    }
                    ntf(chan_async_state.async_ok, pck._msg, new csp_result(pck._invokeMs, pck._notify));
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed, default(T), null);
                }
                else if (ms >= 0)
                {
                    async_timer timer = new async_timer(_strand);
                    option_node node = _waitQueue.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            timer.cancel();
                            if (chan_async_state.async_ok == state)
                            {
                                pop(ntf, ntfSign);
                            }
                            else
                            {
                                ntf(state, default(T), null);
                            }
                        }
                    });
                    ntfSign?.set(node);
                    timer.timeout(ms, delegate ()
                    {
                        ntfSign?.clear();
                        notify_pck waitNtf = node.Value;
                        _waitQueue.Remove(node);
                        waitNtf.Invoke(chan_async_state.async_overtime);
                    });
                    if (0 != _sendQueue.Count)
                    {
                        notify_pck sendWait = _sendQueue.First.Value;
                        _sendQueue.RemoveFirst();
                        sendWait.Invoke(chan_async_state.async_ok);
                    }
                }
                else
                {
                    chan_notify_sign.set_node(ntfSign, _waitQueue.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            if (chan_async_state.async_ok == state)
                            {
                                pop(ntf, ntfSign);
                            }
                            else
                            {
                                ntf(state, default(T), null);
                            }
                        }
                    }));
                    if (0 != _sendQueue.Count)
                    {
                        notify_pck sendWait = _sendQueue.First.Value;
                        _sendQueue.RemoveFirst();
                        sendWait.Invoke(chan_async_state.async_ok);
                    }
                }
            });
        }

        internal override void append_pop_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                if (_msg._has)
                {
                    ntf(chan_async_state.async_ok);
                }
                else if (_closed)
                {
                    ntf(chan_async_state.async_closed);
                }
                else
                {
                    ntfSign._ntfNode = _waitQueue.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign._ntfNode = null;
                            ntfSign._success = chan_async_state.async_ok == state;
                            ntf(state);
                        }
                    });
                    if (0 != _sendQueue.Count)
                    {
                        notify_pck sendNtf = _sendQueue.First.Value;
                        _sendQueue.RemoveFirst();
                        sendNtf.Invoke(chan_async_state.async_ok);
                    }
                }
            });
        }

        internal override void try_pop_and_append_notify(Action<chan_async_state, T, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                ntfSign.reset_success();
                if (_msg._has)
                {
                    send_pck pck = _msg;
                    _msg.cancel_timer();
                    _isTryMsg = false;
                    if (0 != _sendQueue.Count)
                    {
                        notify_pck sendNtf = _sendQueue.First.Value;
                        _sendQueue.RemoveFirst();
                        sendNtf.Invoke(chan_async_state.async_ok);
                    }
                    if (!ntfSign._selectOnce)
                    {
                        append_pop_notify(msgNtf, ntfSign);
                    }
                    cb(chan_async_state.async_ok, pck._msg, new csp_result(pck._invokeMs, pck._notify));
                }
                else if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed, default(T), null);
                }
                else if (0 != _sendQueue.Count && 0 == _waitQueue.Count)
                {
                    _isTryPop = true;
                    chan_notify_sign.set_node(ntfSign, _waitQueue.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            _isTryPop = false;
                            ntfSign?.clear();
                            if (chan_async_state.async_ok == state)
                            {
                                pop(cb, ntfSign);
                            }
                            else
                            {
                                cb(state, default(T), null);
                            }
                        }
                    }));
                    notify_pck sendNtf = _sendQueue.First.Value;
                    _sendQueue.RemoveFirst();
                    sendNtf.Invoke(chan_async_state.async_ok);
                }
                else
                {
                    append_pop_notify(msgNtf, ntfSign);
                    cb(chan_async_state.async_fail, default(T), null);
                }
            });
        }

        internal override void remove_pop_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                bool effect = null != ntfSign._ntfNode;
                bool success = ntfSign._success;
                ntfSign._success = false;
                if (effect)
                {
                    _isTryPop &= _waitQueue.First != ntfSign._ntfNode;
                    _waitQueue.Remove(ntfSign._ntfNode);
                    ntfSign._ntfNode = null;
                }
                if (success && _msg._has)
                {
                    if (0 != _waitQueue.Count)
                    {
                        notify_pck popNtf = _waitQueue.First.Value;
                        _waitQueue.RemoveFirst();
                        popNtf.Invoke(chan_async_state.async_ok);
                    }
                    else if (_isTryMsg)
                    {
                        _isTryMsg = false;
                        _msg.cancel_timer();
                        Action<chan_async_state, object> ntf_ = _msg._notify;
                        ntf_(chan_async_state.async_fail, null);
                    }
                }
                ntf(effect || success ? chan_async_state.async_ok : chan_async_state.async_fail);
            });
        }

        internal override void append_push_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
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
                    ntfSign._ntfNode = _sendQueue.AddLast(new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign._ntfNode = null;
                            ntfSign._success = chan_async_state.async_ok == state;
                            ntf(state);
                        }
                    });
                }
            });
        }

        internal override void try_push_and_append_notify(Action<chan_async_state, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, T msg)
        {
            _strand.distribute(delegate ()
            {
                ntfSign.reset_success();
                if (_closed)
                {
                    msgNtf(chan_async_state.async_closed);
                    cb(chan_async_state.async_closed, null);
                    return;
                }
                if (!_msg._has && 0 != _waitQueue.Count)
                {
                    _isTryMsg = true;
                    _msg.set(cb, msg);
                    if (!ntfSign._selectOnce)
                    {
                        append_push_notify(msgNtf, ntfSign);
                    }
                    notify_pck handler = _waitQueue.First.Value;
                    _waitQueue.RemoveFirst();
                    handler.Invoke(chan_async_state.async_ok);
                }
                else
                {
                    append_push_notify(msgNtf, ntfSign);
                    cb(chan_async_state.async_fail, null);
                }
            });
        }

        internal override void remove_push_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            _strand.distribute(delegate ()
            {
                bool effect = null != ntfSign._ntfNode;
                bool success = ntfSign._success;
                ntfSign._success = false;
                if (effect)
                {
                    _sendQueue.Remove(ntfSign._ntfNode);
                    ntfSign._ntfNode = null;
                }
                if (success && !_msg._has)
                {
                    if (0 != _sendQueue.Count)
                    {
                        notify_pck sendNtf = _sendQueue.First.Value;
                        _sendQueue.RemoveFirst();
                        sendNtf.Invoke(chan_async_state.async_ok);
                    }
                    else if (0 != _waitQueue.Count && _isTryPop)
                    {
                        notify_pck popNtf = _waitQueue.First.Value;
                        _waitQueue.RemoveFirst();
                        popNtf.Invoke(chan_async_state.async_fail);
                    }
                }
                ntf(effect || success ? chan_async_state.async_ok : chan_async_state.async_fail);
            });
        }

        internal override void clear(Action ntf)
        {
            _strand.distribute(delegate ()
            {
                _msg.cancel_timer();
                _isTryMsg = false;
                safe_callback(ref _sendQueue, chan_async_state.async_fail);
                ntf();
            });
        }

        internal override void close(Action ntf, bool isClear = false)
        {
            _strand.distribute(delegate ()
            {
                _closed = true;
                Action<chan_async_state, object> hasMsg = null;
                if (_msg._has)
                {
                    _msg.cancel_timer();
                    _isTryMsg = false;
                    hasMsg = _msg._notify;
                }
                safe_callback(ref _sendQueue, ref _waitQueue, chan_async_state.async_closed);
                hasMsg?.Invoke(chan_async_state.async_closed, null);
                ntf();
            });
        }

        internal override void cancel(Action ntf, bool isClear = false)
        {
            _strand.distribute(delegate ()
            {
                Action<chan_async_state, object> hasMsg = null;
                if (_msg._has)
                {
                    _msg.cancel_timer();
                    _isTryMsg = false;
                    hasMsg = _msg._notify;
                }
                safe_callback(ref _sendQueue, ref _waitQueue, chan_async_state.async_cancel);
                hasMsg?.Invoke(chan_async_state.async_cancel, null);
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
