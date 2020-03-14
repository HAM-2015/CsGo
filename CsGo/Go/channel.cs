using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Go
{
    public enum chan_state
    {
        undefined,
        ok,
        fail,
        csp_fail,
        cancel,
        closed,
        overtime
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
        public Action<chan_state> ntf;

        public void cancel_timer()
        {
            timer?.cancel();
        }

        public bool Invoke(chan_state state)
        {
            if (null != ntf)
            {
                ntf(state);
                return true;
            }
            return false;
        }
    }

    public class chan_notify_sign
    {
        internal priority_queue_node<notify_pck> _ntfNode;
        internal bool _selectOnce = false;
        internal bool _disable = false;
        internal bool _success = false;

        internal void set_node(priority_queue_node<notify_pck> node)
        {
            _ntfNode = node;
        }

        internal void reset_node()
        {
            _ntfNode = default(priority_queue_node<notify_pck>);
        }

        internal bool reset_success()
        {
            bool success = _success;
            _success = false;
            return success;
        }

        static internal void set_node(chan_notify_sign sign, priority_queue_node<notify_pck> node)
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
        public LinkedList<T> _queue0;
        public LinkedList<T> _queue1;

        private static priority_queue_node<T> AddFirst(int priority, ref LinkedList<T> queue, T value)
        {
            if (null == queue)
            {
                queue = new LinkedList<T>();
            }
            return new priority_queue_node<T>() { _priority = priority, _node = queue.AddFirst(value) };
        }

        private static priority_queue_node<T> AddLast(int priority, ref LinkedList<T> queue, T value)
        {
            if (null == queue)
            {
                queue = new LinkedList<T>();
            }
            return new priority_queue_node<T>() { _priority = priority, _node = queue.AddLast(value) };
        }

        public priority_queue_node<T> AddFirst(int priority, T value)
        {
            switch (priority)
            {
                case 0: return AddFirst(priority, ref _queue0, value);
                case 1: return AddFirst(priority, ref _queue1, value);
                default: return default(priority_queue_node<T>);
            }
        }

        public priority_queue_node<T> AddLast(int priority, T value)
        {
            switch (priority)
            {
                case 0: return AddLast(priority, ref _queue0, value);
                case 1: return AddLast(priority, ref _queue1, value);
                default: return default(priority_queue_node<T>);
            }
        }

        public priority_queue_node<T> AddFirst(T value)
        {
            return AddFirst(0, ref _queue0, value);
        }

        public priority_queue_node<T> AddLast(T value)
        {
            return AddLast(1, ref _queue1, value);
        }

        public bool Empty
        {
            get
            {
                return 0 == (null == _queue0 ? 0 : _queue0.Count) + (null == _queue1 ? 0 : _queue1.Count);
            }
        }

        public int Count
        {
            get
            {
                return (null == _queue0 ? 0 : _queue0.Count) + (null == _queue1 ? 0 : _queue1.Count);
            }
        }

        public int Count0
        {
            get
            {
                return null == _queue0 ? 0 : _queue0.Count;
            }
        }

        public int Count1
        {
            get
            {
                return null == _queue1 ? 0 : _queue1.Count;
            }
        }

        public priority_queue_node<T> First
        {
            get
            {
                if (null != _queue0 && 0 != _queue0.Count)
                {
                    return new priority_queue_node<T>() { _priority = 0, _node = _queue0.First };
                }
                else if (null != _queue1 && 0 != _queue1.Count)
                {
                    return new priority_queue_node<T>() { _priority = 1, _node = _queue1.First };
                }
                return new priority_queue_node<T>();
            }
        }

        public priority_queue_node<T> Last
        {
            get
            {
                if (null != _queue1 && 0 != _queue1.Count)
                {
                    return new priority_queue_node<T>() { _priority = 1, _node = _queue1.Last };
                }
                else if (null != _queue0 && 0 != _queue0.Count)
                {
                    return new priority_queue_node<T>() { _priority = 0, _node = _queue0.Last };
                }
                return new priority_queue_node<T>();
            }
        }

        public T RemoveFirst()
        {
            if (null != _queue0 && 0 != _queue0.Count)
            {
                T first = _queue0.First.Value;
                _queue0.RemoveFirst();
                return first;
            }
            else if (null != _queue1 && 0 != _queue1.Count)
            {
                T first = _queue1.First.Value;
                _queue1.RemoveFirst();
                return first;
            }
            return default(T);
        }

        public T RemoveLast()
        {
            if (null != _queue1 && 0 != _queue1.Count)
            {
                T last = _queue1.Last.Value;
                _queue1.RemoveLast();
                return last;
            }
            else if (null != _queue0 && 0 != _queue0.Count)
            {
                T last = _queue0.Last.Value;
                _queue0.RemoveLast();
                return last;
            }
            return default(T);
        }

        public T Remove(priority_queue_node<T> node)
        {
            if (null != node._node)
            {
                switch (node._priority)
                {
                    case 0: _queue0.Remove(node._node); break;
                    case 1: _queue1.Remove(node._node); break;
                }
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
        public bool enable;
        public chan_notify_sign ntfSign = new chan_notify_sign();
        public Action<chan_state> nextSelect;
        public bool disabled() { return ntfSign._disable; }
        public abstract void begin(generator host);
        public abstract Task<select_chan_state> invoke(Func<Task> stepOne = null);
        public abstract ValueTask<bool> errInvoke(chan_state state);
        public abstract Task end();
        public abstract bool is_read();
        public abstract chan_base channel();
    }

    public abstract class chan_base
    {
        private shared_strand _strand;
        protected bool _mustTick;
        protected bool _closed;
        protected chan_base(shared_strand strand) { _strand = strand; _mustTick = false; _closed = false; }
        public abstract chan_type type();
        protected abstract void async_clear_(Action ntf);
        protected abstract void async_close_(Action ntf, bool isClear = false);
        protected abstract void async_cancel_(Action ntf, bool isClear = false);
        protected abstract void async_append_recv_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign, int ms);
        protected abstract void async_remove_recv_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign);
        protected abstract void async_append_send_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign, int ms);
        protected abstract void async_remove_send_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign);
        protected virtual void async_append_recv_notify_(Action<chan_state> ntf, broadcast_token token, chan_notify_sign ntfSign, int ms) { async_append_recv_notify_(ntf, ntfSign, ms); }
        public void clear() { async_clear(nil_action.action); }
        public void close(bool isClear = false) { async_close(nil_action.action, isClear); }
        public void cancel(bool isClear = false) { async_cancel(nil_action.action, isClear); }
        public bool is_closed() { return _closed; }
        public shared_strand self_strand() { return _strand; }

        public void async_clear(Action ntf)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_clear_(ntf);
                else self_strand().add_next(() => async_clear_(ntf));
            else self_strand().post(() => async_clear_(ntf));
        }

        public void async_close(Action ntf, bool isClear = false)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_close_(ntf);
                else self_strand().add_next(() => async_close_(ntf));
            else self_strand().post(() => async_close_(ntf));
        }

        public void async_cancel(Action ntf, bool isClear = false)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_cancel_(ntf, isClear);
                else self_strand().add_next(() => async_cancel_(ntf, isClear));
            else self_strand().post(() => async_cancel_(ntf, isClear));
        }

        public void async_append_recv_notify(Action<chan_state> ntf, chan_notify_sign ntfSign, int ms = -1)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_append_recv_notify_(ntf, ntfSign, ms);
                else self_strand().add_next(() => async_append_recv_notify_(ntf, ntfSign, ms));
            else self_strand().post(() => async_append_recv_notify_(ntf, ntfSign, ms));
        }

        public void async_remove_recv_notify(Action<chan_state> ntf, chan_notify_sign ntfSign)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_remove_recv_notify_(ntf, ntfSign);
                else self_strand().add_next(() => async_remove_recv_notify_(ntf, ntfSign));
            else self_strand().post(() => async_remove_recv_notify_(ntf, ntfSign));
        }

        public void async_append_send_notify(Action<chan_state> ntf, chan_notify_sign ntfSign, int ms = -1)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_append_send_notify_(ntf, ntfSign, ms);
                else self_strand().add_next(() => async_append_send_notify_(ntf, ntfSign, ms));
            else self_strand().post(() => async_append_send_notify_(ntf, ntfSign, ms));
        }

        public void async_remove_send_notify(Action<chan_state> ntf, chan_notify_sign ntfSign)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_remove_send_notify_(ntf, ntfSign);
                else self_strand().add_next(() => async_remove_send_notify_(ntf, ntfSign));
            else self_strand().post(() => async_remove_send_notify_(ntf, ntfSign));
        }

        public void async_append_recv_notify(Action<chan_state> ntf, broadcast_token token, chan_notify_sign ntfSign, int ms = -1)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_append_recv_notify_(ntf, token, ntfSign, ms);
                else self_strand().add_next(() => async_append_recv_notify_(ntf, token, ntfSign, ms));
            else self_strand().post(() => async_append_recv_notify_(ntf, token, ntfSign, ms));
        }

        static private void queue_callback(ref priority_queue<notify_pck> queue, chan_state state)
        {
            LinkedList<notify_pck> queue0 = queue._queue0;
            LinkedList<notify_pck> queue1 = queue._queue1;
            if (null != queue0)
            {
                while (0 != queue0.Count)
                {
                    notify_pck ntf = queue0.First.Value;
                    queue0.RemoveFirst();
                    ntf.Invoke(state);
                }
            }
            if (null != queue1)
            {
                while (0 != queue1.Count)
                {
                    notify_pck ntf = queue1.First.Value;
                    queue1.RemoveFirst();
                    ntf.Invoke(state);
                }
            }
        }

        internal void safe_callback(ref priority_queue<notify_pck> queue, chan_state state)
        {
            _mustTick = true;
            queue_callback(ref queue, state);
            _mustTick = false;
        }

        internal void safe_callback(ref priority_queue<notify_pck> queue1, ref priority_queue<notify_pck> queue2, chan_state state)
        {
            _mustTick = true;
            queue_callback(ref queue1, state);
            queue_callback(ref queue2, state);
            _mustTick = false;
        }
    }

    public abstract class chan<T> : chan_base
    {
        internal class select_chan_reader : select_chan_base
        {
            public broadcast_token _token = broadcast_token._defToken;
            public chan<T> _chan;
            public Func<T, Task> _handler;
            public Func<chan_state, Task<bool>> _errHandler;
            public chan_lost_msg<T> _lostMsg;
            public int _chanTimeout = -1;
            async_result_wrap<chan_recv_wrap<T>> _tryRecvRes;
            generator _host;

            public override void begin(generator host)
            {
                ntfSign._disable = false;
                _host = host;
                _tryRecvRes = new async_result_wrap<chan_recv_wrap<T>> { value1 = chan_recv_wrap<T>.def };
                if (enable)
                {
                    _chan.async_append_recv_notify(nextSelect, ntfSign, _chanTimeout);
                }
            }

            public override async Task<select_chan_state> invoke(Func<Task> stepOne)
            {
                try
                {
                    _tryRecvRes.value1 = chan_recv_wrap<T>.def;
                    _chan.async_try_recv_and_append_notify(_host.unsafe_async_result(_tryRecvRes), nextSelect, _token, ntfSign, _chanTimeout);
                    await _host.async_wait();
                }
                catch (generator.stop_exception)
                {
                    _chan.async_remove_recv_notify(_host.unsafe_async_ignore<chan_state>(), ntfSign);
                    await _host.async_wait();
                    if (chan_state.ok == _tryRecvRes.value1.state)
                    {
                        _lostMsg?.set(_tryRecvRes.value1.msg);
                    }
                    throw;
                }
                select_chan_state chanState = new select_chan_state() { failed = false, nextRound = true };
                if (chan_state.ok == _tryRecvRes.value1.state)
                {
                    bool invoked = false;
                    try
                    {
                        if (null != stepOne)
                        {
                            await stepOne();
                        }
                        await _host.unlock_suspend_();
                        invoked = true;
                        await _handler(_tryRecvRes.value1.msg);
                    }
                    catch (generator.stop_exception)
                    {
                        if (!invoked)
                        {
                            _lostMsg?.set(_tryRecvRes.value1.msg);
                        }
                        throw;
                    }
                    finally
                    {
                        _host.lock_suspend_();
                    }
                }
                else if (chan_state.closed == _tryRecvRes.value1.state)
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

            private async Task<bool> errInvoke_(chan_state state)
            {
                try
                {
                    await _host.unlock_suspend_();
                    if (!await _errHandler(state) && chan_state.closed != state)
                    {
                        _chan.async_append_recv_notify(nextSelect, ntfSign, _chanTimeout);
                        return false;
                    }
                }
                finally
                {
                    _host.lock_suspend_();
                }
                return true;
            }

            public override ValueTask<bool> errInvoke(chan_state state)
            {
                if (null != _errHandler)
                {
                    return new ValueTask<bool>(errInvoke_(state));
                }
                return new ValueTask<bool>(true);
            }

            public override Task end()
            {
                ntfSign._disable = true;
                _chan.async_remove_recv_notify(_host.unsafe_async_ignore<chan_state>(), ntfSign);
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
            public Func<chan_state, Task<bool>> _errHandler;
            public chan_lost_msg<T> _lostMsg;
            public int _chanTimeout = -1;
            async_result_wrap<chan_send_wrap> _trySendRes;
            generator _host;

            public override void begin(generator host)
            {
                ntfSign._disable = false;
                _host = host;
                _trySendRes = new async_result_wrap<chan_send_wrap> { value1 = chan_send_wrap.def };
                if (enable)
                {
                    _chan.async_append_send_notify(nextSelect, ntfSign, _chanTimeout);
                }
            }

            public override async Task<select_chan_state> invoke(Func<Task> stepOne)
            {
                try
                {
                    _trySendRes.value1 = chan_send_wrap.def;
                    _chan.async_try_send_and_append_notify(_host.unsafe_async_result(_trySendRes), nextSelect, ntfSign, _msg.value1, _chanTimeout);
                    await _host.async_wait();
                }
                catch (generator.stop_exception)
                {
                    _chan.async_remove_send_notify(_host.unsafe_async_ignore<chan_state>(), ntfSign);
                    await _host.async_wait();
                    if (chan_state.ok != _trySendRes.value1.state)
                    {
                        _lostMsg?.set(_msg.value1);
                    }
                    throw;
                }
                select_chan_state chanState = new select_chan_state() { failed = false, nextRound = true };
                if (chan_state.ok == _trySendRes.value1.state)
                {
                    try
                    {
                        if (null != stepOne)
                        {
                            await stepOne();
                        }
                        await _host.unlock_suspend_();
                        await _handler();
                    }
                    finally
                    {
                        _host.lock_suspend_();
                    }
                }
                else if (chan_state.closed == _trySendRes.value1.state)
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

            private async Task<bool> errInvoke_(chan_state state)
            {
                try
                {
                    await _host.unlock_suspend_();
                    if (!await _errHandler(state) && chan_state.closed != state)
                    {
                        _chan.async_append_send_notify(nextSelect, ntfSign, _chanTimeout);
                        return false;
                    }
                }
                finally
                {
                    _host.lock_suspend_();
                }
                return true;
            }

            public override ValueTask<bool> errInvoke(chan_state state)
            {
                if (null != _errHandler)
                {
                    return new ValueTask<bool>(errInvoke_(state));
                }
                return new ValueTask<bool>(true);
            }

            public override Task end()
            {
                ntfSign._disable = true;
                _chan.async_remove_send_notify(_host.unsafe_async_ignore<chan_state>(), ntfSign);
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

        protected chan(shared_strand strand) : base(strand) { }
        protected abstract void async_send_(Action<chan_send_wrap> ntf, T msg, chan_notify_sign ntfSign);
        protected abstract void async_recv_(Action<chan_recv_wrap<T>> ntf, chan_notify_sign ntfSign);
        protected abstract void async_try_send_(Action<chan_send_wrap> ntf, T msg, chan_notify_sign ntfSign);
        protected abstract void async_try_recv_(Action<chan_recv_wrap<T>> ntf, chan_notify_sign ntfSign);
        protected abstract void async_timed_send_(int ms, Action<chan_send_wrap> ntf, T msg, chan_notify_sign ntfSign);
        protected abstract void async_timed_recv_(int ms, Action<chan_recv_wrap<T>> ntf, chan_notify_sign ntfSign);
        protected abstract void async_try_recv_and_append_notify_(Action<chan_recv_wrap<T>> cb, Action<chan_state> msgNtf, chan_notify_sign ntfSign, int ms);
        protected abstract void async_try_send_and_append_notify_(Action<chan_send_wrap> cb, Action<chan_state> msgNtf, chan_notify_sign ntfSign, T msg, int ms);

        public void async_send(Action<chan_send_wrap> ntf, T msg, chan_notify_sign ntfSign = null)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_send_(ntf, msg, ntfSign);
                else self_strand().add_next(() => async_send_(ntf, msg, ntfSign));
            else self_strand().post(() => async_send_(ntf, msg, ntfSign));
        }

        public void async_recv(Action<chan_recv_wrap<T>> ntf, chan_notify_sign ntfSign = null)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_recv_(ntf, ntfSign);
                else self_strand().add_next(() => async_recv_(ntf, ntfSign));
            else self_strand().post(() => async_recv_(ntf, ntfSign));
        }

        public void async_try_send(Action<chan_send_wrap> ntf, T msg, chan_notify_sign ntfSign = null)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_try_send_(ntf, msg, ntfSign);
                else self_strand().add_next(() => async_try_send_(ntf, msg, ntfSign));
            else self_strand().post(() => async_try_send_(ntf, msg, ntfSign));
        }

        public void async_try_recv(Action<chan_recv_wrap<T>> ntf, chan_notify_sign ntfSign = null)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_try_recv_(ntf, ntfSign);
                else self_strand().add_next(() => async_try_recv_(ntf, ntfSign));
            else self_strand().post(() => async_try_recv_(ntf, ntfSign));
        }

        public void async_timed_send(int ms, Action<chan_send_wrap> ntf, T msg, chan_notify_sign ntfSign = null)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_timed_send_(ms, ntf, msg, ntfSign);
                else self_strand().add_next(() => async_timed_send_(ms, ntf, msg, ntfSign));
            else self_strand().post(() => async_timed_send_(ms, ntf, msg, ntfSign));
        }

        public void async_timed_recv(int ms, Action<chan_recv_wrap<T>> ntf, chan_notify_sign ntfSign = null)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_timed_recv_(ms, ntf, ntfSign);
                else self_strand().add_next(() => async_timed_recv_(ms, ntf, ntfSign));
            else self_strand().post(() => async_timed_recv_(ms, ntf, ntfSign));
        }

        public void async_try_recv_and_append_notify(Action<chan_recv_wrap<T>> cb, Action<chan_state> msgNtf, chan_notify_sign ntfSign, int ms = -1)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_try_recv_and_append_notify_(cb, msgNtf, ntfSign, ms);
                else self_strand().add_next(() => async_try_recv_and_append_notify_(cb, msgNtf, ntfSign, ms));
            else self_strand().post(() => async_try_recv_and_append_notify_(cb, msgNtf, ntfSign, ms));
        }

        public void async_try_send_and_append_notify(Action<chan_send_wrap> cb, Action<chan_state> msgNtf, chan_notify_sign ntfSign, T msg, int ms = -1)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_try_send_and_append_notify_(cb, msgNtf, ntfSign, msg, ms);
                else self_strand().add_next(() => async_try_send_and_append_notify_(cb, msgNtf, ntfSign, msg, ms));
            else self_strand().post(() => async_try_send_and_append_notify_(cb, msgNtf, ntfSign, msg, ms));
        }

        public Task unsafe_send(async_result_wrap<chan_send_wrap> res, T msg)
        {
            return generator.unsafe_chan_send(res, this, msg);
        }

        public ValueTask<chan_send_wrap> send(T msg, chan_lost_msg<T> lostMsg = null)
        {
            return generator.chan_send(this, msg, lostMsg);
        }

        public Task unsafe_receive(async_result_wrap<chan_recv_wrap<T>> res)
        {
            return generator.unsafe_chan_receive(res, this);
        }

        public ValueTask<chan_recv_wrap<T>> receive(chan_lost_msg<T> lostMsg = null)
        {
            return generator.chan_receive(this, lostMsg);
        }

        public Task unsafe_try_send(async_result_wrap<chan_send_wrap> res, T msg)
        {
            return generator.unsafe_chan_try_send(res, this, msg);
        }

        public ValueTask<chan_send_wrap> try_send(T msg, chan_lost_msg<T> lostMsg = null)
        {
            return generator.chan_try_send(this, msg, lostMsg);
        }

        public Task unsafe_try_receive(async_result_wrap<chan_recv_wrap<T>> res)
        {
            return generator.unsafe_chan_try_receive(res, this);
        }

        public ValueTask<chan_recv_wrap<T>> try_receive(chan_lost_msg<T> lostMsg = null)
        {
            return generator.chan_try_receive(this, lostMsg);
        }

        public Task unsafe_timed_send(async_result_wrap<chan_send_wrap> res, int ms, T msg)
        {
            return generator.unsafe_chan_timed_send(res, this, ms, msg);
        }

        public ValueTask<chan_send_wrap> timed_send(int ms, T msg, chan_lost_msg<T> lostMsg = null)
        {
            return generator.chan_timed_send(this, ms, msg, lostMsg);
        }

        public Task unsafe_timed_receive(async_result_wrap<chan_recv_wrap<T>> res, int ms)
        {
            return generator.unsafe_chan_timed_receive(res, this, ms);
        }

        public ValueTask<chan_recv_wrap<T>> timed_receive(int ms, chan_lost_msg<T> lostMsg = null)
        {
            return generator.chan_timed_receive(this, ms, lostMsg);
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
            return make(shared_strand.default_strand(), len);
        }

        public void post(T msg)
        {
            async_send(nil_action<chan_send_wrap>.action, msg, null);
        }

        public void try_post(T msg)
        {
            async_try_send(nil_action<chan_send_wrap>.action, msg, null);
        }

        public void timed_post(int ms, T msg)
        {
            async_timed_send(ms, nil_action<chan_send_wrap>.action, msg, null);
        }

        public void discard()
        {
            async_recv(nil_action<chan_recv_wrap<T>>.action, null);
        }

        public void try_discard()
        {
            async_try_recv(nil_action<chan_recv_wrap<T>>.action, null);
        }

        public void timed_discard(int ms)
        {
            async_timed_recv(ms, nil_action<chan_recv_wrap<T>>.action, null);
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

        internal select_chan_base make_select_reader(Func<T, Task> handler, Func<chan_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return new select_chan_reader() { _chan = this, _handler = handler, _errHandler = errHandler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_reader(int ms, Func<T, Task> handler, chan_lost_msg<T> lostMsg)
        {
            return new select_chan_reader() { _chanTimeout = ms, _chan = this, _handler = handler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_reader(int ms, Func<T, Task> handler, Func<chan_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return new select_chan_reader() { _chanTimeout = ms, _chan = this, _handler = handler, _errHandler = errHandler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_writer(async_result_wrap<T> msg, Func<Task> handler, Func<chan_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return new select_chan_writer() { _chan = this, _msg = msg, _handler = handler, _errHandler = errHandler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_writer(T msg, Func<Task> handler, Func<chan_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return make_select_writer(new async_result_wrap<T> { value1 = msg }, handler, errHandler, lostMsg);
        }

        internal select_chan_base make_select_writer(int ms, async_result_wrap<T> msg, Func<Task> handler, Func<chan_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return new select_chan_writer() { _chanTimeout = ms, _chan = this, _msg = msg, _handler = handler, _errHandler = errHandler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_writer(int ms, T msg, Func<Task> handler, Func<chan_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return make_select_writer(ms, new async_result_wrap<T> { value1 = msg }, handler, errHandler, lostMsg);
        }

        protected virtual void async_recv_(Action<chan_recv_wrap<T>> ntf, broadcast_token token, chan_notify_sign ntfSign = null)
        {
            async_recv_(ntf, ntfSign);
        }

        protected virtual void async_try_recv_(Action<chan_recv_wrap<T>> ntf, broadcast_token token, chan_notify_sign ntfSign = null)
        {
            async_try_recv_(ntf, ntfSign);
        }

        protected virtual void async_timed_recv_(int ms, Action<chan_recv_wrap<T>> ntf, broadcast_token token, chan_notify_sign ntfSign = null)
        {
            async_timed_recv_(ms, ntf, ntfSign);
        }

        protected virtual void async_try_recv_and_append_notify_(Action<chan_recv_wrap<T>> cb, Action<chan_state> msgNtf, broadcast_token token, chan_notify_sign ntfSign, int ms = -1)
        {
            async_try_recv_and_append_notify_(cb, msgNtf, ntfSign, ms);
        }

        public void async_recv(Action<chan_recv_wrap<T>> ntf, broadcast_token token, chan_notify_sign ntfSign = null)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_recv_(ntf, token, ntfSign);
                else self_strand().add_next(() => async_recv_(ntf, token, ntfSign));
            else self_strand().post(() => async_recv_(ntf, token, ntfSign));
        }

        public void async_try_recv(Action<chan_recv_wrap<T>> ntf, broadcast_token token, chan_notify_sign ntfSign = null)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_try_recv_(ntf, token, ntfSign);
                else self_strand().add_next(() => async_try_recv_(ntf, token, ntfSign));
            else self_strand().post(() => async_try_recv_(ntf, token, ntfSign));
        }

        public void async_timed_recv(int ms, Action<chan_recv_wrap<T>> ntf, broadcast_token token, chan_notify_sign ntfSign = null)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_timed_recv_(ms, ntf, token, ntfSign);
                else self_strand().add_next(() => async_timed_recv_(ms, ntf, token, ntfSign));
            else self_strand().post(() => async_timed_recv_(ms, ntf, token, ntfSign));
        }

        public void async_try_recv_and_append_notify(Action<chan_recv_wrap<T>> cb, Action<chan_state> msgNtf, broadcast_token token, chan_notify_sign ntfSign, int ms = -1)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_try_recv_and_append_notify_(cb, msgNtf, token, ntfSign, ms);
                else self_strand().add_next(() => async_try_recv_and_append_notify_(cb, msgNtf, token, ntfSign, ms));
            else self_strand().post(() => async_try_recv_and_append_notify_(cb, msgNtf, token, ntfSign, ms));
        }

        public Task unsafe_receive(async_result_wrap<chan_recv_wrap<T>> res, broadcast_token token)
        {
            return generator.unsafe_chan_receive(res, this, token);
        }

        public ValueTask<chan_recv_wrap<T>> receive(broadcast_token token, chan_lost_msg<T> lostMsg = null)
        {
            return generator.chan_receive(this, token, lostMsg);
        }

        public Task unsafe_try_receive(async_result_wrap<chan_recv_wrap<T>> res, broadcast_token token)
        {
            return generator.unsafe_chan_try_receive(res, this, token);
        }

        public ValueTask<chan_recv_wrap<T>> try_receive(broadcast_token token, chan_lost_msg<T> lostMsg = null)
        {
            return generator.chan_try_receive(this, token, lostMsg);
        }

        public Task unsafe_timed_receive(async_result_wrap<chan_recv_wrap<T>> res, int ms, broadcast_token token)
        {
            return generator.unsafe_chan_timed_receive(res, this, ms, token);
        }

        public ValueTask<chan_recv_wrap<T>> timed_receive(int ms, broadcast_token token, chan_lost_msg<T> lostMsg = null)
        {
            return generator.chan_timed_receive(this, ms, token, lostMsg);
        }

        internal virtual select_chan_base make_select_reader(Func<T, Task> handler, broadcast_token token, chan_lost_msg<T> lostMsg)
        {
            return make_select_reader(handler, lostMsg);
        }

        internal virtual select_chan_base make_select_reader(Func<T, Task> handler, broadcast_token token, Func<chan_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return make_select_reader(handler, errHandler, lostMsg);
        }

        internal virtual select_chan_base make_select_reader(int ms, Func<T, Task> handler, broadcast_token token, chan_lost_msg<T> lostMsg)
        {
            return make_select_reader(ms, handler, lostMsg);
        }

        internal virtual select_chan_base make_select_reader(int ms, Func<T, Task> handler, broadcast_token token, Func<chan_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return make_select_reader(ms, handler, errHandler, lostMsg);
        }
    }

    abstract class msg_queue<T>
    {
        public abstract void AddLast(T msg);
        public abstract T RemoveFirst();
        public abstract int Count { get; }
        public abstract bool Empty { get; }
        public abstract void Clear();
    }

    class no_void_msg_queue<T> : msg_queue<T>
    {
        MsgQueue<T> _msgBuff;

        public no_void_msg_queue()
        {
            _msgBuff = new MsgQueue<T>();
        }

        public override void AddLast(T msg)
        {
            _msgBuff.AddLast(msg);
        }

        public override T RemoveFirst()
        {
            T first = _msgBuff.First.Value;
            _msgBuff.RemoveFirst();
            return first;
        }

        public override int Count
        {
            get
            {
                return _msgBuff.Count;
            }
        }

        public override bool Empty
        {
            get
            {
                return 0 == _msgBuff.Count;
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

        public override T RemoveFirst()
        {
            _count--;
            return default(T);
        }

        public override int Count
        {
            get
            {
                return _count;
            }
        }

        public override bool Empty
        {
            get
            {
                return 0 == _count;
            }
        }

        public override void Clear()
        {
            _count = 0;
        }
    }

    public class unlimit_chan<T> : chan<T>
    {
        msg_queue<T> _msgQueue;
        priority_queue<notify_pck> _recvQueue;

        public unlimit_chan(shared_strand strand) : base(strand)
        {
            _msgQueue = default(T) is void_type ? (msg_queue<T>)new void_msg_queue<T>() : new no_void_msg_queue<T>();
            _recvQueue = new priority_queue<notify_pck>();
        }

        public unlimit_chan() : this(shared_strand.default_strand()) { }

        public override chan_type type()
        {
            return chan_type.unlimit;
        }

        protected override void async_send_(Action<chan_send_wrap> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_closed)
            {
                ntf(new chan_send_wrap { state = chan_state.closed });
                return;
            }
            _msgQueue.AddLast(msg);
            _recvQueue.RemoveFirst().Invoke(chan_state.ok);
            ntf(new chan_send_wrap { state = chan_state.ok });
        }

        protected override void async_recv_(Action<chan_recv_wrap<T>> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (!_msgQueue.Empty)
            {
                T msg = _msgQueue.RemoveFirst();
                ntf(new chan_recv_wrap<T> { state = chan_state.ok, msg = msg });
            }
            else if (_closed)
            {
                ntf(new chan_recv_wrap<T> { state = chan_state.closed });
            }
            else
            {
                chan_notify_sign.set_node(ntfSign, _recvQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        if (chan_state.ok == state)
                        {
                            async_recv_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(new chan_recv_wrap<T> { state = state });
                        }
                    }
                }));
            }
        }

        protected override void async_try_send_(Action<chan_send_wrap> ntf, T msg, chan_notify_sign ntfSign)
        {
            async_send_(ntf, msg, ntfSign);
        }

        protected override void async_try_recv_(Action<chan_recv_wrap<T>> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (!_msgQueue.Empty)
            {
                T msg = _msgQueue.RemoveFirst();
                ntf(new chan_recv_wrap<T> { state = chan_state.ok, msg = msg });
            }
            else if (_closed)
            {
                ntf(new chan_recv_wrap<T> { state = chan_state.closed });
            }
            else
            {
                ntf(new chan_recv_wrap<T> { state = chan_state.fail });
            }
        }

        protected override void async_timed_send_(int ms, Action<chan_send_wrap> ntf, T msg, chan_notify_sign ntfSign)
        {
            async_send_(ntf, msg, ntfSign);
        }

        protected override void async_timed_recv_(int ms, Action<chan_recv_wrap<T>> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (!_msgQueue.Empty)
            {
                T msg = _msgQueue.RemoveFirst();
                ntf(new chan_recv_wrap<T> { state = chan_state.ok, msg = msg });
            }
            else if (_closed)
            {
                ntf(new chan_recv_wrap<T> { state = chan_state.closed });
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(self_strand());
                priority_queue_node<notify_pck> node = _recvQueue.AddLast(0, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        timer.cancel();
                        if (chan_state.ok == state)
                        {
                            async_recv_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(new chan_recv_wrap<T> { state = state });
                        }
                    }
                });
                ntfSign?.set_node(node);
                timer.timeout(ms, delegate ()
                {
                    _recvQueue.Remove(node).Invoke(chan_state.overtime);
                });
            }
            else
            {
                chan_notify_sign.set_node(ntfSign, _recvQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        if (chan_state.ok == state)
                        {
                            async_recv_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(new chan_recv_wrap<T> { state = state });
                        }
                    }
                }));
            }
        }

        protected override void async_append_recv_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign, int ms)
        {
            if (!_msgQueue.Empty)
            {
                ntfSign._success = true;
                ntf(chan_state.ok);
            }
            else if (_closed)
            {
                ntf(chan_state.closed);
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(self_strand());
                ntfSign.set_node(_recvQueue.AddLast(1, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_state state)
                    {
                        timer.cancel();
                        ntfSign.reset_node();
                        ntfSign._success = chan_state.ok == state;
                        ntf(state);
                    }
                }));
                timer.timeout(ms, delegate ()
                {
                    _recvQueue.Remove(ntfSign._ntfNode).Invoke(chan_state.overtime);
                });
            }
            else
            {
                ntfSign.set_node(_recvQueue.AddLast(1, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign.reset_node();
                        ntfSign._success = chan_state.ok == state;
                        ntf(state);
                    }
                }));
            }
        }

        protected override void async_try_recv_and_append_notify_(Action<chan_recv_wrap<T>> cb, Action<chan_state> msgNtf, chan_notify_sign ntfSign, int ms)
        {
            ntfSign.reset_success();
            if (!_msgQueue.Empty)
            {
                T msg = _msgQueue.RemoveFirst();
                if (!ntfSign._selectOnce)
                {
                    async_append_recv_notify_(msgNtf, ntfSign, ms);
                }
                cb(new chan_recv_wrap<T> { state = chan_state.ok, msg = msg });
            }
            else if (_closed)
            {
                msgNtf(chan_state.closed);
                cb(new chan_recv_wrap<T> { state = chan_state.closed });
            }
            else
            {
                async_append_recv_notify_(msgNtf, ntfSign, ms);
                cb(new chan_recv_wrap<T> { state = chan_state.fail });
            }
        }

        protected override void async_remove_recv_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign)
        {
            bool effect = ntfSign._ntfNode.effect;
            bool success = ntfSign.reset_success();
            if (effect)
            {
                _recvQueue.Remove(ntfSign._ntfNode).cancel_timer();
                ntfSign.reset_node();
            }
            else if (success && !_msgQueue.Empty)
            {
                _recvQueue.RemoveFirst().Invoke(chan_state.ok);
            }
            ntf(effect ? chan_state.ok : chan_state.fail);
        }

        protected override void async_append_send_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign, int ms)
        {
            if (_closed)
            {
                ntf(chan_state.closed);
                return;
            }
            ntfSign._success = true;
            ntf(chan_state.ok);
        }

        protected override void async_try_send_and_append_notify_(Action<chan_send_wrap> cb, Action<chan_state> msgNtf, chan_notify_sign ntfSign, T msg, int ms)
        {
            ntfSign.reset_success();
            if (_closed)
            {
                msgNtf(chan_state.closed);
                cb(new chan_send_wrap { state = chan_state.closed });
                return;
            }
            _msgQueue.AddLast(msg);
            _recvQueue.RemoveFirst();
            if (!ntfSign._selectOnce)
            {
                async_append_send_notify_(msgNtf, ntfSign, ms);
            }
            cb(new chan_send_wrap { state = chan_state.ok });
        }

        protected override void async_remove_send_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign)
        {
            ntf(chan_state.fail);
        }

        protected override void async_clear_(Action ntf)
        {
            _msgQueue.Clear();
            ntf();
        }

        protected override void async_close_(Action ntf, bool isClear = false)
        {
            _closed = true;
            if (isClear)
            {
                _msgQueue.Clear();
            }
            safe_callback(ref _recvQueue, chan_state.closed);
            ntf();
        }

        protected override void async_cancel_(Action ntf, bool isClear = false)
        {
            if (isClear)
            {
                _msgQueue.Clear();
            }
            safe_callback(ref _recvQueue, chan_state.cancel);
            ntf();
        }
    }

    public class limit_chan<T> : chan<T>
    {
        msg_queue<T> _msgQueue;
        priority_queue<notify_pck> _sendQueue;
        priority_queue<notify_pck> _recvQueue;
        readonly int _maxCount;

        public limit_chan(shared_strand strand, int len) : base(strand)
        {
            Debug.Assert(len > 0, string.Format("limit_chan<{0}>长度必须大于0!", typeof(T).Name));
            _msgQueue = default(T) is void_type ? (msg_queue<T>)new void_msg_queue<T>() : new no_void_msg_queue<T>();
            _sendQueue = new priority_queue<notify_pck>();
            _recvQueue = new priority_queue<notify_pck>();
            _maxCount = len;
        }

        public limit_chan(int len) : this(shared_strand.default_strand(), len) { }

        public override chan_type type()
        {
            return chan_type.limit;
        }

        public void force_post(T msg)
        {
            async_force_send(nil_action<chan_state, bool, T>.action, msg);
        }

        public Action<T> wrap_force()
        {
            return force_post;
        }

        public Action wrap_force_default()
        {
            return () => force_post(default(T));
        }

        protected override void async_send_(Action<chan_send_wrap> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_closed)
            {
                ntf(new chan_send_wrap { state = chan_state.closed });
                return;
            }
            if (_msgQueue.Count == _maxCount)
            {
                chan_notify_sign.set_node(ntfSign, _sendQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        if (chan_state.ok == state)
                        {
                            async_send_(ntf, msg, ntfSign);
                        }
                        else
                        {
                            ntf(new chan_send_wrap { state = state });
                        }
                    }
                }));
            }
            else
            {
                _msgQueue.AddLast(msg);
                _recvQueue.RemoveFirst().Invoke(chan_state.ok);
                ntf(new chan_send_wrap { state = chan_state.ok });
            }
        }

        private void async_force_send_(Action<chan_state, bool, T> ntf, T msg)
        {
            if (_closed)
            {
                ntf(chan_state.closed, false, default(T));
                return;
            }
            bool hasOut = false;
            T outMsg = default(T);
            if (_msgQueue.Count == _maxCount)
            {
                hasOut = true;
                outMsg = _msgQueue.RemoveFirst();
            }
            _msgQueue.AddLast(msg);
            _recvQueue.RemoveFirst().Invoke(chan_state.ok);
            if (hasOut)
            {
                ntf(chan_state.ok, true, outMsg);
            }
            else
            {
                ntf(chan_state.ok, false, default(T));
            }
        }

        public void async_force_send(Action<chan_state, bool, T> ntf, T msg)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_force_send_(ntf, msg);
                else self_strand().add_next(() => async_force_send_(ntf, msg));
            else self_strand().post(() => async_force_send_(ntf, msg));
        }

        public Task unsafe_force_send(async_result_wrap<chan_send_wrap> res, T msg, chan_lost_msg<T> outMsg = null)
        {
            return generator.unsafe_chan_force_send(res, this, msg, outMsg);
        }

        public ValueTask<chan_send_wrap> force_send(T msg, chan_lost_msg<T> outMsg = null, chan_lost_msg<T> lostMsg = null)
        {
            return generator.chan_force_send(this, msg, outMsg, lostMsg);
        }

        protected override void async_recv_(Action<chan_recv_wrap<T>> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (!_msgQueue.Empty)
            {
                T msg = _msgQueue.RemoveFirst();
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
                ntf(new chan_recv_wrap<T> { state = chan_state.ok, msg = msg });
            }
            else if (_closed)
            {
                ntf(new chan_recv_wrap<T> { state = chan_state.closed });
            }
            else
            {
                chan_notify_sign.set_node(ntfSign, _recvQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        if (chan_state.ok == state)
                        {
                            async_recv_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(new chan_recv_wrap<T> { state = state });
                        }
                    }
                }));
            }
        }

        protected override void async_try_send_(Action<chan_send_wrap> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_closed)
            {
                ntf(new chan_send_wrap { state = chan_state.closed });
                return;
            }
            if (_msgQueue.Count == _maxCount)
            {
                ntf(new chan_send_wrap { state = chan_state.fail });
            }
            else
            {
                _msgQueue.AddLast(msg);
                _recvQueue.RemoveFirst().Invoke(chan_state.ok);
                ntf(new chan_send_wrap { state = chan_state.ok });
            }
        }

        protected override void async_try_recv_(Action<chan_recv_wrap<T>> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (!_msgQueue.Empty)
            {
                T msg = _msgQueue.RemoveFirst();
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
                ntf(new chan_recv_wrap<T> { state = chan_state.ok, msg = msg });
            }
            else if (_closed)
            {
                ntf(new chan_recv_wrap<T> { state = chan_state.closed });
            }
            else
            {
                ntf(new chan_recv_wrap<T> { state = chan_state.fail });
            }
        }

        protected override void async_timed_send_(int ms, Action<chan_send_wrap> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_msgQueue.Count == _maxCount)
            {
                if (ms >= 0)
                {
                    async_timer timer = new async_timer(self_strand());
                    priority_queue_node<notify_pck> node = _sendQueue.AddLast(0, new notify_pck()
                    {
                        timer = timer,
                        ntf = delegate (chan_state state)
                        {
                            ntfSign?.reset_node();
                            timer.cancel();
                            if (chan_state.ok == state)
                            {
                                async_send_(ntf, msg, ntfSign);
                            }
                            else
                            {
                                ntf(new chan_send_wrap { state = state });
                            }
                        }
                    });
                    ntfSign?.set_node(node);
                    timer.timeout(ms, delegate ()
                    {
                        _sendQueue.Remove(node).Invoke(chan_state.overtime);
                    });
                }
                else
                {
                    chan_notify_sign.set_node(ntfSign, _sendQueue.AddLast(0, new notify_pck()
                    {
                        ntf = delegate (chan_state state)
                        {
                            ntfSign?.reset_node();
                            if (chan_state.ok == state)
                            {
                                async_send_(ntf, msg, ntfSign);
                            }
                            else
                            {
                                ntf(new chan_send_wrap { state = state });
                            }
                        }
                    }));
                }
            }
            else
            {
                _msgQueue.AddLast(msg);
                _recvQueue.RemoveFirst().Invoke(chan_state.ok);
                ntf(new chan_send_wrap { state = chan_state.ok });
            }
        }

        protected override void async_timed_recv_(int ms, Action<chan_recv_wrap<T>> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (!_msgQueue.Empty)
            {
                T msg = _msgQueue.RemoveFirst();
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
                ntf(new chan_recv_wrap<T> { state = chan_state.ok, msg = msg });
            }
            else if (_closed)
            {
                ntf(new chan_recv_wrap<T> { state = chan_state.closed });
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(self_strand());
                priority_queue_node<notify_pck> node = _recvQueue.AddLast(0, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        timer.cancel();
                        if (chan_state.ok == state)
                        {
                            async_recv_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(new chan_recv_wrap<T> { state = state });
                        }
                    }
                });
                ntfSign?.set_node(node);
                timer.timeout(ms, delegate ()
                {
                    _recvQueue.Remove(node).Invoke(chan_state.overtime);
                });
            }
            else
            {
                chan_notify_sign.set_node(ntfSign, _recvQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        if (chan_state.ok == state)
                        {
                            async_recv_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(new chan_recv_wrap<T> { state = state });
                        }
                    }
                }));
            }
        }

        protected override void async_append_recv_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign, int ms)
        {
            if (!_msgQueue.Empty)
            {
                ntfSign._success = true;
                ntf(chan_state.ok);
            }
            else if (_closed)
            {
                ntf(chan_state.closed);
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(self_strand());
                ntfSign.set_node(_recvQueue.AddLast(1, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_state state)
                    {
                        timer.cancel();
                        ntfSign.reset_node();
                        ntfSign._success = chan_state.ok == state;
                        ntf(state);
                    }
                }));
                timer.timeout(ms, delegate ()
                {
                    _recvQueue.Remove(ntfSign._ntfNode).Invoke(chan_state.overtime);
                });
            }
            else
            {
                ntfSign.set_node(_recvQueue.AddLast(1, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign.reset_node();
                        ntfSign._success = chan_state.ok == state;
                        ntf(state);
                    }
                }));
            }
        }

        protected override void async_try_recv_and_append_notify_(Action<chan_recv_wrap<T>> cb, Action<chan_state> msgNtf, chan_notify_sign ntfSign, int ms)
        {
            ntfSign.reset_success();
            if (!_msgQueue.Empty)
            {
                T msg = _msgQueue.RemoveFirst();
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
                if (!ntfSign._selectOnce)
                {
                    async_append_recv_notify_(msgNtf, ntfSign, ms);
                }
                cb(new chan_recv_wrap<T> { state = chan_state.ok, msg = msg });
            }
            else if (_closed)
            {
                msgNtf(chan_state.closed);
                cb(new chan_recv_wrap<T> { state = chan_state.closed });
            }
            else
            {
                async_append_recv_notify_(msgNtf, ntfSign, ms);
                cb(new chan_recv_wrap<T> { state = chan_state.fail });
            }
        }

        protected override void async_remove_recv_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign)
        {
            bool effect = ntfSign._ntfNode.effect;
            bool success = ntfSign.reset_success();
            if (effect)
            {
                _recvQueue.Remove(ntfSign._ntfNode).cancel_timer();
                ntfSign.reset_node();
            }
            else if (success && !_msgQueue.Empty)
            {
                _recvQueue.RemoveFirst().Invoke(chan_state.ok);
            }
            ntf(effect ? chan_state.ok : chan_state.fail);
        }

        protected override void async_append_send_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign, int ms)
        {
            if (_closed)
            {
                ntf(chan_state.closed);
                return;
            }
            if (_msgQueue.Count != _maxCount)
            {
                ntfSign._success = true;
                ntf(chan_state.ok);
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(self_strand());
                ntfSign.set_node(_sendQueue.AddLast(1, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_state state)
                    {
                        timer.cancel();
                        ntfSign.reset_node();
                        ntfSign._success = chan_state.ok == state;
                        ntf(state);
                    }
                }));
                timer.timeout(ms, delegate ()
                {
                    _sendQueue.Remove(ntfSign._ntfNode).Invoke(chan_state.overtime);
                });
            }
            else
            {
                ntfSign.set_node(_sendQueue.AddLast(1, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign.reset_node();
                        ntfSign._success = chan_state.ok == state;
                        ntf(state);
                    }
                }));
            }
        }

        protected override void async_try_send_and_append_notify_(Action<chan_send_wrap> cb, Action<chan_state> msgNtf, chan_notify_sign ntfSign, T msg, int ms)
        {
            ntfSign.reset_success();
            if (_closed)
            {
                msgNtf(chan_state.closed);
                cb(new chan_send_wrap { state = chan_state.closed });
                return;
            }
            if (_msgQueue.Count != _maxCount)
            {
                _msgQueue.AddLast(msg);
                _recvQueue.RemoveFirst().Invoke(chan_state.ok);
                if (!ntfSign._selectOnce)
                {
                    async_append_send_notify_(msgNtf, ntfSign, ms);
                }
                cb(new chan_send_wrap { state = chan_state.ok });
            }
            else
            {
                async_append_send_notify_(msgNtf, ntfSign, ms);
                cb(new chan_send_wrap { state = chan_state.fail });
            }
        }

        protected override void async_remove_send_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign)
        {
            bool effect = ntfSign._ntfNode.effect;
            bool success = ntfSign.reset_success();
            if (effect)
            {
                _sendQueue.Remove(ntfSign._ntfNode).cancel_timer();
                ntfSign.reset_node();
            }
            else if (success && _msgQueue.Count != _maxCount)
            {
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
            }
            ntf(effect ? chan_state.ok : chan_state.fail);
        }

        protected override void async_clear_(Action ntf)
        {
            _msgQueue.Clear();
            safe_callback(ref _sendQueue, chan_state.fail);
            ntf();
        }

        protected override void async_close_(Action ntf, bool isClear = false)
        {
            _closed = true;
            if (isClear)
            {
                _msgQueue.Clear();
            }
            safe_callback(ref _recvQueue, ref _sendQueue, chan_state.closed);
            ntf();
        }

        protected override void async_cancel_(Action ntf, bool isClear = false)
        {
            if (isClear)
            {
                _msgQueue.Clear();
            }
            safe_callback(ref _recvQueue, ref _sendQueue, chan_state.cancel);
            ntf();
        }
    }

    public class nil_chan<T> : chan<T>
    {
        priority_queue<notify_pck> _sendQueue;
        priority_queue<notify_pck> _recvQueue;
        T _tempMsg;
        bool _isTrySend;
        bool _isTryRecv;
        bool _has;

        public nil_chan(shared_strand strand) : base(strand)
        {
            _sendQueue = new priority_queue<notify_pck>();
            _recvQueue = new priority_queue<notify_pck>();
            _isTrySend = false;
            _isTryRecv = false;
            _has = false;
        }

        public nil_chan() : this(shared_strand.default_strand()) { }

        public override chan_type type()
        {
            return chan_type.nil;
        }

        protected override void async_send_(Action<chan_send_wrap> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_closed)
            {
                ntf(new chan_send_wrap { state = chan_state.closed });
                return;
            }
            if (_has || _recvQueue.Empty)
            {
                chan_notify_sign.set_node(ntfSign, _sendQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        if (chan_state.ok == state)
                        {
                            async_send_(ntf, msg, ntfSign);
                        }
                        else
                        {
                            ntf(new chan_send_wrap { state = state });
                        }
                    }
                }));
            }
            else
            {
                _tempMsg = msg;
                _has = true;
                chan_notify_sign.set_node(ntfSign, _sendQueue.AddFirst(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        ntf(new chan_send_wrap { state = state });
                    }
                }));
                _recvQueue.RemoveFirst().Invoke(chan_state.ok);
            }
        }

        protected override void async_recv_(Action<chan_recv_wrap<T>> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_has)
            {
                T msg = _tempMsg;
                _has = false;
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
                ntf(new chan_recv_wrap<T> { state = chan_state.ok, msg = msg });
            }
            else if (_closed)
            {
                ntf(new chan_recv_wrap<T> { state = chan_state.closed });
            }
            else
            {
                chan_notify_sign.set_node(ntfSign, _recvQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        if (chan_state.ok == state)
                        {
                            async_recv_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(new chan_recv_wrap<T> { state = state });
                        }
                    }
                }));
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
            }
        }

        protected override void async_try_send_(Action<chan_send_wrap> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_closed)
            {
                ntf(new chan_send_wrap { state = chan_state.closed });
                return;
            }
            if (_has || _recvQueue.Empty)
            {
                ntf(new chan_send_wrap { state = chan_state.fail });
            }
            else
            {
                _tempMsg = msg;
                _has = true;
                _isTrySend = true;
                chan_notify_sign.set_node(ntfSign, _sendQueue.AddFirst(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        _isTrySend = false;
                        ntfSign?.reset_node();
                        ntf(new chan_send_wrap { state = state });
                    }
                }));
                _recvQueue.RemoveFirst().Invoke(chan_state.ok);
            }
        }

        protected override void async_try_recv_(Action<chan_recv_wrap<T>> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_has)
            {
                T msg = _tempMsg;
                _has = false;
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
                ntf(new chan_recv_wrap<T> { state = chan_state.ok, msg = msg });
            }
            else if (_closed)
            {
                ntf(new chan_recv_wrap<T> { state = chan_state.closed });
            }
            else if (!_sendQueue.Empty && _recvQueue.Empty)
            {
                _isTryRecv = true;
                chan_notify_sign.set_node(ntfSign, _recvQueue.AddFirst(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        _isTryRecv = false;
                        ntfSign?.reset_node();
                        if (chan_state.ok == state)
                        {
                            async_recv_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(new chan_recv_wrap<T> { state = state });
                        }
                    }
                }));
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
            }
            else
            {
                ntf(new chan_recv_wrap<T> { state = chan_state.fail });
            }
        }

        protected override void async_timed_send_(int ms, Action<chan_send_wrap> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_closed)
            {
                ntf(new chan_send_wrap { state = chan_state.closed });
                return;
            }
            if (_has || _recvQueue.Empty)
            {
                if (ms >= 0)
                {
                    async_timer timer = new async_timer(self_strand());
                    priority_queue_node<notify_pck> node = _sendQueue.AddLast(0, new notify_pck()
                    {
                        timer = timer,
                        ntf = delegate (chan_state state)
                        {
                            ntfSign?.reset_node();
                            timer.cancel();
                            if (chan_state.ok == state)
                            {
                                async_send_(ntf, msg, ntfSign);
                            }
                            else
                            {
                                ntf(new chan_send_wrap { state = state });
                            }
                        }
                    });
                    ntfSign?.set_node(node);
                    timer.timeout(ms, delegate ()
                    {
                        _sendQueue.Remove(node).Invoke(chan_state.overtime);
                    });
                }
                else
                {
                    chan_notify_sign.set_node(ntfSign, _sendQueue.AddLast(0, new notify_pck()
                    {
                        ntf = delegate (chan_state state)
                        {
                            ntfSign?.reset_node();
                            if (chan_state.ok == state)
                            {
                                async_send_(ntf, msg, ntfSign);
                            }
                            else
                            {
                                ntf(new chan_send_wrap { state = state });
                            }
                        }
                    }));
                }
            }
            else if (ms >= 0)
            {
                _tempMsg = msg;
                _has = true;
                async_timer timer = new async_timer(self_strand());
                priority_queue_node<notify_pck> node = _sendQueue.AddFirst(0, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        timer.cancel();
                        ntf(new chan_send_wrap { state = state });
                    }
                });
                ntfSign?.set_node(node);
                timer.timeout(ms, delegate ()
                {
                    _has = false;
                    _sendQueue.Remove(node).Invoke(chan_state.overtime);
                });
                _recvQueue.RemoveFirst().Invoke(chan_state.ok);
            }
            else
            {
                _tempMsg = msg;
                _has = true;
                chan_notify_sign.set_node(ntfSign, _sendQueue.AddFirst(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        ntf(new chan_send_wrap { state = state });
                    }
                }));
                _recvQueue.RemoveFirst().Invoke(chan_state.ok);
            }
        }

        protected override void async_timed_recv_(int ms, Action<chan_recv_wrap<T>> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_has)
            {
                T msg = _tempMsg;
                _has = false;
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
                ntf(new chan_recv_wrap<T> { state = chan_state.ok, msg = msg });
            }
            else if (_closed)
            {
                ntf(new chan_recv_wrap<T> { state = chan_state.closed });
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(self_strand());
                priority_queue_node<notify_pck> node = _recvQueue.AddLast(0, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        timer.cancel();
                        if (chan_state.ok == state)
                        {
                            async_recv_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(new chan_recv_wrap<T> { state = state });
                        }
                    }
                });
                ntfSign?.set_node(node);
                timer.timeout(ms, delegate ()
                {
                    _recvQueue.Remove(node).Invoke(chan_state.overtime);
                });
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
            }
            else
            {
                chan_notify_sign.set_node(ntfSign, _recvQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        if (chan_state.ok == state)
                        {
                            async_recv_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(new chan_recv_wrap<T> { state = state });
                        }
                    }
                }));
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
            }
        }

        protected override void async_append_recv_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign, int ms)
        {
            if (_has)
            {
                ntfSign._success = true;
                ntf(chan_state.ok);
            }
            else if (_closed)
            {
                ntf(chan_state.closed);
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(self_strand());
                ntfSign.set_node(_recvQueue.AddLast(1, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_state state)
                    {
                        timer.cancel();
                        ntfSign.reset_node();
                        ntfSign._success = chan_state.ok == state;
                        ntf(state);
                    }
                }));
                timer.timeout(ms, delegate ()
                {
                    _recvQueue.Remove(ntfSign._ntfNode).Invoke(chan_state.overtime);
                });
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
            }
            else
            {
                ntfSign.set_node(_recvQueue.AddLast(1, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign.reset_node();
                        ntfSign._success = chan_state.ok == state;
                        ntf(state);
                    }
                }));
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
            }
        }

        protected override void async_try_recv_and_append_notify_(Action<chan_recv_wrap<T>> cb, Action<chan_state> msgNtf, chan_notify_sign ntfSign, int ms)
        {
            ntfSign.reset_success();
            if (_has)
            {
                T msg = _tempMsg;
                _has = false;
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
                if (!ntfSign._selectOnce)
                {
                    async_append_recv_notify_(msgNtf, ntfSign, ms);
                }
                cb(new chan_recv_wrap<T> { state = chan_state.ok, msg = msg });
            }
            else if (_closed)
            {
                msgNtf(chan_state.closed);
                cb(new chan_recv_wrap<T> { state = chan_state.closed });
            }
            else if (!_sendQueue.Empty && _recvQueue.Empty)
            {
                _isTryRecv = true;
                chan_notify_sign.set_node(ntfSign, _recvQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        _isTryRecv = false;
                        ntfSign?.reset_node();
                        if (chan_state.ok == state)
                        {
                            async_recv_(cb, ntfSign);
                        }
                        else
                        {
                            cb(new chan_recv_wrap<T> { state = state });
                        }
                    }
                }));
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
            }
            else
            {
                async_append_recv_notify_(msgNtf, ntfSign, ms);
                cb(new chan_recv_wrap<T> { state = chan_state.fail });
            }
        }

        protected override void async_remove_recv_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign)
        {
            bool effect = ntfSign._ntfNode.effect;
            bool success = ntfSign.reset_success();
            if (effect)
            {
                _isTryRecv &= _recvQueue.First._node != ntfSign._ntfNode._node;
                _recvQueue.Remove(ntfSign._ntfNode).cancel_timer();
                ntfSign.reset_node();
            }
            else if (success && _has)
            {
                if (!_recvQueue.RemoveFirst().Invoke(chan_state.ok) && _isTrySend)
                {
                    _has = !_sendQueue.RemoveFirst().Invoke(chan_state.fail);
                }
            }
            ntf(effect ? chan_state.ok : chan_state.fail);
        }

        protected override void async_append_send_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign, int ms)
        {
            if (_closed)
            {
                ntf(chan_state.closed);
                return;
            }
            if (!_recvQueue.Empty)
            {
                ntfSign._success = true;
                ntf(chan_state.ok);
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(self_strand());
                ntfSign.set_node(_sendQueue.AddLast(1, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_state state)
                    {
                        timer.cancel();
                        ntfSign.reset_node();
                        ntfSign._success = chan_state.ok == state;
                        ntf(state);
                    }
                }));
                timer.timeout(ms, delegate ()
                {
                    _sendQueue.Remove(ntfSign._ntfNode).Invoke(chan_state.overtime);
                });
            }
            else
            {
                ntfSign.set_node(_sendQueue.AddLast(1, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign.reset_node();
                        ntfSign._success = chan_state.ok == state;
                        ntf(state);
                    }
                }));
            }
        }

        protected override void async_try_send_and_append_notify_(Action<chan_send_wrap> cb, Action<chan_state> msgNtf, chan_notify_sign ntfSign, T msg, int ms)
        {
            ntfSign.reset_success();
            if (_closed)
            {
                msgNtf(chan_state.closed);
                cb(new chan_send_wrap { state = chan_state.closed });
                return;
            }
            if (!_has && !_recvQueue.Empty)
            {
                _has = true;
                _tempMsg = msg;
                _isTrySend = true;
                ntfSign.set_node(_sendQueue.AddFirst(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        _isTrySend = false;
                        ntfSign.reset_node();
                        if (!ntfSign._selectOnce)
                        {
                            async_append_send_notify_(msgNtf, ntfSign, ms);
                        }
                        cb(new chan_send_wrap { state = state });
                    }
                }));
                _recvQueue.RemoveFirst().Invoke(chan_state.ok);
            }
            else
            {
                async_append_send_notify_(msgNtf, ntfSign, ms);
                cb(new chan_send_wrap { state = chan_state.fail });
            }
        }

        protected override void async_remove_send_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign)
        {
            bool effect = ntfSign._ntfNode.effect;
            bool success = ntfSign.reset_success();
            if (effect)
            {
                if (_sendQueue.First._node == ntfSign._ntfNode._node)
                {
                    _isTrySend = _has = false;
                }
                _sendQueue.Remove(ntfSign._ntfNode).cancel_timer();
                ntfSign.reset_node();
            }
            else if (success && !_has)
            {
                if (!_sendQueue.RemoveFirst().Invoke(chan_state.ok) && _isTryRecv)
                {
                    _recvQueue.RemoveFirst().Invoke(chan_state.fail);
                }
            }
            ntf(effect ? chan_state.ok : chan_state.fail);
        }

        protected override void async_clear_(Action ntf)
        {
            _has = false;
            safe_callback(ref _sendQueue, chan_state.fail);
            ntf();
        }

        protected override void async_close_(Action ntf, bool isClear = false)
        {
            _closed = true;
            _has = false;
            safe_callback(ref _recvQueue, ref _sendQueue, chan_state.closed);
            ntf();
        }

        protected override void async_cancel_(Action ntf, bool isClear = false)
        {
            _has = false;
            safe_callback(ref _recvQueue, ref _sendQueue, chan_state.cancel);
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
        priority_queue<notify_pck> _recvQueue;
        T _msg;
        long _pushCount;
        bool _has;

        public broadcast_chan(shared_strand strand) : base(strand)
        {
            _recvQueue = new priority_queue<notify_pck>();
            _pushCount = 0;
            _has = false;
        }

        public broadcast_chan() : this(shared_strand.default_strand()) { }

        internal override select_chan_base make_select_reader(Func<T, Task> handler, broadcast_token token, chan_lost_msg<T> lostMsg)
        {
            return new select_chan_reader() { _token = null != token ? token : new broadcast_token(), _chan = this, _handler = handler, _lostMsg = lostMsg };
        }

        internal override select_chan_base make_select_reader(Func<T, Task> handler, broadcast_token token, Func<chan_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return new select_chan_reader() { _token = null != token ? token : new broadcast_token(), _chan = this, _handler = handler, _errHandler = errHandler, _lostMsg = lostMsg };
        }

        internal override select_chan_base make_select_reader(int ms, Func<T, Task> handler, broadcast_token token, chan_lost_msg<T> lostMsg)
        {
            return new select_chan_reader() { _chanTimeout = ms, _token = null != token ? token : new broadcast_token(), _chan = this, _handler = handler, _lostMsg = lostMsg };
        }

        internal override select_chan_base make_select_reader(int ms, Func<T, Task> handler, broadcast_token token, Func<chan_state, Task<bool>> errHandler, chan_lost_msg<T> lostMsg)
        {
            return new select_chan_reader() { _chanTimeout = ms, _token = null != token ? token : new broadcast_token(), _chan = this, _handler = handler, _errHandler = errHandler, _lostMsg = lostMsg };
        }

        public override chan_type type()
        {
            return chan_type.broadcast;
        }

        protected override void async_send_(Action<chan_send_wrap> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_closed)
            {
                ntf(new chan_send_wrap { state = chan_state.closed });
                return;
            }
            _pushCount++;
            _msg = msg;
            _has = true;
            safe_callback(ref _recvQueue, chan_state.ok);
            ntf(new chan_send_wrap { state = chan_state.ok });
        }

        protected override void async_recv_(Action<chan_recv_wrap<T>> ntf, chan_notify_sign ntfSign)
        {
            async_recv_(ntf, broadcast_token._defToken, ntfSign);
        }

        protected override void async_recv_(Action<chan_recv_wrap<T>> ntf, broadcast_token token, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_has && token._lastId != _pushCount)
            {
                if (!token.is_default())
                {
                    token._lastId = _pushCount;
                }
                ntf(new chan_recv_wrap<T> { state = chan_state.ok, msg = _msg });
            }
            else if (_closed)
            {
                ntf(new chan_recv_wrap<T> { state = chan_state.closed });
            }
            else
            {
                chan_notify_sign.set_node(ntfSign, _recvQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        if (chan_state.ok == state)
                        {
                            async_recv_(ntf, token, ntfSign);
                        }
                        else
                        {
                            ntf(new chan_recv_wrap<T> { state = state });
                        }
                    }
                }));
            }
        }

        protected override void async_try_send_(Action<chan_send_wrap> ntf, T msg, chan_notify_sign ntfSign)
        {
            async_send_(ntf, msg, ntfSign);
        }

        protected override void async_try_recv_(Action<chan_recv_wrap<T>> ntf, chan_notify_sign ntfSign)
        {
            async_try_recv_(ntf, broadcast_token._defToken, ntfSign);
        }

        protected override void async_try_recv_(Action<chan_recv_wrap<T>> ntf, broadcast_token token, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_has && token._lastId != _pushCount)
            {
                if (!token.is_default())
                {
                    token._lastId = _pushCount;
                }
                ntf(new chan_recv_wrap<T> { state = chan_state.ok, msg = _msg });
            }
            else if (_closed)
            {
                ntf(new chan_recv_wrap<T> { state = chan_state.closed });
            }
            else
            {
                ntf(new chan_recv_wrap<T> { state = chan_state.fail });
            }
        }

        protected override void async_timed_send_(int ms, Action<chan_send_wrap> ntf, T msg, chan_notify_sign ntfSign)
        {
            async_send_(ntf, msg, ntfSign);
        }

        protected override void async_timed_recv_(int ms, Action<chan_recv_wrap<T>> ntf, chan_notify_sign ntfSign)
        {
            async_timed_recv_(ms, ntf, broadcast_token._defToken, ntfSign);
        }

        protected override void async_timed_recv_(int ms, Action<chan_recv_wrap<T>> ntf, broadcast_token token, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            _timed_check_pop(system_tick.get_tick_ms() + ms, ntf, token, ntfSign);
        }

        void _timed_check_pop(long deadms, Action<chan_recv_wrap<T>> ntf, broadcast_token token, chan_notify_sign ntfSign)
        {
            if (_has && token._lastId != _pushCount)
            {
                if (!token.is_default())
                {
                    token._lastId = _pushCount;
                }
                ntf(new chan_recv_wrap<T> { state = chan_state.ok, msg = _msg });
            }
            else if (_closed)
            {
                ntf(new chan_recv_wrap<T> { state = chan_state.closed });
            }
            else
            {
                async_timer timer = new async_timer(self_strand());
                priority_queue_node<notify_pck> node = _recvQueue.AddLast(0, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        timer.cancel();
                        if (chan_state.ok == state)
                        {
                            _timed_check_pop(deadms, ntf, token, ntfSign);
                        }
                        else
                        {
                            ntf(new chan_recv_wrap<T> { state = state });
                        }
                    }
                });
                ntfSign?.set_node(node);
                timer.deadline(deadms, delegate ()
                {
                    _recvQueue.Remove(node).Invoke(chan_state.overtime);
                });
            }
        }

        protected override void async_append_recv_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign, int ms)
        {
            async_append_recv_notify_(ntf, broadcast_token._defToken, ntfSign, ms);
        }

        protected override void async_append_recv_notify_(Action<chan_state> ntf, broadcast_token token, chan_notify_sign ntfSign, int ms)
        {
            _append_recv_notify(ntf, token, ntfSign, ms);
        }

        bool _append_recv_notify(Action<chan_state> ntf, broadcast_token token, chan_notify_sign ntfSign, int ms)
        {
            if (_has && token._lastId != _pushCount)
            {
                if (!token.is_default())
                {
                    token._lastId = _pushCount;
                }
                ntfSign._success = true;
                ntf(chan_state.ok);
                return true;
            }
            else if (_closed)
            {
                return false;
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(self_strand());
                ntfSign.set_node(_recvQueue.AddLast(1, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_state state)
                    {
                        timer.cancel();
                        ntfSign.reset_node();
                        ntfSign._success = chan_state.ok == state;
                        ntf(state);
                    }
                }));
                timer.timeout(ms, delegate ()
                {
                    _recvQueue.Remove(ntfSign._ntfNode).Invoke(chan_state.overtime);
                });
                return false;
            }
            else
            {
                ntfSign.set_node(_recvQueue.AddLast(1, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign.reset_node();
                        ntfSign._success = chan_state.ok == state;
                        ntf(state);
                    }
                }));
                return false;
            }
        }

        protected override void async_try_recv_and_append_notify_(Action<chan_recv_wrap<T>> cb, Action<chan_state> msgNtf, chan_notify_sign ntfSign, int ms)
        {
            async_try_recv_and_append_notify_(cb, msgNtf, broadcast_token._defToken, ntfSign, ms);
        }

        protected override void async_try_recv_and_append_notify_(Action<chan_recv_wrap<T>> cb, Action<chan_state> msgNtf, broadcast_token token, chan_notify_sign ntfSign, int ms = -1)
        {
            ntfSign.reset_success();
            if (_append_recv_notify(msgNtf, token, ntfSign, ms))
            {
                cb(new chan_recv_wrap<T> { state = chan_state.ok, msg = _msg });
            }
            else if (_closed)
            {
                msgNtf(chan_state.closed);
                cb(new chan_recv_wrap<T> { state = chan_state.closed });
            }
            else
            {
                cb(new chan_recv_wrap<T> { state = chan_state.fail });
            }
        }

        protected override void async_remove_recv_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign)
        {
            bool effect = ntfSign._ntfNode.effect;
            bool success = ntfSign.reset_success();
            if (effect)
            {
                _recvQueue.Remove(ntfSign._ntfNode).cancel_timer();
                ntfSign.reset_node();
            }
            else if (success && _has)
            {
                _recvQueue.RemoveFirst().Invoke(chan_state.ok);
            }
            ntf(effect ? chan_state.ok : chan_state.fail);
        }

        protected override void async_append_send_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign, int ms)
        {
            if (_closed)
            {
                ntf(chan_state.closed);
                return;
            }
            ntfSign._success = true;
            ntf(chan_state.ok);
        }

        protected override void async_try_send_and_append_notify_(Action<chan_send_wrap> cb, Action<chan_state> msgNtf, chan_notify_sign ntfSign, T msg, int ms)
        {
            ntfSign.reset_success();
            if (_closed)
            {
                msgNtf(chan_state.closed);
                cb(new chan_send_wrap { state = chan_state.closed });
                return;
            }
            _pushCount++;
            _msg = msg;
            _has = true;
            msgNtf(chan_state.ok);
            cb(new chan_send_wrap { state = chan_state.ok });
        }

        protected override void async_remove_send_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign)
        {
            ntf(chan_state.fail);
        }

        protected override void async_clear_(Action ntf)
        {
            _has = false;
            ntf();
        }

        protected override void async_close_(Action ntf, bool isClear = false)
        {
            _closed = true;
            _has &= !isClear;
            safe_callback(ref _recvQueue, chan_state.closed);
            ntf();
        }

        protected override void async_cancel_(Action ntf, bool isClear = false)
        {
            _has &= !isClear;
            safe_callback(ref _recvQueue, chan_state.cancel);
            ntf();
        }
    }

    public class csp_chan<R, T> : chan_base
    {
        struct send_pck
        {
            public Action<csp_invoke_wrap<R>> _notify;
            public T _msg;
            public bool _has;
            public bool _isTryMsg;
            public int _invokeMs;
            async_timer _timer;

            public void set(Action<csp_invoke_wrap<R>> ntf, T msg, async_timer timer, int ms = -1)
            {
                _notify = ntf;
                _msg = msg;
                _has = true;
                _invokeMs = ms;
                _timer = timer;
            }

            public void set(Action<csp_invoke_wrap<R>> ntf, T msg, int ms = -1)
            {
                _notify = ntf;
                _msg = msg;
                _has = true;
                _invokeMs = ms;
                _timer = null;
            }

            public Action<csp_invoke_wrap<R>> cancel()
            {
                _isTryMsg = _has = false;
                _timer?.cancel();
                return _notify;
            }
        }

        public class csp_result
        {
            internal int _invokeMs;
            internal Action<csp_invoke_wrap<R>> _notify;
            async_timer _invokeTimer;
#if DEBUG
            shared_strand _hostStrand;
#endif

            internal csp_result(int ms, Action<csp_invoke_wrap<R>> notify)
            {
                _invokeMs = ms;
                _notify = notify;
                _invokeTimer = null;
            }

            internal void start_invoke_timer(generator host)
            {
#if DEBUG
                _hostStrand = host.strand;
#endif
                if (_invokeMs >= 0)
                {
                    _invokeTimer = new async_timer(host.strand);
                    _invokeTimer.timeout(_invokeMs, fail);
                }
            }

            public bool complete(R res)
            {
#if DEBUG
                if (null != _hostStrand)
                {
                    Debug.Assert(_hostStrand.running_in_this_thread(), "不正确的 complete 调用!");
                }
#endif
                _invokeTimer?.cancel();
                _invokeTimer = null;
                if (null != _notify)
                {
                    Action<csp_invoke_wrap<R>> ntf = _notify;
                    _notify = null;
                    ntf.Invoke(new csp_invoke_wrap<R> { state = chan_state.ok, result = res });
                    return true;
                }
                return false;
            }

            public void fail()
            {
#if DEBUG
                if (null != _hostStrand)
                {
                    Debug.Assert(_hostStrand.running_in_this_thread(), "不正确的 fail 调用!");
                }
#endif
                _invokeTimer?.cancel();
                _invokeTimer = null;
                Action<csp_invoke_wrap<R>> ntf = _notify;
                _notify = null;
                ntf?.Invoke(new csp_invoke_wrap<R> { state = chan_state.csp_fail });
            }
        }

        internal class select_csp_reader : select_chan_base
        {
            public csp_chan<R, T> _chan;
            public Func<T, Task<R>> _handler;
            public Func<T, ValueTask<R>> _gohandler;
            public Func<chan_state, Task<bool>> _errHandler;
            public chan_lost_msg<csp_wait_wrap<R, T>> _lostMsg;
            public int _chanTimeout = -1;
            async_result_wrap<csp_wait_wrap<R, T>> _tryRecvRes;
            generator _host;

            public override void begin(generator host)
            {
                ntfSign._disable = false;
                _host = host;
                _tryRecvRes = new async_result_wrap<csp_wait_wrap<R, T>> { value1 = csp_wait_wrap<R, T>.def };
                if (enable)
                {
                    _chan.async_append_recv_notify(nextSelect, ntfSign, _chanTimeout);
                }
            }

            public override async Task<select_chan_state> invoke(Func<Task> stepOne)
            {
                try
                {
                    _tryRecvRes.value1 = csp_wait_wrap<R, T>.def;
                    _chan.async_try_recv_and_append_notify(_host.unsafe_async_result(_tryRecvRes), nextSelect, ntfSign, _chanTimeout);
                    await _host.async_wait();
                }
                catch (generator.stop_exception)
                {
                    _chan.async_remove_recv_notify(_host.unsafe_async_ignore<chan_state>(), ntfSign);
                    await _host.async_wait();
                    if (chan_state.ok == _tryRecvRes.value1.state)
                    {
                        if (null != _lostMsg)
                        {
                            _lostMsg.set(_tryRecvRes.value1);
                        }
                        else
                        {
                            _tryRecvRes.value1.fail();
                        }
                    }
                    throw;
                }
                select_chan_state chanState = new select_chan_state() { failed = false, nextRound = true };
                if (chan_state.ok == _tryRecvRes.value1.state)
                {
                    bool invoked = false;
                    try
                    {
                        if (null != stepOne)
                        {
                            await stepOne();
                        }
                        _tryRecvRes.value1.result.start_invoke_timer(_host);
                        await _host.unlock_suspend_();
                        invoked = true;
                        _tryRecvRes.value1.complete(null != _handler ? await _handler(_tryRecvRes.value1.msg) : await _gohandler(_tryRecvRes.value1.msg));
                    }
                    catch (csp_fail_exception)
                    {
                        _tryRecvRes.value1.fail();
                    }
                    catch (generator.select_stop_all_exception)
                    {
                        _tryRecvRes.value1.fail();
                        throw;
                    }
                    catch (generator.select_stop_current_exception)
                    {
                        _tryRecvRes.value1.fail();
                        throw;
                    }
                    catch (generator.stop_exception)
                    {
                        if (!invoked && null != _lostMsg)
                        {
                            _lostMsg.set(_tryRecvRes.value1);
                        }
                        else
                        {
                            _tryRecvRes.value1.fail();
                        }
                        throw;
                    }
                    finally
                    {
                        _host.lock_suspend_();
                    }
                }
                else if (chan_state.closed == _tryRecvRes.value1.state)
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

            private async Task<bool> errInvoke_(chan_state state)
            {
                try
                {
                    await _host.unlock_suspend_();
                    if (!await _errHandler(state) && chan_state.closed != state)
                    {
                        _chan.async_append_recv_notify(nextSelect, ntfSign, _chanTimeout);
                        return false;
                    }
                }
                finally
                {
                    _host.lock_suspend_();
                }
                return true;
            }

            public override ValueTask<bool> errInvoke(chan_state state)
            {
                if (null != _errHandler)
                {
                    return new ValueTask<bool>(errInvoke_(state));
                }
                return new ValueTask<bool>(true);
            }

            public override Task end()
            {
                ntfSign._disable = true;
                _chan.async_remove_recv_notify(_host.unsafe_async_ignore<chan_state>(), ntfSign);
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
            public Func<chan_state, Task<bool>> _errHandler;
            public Action<csp_invoke_wrap<R>> _lostRes;
            public chan_lost_msg<T> _lostMsg;
            public int _chanTimeout = -1;
            async_result_wrap<csp_invoke_wrap<R>> _trySendRes;
            generator _host;

            public override void begin(generator host)
            {
                ntfSign._disable = false;
                _host = host;
                _trySendRes = new async_result_wrap<csp_invoke_wrap<R>> { value1 = csp_invoke_wrap<R>.def };
                if (enable)
                {
                    _chan.async_append_send_notify(nextSelect, ntfSign, _chanTimeout);
                }
            }

            public override async Task<select_chan_state> invoke(Func<Task> stepOne)
            {
                try
                {
                    _trySendRes.value1 = csp_invoke_wrap<R>.def;
                    _chan.async_try_send_and_append_notify(null == _lostRes ? _host.unsafe_async_result(_trySendRes) : _host.async_result(_trySendRes, _lostRes), nextSelect, ntfSign, _msg.value1, _chanTimeout);
                    await _host.async_wait();
                }
                catch (generator.stop_exception)
                {
                    chan_state rmState = chan_state.undefined;
                    _chan.async_remove_send_notify(null == _lostMsg ? _host.unsafe_async_ignore<chan_state>() : _host.unsafe_async_callback((chan_state state) => rmState = state), ntfSign);
                    await _host.async_wait();
                    if (chan_state.ok == rmState)
                    {
                        _lostMsg?.set(_msg.value1);
                    }
                    throw;
                }
                select_chan_state chanState = new select_chan_state() { failed = false, nextRound = true };
                if (chan_state.ok == _trySendRes.value1.state)
                {
                    bool invoked = false;
                    try
                    {
                        if (null != stepOne)
                        {
                            await stepOne();
                        }
                        await _host.unlock_suspend_();
                        invoked = true;
                        await _handler(_trySendRes.value1.result);
                    }
                    catch (generator.stop_exception)
                    {
                        if (!invoked)
                        {
                            _lostRes?.Invoke(_trySendRes.value1);
                        }
                        throw;
                    }
                    finally
                    {
                        _host.lock_suspend_();
                    }
                }
                else if (chan_state.closed == _trySendRes.value1.state)
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

            private async Task<bool> errInvoke_(chan_state state)
            {
                try
                {
                    await _host.unlock_suspend_();
                    if (!await _errHandler(state) && chan_state.closed != state)
                    {
                        _chan.async_append_send_notify(nextSelect, ntfSign, _chanTimeout);
                        return false;
                    }
                }
                finally
                {
                    _host.lock_suspend_();
                }
                return true;
            }

            public override ValueTask<bool> errInvoke(chan_state state)
            {
                if (null != _errHandler)
                {
                    return new ValueTask<bool>(errInvoke_(state));
                }
                return new ValueTask<bool>(true);
            }

            public override Task end()
            {
                ntfSign._disable = true;
                _chan.async_remove_send_notify(_host.unsafe_async_ignore<chan_state>(), ntfSign);
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
        priority_queue<notify_pck> _recvQueue;
        send_pck _tempMsg;
        bool _isTryRecv;

        public csp_chan(shared_strand strand) : base(strand)
        {
            _sendQueue = new priority_queue<notify_pck>();
            _recvQueue = new priority_queue<notify_pck>();
            _tempMsg.cancel();
            _isTryRecv = false;
        }

        public csp_chan() : this(shared_strand.default_strand()) { }

        internal select_chan_base make_select_reader(Func<T, Task<R>> handler, chan_lost_msg<csp_wait_wrap<R, T>> lostMsg)
        {
            return new select_csp_reader() { _chan = this, _handler = handler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_reader(Func<T, Task<R>> handler, Func<chan_state, Task<bool>> errHandler, chan_lost_msg<csp_wait_wrap<R, T>> lostMsg)
        {
            return new select_csp_reader() { _chan = this, _handler = handler, _errHandler = errHandler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_reader(int ms, Func<T, Task<R>> handler, chan_lost_msg<csp_wait_wrap<R, T>> lostMsg)
        {
            return new select_csp_reader() { _chanTimeout = ms, _chan = this, _handler = handler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_reader(int ms, Func<T, Task<R>> handler, Func<chan_state, Task<bool>> errHandler, chan_lost_msg<csp_wait_wrap<R, T>> lostMsg)
        {
            return new select_csp_reader() { _chanTimeout = ms, _chan = this, _handler = handler, _errHandler = errHandler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_reader(Func<T, ValueTask<R>> handler, chan_lost_msg<csp_wait_wrap<R, T>> lostMsg)
        {
            return new select_csp_reader() { _chan = this, _gohandler = handler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_reader(Func<T, ValueTask<R>> handler, Func<chan_state, Task<bool>> errHandler, chan_lost_msg<csp_wait_wrap<R, T>> lostMsg)
        {
            return new select_csp_reader() { _chan = this, _gohandler = handler, _errHandler = errHandler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_reader(int ms, Func<T, ValueTask<R>> handler, chan_lost_msg<csp_wait_wrap<R, T>> lostMsg)
        {
            return new select_csp_reader() { _chanTimeout = ms, _chan = this, _gohandler = handler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_reader(int ms, Func<T, ValueTask<R>> handler, Func<chan_state, Task<bool>> errHandler, chan_lost_msg<csp_wait_wrap<R, T>> lostMsg)
        {
            return new select_csp_reader() { _chanTimeout = ms, _chan = this, _gohandler = handler, _errHandler = errHandler, _lostMsg = lostMsg };
        }

        internal select_chan_base make_select_writer(int ms, async_result_wrap<T> msg, Func<R, Task> handler, Func<chan_state, Task<bool>> errHandler, Action<R> lostRes, chan_lost_msg<T> lostMsg)
        {
            return new select_csp_writer()
            {
                _chanTimeout = ms,
                _chan = this,
                _msg = msg,
                _handler = handler,
                _errHandler = errHandler,
                _lostMsg = lostMsg,
                _lostRes = null == lostRes ? (Action<csp_invoke_wrap<R>>)null : delegate (csp_invoke_wrap<R> cspRes)
                {
                    if (chan_state.ok == cspRes.state)
                    {
                        lostRes(cspRes.result);
                    }
                }
            };
        }

        internal select_chan_base make_select_writer(async_result_wrap<T> msg, Func<R, Task> handler, Func<chan_state, Task<bool>> errHandler, Action<R> lostRes, chan_lost_msg<T> lostMsg)
        {
            return make_select_writer(-1, msg, handler, errHandler, lostRes, lostMsg);
        }

        internal select_chan_base make_select_writer(T msg, Func<R, Task> handler, Func<chan_state, Task<bool>> errHandler, Action<R> lostRes, chan_lost_msg<T> lostMsg)
        {
            return make_select_writer(-1, new async_result_wrap<T> { value1 = msg }, handler, errHandler, lostRes, lostMsg);
        }

        internal select_chan_base make_select_writer(int ms, T msg, Func<R, Task> handler, Func<chan_state, Task<bool>> errHandler, Action<R> lostRes, chan_lost_msg<T> lostMsg)
        {
            return make_select_writer(ms, new async_result_wrap<T> { value1 = msg }, handler, errHandler, lostRes, lostMsg);
        }

        public override chan_type type()
        {
            return chan_type.csp;
        }

        public void async_send(Action<csp_invoke_wrap<R>> ntf, T msg, chan_notify_sign ntfSign = null)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_send_(ntf, msg, ntfSign);
                else self_strand().add_next(() => async_send_(ntf, msg, ntfSign));
            else self_strand().post(() => async_send_(ntf, msg, ntfSign));
        }

        public void async_send(int invokeMs, Action<csp_invoke_wrap<R>> ntf, T msg, chan_notify_sign ntfSign = null)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_send_(invokeMs, ntf, msg, ntfSign);
                else self_strand().add_next(() => async_send_(invokeMs, ntf, msg, ntfSign));
            else self_strand().post(() => async_send_(invokeMs, ntf, msg, ntfSign));
        }

        public void async_recv(Action<csp_wait_wrap<R, T>> ntf, chan_notify_sign ntfSign = null)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_recv_(ntf, ntfSign);
                else self_strand().add_next(() => async_recv_(ntf, ntfSign));
            else self_strand().post(() => async_recv_(ntf, ntfSign));
        }

        public void async_try_send(Action<csp_invoke_wrap<R>> ntf, T msg, chan_notify_sign ntfSign = null)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_try_send_(ntf, msg, ntfSign);
                else self_strand().add_next(() => async_try_send_(ntf, msg, ntfSign));
            else self_strand().post(() => async_try_send_(ntf, msg, ntfSign));
        }

        public void async_try_send(int invokeMs, Action<csp_invoke_wrap<R>> ntf, T msg, chan_notify_sign ntfSign = null)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_try_send_(invokeMs, ntf, msg, ntfSign);
                else self_strand().add_next(() => async_try_send_(invokeMs, ntf, msg, ntfSign));
            else self_strand().post(() => async_try_send_(invokeMs, ntf, msg, ntfSign));
        }

        public void async_try_recv(Action<csp_wait_wrap<R, T>> ntf, chan_notify_sign ntfSign = null)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_try_recv_(ntf, ntfSign);
                else self_strand().add_next(() => async_try_recv_(ntf, ntfSign));
            else self_strand().post(() => async_try_recv_(ntf, ntfSign));
        }

        public void async_timed_send(int ms, Action<csp_invoke_wrap<R>> ntf, T msg, chan_notify_sign ntfSign = null)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_timed_send_(ms, ntf, msg, ntfSign);
                else self_strand().add_next(() => async_timed_send_(ms, ntf, msg, ntfSign));
            else self_strand().post(() => async_timed_send_(ms, ntf, msg, ntfSign));
        }

        public void async_timed_send(int ms, int invokeMs, Action<csp_invoke_wrap<R>> ntf, T msg, chan_notify_sign ntfSign = null)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_timed_send_(ms, invokeMs, ntf, msg, ntfSign);
                else self_strand().add_next(() => async_timed_send_(ms, invokeMs, ntf, msg, ntfSign));
            else self_strand().post(() => async_timed_send_(ms, invokeMs, ntf, msg, ntfSign));
        }

        public void async_timed_recv(int ms, Action<csp_wait_wrap<R, T>> ntf, chan_notify_sign ntfSign = null)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_timed_recv_(ms, ntf, ntfSign);
                else self_strand().add_next(() => async_timed_recv_(ms, ntf, ntfSign));
            else self_strand().post(() => async_timed_recv_(ms, ntf, ntfSign));
        }

        public void async_try_recv_and_append_notify(Action<csp_wait_wrap<R, T>> cb, Action<chan_state> msgNtf, chan_notify_sign ntfSign, int ms = -1)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_try_recv_and_append_notify_(cb, msgNtf, ntfSign, ms);
                else self_strand().add_next(() => async_try_recv_and_append_notify_(cb, msgNtf, ntfSign, ms));
            else self_strand().post(() => async_try_recv_and_append_notify_(cb, msgNtf, ntfSign, ms));
        }

        public void async_try_send_and_append_notify(Action<csp_invoke_wrap<R>> cb, Action<chan_state> msgNtf, chan_notify_sign ntfSign, T msg, int ms = -1)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_try_send_and_append_notify_(cb, msgNtf, ntfSign, msg, ms);
                else self_strand().add_next(() => async_try_send_and_append_notify_(cb, msgNtf, ntfSign, msg, ms));
            else self_strand().post(() => async_try_send_and_append_notify_(cb, msgNtf, ntfSign, msg, ms));
        }

        public Task unsafe_invoke(async_result_wrap<csp_invoke_wrap<R>> res, T msg, int invokeMs = -1)
        {
            return generator.unsafe_csp_invoke(res, this, msg, invokeMs);
        }

        public ValueTask<csp_invoke_wrap<R>> invoke(T msg, int invokeMs = -1, Action<R> lostRes = null, chan_lost_msg<T> lostMsg = null)
        {
            return generator.csp_invoke(this, msg, invokeMs, lostRes, lostMsg);
        }

        public Task unsafe_wait(async_result_wrap<csp_wait_wrap<R, T>> res)
        {
            return generator.unsafe_csp_wait(res, this);
        }

        public ValueTask<csp_wait_wrap<R, T>> wait(chan_lost_msg<csp_wait_wrap<R, T>> lostMsg = null)
        {
            return generator.csp_wait(this, lostMsg);
        }

        public ValueTask<chan_state> wait(Func<T, Task<R>> handler, chan_lost_msg<csp_wait_wrap<R, T>> lostMsg = null)
        {
            return generator.csp_wait(this, handler, lostMsg);
        }

        public ValueTask<chan_state> wait(Func<T, ValueTask<R>> handler, chan_lost_msg<csp_wait_wrap<R, T>> lostMsg = null)
        {
            return generator.csp_wait(this, handler, lostMsg);
        }

        public Task unsafe_try_invoke(async_result_wrap<csp_invoke_wrap<R>> res, T msg, int invokeMs = -1)
        {
            return generator.unsafe_csp_try_invoke(res, this, msg, invokeMs);
        }

        public ValueTask<csp_invoke_wrap<R>> try_invoke(T msg, int invokeMs = -1, Action<R> lostRes = null, chan_lost_msg<T> lostMsg = null)
        {
            return generator.csp_try_invoke(this, msg, invokeMs, lostRes, lostMsg);
        }

        public Task unsafe_try_wait(async_result_wrap<csp_wait_wrap<R, T>> res)
        {
            return generator.unsafe_csp_try_wait(res, this);
        }

        public ValueTask<csp_wait_wrap<R, T>> try_wait(chan_lost_msg<csp_wait_wrap<R, T>> lostMsg = null)
        {
            return generator.csp_try_wait(this, lostMsg);
        }

        public ValueTask<chan_state> try_wait(Func<T, Task<R>> handler, chan_lost_msg<csp_wait_wrap<R, T>> lostMsg = null)
        {
            return generator.csp_try_wait(this, handler, lostMsg);
        }

        public ValueTask<chan_state> try_wait(Func<T, ValueTask<R>> handler, chan_lost_msg<csp_wait_wrap<R, T>> lostMsg = null)
        {
            return generator.csp_try_wait(this, handler, lostMsg);
        }

        public Task unsafe_timed_invoke(async_result_wrap<csp_invoke_wrap<R>> res, tuple<int, int> ms, T msg)
        {
            return generator.unsafe_csp_timed_invoke(res, this, ms, msg);
        }

        public ValueTask<csp_invoke_wrap<R>> timed_invoke(tuple<int, int> ms, T msg, Action<R> lostRes = null, chan_lost_msg<T> lostMsg = null)
        {
            return generator.csp_timed_invoke(this, ms, msg, lostRes, lostMsg);
        }

        public Task unsafe_timed_invoke(async_result_wrap<csp_invoke_wrap<R>> res, int ms, T msg)
        {
            return generator.unsafe_csp_timed_invoke(res, this, ms, msg);
        }

        public ValueTask<csp_invoke_wrap<R>> timed_invoke(int ms, T msg, Action<R> lostRes = null, chan_lost_msg<T> lostMsg = null)
        {
            return generator.csp_timed_invoke(this, ms, msg, lostRes, lostMsg);
        }

        public Task unsafe_timed_wait(async_result_wrap<csp_wait_wrap<R, T>> res, int ms)
        {
            return generator.unsafe_csp_timed_wait(res, this, ms);
        }

        public ValueTask<csp_wait_wrap<R, T>> timed_wait(int ms, chan_lost_msg<csp_wait_wrap<R, T>> lostMsg = null)
        {
            return generator.csp_timed_wait(this, ms, lostMsg);
        }

        public ValueTask<chan_state> timed_wait(int ms, Func<T, Task<R>> handler, chan_lost_msg<csp_wait_wrap<R, T>> lostMsg = null)
        {
            return generator.csp_timed_wait(this, ms, handler, lostMsg);
        }

        public ValueTask<chan_state> timed_wait(int ms, Func<T, ValueTask<R>> handler, chan_lost_msg<csp_wait_wrap<R, T>> lostMsg = null)
        {
            return generator.csp_timed_wait(this, ms, handler, lostMsg);
        }

        private void async_send_(Action<csp_invoke_wrap<R>> ntf, T msg, chan_notify_sign ntfSign)
        {
            async_send_(-1, ntf, msg, ntfSign);
        }

        private void async_send_(int invokeMs, Action<csp_invoke_wrap<R>> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_closed)
            {
                ntf(new csp_invoke_wrap<R> { state = chan_state.closed });
                return;
            }
            if (_tempMsg._has || _recvQueue.Empty)
            {
                chan_notify_sign.set_node(ntfSign, _sendQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        if (chan_state.ok == state)
                        {
                            async_send_(invokeMs, ntf, msg, ntfSign);
                        }
                        else
                        {
                            ntf(new csp_invoke_wrap<R> { state = state });
                        }
                    }
                }));
            }
            else
            {
                _tempMsg.set(ntf, msg, invokeMs);
                _recvQueue.RemoveFirst().Invoke(chan_state.ok);
            }
        }

        private void async_recv_(Action<csp_wait_wrap<R, T>> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_tempMsg._has)
            {
                send_pck msg = _tempMsg;
                _tempMsg.cancel();
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
                ntf(new csp_wait_wrap<R, T> { state = chan_state.ok, msg = msg._msg, result = new csp_result(msg._invokeMs, msg._notify) });
            }
            else if (_closed)
            {
                ntf(new csp_wait_wrap<R, T> { state = chan_state.closed });
            }
            else
            {
                chan_notify_sign.set_node(ntfSign, _recvQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        if (chan_state.ok == state)
                        {
                            async_recv_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(new csp_wait_wrap<R, T> { state = state });
                        }
                    }
                }));
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
            }
        }

        private void async_try_send_(Action<csp_invoke_wrap<R>> ntf, T msg, chan_notify_sign ntfSign)
        {
            async_try_send_(-1, ntf, msg, ntfSign);
        }

        private void async_try_send_(int invokeMs, Action<csp_invoke_wrap<R>> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_closed)
            {
                ntf(new csp_invoke_wrap<R> { state = chan_state.closed });
                return;
            }
            if (_tempMsg._has || _recvQueue.Empty)
            {
                ntf(new csp_invoke_wrap<R> { state = chan_state.fail });
            }
            else
            {
                _tempMsg.set(ntf, msg, invokeMs);
                _tempMsg._isTryMsg = true;
                _recvQueue.RemoveFirst().Invoke(chan_state.ok);
            }
        }

        private void async_try_recv_(Action<csp_wait_wrap<R, T>> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_tempMsg._has)
            {
                send_pck msg = _tempMsg;
                _tempMsg.cancel();
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
                ntf(new csp_wait_wrap<R, T> { state = chan_state.ok, msg = msg._msg, result = new csp_result(msg._invokeMs, msg._notify) });
            }
            else if (_closed)
            {
                ntf(new csp_wait_wrap<R, T> { state = chan_state.closed });
            }
            else if (!_sendQueue.Empty && _recvQueue.Empty)
            {
                _isTryRecv = true;
                chan_notify_sign.set_node(ntfSign, _recvQueue.AddFirst(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        _isTryRecv = false;
                        ntfSign?.reset_node();
                        if (chan_state.ok == state)
                        {
                            async_recv_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(new csp_wait_wrap<R, T> { state = state });
                        }
                    }
                }));
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
            }
            else
            {
                ntf(new csp_wait_wrap<R, T> { state = chan_state.fail });
            }
        }

        private void async_timed_send_(int ms, Action<csp_invoke_wrap<R>> ntf, T msg, chan_notify_sign ntfSign)
        {
            async_timed_send_(ms, -1, ntf, msg, ntfSign);
        }

        private void async_timed_send_(int ms, int invokeMs, Action<csp_invoke_wrap<R>> ntf, T msg, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_closed)
            {
                ntf(new csp_invoke_wrap<R> { state = chan_state.closed });
                return;
            }
            if (_tempMsg._has || _recvQueue.Empty)
            {
                if (ms >= 0)
                {
                    async_timer timer = new async_timer(self_strand());
                    priority_queue_node<notify_pck> node = _sendQueue.AddLast(0, new notify_pck()
                    {
                        timer = timer,
                        ntf = delegate (chan_state state)
                        {
                            ntfSign?.reset_node();
                            timer.cancel();
                            if (chan_state.ok == state)
                            {
                                async_send_(invokeMs, ntf, msg, ntfSign);
                            }
                            else
                            {
                                ntf(new csp_invoke_wrap<R> { state = state });
                            }
                        }
                    });
                    ntfSign?.set_node(node);
                    timer.timeout(ms, delegate ()
                    {
                        _sendQueue.Remove(node).Invoke(chan_state.overtime);
                    });
                }
                else
                {
                    chan_notify_sign.set_node(ntfSign, _sendQueue.AddLast(0, new notify_pck()
                    {
                        ntf = delegate (chan_state state)
                        {
                            ntfSign?.reset_node();
                            if (chan_state.ok == state)
                            {
                                async_send_(invokeMs, ntf, msg, ntfSign);
                            }
                            else
                            {
                                ntf(new csp_invoke_wrap<R> { state = state });
                            }
                        }
                    }));
                }
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(self_strand());
                _tempMsg.set(ntf, msg, timer, invokeMs);
                timer.timeout(ms, delegate ()
                {
                    _tempMsg.cancel();
                    ntf(new csp_invoke_wrap<R> { state = chan_state.overtime });
                });
                _recvQueue.RemoveFirst().Invoke(chan_state.ok);
            }
            else
            {
                _tempMsg.set(ntf, msg, invokeMs);
                _recvQueue.RemoveFirst().Invoke(chan_state.ok);
            }
        }

        private void async_timed_recv_(int ms, Action<csp_wait_wrap<R, T>> ntf, chan_notify_sign ntfSign)
        {
            ntfSign?.reset_success();
            if (_tempMsg._has)
            {
                send_pck msg = _tempMsg;
                _tempMsg.cancel();
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
                ntf(new csp_wait_wrap<R, T> { state = chan_state.ok, msg = msg._msg, result = new csp_result(msg._invokeMs, msg._notify) });
            }
            else if (_closed)
            {
                ntf(new csp_wait_wrap<R, T> { state = chan_state.closed });
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(self_strand());
                priority_queue_node<notify_pck> node = _recvQueue.AddLast(0, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        timer.cancel();
                        if (chan_state.ok == state)
                        {
                            async_recv_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(new csp_wait_wrap<R, T> { state = state });
                        }
                    }
                });
                ntfSign?.set_node(node);
                timer.timeout(ms, delegate ()
                {
                    _recvQueue.Remove(node).Invoke(chan_state.overtime);
                });
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
            }
            else
            {
                chan_notify_sign.set_node(ntfSign, _recvQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign?.reset_node();
                        if (chan_state.ok == state)
                        {
                            async_recv_(ntf, ntfSign);
                        }
                        else
                        {
                            ntf(new csp_wait_wrap<R, T> { state = state });
                        }
                    }
                }));
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
            }
        }

        protected override void async_append_recv_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign, int ms)
        {
            if (_tempMsg._has)
            {
                ntfSign._success = true;
                ntf(chan_state.ok);
            }
            else if (_closed)
            {
                ntf(chan_state.closed);
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(self_strand());
                ntfSign.set_node(_recvQueue.AddLast(1, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_state state)
                    {
                        timer.cancel();
                        ntfSign.reset_node();
                        ntfSign._success = chan_state.ok == state;
                        ntf(state);
                    }
                }));
                timer.timeout(ms, delegate ()
                {
                    _recvQueue.Remove(ntfSign._ntfNode).Invoke(chan_state.overtime);
                });
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
            }
            else
            {
                ntfSign.set_node(_recvQueue.AddLast(1, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign.reset_node();
                        ntfSign._success = chan_state.ok == state;
                        ntf(state);
                    }
                }));
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
            }
        }

        private void async_try_recv_and_append_notify_(Action<csp_wait_wrap<R, T>> cb, Action<chan_state> msgNtf, chan_notify_sign ntfSign, int ms)
        {
            ntfSign.reset_success();
            if (_tempMsg._has)
            {
                send_pck msg = _tempMsg;
                _tempMsg.cancel();
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
                if (!ntfSign._selectOnce)
                {
                    async_append_recv_notify_(msgNtf, ntfSign, ms);
                }
                cb(new csp_wait_wrap<R, T> { state = chan_state.ok, msg = msg._msg, result = new csp_result(msg._invokeMs, msg._notify) });
            }
            else if (_closed)
            {
                msgNtf(chan_state.closed);
                cb(new csp_wait_wrap<R, T> { state = chan_state.closed });
            }
            else if (!_sendQueue.Empty && _recvQueue.Empty)
            {
                _isTryRecv = true;
                chan_notify_sign.set_node(ntfSign, _recvQueue.AddLast(0, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        _isTryRecv = false;
                        ntfSign?.reset_node();
                        if (chan_state.ok == state)
                        {
                            async_recv_(cb, ntfSign);
                        }
                        else
                        {
                            cb(new csp_wait_wrap<R, T> { state = state });
                        }
                    }
                }));
                _sendQueue.RemoveFirst().Invoke(chan_state.ok);
            }
            else
            {
                async_append_recv_notify_(msgNtf, ntfSign, ms);
                cb(new csp_wait_wrap<R, T> { state = chan_state.fail });
            }
        }

        protected override void async_remove_recv_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign)
        {
            bool effect = ntfSign._ntfNode.effect;
            bool success = ntfSign.reset_success();
            if (effect)
            {
                _isTryRecv &= _recvQueue.First._node != ntfSign._ntfNode._node;
                _recvQueue.Remove(ntfSign._ntfNode).cancel_timer();
                ntfSign.reset_node();
            }
            else if (success && _tempMsg._has)
            {
                if (!_recvQueue.RemoveFirst().Invoke(chan_state.ok) && _tempMsg._isTryMsg)
                {
                    _tempMsg.cancel().Invoke(new csp_invoke_wrap<R> { state = chan_state.fail });
                }
            }
            ntf(effect ? chan_state.ok : chan_state.fail);
        }

        protected override void async_append_send_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign, int ms)
        {
            if (_closed)
            {
                ntf(chan_state.closed);
                return;
            }
            if (!_recvQueue.Empty)
            {
                ntfSign._success = true;
                ntf(chan_state.ok);
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(self_strand());
                ntfSign.set_node(_sendQueue.AddLast(1, new notify_pck()
                {
                    timer = timer,
                    ntf = delegate (chan_state state)
                    {
                        timer.cancel();
                        ntfSign.reset_node();
                        ntfSign._success = chan_state.ok == state;
                        ntf(state);
                    }
                }));
                timer.timeout(ms, delegate ()
                {
                    _sendQueue.Remove(ntfSign._ntfNode).Invoke(chan_state.overtime);
                });
            }
            else
            {
                ntfSign.set_node(_sendQueue.AddLast(1, new notify_pck()
                {
                    ntf = delegate (chan_state state)
                    {
                        ntfSign.reset_node();
                        ntfSign._success = chan_state.ok == state;
                        ntf(state);
                    }
                }));
            }
        }

        private void async_try_send_and_append_notify_(Action<csp_invoke_wrap<R>> cb, Action<chan_state> msgNtf, chan_notify_sign ntfSign, T msg, int ms)
        {
            ntfSign.reset_success();
            if (_closed)
            {
                msgNtf(chan_state.closed);
                cb(new csp_invoke_wrap<R> { state = chan_state.closed });
                return;
            }
            if (!_tempMsg._has && !_recvQueue.Empty)
            {
                _tempMsg.set(cb, msg);
                _tempMsg._isTryMsg = true;
                if (!ntfSign._selectOnce)
                {
                    async_append_send_notify_(msgNtf, ntfSign, ms);
                }
                _recvQueue.RemoveFirst().Invoke(chan_state.ok);
            }
            else
            {
                async_append_send_notify_(msgNtf, ntfSign, ms);
                cb(new csp_invoke_wrap<R> { state = chan_state.fail });
            }
        }

        protected override void async_remove_send_notify_(Action<chan_state> ntf, chan_notify_sign ntfSign)
        {
            bool effect = ntfSign._ntfNode.effect;
            bool success = ntfSign.reset_success();
            if (effect)
            {
                _sendQueue.Remove(ntfSign._ntfNode).cancel_timer();
                ntfSign.reset_node();
            }
            else if (success && !_tempMsg._has)
            {
                if (!_sendQueue.RemoveFirst().Invoke(chan_state.ok) && _isTryRecv)
                {
                    _recvQueue.RemoveFirst().Invoke(chan_state.fail);
                }
            }
            ntf(effect ? chan_state.ok : chan_state.fail);
        }

        protected override void async_clear_(Action ntf)
        {
            _tempMsg.cancel();
            safe_callback(ref _sendQueue, chan_state.fail);
            ntf();
        }

        protected override void async_close_(Action ntf, bool isClear = false)
        {
            _closed = true;
            Action<csp_invoke_wrap<R>> hasMsg = null;
            if (_tempMsg._has)
            {
                hasMsg = _tempMsg.cancel();
            }
            safe_callback(ref _sendQueue, ref _recvQueue, chan_state.closed);
            hasMsg?.Invoke(new csp_invoke_wrap<R> { state = chan_state.closed });
            ntf();
        }

        protected override void async_cancel_(Action ntf, bool isClear = false)
        {
            Action<csp_invoke_wrap<R>> hasMsg = null;
            if (_tempMsg._has)
            {
                hasMsg = _tempMsg.cancel();
            }
            safe_callback(ref _sendQueue, ref _recvQueue, chan_state.cancel);
            hasMsg?.Invoke(new csp_invoke_wrap<R> { state = chan_state.cancel });
            ntf();
        }
    }
}
