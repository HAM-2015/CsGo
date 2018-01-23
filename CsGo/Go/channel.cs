using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Go
{
    using option_node = priority_queue_node<notify_pck>;

    public enum chan_async_state
    {
        async_undefined,
        async_ok,
        async_fail,
        async_csp_fail,
        async_cancel,
        async_closed,
        async_overtime
    }

    public enum chan_type
    {
        undefined,
        broadcast,
        unlimit,
        limit,
        nil,
        csp
    }

    struct notify_pck
    {
        public async_timer timer;
        public Action<chan_async_state> ntf;

        public void cancel_timer()
        {
            timer?.cancel();
        }

        public bool Invoke(chan_async_state state)
        {
            if (null != ntf)
            {
                ntf(state);
                return true;
            }
            return false;
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
            _ntfNode = default(priority_queue_node<notify_pck>);
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

    struct priority_queue_node<T>
    {
        public int _priority;
        public LinkedListNode<T> _node;

        public bool effect
        {
            get
            {
                return null != _node;
            }
        }

        public T Value
        {
            get
            {
                return _node.Value;
            }
        }
    }

    struct priority_queue<T>
    {
        public LinkedList<T>[] _queues;

        public priority_queue(int maxPri)
        {
            _queues = new LinkedList<T>[maxPri];
        }

        public bool Null
        {
            get
            {
                return null == _queues;
            }
        }

        public priority_queue_node<T> AddFirst(int priority, T value)
        {
            LinkedList<T> queue = _queues[priority];
            if (null == queue)
            {
                queue = new LinkedList<T>();
                _queues[priority] = queue;
            }
            return new priority_queue_node<T>() { _priority = priority, _node = queue.AddFirst(value) };
        }

        public priority_queue_node<T> AddLast(int priority, T value)
        {
            LinkedList<T> queue = _queues[priority];
            if (null == queue)
            {
                queue = new LinkedList<T>();
                _queues[priority] = queue;
            }
            return new priority_queue_node<T>() { _priority = priority, _node = queue.AddLast(value) };
        }

        public priority_queue_node<T> AddFirst(T value)
        {
            return AddFirst(0, value);
        }

        public priority_queue_node<T> AddLast(T value)
        {
            return AddLast(_queues.Length - 1, value);
        }

        public bool Empty
        {
            get
            {
                int length = _queues.Length;
                for (int i = 0; i < length; i++)
                {
                    LinkedList<T> queue = _queues[i];
                    if (null != queue && 0 != queue.Count)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public priority_queue_node<T> First
        {
            get
            {
                int length = _queues.Length;
                for (int i = 0; i < length; i++)
                {
                    LinkedList<T> queue = _queues[i];
                    if (null != queue && 0 != queue.Count)
                    {
                        return new priority_queue_node<T>() { _priority = i, _node = queue.First };
                    }
                }
                return new priority_queue_node<T>();
            }
        }

        public priority_queue_node<T> Last
        {
            get
            {
                int length = _queues.Length;
                for (int i = length - 1; i >= 0; i--)
                {
                    LinkedList<T> queue = _queues[i];
                    if (null != queue && 0 != queue.Count)
                    {
                        return new priority_queue_node<T>() { _priority = i, _node = queue.Last };
                    }
                }
                return new priority_queue_node<T>();
            }
        }

        public T RemoveFirst()
        {
            int length = _queues.Length;
            for (int i = 0; i < length; i++)
            {
                LinkedList<T> queue = _queues[i];
                if (null != queue && 0 != queue.Count)
                {
                    T first = queue.First.Value;
                    queue.RemoveFirst();
                    return first;
                }
            }
            return default(T);
        }

        public T RemoveLast()
        {
            int length = _queues.Length;
            for (int i = length - 1; i >= 0; i--)
            {
                LinkedList<T> queue = _queues[i];
                if (null != queue && 0 != queue.Count)
                {
                    T last = queue.Last.Value;
                    queue.RemoveLast();
                    return last;
                }
            }
            return default(T);
        }

        public T Remove(priority_queue_node<T> node)
        {
            if (null != node._node)
            {
                _queues[node._priority].Remove(node._node);
                return node._node.Value;
            }
            return default(T);
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
        public abstract void begin(generator host);
        public abstract Task<select_chan_state> invoke(Func<Task> stepOne = null);
        public abstract Task<bool> errInvoke(chan_async_state state);
        public abstract Task end();
        public abstract bool is_read();
        public abstract chan_base channel();
    }

    public abstract class chan_base
    {
        protected shared_strand _strand;
        protected bool _closed;
        public abstract chan_type type();
        internal abstract void clear_(Action ntf);
        internal abstract void close_(Action ntf, bool isClear = false);
        internal abstract void cancel_(Action ntf, bool isClear = false);
        internal abstract void append_pop_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign, int ms);
        internal abstract void remove_pop_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign);
        internal abstract void append_push_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign, int ms);
        internal abstract void remove_push_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign);
        internal virtual void append_pop_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign, broadcast_token token, int ms) { append_pop_notify_(ntf, ntfSign, ms); }
        public void clear() { clear(nil_action.action); }
        public void close(bool isClear = false) { close(nil_action.action, isClear); }
        public void cancel(bool isClear = false) { cancel(nil_action.action, isClear); }
        public bool is_closed() { return _closed; }
        public shared_strand self_strand() { return _strand; }

        internal void clear(Action ntf)
        {
            if (_strand.running_in_this_thread()) clear_(ntf);
            else _strand.post(() => clear_(ntf));
        }

        internal void close(Action ntf, bool isClear = false)
        {
            if (_strand.running_in_this_thread()) close_(ntf);
            else _strand.post(() => close_(ntf));
        }

        internal void cancel(Action ntf, bool isClear = false)
        {
            if (_strand.running_in_this_thread()) cancel_(ntf, isClear);
            else _strand.post(() => cancel_(ntf, isClear));
        }

        internal void append_pop_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign, int ms = -1)
        {
            if (_strand.running_in_this_thread()) append_pop_notify_(ntf, ntfSign, ms);
            else _strand.post(() => append_pop_notify_(ntf, ntfSign, ms));
        }

        internal void remove_pop_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            if (_strand.running_in_this_thread()) remove_pop_notify_(ntf, ntfSign);
            else _strand.post(() => remove_pop_notify_(ntf, ntfSign));
        }

        internal void append_push_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign, int ms = -1)
        {
            if (_strand.running_in_this_thread()) append_push_notify_(ntf, ntfSign, ms);
            else _strand.post(() => append_push_notify_(ntf, ntfSign, ms));
        }

        internal void remove_push_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            if (_strand.running_in_this_thread()) remove_push_notify_(ntf, ntfSign);
            else _strand.post(() => remove_push_notify_(ntf, ntfSign));
        }

        internal void append_pop_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign, broadcast_token token, int ms = -1)
        {
            if (_strand.running_in_this_thread()) append_pop_notify_(ntf, ntfSign, token, ms);
            else _strand.post(() => append_pop_notify_(ntf, ntfSign, token, ms));
        }

        static internal void safe_callback(ref priority_queue<notify_pck> callback, chan_async_state state)
        {
            if (!callback.Null && !callback.Empty)
            {
                priority_queue<notify_pck> tempCb = callback;
                callback = chan_async_state.async_closed == state ? default(priority_queue<notify_pck>) : new priority_queue<notify_pck>(2);
                int length = tempCb._queues.Length;
                for (int i = 0; i < length; i++)
                {
                    LinkedList<notify_pck> queue = tempCb._queues[i];
                    if (null != queue)
                    {
                        for (LinkedListNode<notify_pck> it = queue.First; null != it; it = it.Next)
                        {
                            it.Value.Invoke(state);
                        }
                    }
                }
            }
        }

        static internal void safe_callback(ref priority_queue<notify_pck> callback1, ref priority_queue<notify_pck> callback2, chan_async_state state)
        {
            priority_queue<notify_pck> tempCb1 = default(priority_queue<notify_pck>);
            priority_queue<notify_pck> tempCb2 = default(priority_queue<notify_pck>);
            if (!callback1.Null && !callback1.Empty)
            {
                tempCb1 = callback1;
                callback1 = chan_async_state.async_closed == state ? default(priority_queue<notify_pck>) : new priority_queue<notify_pck>(2);
            }
            if (!callback2.Null && !callback2.Empty)
            {
                tempCb2 = callback2;
                callback2 = chan_async_state.async_closed == state ? default(priority_queue<notify_pck>) : new priority_queue<notify_pck>(2);
            }
            if (!tempCb1.Null && !tempCb1.Empty)
            {
                int length = tempCb1._queues.Length;
                for (int i = 0; i < length; i++)
                {
                    LinkedList<notify_pck> queue = tempCb1._queues[i];
                    if (null != queue)
                    {
                        for (LinkedListNode<notify_pck> it = queue.First; null != it; it = it.Next)
                        {
                            it.Value.Invoke(state);
                        }
                    }
                }
            }
            if (!tempCb2.Null && !tempCb2.Empty)
            {
                int length = tempCb2._queues.Length;
                for (int i = 0; i < length; i++)
                {
                    LinkedList<notify_pck> queue = tempCb2._queues[i];
                    if (null != queue)
                    {
                        for (LinkedListNode<notify_pck> it = queue.First; null != it; it = it.Next)
                        {
                            it.Value.Invoke(state);
                        }
                    }
                }
            }
        }
    }

    public abstract class chan<T> : chan_base
    {
        internal class select_chan_reader : select_chan_base
        {
            public broadcast_token _token = broadcast_token._defToken;
            public chan<T> _chan;
            public Func<T, Task> _handler;
            public Func<chan_async_state, Task<bool>> _errHandler;
            public chan_lost_msg<T> _lostMsg;
            public int _chanTimeout = -1;
            chan_recv_wrap<T> _tempResult = default(chan_recv_wrap<T>);
            Action<chan_async_state, T, object> _tryPushHandler;
            generator _host;

            public override void begin(generator host)
            {
                ntfSign._disable = false;
                _host = host;
                _chan.append_pop_notify(nextSelect, ntfSign, _chanTimeout);
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
                    _tempResult = new chan_recv_wrap<T> { state = chan_async_state.async_undefined };
                    _chan.try_pop_and_append_notify(_host.async_callback(_tryPushHandler), nextSelect, ntfSign, _token, _chanTimeout);
                    await _host.async_wait();
                }
                catch (generator.stop_exception)
                {
                    _chan.remove_pop_notify(_host.async_ignore<chan_async_state>(), ntfSign);
                    await _host.async_wait();
                    if (chan_async_state.async_ok == _tempResult.state)
                    {
                        _lostMsg?.set(_tempResult.msg);
                    }
                    throw;
                }
                select_chan_state chanState = new select_chan_state() { failed = false, nextRound = true };
                if (chan_async_state.async_ok == _tempResult.state)
                {
                    _lostMsg?.set(_tempResult.msg);
                    if (null != stepOne)
                    {
                        await stepOne();
                    }
                    try
                    {
                        await generator.unlock_suspend();
                        _lostMsg?.clear();
                        await _handler(_tempResult.msg);
                    }
                    finally
                    {
                        generator.lock_suspend();
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

            public override async Task<bool> errInvoke(chan_async_state state)
            {
                if (null != _errHandler)
                {
                    try
                    {
                        await generator.unlock_suspend();
                        if (!await _errHandler(state) && chan_async_state.async_closed != state)
                        {
                            _chan.append_pop_notify(nextSelect, ntfSign, _chanTimeout);
                            return false;
                        }
                    }
                    finally
                    {
                        generator.lock_suspend();
                    }
                }
                return true;
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

            public override chan_base channel()
            {
                return _chan;
            }
        }

        internal class select_chan_writer : select_chan_base
        {
            public chan<T> _chan;
            public async_result_wrap<T> _msg;
            public Func<Task> _handler;
            public Func<chan_async_state, Task<bool>> _errHandler;
            public chan_lost_msg<T> _lostMsg;
            public int _chanTimeout = -1;
            chan_async_state _tempResult = chan_async_state.async_undefined;
            Action<chan_async_state, object> _tryPushHandler;
            generator _host;

            public override void begin(generator host)
            {
                ntfSign._disable = false;
                _host = host;
                _chan.append_push_notify(nextSelect, ntfSign, _chanTimeout);
            }

            public override async Task<select_chan_state> invoke(Func<Task> stepOne)
            {
                if (null == _tryPushHandler)
                {
                    _tryPushHandler = (chan_async_state state, object _) => _tempResult = state;
                }
                try
                {
                    _tempResult = chan_async_state.async_undefined;
                    _chan.try_push_and_append_notify(_host.async_callback(_tryPushHandler), nextSelect, ntfSign, _msg.value1, _chanTimeout);
                    await _host.async_wait();
                }
                catch (generator.stop_exception)
                {
                    _chan.remove_push_notify(_host.async_callback(nil_action<chan_async_state>.action), ntfSign);
                    await _host.async_wait();
                    if (chan_async_state.async_ok != _tempResult)
                    {
                        _lostMsg?.set(_msg.value1);
                    }
                    throw;
                }
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

            public override async Task<bool> errInvoke(chan_async_state state)
            {
                if (null != _errHandler)
                {
                    try
                    {
                        await generator.unlock_suspend();
                        if (!await _errHandler(state) && chan_async_state.async_closed != state)
                        {
                            _chan.append_push_notify(nextSelect, ntfSign, _chanTimeout);
                            return false;
                        }
                    }
                    finally
                    {
                        generator.lock_suspend();
                    }
                }
                return true;
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

            public override chan_base channel()
            {
                return _chan;
            }
        }

        internal abstract void push_(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign);
        internal abstract void pop_(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign);
        internal abstract void try_push_(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign);
        internal abstract void try_pop_(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign);
        internal abstract void timed_push_(int ms, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign);
        internal abstract void timed_pop_(int ms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign);
        internal abstract void try_pop_and_append_notify_(Action<chan_async_state, T, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, int ms);
        internal abstract void try_push_and_append_notify_(Action<chan_async_state, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, T msg, int ms);

        internal void push(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            if (_strand.running_in_this_thread()) push_(ntf, msg, ntfSign);
            else _strand.post(() => push_(ntf, msg, ntfSign));
        }

        internal void pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            if (_strand.running_in_this_thread()) pop_(ntf, ntfSign);
            else _strand.post(() => pop_(ntf, ntfSign));
        }

        internal void try_push(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            if (_strand.running_in_this_thread()) try_push_(ntf, msg, ntfSign);
            else _strand.post(() => try_push_(ntf, msg, ntfSign));
        }

        internal void try_pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            if (_strand.running_in_this_thread()) try_pop_(ntf, ntfSign);
            else _strand.post(() => try_pop_(ntf, ntfSign));
        }

        internal void timed_push(int ms, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            if (_strand.running_in_this_thread()) timed_push_(ms, ntf, msg, ntfSign);
            else _strand.post(() => timed_push_(ms, ntf, msg, ntfSign));
        }

        internal void timed_pop(int ms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            if (_strand.running_in_this_thread()) timed_pop_(ms, ntf, ntfSign);
            else _strand.post(() => timed_pop_(ms, ntf, ntfSign));
        }

        internal void try_pop_and_append_notify(Action<chan_async_state, T, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, int ms = -1)
        {
            if (_strand.running_in_this_thread()) try_pop_and_append_notify_(cb, msgNtf, ntfSign, ms);
            else _strand.post(() => try_pop_and_append_notify_(cb, msgNtf, ntfSign, ms));
        }

        internal void try_push_and_append_notify(Action<chan_async_state, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, T msg, int ms = -1)
        {
            if (_strand.running_in_this_thread()) try_push_and_append_notify_(cb, msgNtf, ntfSign, msg, ms);
            else _strand.post(() => try_push_and_append_notify_(cb, msgNtf, ntfSign, msg, ms));
        }

        static public chan<T> make(shared_strand strand, int len)
        {
            if (0 == len)
            {
                return new nil_chan<T>(strand);
            }
            else if (0 < len)
            {
                return new limit_chan<T>(strand, len);
            }
            return new unlimit_chan<T>(strand);
        }

        static public chan<T> make(int len)
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

        internal select_chan_base make_select_reader(Func<T, Task> handler, chan_lost_msg<T> lostMsg)
        {
            return new select_chan_reader() { _chan = this, _handler = handler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_reader(Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return new select_chan_reader() { _chan = this, _handler = handler, _errHandler = errHandler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_reader(int ms, Func<T, Task> handler, chan_lost_msg<T> lostMsg)
        {
            return new select_chan_reader() { _chanTimeout = ms, _chan = this, _handler = handler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_reader(int ms, Func<T, Task> handler, Func<chan_async_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return new select_chan_reader() { _chanTimeout = ms, _chan = this, _handler = handler, _errHandler = errHandler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_writer(async_result_wrap<T> msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return new select_chan_writer() { _chan = this, _msg = msg, _handler = handler, _errHandler = errHandler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_writer(T msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return make_select_writer(new async_result_wrap<T> { value1 = msg }, handler, errHandler, lostMsg);
        }

        internal select_chan_base make_select_writer(int ms, async_result_wrap<T> msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return new select_chan_writer() { _chanTimeout = ms, _chan = this, _msg = msg, _handler = handler, _errHandler = errHandler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_writer(int ms, T msg, Func<Task> handler, Func<chan_async_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return make_select_writer(ms, new async_result_wrap<T> { value1 = msg }, handler, errHandler, lostMsg);
        }

        internal virtual void pop_(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign, broadcast_token token)
        {
            pop_(ntf, ntfSign);
        }

        internal virtual void try_pop_(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign, broadcast_token token)
        {
            try_pop_(ntf, ntfSign);
        }

        internal virtual void timed_pop_(int ms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign, broadcast_token token)
        {
            timed_pop_(ms, ntf, ntfSign);
        }

        internal virtual void try_pop_and_append_notify_(Action<chan_async_state, T, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, broadcast_token token, int ms = -1)
        {
            try_pop_and_append_notify_(cb, msgNtf, ntfSign, ms);
        }

        internal void pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign, broadcast_token token)
        {
            if (_strand.running_in_this_thread()) pop_(ntf, ntfSign, token);
            else _strand.post(() => pop_(ntf, ntfSign, token));
        }

        internal void try_pop(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign, broadcast_token token)
        {
            if (_strand.running_in_this_thread()) try_pop_(ntf, ntfSign, token);
            else _strand.post(() => try_pop_(ntf, ntfSign, token));
        }

        internal void timed_pop(int ms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign, broadcast_token token)
        {
            if (_strand.running_in_this_thread()) timed_pop_(ms, ntf, ntfSign, token);
            else _strand.post(() => timed_pop_(ms, ntf, ntfSign, token));
        }

        internal void try_pop_and_append_notify(Action<chan_async_state, T, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, broadcast_token token, int ms = -1)
        {
            if (_strand.running_in_this_thread()) try_pop_and_append_notify_(cb, msgNtf, ntfSign, token, ms);
            else _strand.post(() => try_pop_and_append_notify_(cb, msgNtf, ntfSign, token, ms));
        }

        internal virtual select_chan_base make_select_reader(Func<T, Task> handler, broadcast_token token, chan_lost_msg<T> lostMsg)
        {
            return make_select_reader(handler, lostMsg);
        }

        internal virtual select_chan_base make_select_reader(Func<T, Task> handler, broadcast_token token, Func<chan_async_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return make_select_reader(handler, errHandler, lostMsg);
        }

        internal virtual select_chan_base make_select_reader(int ms, Func<T, Task> handler, broadcast_token token, chan_lost_msg<T> lostMsg)
        {
            return make_select_reader(ms, handler, lostMsg);
        }

        internal virtual select_chan_base make_select_reader(int ms, Func<T, Task> handler, broadcast_token token, Func<chan_async_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return make_select_reader(ms, handler, errHandler, lostMsg);
        }
    }

    abstract class msg_queue<T>
    {
        public abstract void AddLast(T msg);
        public abstract T First();
        public abstract void RemoveFirst();
        public abstract int Count { get; }
        public abstract void Clear();
    }

    class no_void_msg_queue<T> : msg_queue<T>
    {
        LinkedList<T> _msgBuff;

        public no_void_msg_queue()
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

    class void_msg_queue<T> : msg_queue<T>
    {
        int _count;

        public void_msg_queue()
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

    public class unlimit_chan<T> : chan<T>
    {
        msg_queue<T> _buffer;
        priority_queue<notify_pck> _waitQueue;

        public unlimit_chan(shared_strand strand)
        {
            init(strand);
        }

        public unlimit_chan()
        {
            shared_strand strand = generator.self_strand();
            init(null != strand ? strand : new shared_strand());
        }

        private void init(shared_strand strand)
        {
            _strand = strand;
            _closed = false;
            _buffer = typeof(T) == typeof(void_type) ? (msg_queue<T>)new void_msg_queue<T>() : new no_void_msg_queue<T>();
            _waitQueue = new priority_queue<notify_pck>(2);
        }

        public override chan_type type()
        {
            return chan_type.unlimit;
        }

        internal override void push_(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_closed)
            {
                ntf(chan_async_state.async_closed, null);
                return;
            }
            _buffer.AddLast(msg);
            _waitQueue.RemoveFirst().Invoke(chan_async_state.async_ok);
            ntf(chan_async_state.async_ok, null);
        }

        internal override void pop_(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
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
                chan_notify_sign.set_node(ntfSign, _waitQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign?.clear();
                        if (chan_async_state.async_ok == state)
                        {
                            pop_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(state, default(T), null);
                        }
                    }
                }));
            }
        }

        internal override void try_push_(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            push_(ntf, msg, ntfSign);
        }

        internal override void try_pop_(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
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
        }

        internal override void timed_push_(int ms, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            push_(ntf, msg, ntfSign);
        }

        internal override void timed_pop_(int ms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
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
                option_node node = _waitQueue.AddLast(0, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign?.clear();
                        timer.cancel();
                        if (chan_async_state.async_ok == state)
                        {
                            pop_(ntf, ntfSign);
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
                    _waitQueue.Remove(node).Invoke(chan_async_state.async_overtime);
                });
            }
            else
            {
                chan_notify_sign.set_node(ntfSign, _waitQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign?.clear();
                        if (chan_async_state.async_ok == state)
                        {
                            pop_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(state, default(T), null);
                        }
                    }
                }));
            }
        }

        internal override void append_pop_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign, int ms)
        {
            if (0 != _buffer.Count)
            {
                ntf(chan_async_state.async_ok);
            }
            else if (_closed)
            {
                ntf(chan_async_state.async_closed);
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(_strand);
                ntfSign._ntfNode = _waitQueue.AddLast(1, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_async_state state)
                    {
                        timer.cancel();
                        ntfSign._ntfNode = default(option_node);
                        ntfSign._success = chan_async_state.async_ok == state;
                        ntf(state);
                    }
                });
                timer.timeout(ms, delegate ()
                {
                    _waitQueue.Remove(ntfSign._ntfNode).Invoke(chan_async_state.async_overtime);
                });
            }
            else
            {
                ntfSign._ntfNode = _waitQueue.AddLast(1, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign._ntfNode = default(option_node);
                        ntfSign._success = chan_async_state.async_ok == state;
                        ntf(state);
                    }
                });
            }
        }

        internal override void try_pop_and_append_notify_(Action<chan_async_state, T, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, int ms)
        {
            ntfSign.reset_success();
            if (0 != _buffer.Count)
            {
                T msg = _buffer.First();
                _buffer.RemoveFirst();
                if (!ntfSign._selectOnce)
                {
                    append_pop_notify_(msgNtf, ntfSign, ms);
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
                append_pop_notify_(msgNtf, ntfSign, ms);
                cb(chan_async_state.async_fail, default(T), null);
            }
        }

        internal override void remove_pop_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            bool effect = ntfSign._ntfNode.effect;
            bool success = ntfSign._success;
            ntfSign._success = false;
            if (effect)
            {
                _waitQueue.Remove(ntfSign._ntfNode).cancel_timer();
                ntfSign._ntfNode = default(option_node);
            }
            else if (success && 0 != _buffer.Count)
            {
                _waitQueue.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
            ntf(effect ? chan_async_state.async_ok : chan_async_state.async_fail);
        }

        internal override void append_push_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign, int ms)
        {
            if (_closed)
            {
                ntf(chan_async_state.async_closed);
                return;
            }
            ntf(chan_async_state.async_ok);
        }

        internal override void try_push_and_append_notify_(Action<chan_async_state, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, T msg, int ms)
        {
            ntfSign.reset_success();
            if (_closed)
            {
                msgNtf(chan_async_state.async_closed);
                cb(chan_async_state.async_closed, null);
                return;
            }
            _buffer.AddLast(msg);
            _waitQueue.RemoveFirst();
            if (!ntfSign._selectOnce)
            {
                append_push_notify_(msgNtf, ntfSign, ms);
            }
            cb(chan_async_state.async_ok, null);
        }

        internal override void remove_push_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            ntf(chan_async_state.async_fail);
        }

        internal override void clear_(Action ntf)
        {
            _buffer.Clear();
            ntf();
        }

        internal override void close_(Action ntf, bool isClear = false)
        {
            _closed = true;
            if (isClear)
            {
                _buffer.Clear();
            }
            safe_callback(ref _waitQueue, chan_async_state.async_closed);
            ntf();
        }

        internal override void cancel_(Action ntf, bool isClear = false)
        {
            if (isClear)
            {
                _buffer.Clear();
            }
            safe_callback(ref _waitQueue, chan_async_state.async_cancel);
            ntf();
        }
    }

    public class limit_chan<T> : chan<T>
    {
        msg_queue<T> _buffer;
        priority_queue<notify_pck> _pushWait;
        priority_queue<notify_pck> _popWait;
        int _length;

        public limit_chan(shared_strand strand, int len)
        {
#if DEBUG
            Trace.Assert(len > 0, string.Format("limit_chan<{0}>长度必须大于0!", typeof(T).Name));
#endif
            init(strand, len);
        }

        public limit_chan(int len)
        {
            shared_strand strand = generator.self_strand();
            init(null != strand ? strand : new shared_strand(), len);
        }

        private void init(shared_strand strand, int len)
        {
            _strand = strand;
            _buffer = typeof(T) == typeof(void_type) ? (msg_queue<T>)new void_msg_queue<T>() : new no_void_msg_queue<T>();
            _pushWait = new priority_queue<notify_pck>(2);
            _popWait = new priority_queue<notify_pck>(2);
            _length = len;
            _closed = false;
        }

        public override chan_type type()
        {
            return chan_type.limit;
        }

        internal override void push_(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_closed)
            {
                ntf(chan_async_state.async_closed, null);
                return;
            }
            if (_buffer.Count == _length)
            {
                chan_notify_sign.set_node(ntfSign, _pushWait.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign?.clear();
                        if (chan_async_state.async_ok == state)
                        {
                            push_(ntf, msg, ntfSign);
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
                _popWait.RemoveFirst().Invoke(chan_async_state.async_ok);
                ntf(chan_async_state.async_ok, null);
            }
        }

        internal void force_push_(Action<chan_async_state, bool, T> ntf, T msg)
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
            _popWait.RemoveFirst().Invoke(chan_async_state.async_ok);
            if (hasOut)
            {
                ntf(chan_async_state.async_ok, true, outMsg);
            }
            else
            {
                ntf(chan_async_state.async_ok, false, default(T));
            }
        }

        internal void force_push(Action<chan_async_state, bool, T> ntf, T msg)
        {
            if (_strand.running_in_this_thread()) force_push_(ntf, msg);
            else _strand.post(() => force_push_(ntf, msg));
        }

        internal override void pop_(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (0 != _buffer.Count)
            {
                T msg = _buffer.First();
                _buffer.RemoveFirst();
                _pushWait.RemoveFirst().Invoke(chan_async_state.async_ok);
                ntf(chan_async_state.async_ok, msg, null);
            }
            else if (_closed)
            {
                ntf(chan_async_state.async_closed, default(T), null);
            }
            else
            {
                chan_notify_sign.set_node(ntfSign, _popWait.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign?.clear();
                        if (chan_async_state.async_ok == state)
                        {
                            pop_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(state, default(T), null);
                        }
                    }
                }));
            }
        }

        internal override void try_push_(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
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
                _popWait.RemoveFirst().Invoke(chan_async_state.async_ok);
                ntf(chan_async_state.async_ok, null);
            }
        }

        internal override void try_pop_(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (0 != _buffer.Count)
            {
                T msg = _buffer.First();
                _buffer.RemoveFirst();
                _pushWait.RemoveFirst().Invoke(chan_async_state.async_ok);
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
        }

        internal override void timed_push_(int ms, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_buffer.Count == _length)
            {
                if (ms >= 0)
                {
                    async_timer timer = new async_timer(_strand);
                    option_node node = _pushWait.AddLast(0, new notify_pck()
                    {
                        timer = timer,
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            timer.cancel();
                            if (chan_async_state.async_ok == state)
                            {
                                push_(ntf, msg, ntfSign);
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
                        _pushWait.Remove(node).Invoke(chan_async_state.async_overtime);
                    });
                }
                else
                {
                    chan_notify_sign.set_node(ntfSign, _pushWait.AddLast(0, new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            if (chan_async_state.async_ok == state)
                            {
                                push_(ntf, msg, ntfSign);
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
                _popWait.RemoveFirst().Invoke(chan_async_state.async_ok);
                ntf(chan_async_state.async_ok, null);
            }
        }

        internal override void timed_pop_(int ms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (0 != _buffer.Count)
            {
                T msg = _buffer.First();
                _buffer.RemoveFirst();
                _pushWait.RemoveFirst().Invoke(chan_async_state.async_ok);
                ntf(chan_async_state.async_ok, msg, null);
            }
            else if (_closed)
            {
                ntf(chan_async_state.async_closed, default(T), null);
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(_strand);
                option_node node = _popWait.AddLast(0, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign?.clear();
                        timer.cancel();
                        if (chan_async_state.async_ok == state)
                        {
                            pop_(ntf, ntfSign);
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
                    _popWait.Remove(node).Invoke(chan_async_state.async_overtime);
                });
            }
            else
            {
                chan_notify_sign.set_node(ntfSign, _popWait.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign?.clear();
                        if (chan_async_state.async_ok == state)
                        {
                            pop_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(state, default(T), null);
                        }
                    }
                }));
            }
        }

        internal override void append_pop_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign, int ms)
        {
            if (0 != _buffer.Count)
            {
                ntf(chan_async_state.async_ok);
            }
            else if (_closed)
            {
                ntf(chan_async_state.async_closed);
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(_strand);
                ntfSign._ntfNode = _popWait.AddLast(1, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_async_state state)
                    {
                        timer.cancel();
                        ntfSign._ntfNode = default(option_node);
                        ntfSign._success = chan_async_state.async_ok == state;
                        ntf(state);
                    }
                });
                timer.timeout(ms, delegate ()
                {
                    _popWait.Remove(ntfSign._ntfNode).Invoke(chan_async_state.async_overtime);
                });
            }
            else
            {
                ntfSign._ntfNode = _popWait.AddLast(1, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign._ntfNode = default(option_node);
                        ntfSign._success = chan_async_state.async_ok == state;
                        ntf(state);
                    }
                });
            }
        }

        internal override void try_pop_and_append_notify_(Action<chan_async_state, T, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, int ms)
        {
            ntfSign.reset_success();
            if (0 != _buffer.Count)
            {
                T msg = _buffer.First();
                _buffer.RemoveFirst();
                _pushWait.RemoveFirst().Invoke(chan_async_state.async_ok);
                if (!ntfSign._selectOnce)
                {
                    append_pop_notify_(msgNtf, ntfSign, ms);
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
                append_pop_notify_(msgNtf, ntfSign, ms);
                cb(chan_async_state.async_fail, default(T), null);
            }
        }
        internal override void remove_pop_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            bool effect = ntfSign._ntfNode.effect;
            bool success = ntfSign._success;
            ntfSign._success = false;
            if (effect)
            {
                _popWait.Remove(ntfSign._ntfNode).cancel_timer();
                ntfSign._ntfNode = default(option_node);
            }
            else if (success && 0 != _buffer.Count)
            {
                _popWait.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
            ntf(effect ? chan_async_state.async_ok : chan_async_state.async_fail);
        }

        internal override void append_push_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign, int ms)
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
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(_strand);
                ntfSign._ntfNode = _pushWait.AddLast(1, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_async_state state)
                    {
                        timer.cancel();
                        ntfSign._ntfNode = default(option_node);
                        ntfSign._success = chan_async_state.async_ok == state;
                        ntf(state);
                    }
                });
                timer.timeout(ms, delegate ()
                {
                    _pushWait.Remove(ntfSign._ntfNode).Invoke(chan_async_state.async_overtime);
                });
            }
            else
            {
                ntfSign._ntfNode = _pushWait.AddLast(1, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign._ntfNode = default(option_node);
                        ntfSign._success = chan_async_state.async_ok == state;
                        ntf(state);
                    }
                });
            }
        }

        internal override void try_push_and_append_notify_(Action<chan_async_state, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, T msg, int ms)
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
                _popWait.RemoveFirst().Invoke(chan_async_state.async_ok);
                if (!ntfSign._selectOnce)
                {
                    append_push_notify_(msgNtf, ntfSign, ms);
                }
                cb(chan_async_state.async_ok, null);
            }
            else
            {
                append_push_notify_(msgNtf, ntfSign, ms);
                cb(chan_async_state.async_fail, null);
            }
        }

        internal override void remove_push_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            bool effect = ntfSign._ntfNode.effect;
            bool success = ntfSign._success;
            ntfSign._success = false;
            if (effect)
            {
                _pushWait.Remove(ntfSign._ntfNode).cancel_timer();
                ntfSign._ntfNode = default(option_node);
            }
            else if (success && _buffer.Count != _length)
            {
                _pushWait.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
            ntf(effect ? chan_async_state.async_ok : chan_async_state.async_fail);
        }

        internal override void clear_(Action ntf)
        {
            _buffer.Clear();
            safe_callback(ref _pushWait, chan_async_state.async_fail);
            ntf();
        }

        internal override void close_(Action ntf, bool isClear = false)
        {
            _closed = true;
            if (isClear)
            {
                _buffer.Clear();
            }
            safe_callback(ref _popWait, ref _pushWait, chan_async_state.async_closed);
            ntf();
        }

        internal override void cancel_(Action ntf, bool isClear = false)
        {
            if (isClear)
            {
                _buffer.Clear();
            }
            safe_callback(ref _popWait, ref _pushWait, chan_async_state.async_cancel);
            ntf();
        }
    }

    public class nil_chan<T> : chan<T>
    {
        priority_queue<notify_pck> _pushWait;
        priority_queue<notify_pck> _popWait;
        T _msg;
        bool _isTryPush;
        bool _isTryPop;
        bool _has;

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
            _pushWait = new priority_queue<notify_pck>(2);
            _popWait = new priority_queue<notify_pck>(2);
            _isTryPush = false;
            _isTryPop = false;
            _has = false;
            _closed = false;
        }

        public override chan_type type()
        {
            return chan_type.nil;
        }

        internal override void push_(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_closed)
            {
                ntf(chan_async_state.async_closed, null);
                return;
            }
            if (_has || _popWait.Empty)
            {
                chan_notify_sign.set_node(ntfSign, _pushWait.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign?.clear();
                        if (chan_async_state.async_ok == state)
                        {
                            push_(ntf, msg, ntfSign);
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
                chan_notify_sign.set_node(ntfSign, _pushWait.AddFirst(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign?.clear();
                        ntf(state, null);
                    }
                }));
                _popWait.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
        }

        internal override void pop_(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_has)
            {
                T msg = _msg;
                _has = false;
                _pushWait.RemoveFirst().Invoke(chan_async_state.async_ok);
                ntf(chan_async_state.async_ok, msg, default(T));
            }
            else if (_closed)
            {
                ntf(chan_async_state.async_closed, default(T), null);
            }
            else
            {
                chan_notify_sign.set_node(ntfSign, _popWait.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign?.clear();
                        if (chan_async_state.async_ok == state)
                        {
                            pop_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(state, default(T), null);
                        }
                    }
                }));
                _pushWait.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
        }

        internal override void try_push_(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_closed)
            {
                ntf(chan_async_state.async_closed, null);
                return;
            }
            if (_has || _popWait.Empty)
            {
                ntf(chan_async_state.async_fail, null);
            }
            else
            {
                _msg = msg;
                _has = true;
                _isTryPush = true;
                chan_notify_sign.set_node(ntfSign, _pushWait.AddFirst(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        _isTryPush = false;
                        ntfSign?.clear();
                        ntf(state, null);
                    }
                }));
                _popWait.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
        }

        internal override void try_pop_(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_has)
            {
                T msg = _msg;
                _has = false;
                _pushWait.RemoveFirst().Invoke(chan_async_state.async_ok);
                ntf(chan_async_state.async_ok, msg, null);
            }
            else if (_closed)
            {
                ntf(chan_async_state.async_closed, default(T), null);
            }
            else if (!_pushWait.Empty && _popWait.Empty)
            {
                _isTryPop = true;
                chan_notify_sign.set_node(ntfSign, _popWait.AddFirst(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        _isTryPop = false;
                        ntfSign?.clear();
                        if (chan_async_state.async_ok == state)
                        {
                            pop_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(state, default(T), null);
                        }
                    }
                }));
                _pushWait.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
            else
            {
                ntf(chan_async_state.async_fail, default(T), null);
            }
        }

        internal override void timed_push_(int ms, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_closed)
            {
                ntf(chan_async_state.async_closed, null);
                return;
            }
            if (_has || _popWait.Empty)
            {
                if (ms >= 0)
                {
                    async_timer timer = new async_timer(_strand);
                    option_node node = _pushWait.AddLast(0, new notify_pck()
                    {
                        timer = timer,
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            timer.cancel();
                            if (chan_async_state.async_ok == state)
                            {
                                push_(ntf, msg, ntfSign);
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
                        _pushWait.Remove(node).Invoke(chan_async_state.async_overtime);
                    });
                }
                else
                {
                    chan_notify_sign.set_node(ntfSign, _pushWait.AddLast(0, new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            if (chan_async_state.async_ok == state)
                            {
                                push_(ntf, msg, ntfSign);
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
                option_node node = _pushWait.AddFirst(0, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign?.clear();
                        timer.cancel();
                        ntf(state, null);
                    }
                });
                ntfSign?.set(node);
                timer.timeout(ms, delegate ()
                {
                    _has = false;
                    _pushWait.Remove(node).Invoke(chan_async_state.async_overtime);
                });
                _popWait.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
            else
            {
                _msg = msg;
                _has = true;
                chan_notify_sign.set_node(ntfSign, _pushWait.AddFirst(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign?.clear();
                        ntf(state, null);
                    }
                }));
                _popWait.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
        }

        internal override void timed_pop_(int ms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_has)
            {
                T msg = _msg;
                _has = false;
                _pushWait.RemoveFirst().Invoke(chan_async_state.async_ok);
                ntf(chan_async_state.async_ok, msg, null);
            }
            else if (_closed)
            {
                ntf(chan_async_state.async_closed, default(T), null);
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(_strand);
                option_node node = _popWait.AddLast(0, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign?.clear();
                        timer.cancel();
                        if (chan_async_state.async_ok == state)
                        {
                            pop_(ntf, ntfSign);
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
                    _popWait.Remove(node).Invoke(chan_async_state.async_overtime);
                });
                _pushWait.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
            else
            {
                chan_notify_sign.set_node(ntfSign, _popWait.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign?.clear();
                        if (chan_async_state.async_ok == state)
                        {
                            pop_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(state, default(T), null);
                        }
                    }
                }));
                _pushWait.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
        }

        internal override void append_pop_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign, int ms)
        {
            if (_has)
            {
                ntf(chan_async_state.async_ok);
            }
            else if (_closed)
            {
                ntf(chan_async_state.async_closed);
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(_strand);
                ntfSign._ntfNode = _popWait.AddLast(1, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_async_state state)
                    {
                        timer.cancel();
                        ntfSign._ntfNode = default(option_node);
                        ntfSign._success = chan_async_state.async_ok == state;
                        ntf(state);
                    }
                });
                timer.timeout(ms, delegate ()
                {
                    _popWait.Remove(ntfSign._ntfNode).Invoke(chan_async_state.async_overtime);
                });
                _pushWait.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
            else
            {
                ntfSign._ntfNode = _popWait.AddLast(1, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign._ntfNode = default(option_node);
                        ntfSign._success = chan_async_state.async_ok == state;
                        ntf(state);
                    }
                });
                _pushWait.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
        }

        internal override void try_pop_and_append_notify_(Action<chan_async_state, T, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, int ms)
        {
            ntfSign.reset_success();
            if (_has)
            {
                T msg = _msg;
                _has = false;
                _pushWait.RemoveFirst().Invoke(chan_async_state.async_ok);
                if (!ntfSign._selectOnce)
                {
                    append_pop_notify_(msgNtf, ntfSign, ms);
                }
                cb(chan_async_state.async_ok, msg, null);
            }
            else if (_closed)
            {
                msgNtf(chan_async_state.async_closed);
                cb(chan_async_state.async_closed, default(T), null);
            }
            else if (!_pushWait.Empty && _popWait.Empty)
            {
                _isTryPop = true;
                chan_notify_sign.set_node(ntfSign, _popWait.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        _isTryPop = false;
                        ntfSign?.clear();
                        if (chan_async_state.async_ok == state)
                        {
                            pop_(cb, ntfSign);
                        }
                        else
                        {
                            cb(state, default(T), null);
                        }
                    }
                }));
                _pushWait.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
            else
            {
                append_pop_notify_(msgNtf, ntfSign, ms);
                cb(chan_async_state.async_fail, default(T), null);
            }
        }

        internal override void remove_pop_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            bool effect = ntfSign._ntfNode.effect;
            bool success = ntfSign._success;
            ntfSign._success = false;
            if (effect)
            {
                _isTryPop &= _popWait.First._node != ntfSign._ntfNode._node;
                _popWait.Remove(ntfSign._ntfNode).cancel_timer();
                ntfSign._ntfNode = default(option_node);
            }
            else if (success && _has)
            {
                if (!_popWait.RemoveFirst().Invoke(chan_async_state.async_ok) && _isTryPush)
                {
                    _has = !_pushWait.RemoveFirst().Invoke(chan_async_state.async_fail);
                }
            }
            ntf(effect ? chan_async_state.async_ok : chan_async_state.async_fail);
        }

        internal override void append_push_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign, int ms)
        {
            if (_closed)
            {
                ntf(chan_async_state.async_closed);
                return;
            }
            if (!_popWait.Empty)
            {
                ntf(chan_async_state.async_ok);
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(_strand);
                ntfSign._ntfNode = _pushWait.AddLast(1, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_async_state state)
                    {
                        timer.cancel();
                        ntfSign._ntfNode = default(option_node);
                        ntfSign._success = chan_async_state.async_ok == state;
                        ntf(state);
                    }
                });
                timer.timeout(ms, delegate ()
                {
                    _pushWait.Remove(ntfSign._ntfNode).Invoke(chan_async_state.async_overtime);
                });
            }
            else
            {
                ntfSign._ntfNode = _pushWait.AddLast(1, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign._ntfNode = default(option_node);
                        ntfSign._success = chan_async_state.async_ok == state;
                        ntf(state);
                    }
                });
            }
        }

        internal override void try_push_and_append_notify_(Action<chan_async_state, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, T msg, int ms)
        {
            ntfSign.reset_success();
            if (_closed)
            {
                msgNtf(chan_async_state.async_closed);
                cb(chan_async_state.async_closed, null);
                return;
            }
            if (!_has && !_popWait.Empty)
            {
                _has = true;
                _msg = msg;
                _isTryPush = true;
                ntfSign._ntfNode = _pushWait.AddFirst(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        _isTryPush = false;
                        ntfSign._ntfNode = default(option_node);
                        if (!ntfSign._selectOnce)
                        {
                            append_push_notify_(msgNtf, ntfSign, ms);
                        }
                        cb(state, null);
                    }
                });
                _popWait.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
            else
            {
                append_push_notify_(msgNtf, ntfSign, ms);
                cb(chan_async_state.async_fail, null);
            }
        }

        internal override void remove_push_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            bool effect = ntfSign._ntfNode.effect;
            bool success = ntfSign._success;
            ntfSign._success = false;
            if (effect)
            {
                if (_pushWait.First._node == ntfSign._ntfNode._node)
                {
                    _isTryPush = _has = false;
                }
                _pushWait.Remove(ntfSign._ntfNode).cancel_timer();
                ntfSign._ntfNode = default(option_node);
            }
            else if (success && !_has)
            {
                if (!_pushWait.RemoveFirst().Invoke(chan_async_state.async_ok) && _isTryPop)
                {
                    _popWait.RemoveFirst().Invoke(chan_async_state.async_fail);
                }
            }
            ntf(effect ? chan_async_state.async_ok : chan_async_state.async_fail);
        }

        internal override void clear_(Action ntf)
        {
            _has = false;
            safe_callback(ref _pushWait, chan_async_state.async_fail);
            ntf();
        }

        internal override void close_(Action ntf, bool isClear = false)
        {
            _closed = true;
            _has = false;
            safe_callback(ref _popWait, ref _pushWait, chan_async_state.async_closed);
            ntf();
        }

        internal override void cancel_(Action ntf, bool isClear = false)
        {
            _has = false;
            safe_callback(ref _popWait, ref _pushWait, chan_async_state.async_cancel);
            ntf();
        }
    }

    public class broadcast_token
    {
        internal long _lastId = -1;
        internal static readonly broadcast_token _defToken = new broadcast_token();

        public void reset()
        {
            _lastId = -1;
        }

        public bool is_default()
        {
            return this == _defToken;
        }
    }

    public class broadcast_chan<T> : chan<T>
    {
        priority_queue<notify_pck> _popWait;
        T _msg;
        bool _has;
        long _pushCount;

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
            _popWait = new priority_queue<notify_pck>(2);
            _has = false;
            _pushCount = 0;
            _closed = false;
        }

        internal override select_chan_base make_select_reader(Func<T, Task> handler, broadcast_token token, chan_lost_msg<T> lostMsg)
        {
            return new select_chan_reader() { _token = null != token ? token : new broadcast_token(), _chan = this, _handler = handler, _lostMsg = lostMsg };
        }

        internal override select_chan_base make_select_reader(Func<T, Task> handler, broadcast_token token, Func<chan_async_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return new select_chan_reader() { _token = null != token ? token : new broadcast_token(), _chan = this, _handler = handler, _errHandler = errHandler, _lostMsg = lostMsg };
        }

        internal override select_chan_base make_select_reader(int ms, Func<T, Task> handler, broadcast_token token, chan_lost_msg<T> lostMsg)
        {
            return new select_chan_reader() { _chanTimeout = ms, _token = null != token ? token : new broadcast_token(), _chan = this, _handler = handler, _lostMsg = lostMsg };
        }

        internal override select_chan_base make_select_reader(int ms, Func<T, Task> handler, broadcast_token token, Func<chan_async_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return new select_chan_reader() { _chanTimeout = ms, _token = null != token ? token : new broadcast_token(), _chan = this, _handler = handler, _errHandler = errHandler, _lostMsg = lostMsg };
        }

        public override chan_type type()
        {
            return chan_type.broadcast;
        }

        internal override void push_(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
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
            safe_callback(ref _popWait, chan_async_state.async_ok);
            ntf(chan_async_state.async_ok, null);
        }

        internal override void pop_(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            pop_(ntf, ntfSign, broadcast_token._defToken);
        }

        internal override void pop_(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign, broadcast_token token)
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
                chan_notify_sign.set_node(ntfSign, _popWait.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign?.clear();
                        if (chan_async_state.async_ok == state)
                        {
                            pop_(ntf, ntfSign, token);
                        }
                        else
                        {
                            ntf(state, default(T), null);
                        }
                    }
                }));
            }
        }

        internal override void try_push_(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            push_(ntf, msg, ntfSign);
        }

        internal override void try_pop_(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            try_pop_(ntf, ntfSign, broadcast_token._defToken);
        }

        internal override void try_pop_(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign, broadcast_token token)
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
        }

        internal override void timed_push_(int ms, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            push_(ntf, msg, ntfSign);
        }

        internal override void timed_pop_(int ms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            timed_pop_(ms, ntf, ntfSign, broadcast_token._defToken);
        }

        internal override void timed_pop_(int ms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign, broadcast_token token)
        {
            ntfSign?.reset_success();
            _timed_check_pop(system_tick.get_tick_ms() + ms, ntf, ntfSign, token);
        }

        void _timed_check_pop(long deadms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign, broadcast_token token)
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
                option_node node = _popWait.AddLast(0, new notify_pck()
                {
                    timer = timer,
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
                    _popWait.Remove(node).Invoke(chan_async_state.async_overtime);
                });
            }
        }

        internal override void append_pop_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign, int ms)
        {
            append_pop_notify_(ntf, ntfSign, broadcast_token._defToken, ms);
        }

        internal override void append_pop_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign, broadcast_token token, int ms)
        {
            _append_pop_notify(ntf, ntfSign, token, ms);
        }

        bool _append_pop_notify(Action<chan_async_state> ntf, chan_notify_sign ntfSign, broadcast_token token, int ms)
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
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(_strand);
                ntfSign._ntfNode = _popWait.AddLast(1, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_async_state state)
                    {
                        timer.cancel();
                        ntfSign._ntfNode = default(option_node);
                        ntfSign._success = chan_async_state.async_ok == state;
                        ntf(state);
                    }
                });
                timer.timeout(ms, delegate ()
                {
                    _popWait.Remove(ntfSign._ntfNode).Invoke(chan_async_state.async_overtime);
                });
                return false;
            }
            else
            {
                ntfSign._ntfNode = _popWait.AddLast(1, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign._ntfNode = default(option_node);
                        ntfSign._success = chan_async_state.async_ok == state;
                        ntf(state);
                    }
                });
                return false;
            }
        }

        internal override void try_pop_and_append_notify_(Action<chan_async_state, T, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, int ms)
        {
            try_pop_and_append_notify_(cb, msgNtf, ntfSign, broadcast_token._defToken, ms);
        }

        internal override void try_pop_and_append_notify_(Action<chan_async_state, T, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, broadcast_token token, int ms = -1)
        {
            ntfSign.reset_success();
            if (_append_pop_notify(msgNtf, ntfSign, token, ms))
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
        }

        internal override void remove_pop_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            bool effect = ntfSign._ntfNode.effect;
            bool success = ntfSign._success;
            ntfSign._success = false;
            if (effect)
            {
                _popWait.Remove(ntfSign._ntfNode).cancel_timer();
                ntfSign._ntfNode = default(option_node);
            }
            else if (success && _has)
            {
                _popWait.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
            ntf(effect ? chan_async_state.async_ok : chan_async_state.async_fail);
        }

        internal override void append_push_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign, int ms)
        {
            if (_closed)
            {
                ntf(chan_async_state.async_closed);
                return;
            }
            ntf(chan_async_state.async_ok);
        }

        internal override void try_push_and_append_notify_(Action<chan_async_state, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, T msg, int ms)
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
        }

        internal override void remove_push_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            ntf(chan_async_state.async_fail);
        }

        internal override void clear_(Action ntf)
        {
            _has = false;
            ntf();
        }

        internal override void close_(Action ntf, bool isClear = false)
        {
            _closed = true;
            _has &= !isClear;
            safe_callback(ref _popWait, chan_async_state.async_closed);
            ntf();
        }

        internal override void cancel_(Action ntf, bool isClear = false)
        {
            _has &= !isClear;
            safe_callback(ref _popWait, chan_async_state.async_cancel);
            ntf();
        }
    }

    public class csp_chan<R, T> : chan<T>
    {
        struct send_pck
        {
            public Action<chan_async_state, object> _notify;
            public T _msg;
            public bool _has;
            public bool _isTryMsg;
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

            public Action<chan_async_state, object> cancel()
            {
                _isTryMsg = _has = false;
                _timer?.cancel();
                return _notify;
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
            public Func<T, Task<R>> _handler;
            public Func<chan_async_state, Task<bool>> _errHandler;
            public chan_lost_msg<T> _lostMsg;
            public int _chanTimeout = -1;
            csp_wait_wrap<R, T> _tempResult = default(csp_wait_wrap<R, T>);
            Action<chan_async_state, T, object> _tryPopHandler;
            generator _host;

            public override void begin(generator host)
            {
                ntfSign._disable = false;
                _host = host;
                _chan.append_pop_notify(nextSelect, ntfSign, _chanTimeout);
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
                try
                {
                    _tempResult = new csp_wait_wrap<R, T> { state = chan_async_state.async_undefined };
                    _chan.try_pop_and_append_notify(_host.async_callback(_tryPopHandler), nextSelect, ntfSign, _chanTimeout);
                    await _host.async_wait();
                }
                catch (generator.stop_exception)
                {
                    _chan.remove_pop_notify(_host.async_ignore<chan_async_state>(), ntfSign);
                    await _host.async_wait();
                    if (chan_async_state.async_ok == _tempResult.state)
                    {
                        _lostMsg?.set(_tempResult.msg);
                        _tempResult.fail();
                    }
                    throw;
                }
                select_chan_state chanState = new select_chan_state() { failed = false, nextRound = true };
                if (chan_async_state.async_ok == _tempResult.state)
                {
                    _lostMsg?.set(_tempResult.msg);
                    if (null != stepOne)
                    {
                        await stepOne();
                    }
                    try
                    {
                        _tempResult.result.start_invoke_timer(_host);
                        await generator.unlock_suspend();
                        _lostMsg?.clear();
                        _tempResult.complete(await _handler(_tempResult.msg));
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

            public override async Task<bool> errInvoke(chan_async_state state)
            {
                if (null != _errHandler)
                {
                    try
                    {
                        await generator.unlock_suspend();
                        if (!await _errHandler(state) && chan_async_state.async_closed != state)
                        {
                            _chan.append_pop_notify(nextSelect, ntfSign, _chanTimeout);
                            return false;
                        }
                    }
                    finally
                    {
                        generator.lock_suspend();
                    }
                }
                return true;
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

            public override chan_base channel()
            {
                return _chan;
            }
        }

        internal class select_csp_writer : select_chan_base
        {
            public csp_chan<R, T> _chan;
            public async_result_wrap<T> _msg;
            public Func<R, Task> _handler;
            public Func<chan_async_state, Task<bool>> _errHandler;
            public Action<chan_async_state, object> _lostHandler;
            public chan_lost_msg<T> _lostMsg;
            public int _chanTimeout = -1;
            csp_invoke_wrap<R> _tempResult = default(csp_invoke_wrap<R>);
            Action<chan_async_state, object> _tryPushHandler;
            generator _host;

            public override void begin(generator host)
            {
                ntfSign._disable = false;
                _host = host;
                _chan.append_push_notify(nextSelect, ntfSign, _chanTimeout);
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
                try
                {
                    _tempResult = new csp_invoke_wrap<R> { state = chan_async_state.async_undefined };
                    _chan.try_push_and_append_notify(null == _lostHandler ? _host.async_callback(_tryPushHandler) : _host.safe_async_callback(_tryPushHandler, _lostHandler), nextSelect, ntfSign, _msg.value1, _chanTimeout);
                    await _host.async_wait();
                }
                catch (generator.stop_exception)
                {
                    chan_async_state rmState = chan_async_state.async_undefined;
                    _chan.remove_push_notify(_host.async_callback(null == _lostMsg ? nil_action<chan_async_state>.action : (chan_async_state state) => rmState = state), ntfSign);
                    await _host.async_wait();
                    if (chan_async_state.async_ok == rmState)
                    {
                        _lostMsg?.set(_msg.value1);
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
                        _lostMsg?.clear();
                        await _handler(_tempResult.result);
                    }
                    finally
                    {
                        generator.lock_suspend();
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

            public override async Task<bool> errInvoke(chan_async_state state)
            {
                if (null != _errHandler)
                {
                    try
                    {
                        await generator.unlock_suspend();
                        if (!await _errHandler(state) && chan_async_state.async_closed != state)
                        {
                            _chan.append_push_notify(nextSelect, ntfSign, _chanTimeout);
                            return false;
                        }
                    }
                    finally
                    {
                        generator.lock_suspend();
                    }
                }
                return true;
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

            public override chan_base channel()
            {
                return _chan;
            }
        }

        priority_queue<notify_pck> _sendQueue;
        priority_queue<notify_pck> _waitQueue;
        send_pck _msg;
        bool _isTryPop;

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
            _sendQueue = new priority_queue<notify_pck>(2);
            _waitQueue = new priority_queue<notify_pck>(2);
            _msg.cancel();
            _isTryPop = false;
            _closed = false;
        }

        internal select_chan_base make_select_reader(Func<T, Task<R>> handler, chan_lost_msg<T> lostMsg)
        {
            return new select_csp_reader() { _chan = this, _handler = handler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_reader(Func<T, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return new select_csp_reader() { _chan = this, _handler = handler, _errHandler = errHandler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_reader(int ms, Func<T, Task<R>> handler, chan_lost_msg<T> lostMsg)
        {
            return new select_csp_reader() { _chanTimeout = ms, _chan = this, _handler = handler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_reader(int ms, Func<T, Task<R>> handler, Func<chan_async_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return new select_csp_reader() { _chanTimeout = ms, _chan = this, _handler = handler, _errHandler = errHandler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_writer(int ms, async_result_wrap<T> msg, Func<R, Task> handler, Func<chan_async_state, Task<bool>> errHandler, Action<R> lostHandler, chan_lost_msg<T> lostMsg)
        {
            return new select_csp_writer()
            {
                _chanTimeout = ms,
                _chan = this,
                _msg = msg,
                _handler = handler,
                _errHandler = errHandler,
                _lostMsg = lostMsg,
                _lostHandler = null == lostHandler ? (Action<chan_async_state, object>)null : delegate (chan_async_state state, object exObj)
                {
                    if (chan_async_state.async_ok == state)
                    {
                        lostHandler((R)exObj);
                    }
                }
            };
        }

        internal select_chan_base make_select_writer(async_result_wrap<T> msg, Func<R, Task> handler, Func<chan_async_state, Task<bool>> errHandler, Action<R> lostHandler, chan_lost_msg<T> lostMsg)
        {
            return make_select_writer(-1, msg, handler, errHandler, lostHandler, lostMsg);
        }

        internal select_chan_base make_select_writer(T msg, Func<R, Task> handler, Func<chan_async_state, Task<bool>> errHandler, Action<R> lostHandler, chan_lost_msg<T> lostMsg)
        {
            return make_select_writer(-1, new async_result_wrap<T> { value1 = msg }, handler, errHandler, lostHandler, lostMsg);
        }

        internal select_chan_base make_select_writer(int ms, T msg, Func<R, Task> handler, Func<chan_async_state, Task<bool>> errHandler, Action<R> lostHandler, chan_lost_msg<T> lostMsg)
        {
            return make_select_writer(ms, new async_result_wrap<T> { value1 = msg }, handler, errHandler, lostHandler, lostMsg);
        }

        public override chan_type type()
        {
            return chan_type.csp;
        }

        internal override void push_(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            push_(-1, ntf, msg, ntfSign);
        }

        internal void push_(int invokeMs, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_closed)
            {
                ntf(chan_async_state.async_closed, null);
                return;
            }
            if (_msg._has || _waitQueue.Empty)
            {
                chan_notify_sign.set_node(ntfSign, _sendQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign?.clear();
                        if (chan_async_state.async_ok == state)
                        {
                            push_(invokeMs, ntf, msg, ntfSign);
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
                _waitQueue.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
        }

        internal void push(int invokeMs, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            if (_strand.running_in_this_thread()) push_(invokeMs, ntf, msg, ntfSign);
            else _strand.post(() => push_(invokeMs, ntf, msg, ntfSign));
        }

        internal override void pop_(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_msg._has)
            {
                send_pck msg = _msg;
                _msg.cancel();
                _sendQueue.RemoveFirst().Invoke(chan_async_state.async_ok);
                ntf(chan_async_state.async_ok, msg._msg, new csp_result(msg._invokeMs, msg._notify));
            }
            else if (_closed)
            {
                ntf(chan_async_state.async_closed, default(T), null);
            }
            else
            {
                chan_notify_sign.set_node(ntfSign, _waitQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign?.clear();
                        if (chan_async_state.async_ok == state)
                        {
                            pop_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(state, default(T), null);
                        }
                    }
                }));
                _sendQueue.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
        }

        internal override void try_push_(Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            try_push_(-1, ntf, msg, ntfSign);
        }

        internal void try_push_(int invokeMs, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_closed)
            {
                ntf(chan_async_state.async_closed, null);
                return;
            }
            if (_msg._has || _waitQueue.Empty)
            {
                ntf(chan_async_state.async_fail, null);
            }
            else
            {
                _msg.set(ntf, msg, invokeMs);
                _msg._isTryMsg = true;
                _waitQueue.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
        }

        internal void try_push(int invokeMs, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            if (_strand.running_in_this_thread()) try_push_(invokeMs, ntf, msg, ntfSign);
            else _strand.post(() => try_push_(invokeMs, ntf, msg, ntfSign));
        }

        internal override void try_pop_(Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_msg._has)
            {
                send_pck msg = _msg;
                _msg.cancel();
                _sendQueue.RemoveFirst().Invoke(chan_async_state.async_ok);
                ntf(chan_async_state.async_ok, msg._msg, new csp_result(msg._invokeMs, msg._notify));
            }
            else if (_closed)
            {
                ntf(chan_async_state.async_closed, default(T), null);
            }
            else if (!_sendQueue.Empty && _waitQueue.Empty)
            {
                _isTryPop = true;
                chan_notify_sign.set_node(ntfSign, _waitQueue.AddFirst(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        _isTryPop = false;
                        ntfSign?.clear();
                        if (chan_async_state.async_ok == state)
                        {
                            pop_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(state, default(T), null);
                        }
                    }
                }));
                _sendQueue.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
            else
            {
                ntf(chan_async_state.async_fail, default(T), null);
            }
        }

        internal override void timed_push_(int ms, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            timed_push_(ms, -1, ntf, msg, ntfSign);
        }

        internal void timed_push_(int ms, int invokeMs, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_closed)
            {
                ntf(chan_async_state.async_closed, null);
                return;
            }
            if (_msg._has || _waitQueue.Empty)
            {
                if (ms >= 0)
                {
                    async_timer timer = new async_timer(_strand);
                    option_node node = _sendQueue.AddLast(0, new notify_pck()
                    {
                        timer = timer,
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            timer.cancel();
                            if (chan_async_state.async_ok == state)
                            {
                                push_(invokeMs, ntf, msg, ntfSign);
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
                        _sendQueue.Remove(node).Invoke(chan_async_state.async_overtime);
                    });
                }
                else
                {
                    chan_notify_sign.set_node(ntfSign, _sendQueue.AddLast(0, new notify_pck()
                    {
                        ntf = delegate (chan_async_state state)
                        {
                            ntfSign?.clear();
                            if (chan_async_state.async_ok == state)
                            {
                                push_(invokeMs, ntf, msg, ntfSign);
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
                    _msg.cancel();
                    ntf(chan_async_state.async_overtime, null);
                });
                _waitQueue.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
            else
            {
                _msg.set(ntf, msg, invokeMs);
                _waitQueue.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
        }

        internal void timed_push(int ms, int invokeMs, Action<chan_async_state, object> ntf, T msg, chan_notify_sign ntfSign)
        {
            if (_strand.running_in_this_thread()) timed_push_(ms, invokeMs, ntf, msg, ntfSign);
            else _strand.post(() => timed_push_(ms, invokeMs, ntf, msg, ntfSign));
        }

        internal override void timed_pop_(int ms, Action<chan_async_state, T, object> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_msg._has)
            {
                send_pck msg = _msg;
                _msg.cancel();
                _sendQueue.RemoveFirst().Invoke(chan_async_state.async_ok);
                ntf(chan_async_state.async_ok, msg._msg, new csp_result(msg._invokeMs, msg._notify));
            }
            else if (_closed)
            {
                ntf(chan_async_state.async_closed, default(T), null);
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(_strand);
                option_node node = _waitQueue.AddLast(0, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign?.clear();
                        timer.cancel();
                        if (chan_async_state.async_ok == state)
                        {
                            pop_(ntf, ntfSign);
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
                    _waitQueue.Remove(node).Invoke(chan_async_state.async_overtime);
                });
                _sendQueue.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
            else
            {
                chan_notify_sign.set_node(ntfSign, _waitQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign?.clear();
                        if (chan_async_state.async_ok == state)
                        {
                            pop_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(state, default(T), null);
                        }
                    }
                }));
                _sendQueue.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
        }

        internal override void append_pop_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign, int ms)
        {
            if (_msg._has)
            {
                ntf(chan_async_state.async_ok);
            }
            else if (_closed)
            {
                ntf(chan_async_state.async_closed);
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(_strand);
                ntfSign._ntfNode = _waitQueue.AddLast(1, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_async_state state)
                    {
                        timer.cancel();
                        ntfSign._ntfNode = default(option_node);
                        ntfSign._success = chan_async_state.async_ok == state;
                        ntf(state);
                    }
                });
                timer.timeout(ms, delegate ()
                {
                    _waitQueue.Remove(ntfSign._ntfNode).Invoke(chan_async_state.async_overtime);
                });
                _sendQueue.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
            else
            {
                ntfSign._ntfNode = _waitQueue.AddLast(1, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign._ntfNode = default(option_node);
                        ntfSign._success = chan_async_state.async_ok == state;
                        ntf(state);
                    }
                });
                _sendQueue.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
        }

        internal override void try_pop_and_append_notify_(Action<chan_async_state, T, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, int ms)
        {
            ntfSign.reset_success();
            if (_msg._has)
            {
                send_pck msg = _msg;
                _msg.cancel();
                _sendQueue.RemoveFirst().Invoke(chan_async_state.async_ok);
                if (!ntfSign._selectOnce)
                {
                    append_pop_notify_(msgNtf, ntfSign, ms);
                }
                cb(chan_async_state.async_ok, msg._msg, new csp_result(msg._invokeMs, msg._notify));
            }
            else if (_closed)
            {
                msgNtf(chan_async_state.async_closed);
                cb(chan_async_state.async_closed, default(T), null);
            }
            else if (!_sendQueue.Empty && _waitQueue.Empty)
            {
                _isTryPop = true;
                chan_notify_sign.set_node(ntfSign, _waitQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        _isTryPop = false;
                        ntfSign?.clear();
                        if (chan_async_state.async_ok == state)
                        {
                            pop_(cb, ntfSign);
                        }
                        else
                        {
                            cb(state, default(T), null);
                        }
                    }
                }));
                _sendQueue.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
            else
            {
                append_pop_notify_(msgNtf, ntfSign, ms);
                cb(chan_async_state.async_fail, default(T), null);
            }
        }

        internal override void remove_pop_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            bool effect = ntfSign._ntfNode.effect;
            bool success = ntfSign._success;
            ntfSign._success = false;
            if (effect)
            {
                _isTryPop &= _waitQueue.First._node != ntfSign._ntfNode._node;
                _waitQueue.Remove(ntfSign._ntfNode).cancel_timer();
                ntfSign._ntfNode = default(option_node);
            }
            else if (success && _msg._has)
            {
                if (!_waitQueue.RemoveFirst().Invoke(chan_async_state.async_ok) && _msg._isTryMsg)
                {
                    _msg.cancel().Invoke(chan_async_state.async_fail, null);
                }
            }
            ntf(effect ? chan_async_state.async_ok : chan_async_state.async_fail);
        }

        internal override void append_push_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign, int ms)
        {
            if (_closed)
            {
                ntf(chan_async_state.async_closed);
                return;
            }
            if (!_waitQueue.Empty)
            {
                ntf(chan_async_state.async_ok);
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(_strand);
                ntfSign._ntfNode = _sendQueue.AddLast(1, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_async_state state)
                    {
                        timer.cancel();
                        ntfSign._ntfNode = default(option_node);
                        ntfSign._success = chan_async_state.async_ok == state;
                        ntf(state);
                    }
                });
                timer.timeout(ms, delegate ()
                {
                    _sendQueue.Remove(ntfSign._ntfNode).Invoke(chan_async_state.async_overtime);
                });
            }
            else
            {
                ntfSign._ntfNode = _sendQueue.AddLast(1, new notify_pck()
                {
                    ntf = delegate (chan_async_state state)
                    {
                        ntfSign._ntfNode = default(option_node);
                        ntfSign._success = chan_async_state.async_ok == state;
                        ntf(state);
                    }
                });
            }
        }

        internal override void try_push_and_append_notify_(Action<chan_async_state, object> cb, Action<chan_async_state> msgNtf, chan_notify_sign ntfSign, T msg, int ms)
        {
            ntfSign.reset_success();
            if (_closed)
            {
                msgNtf(chan_async_state.async_closed);
                cb(chan_async_state.async_closed, null);
                return;
            }
            if (!_msg._has && !_waitQueue.Empty)
            {
                _msg.set(cb, msg);
                _msg._isTryMsg = true;
                if (!ntfSign._selectOnce)
                {
                    append_push_notify_(msgNtf, ntfSign, ms);
                }
                _waitQueue.RemoveFirst().Invoke(chan_async_state.async_ok);
            }
            else
            {
                append_push_notify_(msgNtf, ntfSign, ms);
                cb(chan_async_state.async_fail, null);
            }
        }

        internal override void remove_push_notify_(Action<chan_async_state> ntf, chan_notify_sign ntfSign)
        {
            bool effect = ntfSign._ntfNode.effect;
            bool success = ntfSign._success;
            ntfSign._success = false;
            if (effect)
            {
                _sendQueue.Remove(ntfSign._ntfNode).cancel_timer();
                ntfSign._ntfNode = default(option_node);
            }
            else if (success && !_msg._has)
            {
                if (!_sendQueue.RemoveFirst().Invoke(chan_async_state.async_ok) && _isTryPop)
                {
                    _waitQueue.RemoveFirst().Invoke(chan_async_state.async_fail);
                }
            }
            ntf(effect ? chan_async_state.async_ok : chan_async_state.async_fail);
        }

        internal override void clear_(Action ntf)
        {
            _msg.cancel();
            safe_callback(ref _sendQueue, chan_async_state.async_fail);
            ntf();
        }

        internal override void close_(Action ntf, bool isClear = false)
        {
            _closed = true;
            Action<chan_async_state, object> hasMsg = null;
            if (_msg._has)
            {
                hasMsg = _msg.cancel();
            }
            safe_callback(ref _sendQueue, ref _waitQueue, chan_async_state.async_closed);
            hasMsg?.Invoke(chan_async_state.async_closed, null);
            ntf();
        }

        internal override void cancel_(Action ntf, bool isClear = false)
        {
            Action<chan_async_state, object> hasMsg = null;
            if (_msg._has)
            {
                hasMsg = _msg.cancel();
            }
            safe_callback(ref _sendQueue, ref _waitQueue, chan_async_state.async_cancel);
            hasMsg?.Invoke(chan_async_state.async_cancel, null);
            ntf();
        }
    }
}
