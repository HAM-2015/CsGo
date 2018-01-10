using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Go
{
    public class mutex
    {
        struct wait_node
        {
            public Action<chan_async_state> _ntf;
            public long _id;
        }

        shared_strand _strand;
        LinkedList<wait_node> _waitQueue;
        protected long _lockID;
        protected int _recCount;

        public mutex(shared_strand strand)
        {
            init(strand);
        }

        public mutex()
        {
            shared_strand strand = generator.self_strand();
            init(null != strand ? strand : new shared_strand());
        }

        private void init(shared_strand strand)
        {
            _strand = strand;
            _waitQueue = new LinkedList<wait_node>();
            _lockID = 0;
            _recCount = 0;
        }

        internal virtual void Lock(long id, Action ntf)
        {
            _strand.distribute(delegate ()
            {
                if (0 == _lockID || id == _lockID)
                {
                    _lockID = id;
                    _recCount++;
                    ntf();
                }
                else
                {
                    _waitQueue.AddLast(new wait_node() { _ntf = (chan_async_state) => ntf(), _id = id });
                }
            });
        }

        internal virtual void try_lock(long id, Action<chan_async_state> ntf)
        {
            _strand.distribute(delegate ()
            {
                if (0 == _lockID || id == _lockID)
                {
                    _lockID = id;
                    _recCount++;
                    ntf(chan_async_state.async_ok);
                }
                else
                {
                    ntf(chan_async_state.async_fail);
                }
            });
        }

        internal virtual void timed_lock(long id, int ms, Action<chan_async_state> ntf)
        {
            _strand.distribute(delegate ()
            {
                if (0 == _lockID || id == _lockID)
                {
                    _lockID = id;
                    _recCount++;
                    ntf(chan_async_state.async_ok);
                }
                else if (ms > 0)
                {
                    async_timer timer = new async_timer(_strand);
                    LinkedListNode<wait_node> it = _waitQueue.AddLast(new wait_node()
                    {
                        _ntf = delegate (chan_async_state state)
                        {
                            timer.cancel();
                            ntf(state);
                        },
                        _id = id
                    });
                    timer.timeout(ms, delegate ()
                    {
                        Action<chan_async_state> waitNtf = it.Value._ntf;
                        _waitQueue.Remove(it);
                        waitNtf(chan_async_state.async_overtime);
                    });
                }
                else
                {
                    ntf(chan_async_state.async_overtime);
                }
            });
        }

        internal virtual void unlock(long id, Action ntf)
        {
            _strand.distribute(delegate ()
            {
#if DEBUG
                Trace.Assert(id == _lockID);
#endif
                if (0 == --_recCount)
                {
                    if (0 != _waitQueue.Count)
                    {
                        _recCount = 1;
                        wait_node queueFront = _waitQueue.First.Value;
                        _waitQueue.RemoveFirst();
                        _lockID = queueFront._id;
                        queueFront._ntf(chan_async_state.async_ok);
                    }
                    else
                    {
                        _lockID = 0;
                    }
                }
                ntf();
            });
        }

        internal virtual void cancel(long id, Action ntf)
        {
            _strand.distribute(delegate ()
            {
                if (id == _lockID)
                {
                    _recCount = 1;
                    unlock(id, ntf);
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
            });
        }

        public shared_strand self_strand()
        {
            return _strand;
        }
    }

    public class shared_mutex : mutex
    {
        enum lock_status
        {
            st_shared,
            st_unique,
            st_upgrade
        };

        struct wait_node
        {
            public Action<chan_async_state> _ntf;
            public long _waitHostID;
            public lock_status _status;
        };

        class shared_count
        {
            public int _count = 0;
        };

        LinkedList<wait_node> _waitQueue;
        Dictionary<long, shared_count> _sharedMap;

        public shared_mutex(shared_strand strand) : base(strand)
        {
            _waitQueue = new LinkedList<wait_node>();
            _sharedMap = new Dictionary<long, shared_count>();
        }

        public shared_mutex() : base()
        {
            _waitQueue = new LinkedList<wait_node>();
            _sharedMap = new Dictionary<long, shared_count>();
        }

        internal override void Lock(long id, Action ntf)
        {
            self_strand().distribute(delegate ()
            {
                if (0 == _sharedMap.Count && (0 == base._lockID || id == base._lockID))
                {
                    base._lockID = id;
                    base._recCount++;
                    ntf();
                }
                else
                {
                    _waitQueue.AddLast(new wait_node() { _ntf = (chan_async_state) => ntf(), _waitHostID = id, _status = lock_status.st_unique });
                }
            });
        }

        internal override void try_lock(long id, Action<chan_async_state> ntf)
        {
            self_strand().distribute(delegate ()
            {
                if (0 == _sharedMap.Count && (0 == base._lockID || id == base._lockID))
                {
                    base._lockID = id;
                    base._recCount++;
                    ntf(chan_async_state.async_ok);
                }
                else
                {
                    ntf(chan_async_state.async_fail);
                }
            });
        }

        internal override void timed_lock(long id, int ms, Action<chan_async_state> ntf)
        {
            self_strand().distribute(delegate ()
            {
                if (0 == _sharedMap.Count && (0 == base._lockID || id == base._lockID))
                {
                    base._lockID = id;
                    base._recCount++;
                    ntf(chan_async_state.async_ok);
                }
                else if (ms > 0)
                {
                    async_timer timer = new async_timer(self_strand());
                    LinkedListNode<wait_node> it = _waitQueue.AddLast(new wait_node()
                    {
                        _ntf = delegate (chan_async_state state)
                        {
                            timer.cancel();
                            ntf(state);
                        },
                        _waitHostID = id,
                        _status = lock_status.st_unique
                    });
                    timer.timeout(ms, delegate ()
                    {
                        Action<chan_async_state> waitNtf = it.Value._ntf;
                        _waitQueue.Remove(it);
                        waitNtf(chan_async_state.async_overtime);
                    });
                }
                else
                {
                    ntf(chan_async_state.async_overtime);
                }
            });
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

        internal void lock_shared(long id, Action ntf)
        {
            self_strand().distribute(delegate ()
            {
                if (0 != _sharedMap.Count || 0 == base._lockID)
                {
                    find_map(id)._count++;
                    ntf();
                }
                else
                {
                    _waitQueue.AddLast(new wait_node() { _ntf = (chan_async_state) => ntf(), _waitHostID = id, _status = lock_status.st_shared });
                }
            });
        }

        internal void lock_pess_shared(long id, Action ntf)
        {
            self_strand().distribute(delegate ()
            {
                if (0 == _waitQueue.Count && (0 != _sharedMap.Count || 0 == base._lockID))
                {
                    find_map(id)._count++;
                    ntf();
                }
                else
                {
                    _waitQueue.AddLast(new wait_node() { _ntf = (chan_async_state) => ntf(), _waitHostID = id, _status = lock_status.st_shared });
                }
            });
        }

        internal void try_lock_shared(long id, Action<chan_async_state> ntf)
        {
            self_strand().distribute(delegate ()
            {
                if (0 != _sharedMap.Count || 0 == base._lockID)
                {
                    find_map(id)._count++;
                    ntf(chan_async_state.async_ok);
                }
                else
                {
                    ntf(chan_async_state.async_fail);
                }
            });
        }

        internal void timed_lock_shared(long id, int ms, Action<chan_async_state> ntf)
        {
            self_strand().distribute(delegate ()
            {
                if (0 != _sharedMap.Count || 0 == base._lockID)
                {
                    find_map(id)._count++;
                    ntf(chan_async_state.async_ok);
                }
                else if (ms > 0)
                {
                    async_timer timer = new async_timer(self_strand());
                    LinkedListNode<wait_node> it = _waitQueue.AddLast(new wait_node()
                    {
                        _ntf = delegate (chan_async_state state)
                        {
                            timer.cancel();
                            ntf(state);
                        },
                        _waitHostID = id,
                        _status = lock_status.st_shared
                    });
                    timer.timeout(ms, delegate ()
                    {
                        Action<chan_async_state> waitNtf = it.Value._ntf;
                        _waitQueue.Remove(it);
                        waitNtf(chan_async_state.async_overtime);
                    });
                }
                else
                {
                    ntf(chan_async_state.async_overtime);
                }
            });
        }

        internal void lock_upgrade(long id, Action ntf)
        {
            base.Lock(id, ntf);
        }

        internal void try_lock_upgrade(long id, Action<chan_async_state> ntf)
        {
            base.try_lock(id, ntf);
        }

        internal override void unlock(long id, Action ntf)
        {
            self_strand().distribute(delegate ()
            {
                if (0 == --base._recCount && 0 != _waitQueue.Count)
                {
                    LinkedList<Action<chan_async_state>> ntfs = new LinkedList<Action<chan_async_state>>();
                    wait_node queueFront = _waitQueue.First.Value;
                    _waitQueue.RemoveFirst();
                    ntfs.AddLast(queueFront._ntf);
                    if (lock_status.st_shared == queueFront._status)
                    {
                        base._lockID = 0;
                        find_map(queueFront._waitHostID)._count++;
                        for (LinkedListNode<wait_node> it = _waitQueue.First; null != it;)
                        {
                            if (lock_status.st_shared == it.Value._status)
                            {
                                find_map(it.Value._waitHostID)._count++;
                                ntfs.AddLast(it.Value._ntf);
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
                    while (0 != ntfs.Count)
                    {
                        ntfs.First.Value(chan_async_state.async_ok);
                        ntfs.RemoveFirst();
                    }
                }
                ntf();
            });
        }

        internal void unlock_shared(long id, Action ntf)
        {
            self_strand().distribute(delegate ()
            {
                if (0 == --find_map(id)._count)
                {
                    _sharedMap.Remove(id);
                    if (0 == _sharedMap.Count && 0 != _waitQueue.Count)
                    {
                        LinkedList<Action<chan_async_state>> ntfs = new LinkedList<Action<chan_async_state>>();
                        wait_node queueFront = _waitQueue.First.Value;
                        _waitQueue.RemoveFirst();
                        ntfs.AddLast(queueFront._ntf);
                        if (lock_status.st_shared == queueFront._status)
                        {
                            base._lockID = 0;
                            find_map(queueFront._waitHostID)._count++;
                            for (LinkedListNode<wait_node> it = _waitQueue.First; null != it;)
                            {
                                if (lock_status.st_shared == it.Value._status)
                                {
                                    find_map(it.Value._waitHostID)._count++;
                                    ntfs.AddLast(it.Value._ntf);
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
                        while (0 != ntfs.Count)
                        {
                            ntfs.First.Value(chan_async_state.async_ok);
                            ntfs.RemoveFirst();
                        }
                    }
                }
                ntf();
            });
        }

        internal void unlock_upgrade(long id, Action ntf)
        {
            base.unlock(id, ntf);
        }

        internal void unlock_and_lock_shared(long id, Action ntf)
        {
            unlock(id, () => lock_shared(id, ntf));
        }

        internal void unlock_and_lock_upgrade(long id, Action ntf)
        {
            unlock_and_lock_shared(id, () => lock_upgrade(id, ntf));
        }

        internal void unlock_upgrade_and_lock(long id, Action ntf)
        {
            unlock_upgrade(id, () => unlock_shared(id, () => Lock(id, ntf)));
        }

        internal void unlock_shared_and_lock(long id, Action ntf)
        {
            unlock_shared(id, () => Lock(id, ntf));
        }

        internal override void cancel(long id, Action ntf)
        {
            self_strand().distribute(delegate ()
            {
                shared_count tempCount;
                if (_sharedMap.TryGetValue(id, out tempCount))
                {
                    base.cancel(id, nil_action.action);
                    tempCount._count = 1;
                    unlock_shared(id, ntf);
                }
                else if (id == base._lockID)
                {
                    base._recCount = 1;
                    unlock(id, ntf);
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
            });
        }
    }

    public class condition_variable
    {
        shared_strand _strand;
        LinkedList<tuple<long, mutex, Action>> _waitQueue;

        public condition_variable(shared_strand strand)
        {
            _strand = strand;
            _waitQueue = new LinkedList<tuple<long, mutex, Action>>();
        }

        public condition_variable()
        {
            shared_strand strand = generator.self_strand();
            init(null != strand ? strand : new shared_strand());
        }

        private void init(shared_strand strand)
        {
            _strand = strand;
            _waitQueue = new LinkedList<tuple<long, mutex, Action>>();
        }

        internal void wait(long id, mutex mutex, Action ntf)
        {
            mutex.unlock(id, delegate ()
            {
                _strand.distribute(delegate ()
                {
                    _waitQueue.AddLast(new tuple<long, mutex, Action>(id, mutex, delegate ()
                    {
                        mutex.Lock(id, ntf);
                    }));
                });
            });
        }

        internal void timed_wait(long id, int ms, mutex mutex, Action<bool> ntf)
        {
            mutex.unlock(id, delegate ()
            {
                _strand.distribute(delegate ()
                {
                    if (ms > 0)
                    {
                        async_timer timer = new async_timer(_strand);
                        LinkedListNode<tuple<long, mutex, Action>> node = _waitQueue.AddLast(new tuple<long, mutex, Action>(id, mutex, delegate ()
                        {
                            timer.cancel();
                            mutex.Lock(id, delegate ()
                            {
                                ntf(true);
                            });
                        }));
                        timer.timeout(ms, delegate ()
                        {
                            _waitQueue.Remove(node);
                            mutex.Lock(id, delegate ()
                            {
                                ntf(false);
                            });
                        });
                    }
                    else
                    {
                        mutex.Lock(id, delegate ()
                        {
                            ntf(false);
                        });
                    }
                });
            });
        }

        public void notify_one()
        {
            _strand.distribute(delegate ()
            {
                if (_waitQueue.Count > 0)
                {
                    Action ntf = _waitQueue.First.Value.value3;
                    _waitQueue.RemoveFirst();
                    ntf();
                }
            });
        }

        public void notify_all()
        {
            _strand.distribute(delegate ()
            {
                if (_waitQueue.Count > 0)
                {
                    LinkedList<tuple<long, mutex, Action>> waitQueue = _waitQueue;
                    _waitQueue = new LinkedList<tuple<long, mutex, Action>>();
                    for (LinkedListNode<tuple<long, mutex, Action>> it = waitQueue.First; null != it; it = it.Next)
                    {
                        it.Value.value3.Invoke();
                    }
                }
            });
        }

        internal void cancel(long id, Action ntf)
        {
            _strand.distribute(delegate ()
            {
                for (LinkedListNode<tuple<long, mutex, Action>> it = _waitQueue.First; null != it; it = it.Next)
                {
                    if (id == it.Value.value1)
                    {
                        it.Value.value2.cancel(id, ntf);
                        return;
                    }
                }
                ntf();
            });
        }
    }
}
