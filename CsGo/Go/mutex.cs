﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Go
{
    public class go_mutex
    {
        struct wait_node
        {
            public Action _ntf;
            public long _id;
        }

        shared_strand _strand;
        LinkedList<wait_node> _waitQueue;
        protected long _lockID;
        protected int _recCount;
        protected bool _mustTick;

        public go_mutex(shared_strand strand)
        {
            _strand = strand;
            _waitQueue = new LinkedList<wait_node>();
            _lockID = 0;
            _recCount = 0;
            _mustTick = false;
        }

        public go_mutex() : this(shared_strand.default_strand()) { }

        protected virtual void async_lock_(long id, Action ntf)
        {
            if (0 == _lockID || id == _lockID)
            {
                _lockID = id;
                _recCount++;
                ntf();
            }
            else
            {
                _waitQueue.AddLast(new wait_node() { _ntf = ntf, _id = id });
            }
        }

        protected virtual void async_try_lock_(long id, Action<bool> ntf)
        {
            if (0 == _lockID || id == _lockID)
            {
                _lockID = id;
                _recCount++;
                ntf(true);
            }
            else
            {
                ntf(false);
            }
        }

        protected virtual void async_timed_lock_(long id, int ms, Action<bool> ntf)
        {
            if (0 == _lockID || id == _lockID)
            {
                _lockID = id;
                _recCount++;
                ntf(true);
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(_strand);
                LinkedListNode<wait_node> node = _waitQueue.AddLast(new wait_node()
                {
                    _ntf = delegate ()
                    {
                        timer.cancel();
                        ntf(true);
                    },
                    _id = id
                });
                timer.timeout(ms, delegate ()
                {
                    _waitQueue.Remove(node);
                    ntf(false);
                });
            }
            else
            {
                _waitQueue.AddLast(new wait_node() { _ntf = () => ntf(true), _id = id });
            }
        }

        protected virtual void async_unlock_(long id, Action ntf)
        {
            Debug.Assert(id == _lockID);
            if (0 == --_recCount)
            {
                if (0 != _waitQueue.Count)
                {
                    _recCount = 1;
                    wait_node queueFront = _waitQueue.First.Value;
                    _waitQueue.RemoveFirst();
                    _lockID = queueFront._id;
                    queueFront._ntf();
                }
                else
                {
                    _lockID = 0;
                }
            }
            ntf();
        }

        protected virtual void async_cancel_(long id, Action ntf)
        {
            if (id == _lockID)
            {
                _recCount = 1;
                async_unlock_(id, ntf);
            }
            else
            {
                for (LinkedListNode<wait_node> it = _waitQueue.Last; null != it; it = it.Previous)
                {
                    if (it.Value._id == id)
                    {
                        _waitQueue.Remove(it);
                        break;
                    }
                }
                ntf();
            }
        }

        internal void async_lock(long id, Action ntf)
        {
            if (_strand.running_in_this_thread())
                if (!_mustTick) async_lock_(id, ntf);
                else _strand.add_last(() => async_lock_(id, ntf));
            else _strand.post(() => async_lock_(id, ntf));
        }

        internal void async_try_lock(long id, Action<bool> ntf)
        {
            if (_strand.running_in_this_thread())
                if (!_mustTick) async_try_lock_(id, ntf);
                else _strand.add_last(() => async_try_lock_(id, ntf));
            else _strand.post(() => async_try_lock_(id, ntf));
        }

        internal virtual void async_timed_lock(long id, int ms, Action<bool> ntf)
        {
            if (_strand.running_in_this_thread())
                if (!_mustTick) async_timed_lock_(id, ms, ntf);
                else _strand.add_last(() => async_timed_lock_(id, ms, ntf));
            else _strand.post(() => async_timed_lock_(id, ms, ntf));
        }

        internal virtual void async_unlock(long id, Action ntf)
        {
            if (_strand.running_in_this_thread())
                if (!_mustTick) async_unlock_(id, ntf);
                else _strand.add_last(() => async_unlock_(id, ntf));
            else _strand.post(() => async_unlock_(id, ntf));
        }

        internal virtual void async_cancel(long id, Action ntf)
        {
            if (_strand.running_in_this_thread())
                if (!_mustTick) async_cancel_(id, ntf);
                else _strand.add_last(() => async_cancel_(id, ntf));
            else _strand.post(() => async_cancel_(id, ntf));
        }

        public Task Lock()
        {
            return generator.mutex_lock(this);
        }

        public Task try_lock(async_result_wrap<bool> res)
        {
            return generator.mutex_try_lock(res, this);
        }

        public ValueTask<bool> try_lock()
        {
            return generator.mutex_try_lock(this);
        }

        public Task timed_lock(async_result_wrap<bool> res, int ms)
        {
            return generator.mutex_timed_lock(res, this, ms);
        }

        public ValueTask<bool> timed_lock(int ms)
        {
            return generator.mutex_timed_lock(this, ms);
        }

        public Task unlock()
        {
            return generator.mutex_unlock(this);
        }

        public shared_strand self_strand()
        {
            return _strand;
        }
    }

    public class go_shared_mutex : go_mutex
    {
        enum lock_status
        {
            st_shared,
            st_unique,
            st_upgrade
        };

        struct wait_node
        {
            public Action _ntf;
            public long _waitHostID;
            public lock_status _status;
        };

        class shared_count
        {
            public int _count = 0;
        };

        LinkedList<wait_node> _waitQueue;
        Dictionary<long, shared_count> _sharedMap;

        public go_shared_mutex(shared_strand strand) : base(strand)
        {
            _waitQueue = new LinkedList<wait_node>();
            _sharedMap = new Dictionary<long, shared_count>();
        }

        public go_shared_mutex() : base()
        {
            _waitQueue = new LinkedList<wait_node>();
            _sharedMap = new Dictionary<long, shared_count>();
        }

        protected override void async_lock_(long id, Action ntf)
        {
            if (0 == _sharedMap.Count && (0 == base._lockID || id == base._lockID))
            {
                base._lockID = id;
                base._recCount++;
                ntf();
            }
            else
            {
                _waitQueue.AddLast(new wait_node() { _ntf = ntf, _waitHostID = id, _status = lock_status.st_unique });
            }
        }

        protected override void async_try_lock_(long id, Action<bool> ntf)
        {
            if (0 == _sharedMap.Count && (0 == base._lockID || id == base._lockID))
            {
                base._lockID = id;
                base._recCount++;
                ntf(true);
            }
            else
            {
                ntf(false);
            }
        }

        protected override void async_timed_lock_(long id, int ms, Action<bool> ntf)
        {
            if (0 == _sharedMap.Count && (0 == base._lockID || id == base._lockID))
            {
                base._lockID = id;
                base._recCount++;
                ntf(true);
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(self_strand());
                LinkedListNode<wait_node> node = _waitQueue.AddLast(new wait_node()
                {
                    _ntf = delegate ()
                    {
                        timer.cancel();
                        ntf(true);
                    },
                    _waitHostID = id,
                    _status = lock_status.st_unique
                });
                timer.timeout(ms, delegate ()
                {
                    _waitQueue.Remove(node);
                    ntf(false);
                });
            }
            else
            {
                _waitQueue.AddLast(new wait_node() { _ntf = () => ntf(true), _waitHostID = id, _status = lock_status.st_unique });
            }
        }

        shared_count find_map(long id)
        {
            shared_count ct = null;
            if (!_sharedMap.TryGetValue(id, out ct))
            {
                ct = new shared_count();
                _sharedMap.Add(id, ct);
            }
            return ct;
        }

        private void async_lock_shared_(long id, Action ntf)
        {
            if (0 != _sharedMap.Count || 0 == base._lockID)
            {
                find_map(id)._count++;
                ntf();
            }
            else
            {
                _waitQueue.AddLast(new wait_node() { _ntf = ntf, _waitHostID = id, _status = lock_status.st_shared });
            }
        }

        private void async_lock_pess_shared_(long id, Action ntf)
        {
            if (0 == _waitQueue.Count && (0 != _sharedMap.Count || 0 == base._lockID))
            {
                find_map(id)._count++;
                ntf();
            }
            else
            {
                _waitQueue.AddLast(new wait_node() { _ntf = ntf, _waitHostID = id, _status = lock_status.st_shared });
            }
        }

        private void async_try_lock_shared_(long id, Action<bool> ntf)
        {
            if (0 != _sharedMap.Count || 0 == base._lockID)
            {
                find_map(id)._count++;
                ntf(true);
            }
            else
            {
                ntf(false);
            }
        }

        private void async_timed_lock_shared_(long id, int ms, Action<bool> ntf)
        {
            if (0 != _sharedMap.Count || 0 == base._lockID)
            {
                find_map(id)._count++;
                ntf(true);
            }
            else if (ms >= 0)
            {
                async_timer timer = new async_timer(self_strand());
                LinkedListNode<wait_node> node = _waitQueue.AddLast(new wait_node()
                {
                    _ntf = delegate ()
                    {
                        timer.cancel();
                        ntf(true);
                    },
                    _waitHostID = id,
                    _status = lock_status.st_shared
                });
                timer.timeout(ms, delegate ()
                {
                    _waitQueue.Remove(node);
                    ntf(false);
                });
            }
            else
            {
                _waitQueue.AddLast(new wait_node() { _ntf = () => ntf(true), _waitHostID = id, _status = lock_status.st_shared });
            }
        }

        private void async_lock_upgrade_(long id, Action ntf)
        {
            base.async_lock_(id, ntf);
        }

        private void async_try_lock_upgrade_(long id, Action<bool> ntf)
        {
            base.async_try_lock_(id, ntf);
        }

        protected override void async_unlock_(long id, Action ntf)
        {
            if (0 == --base._recCount && 0 != _waitQueue.Count)
            {
                _mustTick = true;
                wait_node queueFront = _waitQueue.First.Value;
                _waitQueue.RemoveFirst();
                queueFront._ntf();
                if (lock_status.st_shared == queueFront._status)
                {
                    base._lockID = 0;
                    find_map(queueFront._waitHostID)._count++;
                    for (LinkedListNode<wait_node> it = _waitQueue.First; null != it;)
                    {
                        if (lock_status.st_shared == it.Value._status)
                        {
                            find_map(it.Value._waitHostID)._count++;
                            it.Value._ntf();
                            LinkedListNode<wait_node> oit = it;
                            it = it.Next;
                            _waitQueue.Remove(oit);
                        }
                        else
                        {
                            it = it.Next;
                        }
                    }
                }
                else
                {
                    base._lockID = queueFront._waitHostID;
                    base._recCount++;
                }
                _mustTick = false;
            }
            ntf();
        }

        private void async_unlock_shared_(long id, Action ntf)
        {
            if (0 == --find_map(id)._count)
            {
                _sharedMap.Remove(id);
                if (0 == _sharedMap.Count && 0 != _waitQueue.Count)
                {
                    _mustTick = true;
                    wait_node queueFront = _waitQueue.First.Value;
                    _waitQueue.RemoveFirst();
                    queueFront._ntf();
                    if (lock_status.st_shared == queueFront._status)
                    {
                        base._lockID = 0;
                        find_map(queueFront._waitHostID)._count++;
                        for (LinkedListNode<wait_node> it = _waitQueue.First; null != it;)
                        {
                            if (lock_status.st_shared == it.Value._status)
                            {
                                find_map(it.Value._waitHostID)._count++;
                                it.Value._ntf();
                                LinkedListNode<wait_node> oit = it;
                                it = it.Next;
                                _waitQueue.Remove(oit);
                            }
                            else
                            {
                                it = it.Next;
                            }
                        }
                    }
                    else
                    {
                        base._lockID = queueFront._waitHostID;
                        base._recCount++;
                    }
                    _mustTick = false;
                }
            }
            ntf();
        }

        private void async_unlock_upgrade_(long id, Action ntf)
        {
            base.async_unlock_(id, ntf);
        }

        private void async_unlock_and_lock_shared_(long id, Action ntf)
        {
            async_unlock_(id, () => async_lock_shared_(id, ntf));
        }

        private void async_unlock_and_lock_upgrade_(long id, Action ntf)
        {
            async_unlock_and_lock_shared_(id, () => async_lock_upgrade_(id, ntf));
        }

        private void async_unlock_upgrade_and_lock_(long id, Action ntf)
        {
            async_unlock_upgrade_(id, () => async_unlock_shared_(id, () => async_lock_(id, ntf)));
        }

        private void async_unlock_shared_and_lock_(long id, Action ntf)
        {
            async_unlock_shared_(id, () => async_lock_(id, ntf));
        }

        protected override void async_cancel_(long id, Action ntf)
        {
            shared_count tempCount;
            if (_sharedMap.TryGetValue(id, out tempCount))
            {
                base.async_cancel_(id, nil_action.action);
                tempCount._count = 1;
                async_unlock_shared_(id, ntf);
            }
            else if (id == base._lockID)
            {
                base._recCount = 1;
                async_unlock_(id, ntf);
            }
            else
            {
                for (LinkedListNode<wait_node> it = _waitQueue.Last; null != it; it = it.Previous)
                {
                    if (it.Value._waitHostID == id)
                    {
                        _waitQueue.Remove(it);
                        break;
                    }
                }
                ntf();
            }
        }

        internal void async_lock_shared(long id, Action ntf)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_lock_shared_(id, ntf);
                else self_strand().add_last(() => async_lock_shared_(id, ntf));
            else self_strand().post(() => async_lock_shared_(id, ntf));
        }

        internal void async_lock_pess_shared(long id, Action ntf)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_lock_pess_shared_(id, ntf);
                else self_strand().add_last(() => async_lock_pess_shared_(id, ntf));
            else self_strand().post(() => async_lock_pess_shared_(id, ntf));
        }

        internal void async_try_lock_shared(long id, Action<bool> ntf)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_try_lock_shared_(id, ntf);
                else self_strand().add_last(() => async_try_lock_shared_(id, ntf));
            else self_strand().post(() => async_try_lock_shared_(id, ntf));
        }

        internal void async_timed_lock_shared(long id, int ms, Action<bool> ntf)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_timed_lock_shared_(id, ms, ntf);
                else self_strand().add_last(() => async_timed_lock_shared_(id, ms, ntf));
            else self_strand().post(() => async_timed_lock_shared_(id, ms, ntf));
        }

        internal void async_lock_upgrade(long id, Action ntf)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_lock_upgrade_(id, ntf);
                else self_strand().add_last(() => async_lock_upgrade_(id, ntf));
            else self_strand().post(() => async_lock_upgrade_(id, ntf));
        }

        internal void async_try_lock_upgrade(long id, Action<bool> ntf)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_try_lock_upgrade_(id, ntf);
                else self_strand().add_last(() => async_try_lock_upgrade_(id, ntf));
            else self_strand().post(() => async_try_lock_upgrade_(id, ntf));
        }

        internal void async_unlock_shared(long id, Action ntf)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_unlock_shared_(id, ntf);
                else self_strand().add_last(() => async_unlock_shared_(id, ntf));
            else self_strand().post(() => async_unlock_shared_(id, ntf));
        }

        internal void async_unlock_upgrade(long id, Action ntf)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_unlock_upgrade_(id, ntf);
                else self_strand().add_last(() => async_unlock_upgrade_(id, ntf));
            else self_strand().post(() => async_unlock_upgrade_(id, ntf));
        }

        internal void unlock_and_lock_shared(long id, Action ntf)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_unlock_and_lock_shared_(id, ntf);
                else self_strand().add_last(() => async_unlock_and_lock_shared_(id, ntf));
            else self_strand().post(() => async_unlock_and_lock_shared_(id, ntf));
        }

        internal void unlock_and_lock_upgrade(long id, Action ntf)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_unlock_and_lock_upgrade_(id, ntf);
                else self_strand().add_last(() => async_unlock_and_lock_upgrade_(id, ntf));
            else self_strand().post(() => async_unlock_and_lock_upgrade_(id, ntf));
        }

        internal void unlock_upgrade_and_lock(long id, Action ntf)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_unlock_upgrade_and_lock_(id, ntf);
                else self_strand().add_last(() => async_unlock_upgrade_and_lock_(id, ntf));
            else self_strand().post(() => async_unlock_upgrade_and_lock_(id, ntf));
        }

        internal void unlock_shared_and_lock(long id, Action ntf)
        {
            if (self_strand().running_in_this_thread())
                if (!_mustTick) async_unlock_shared_and_lock_(id, ntf);
                else self_strand().add_last(() => async_unlock_shared_and_lock_(id, ntf));
            else self_strand().post(() => async_unlock_shared_and_lock_(id, ntf));
        }

        public Task lock_shared()
        {
            return generator.mutex_lock_shared(this);
        }

        public Task lock_pess_shared()
        {
            return generator.mutex_lock_pess_shared(this);
        }

        public Task lock_upgrade()
        {
            return generator.mutex_lock_upgrade(this);
        }

        public Task try_lock_shared(async_result_wrap<bool> res)
        {
            return generator.mutex_try_lock_shared(res, this);
        }

        public ValueTask<bool> try_lock_shared()
        {
            return generator.mutex_try_lock_shared(this);
        }

        public Task try_lock_upgrade(async_result_wrap<bool> res)
        {
            return generator.mutex_try_lock_upgrade(res, this);
        }

        public ValueTask<bool> try_lock_upgrade()
        {
            return generator.mutex_try_lock_upgrade(this);
        }

        public Task timed_lock_shared(async_result_wrap<bool> res, int ms)
        {
            return generator.mutex_timed_lock_shared(res, this, ms);
        }

        public ValueTask<bool> timed_lock_shared(int ms)
        {
            return generator.mutex_timed_lock_shared(this, ms);
        }

        public Task unlock_shared()
        {
            return generator.mutex_unlock_shared(this);
        }

        public Task unlock_upgrade()
        {
            return generator.mutex_unlock_upgrade(this);
        }
    }

    public class go_condition_variable
    {
        shared_strand _strand;
        LinkedList<tuple<long, go_mutex, Action>> _waitQueue;
        bool _mustTick;

        public go_condition_variable(shared_strand strand)
        {
            _strand = strand;
            _waitQueue = new LinkedList<tuple<long, go_mutex, Action>>();
            _mustTick = false;
        }

        public go_condition_variable() : this(shared_strand.default_strand()) { }

        internal void async_wait(long id, go_mutex mutex, Action ntf)
        {
            mutex.async_unlock(id, delegate ()
            {
                if (_strand.running_in_this_thread())
                    if (!_mustTick) _waitQueue.AddLast(new tuple<long, go_mutex, Action>(id, mutex, () => mutex.async_lock(id, ntf)));
                    else _strand.add_last(() => _waitQueue.AddLast(new tuple<long, go_mutex, Action>(id, mutex, () => mutex.async_lock(id, ntf))));
                else _strand.post(() => _waitQueue.AddLast(new tuple<long, go_mutex, Action>(id, mutex, () => mutex.async_lock(id, ntf))));
            });
        }

        private void async_timed_wait_(long id, int ms, go_mutex mutex, Action<bool> ntf)
        {
            if (ms >= 0)
            {
                async_timer timer = new async_timer(_strand);
                LinkedListNode<tuple<long, go_mutex, Action>> node = _waitQueue.AddLast(new tuple<long, go_mutex, Action>(id, mutex, delegate ()
                {
                    timer.cancel();
                    mutex.async_lock(id, () => ntf(true));
                }));
                timer.timeout(ms, delegate ()
                {
                    _waitQueue.Remove(node);
                    mutex.async_lock(id, () => ntf(false));
                });
            }
            else
            {
                _waitQueue.AddLast(new tuple<long, go_mutex, Action>(id, mutex, () => mutex.async_lock(id, () => ntf(true))));
            }
        }

        internal void async_timed_wait(long id, int ms, go_mutex mutex, Action<bool> ntf)
        {
            mutex.async_unlock(id, delegate ()
            {
                if (_strand.running_in_this_thread())
                    if (!_mustTick) async_timed_wait_(id, ms, mutex, ntf);
                    else _strand.add_last(() => async_timed_wait_(id, ms, mutex, ntf));
                else _strand.post(() => async_timed_wait_(id, ms, mutex, ntf));
            });
        }

        private void notify_one_()
        {
            if (0 != _waitQueue.Count)
            {
                Action ntf = _waitQueue.First.Value.value3;
                _waitQueue.RemoveFirst();
                ntf();
            }
        }

        public void notify_one()
        {
            if (_strand.running_in_this_thread())
                if (!_mustTick) notify_one_();
                else _strand.add_last(() => notify_one_());
            else _strand.post(() => notify_one_());
        }

        private void notify_all_()
        {
            _mustTick = true;
            while (0 != _waitQueue.Count)
            {
                Action ntf = _waitQueue.First.Value.value3;
                _waitQueue.RemoveFirst();
                ntf();
            }
            _mustTick = false;
        }

        public void notify_all()
        {
            if (_strand.running_in_this_thread())
                if (!_mustTick) notify_all_();
                else _strand.add_last(() => notify_all_());
            else _strand.post(() => notify_all_());
        }

        private void async_cancel_(long id, Action ntf)
        {
            for (LinkedListNode<tuple<long, go_mutex, Action>> it = _waitQueue.First; null != it; it = it.Next)
            {
                if (id == it.Value.value1)
                {
                    go_mutex mtx = it.Value.value2;
                    mtx.async_cancel(id, ntf);
                    return;
                }
            }
            ntf();
        }

        internal void async_cancel(long id, Action ntf)
        {
            if (_strand.running_in_this_thread())
                if (!_mustTick) async_cancel_(id, ntf);
                else _strand.add_last(() => async_cancel_(id, ntf));
            else _strand.post(() => async_cancel_(id, ntf));
        }

        public Task wait(go_mutex mutex)
        {
            return generator.condition_wait(this, mutex);
        }

        public Task timed_wait(async_result_wrap<bool> res, go_mutex mutex, int ms)
        {
            return generator.condition_timed_wait(res, this, mutex, ms);
        }

        public ValueTask<bool> timed_wait(go_mutex mutex, int ms)
        {
            return generator.condition_timed_wait(this, mutex, ms);
        }

        public Task cancel()
        {
            return generator.condition_cancel(this);
        }
    }
}
