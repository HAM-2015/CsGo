using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Go
{
    public class mutex
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

        public mutex(shared_strand strand)
        {
            _strand = strand;
            _waitQueue = new LinkedList<wait_node>();
            _lockID = 0;
            _recCount = 0;
        }

        public mutex() : this(shared_strand.default_strand()) { }

        protected virtual void lock_(long id, Action ntf)
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

        protected virtual void try_lock_(long id, Action<bool> ntf)
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

        protected virtual void timed_lock_(long id, int ms, Action<bool> ntf)
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

        protected virtual void unlock_(long id, Action ntf)
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
                    queueFront._ntf();
                }
                else
                {
                    _lockID = 0;
                }
            }
            ntf();
        }

        protected virtual void cancel_(long id, Action ntf)
        {
            if (id == _lockID)
            {
                _recCount = 1;
                unlock_(id, ntf);
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

        internal void Lock(long id, Action ntf)
        {
            if (_strand.running_in_this_thread()) lock_(id, ntf);
            else _strand.post(() => lock_(id, ntf));
        }

        internal void try_lock(long id, Action<bool> ntf)
        {
            if (_strand.running_in_this_thread()) try_lock_(id, ntf);
            else _strand.post(() => try_lock_(id, ntf));
        }

        internal virtual void timed_lock(long id, int ms, Action<bool> ntf)
        {
            if (_strand.running_in_this_thread()) timed_lock_(id, ms, ntf);
            else _strand.post(() => timed_lock_(id, ms, ntf));
        }

        internal virtual void unlock(long id, Action ntf)
        {
            if (_strand.running_in_this_thread()) unlock_(id, ntf);
            else _strand.post(() => unlock_(id, ntf));
        }

        internal virtual void cancel(long id, Action ntf)
        {
            if (_strand.running_in_this_thread()) cancel_(id, ntf);
            else _strand.post(() => cancel_(id, ntf));
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

        protected override void lock_(long id, Action ntf)
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

        protected override void try_lock_(long id, Action<bool> ntf)
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

        protected override void timed_lock_(long id, int ms, Action<bool> ntf)
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

        private void lock_shared_(long id, Action ntf)
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

        private void lock_pess_shared_(long id, Action ntf)
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

        private void try_lock_shared_(long id, Action<bool> ntf)
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

        private void timed_lock_shared_(long id, int ms, Action<bool> ntf)
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

        private void lock_upgrade_(long id, Action ntf)
        {
            base.lock_(id, ntf);
        }

        private void try_lock_upgrade_(long id, Action<bool> ntf)
        {
            base.try_lock_(id, ntf);
        }

        protected override void unlock_(long id, Action ntf)
        {
            if (0 == --base._recCount && 0 != _waitQueue.Count)
            {
                wait_node queueFront = _waitQueue.First.Value;
                _waitQueue.RemoveFirst();
                self_strand().add_last(queueFront._ntf);
                if (lock_status.st_shared == queueFront._status)
                {
                    base._lockID = 0;
                    find_map(queueFront._waitHostID)._count++;
                    for (LinkedListNode<wait_node> it = _waitQueue.First; null != it;)
                    {
                        if (lock_status.st_shared == it.Value._status)
                        {
                            find_map(it.Value._waitHostID)._count++;
                            self_strand().add_last(it.Value._ntf);
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
            }
            ntf();
        }

        private void unlock_shared_(long id, Action ntf)
        {
            if (0 == --find_map(id)._count)
            {
                _sharedMap.Remove(id);
                if (0 == _sharedMap.Count && 0 != _waitQueue.Count)
                {
                    wait_node queueFront = _waitQueue.First.Value;
                    _waitQueue.RemoveFirst();
                    self_strand().add_last(queueFront._ntf);
                    if (lock_status.st_shared == queueFront._status)
                    {
                        base._lockID = 0;
                        find_map(queueFront._waitHostID)._count++;
                        for (LinkedListNode<wait_node> it = _waitQueue.First; null != it;)
                        {
                            if (lock_status.st_shared == it.Value._status)
                            {
                                find_map(it.Value._waitHostID)._count++;
                                self_strand().add_last(it.Value._ntf);
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
                }
            }
            ntf();
        }

        private void unlock_upgrade_(long id, Action ntf)
        {
            base.unlock_(id, ntf);
        }

        private void unlock_and_lock_shared_(long id, Action ntf)
        {
            unlock_(id, () => lock_shared_(id, ntf));
        }

        private void unlock_and_lock_upgrade_(long id, Action ntf)
        {
            unlock_and_lock_shared_(id, () => lock_upgrade_(id, ntf));
        }

        private void unlock_upgrade_and_lock_(long id, Action ntf)
        {
            unlock_upgrade_(id, () => unlock_shared_(id, () => lock_(id, ntf)));
        }

        private void unlock_shared_and_lock_(long id, Action ntf)
        {
            unlock_shared_(id, () => lock_(id, ntf));
        }

        protected override void cancel_(long id, Action ntf)
        {
            shared_count tempCount;
            if (_sharedMap.TryGetValue(id, out tempCount))
            {
                base.cancel_(id, nil_action.action);
                tempCount._count = 1;
                unlock_shared_(id, ntf);
            }
            else if (id == base._lockID)
            {
                base._recCount = 1;
                unlock_(id, ntf);
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

        internal void lock_shared(long id, Action ntf)
        {
            if (self_strand().running_in_this_thread()) lock_shared_(id, ntf);
            else self_strand().post(() => lock_shared_(id, ntf));
        }

        internal void lock_pess_shared(long id, Action ntf)
        {
            if (self_strand().running_in_this_thread()) lock_pess_shared_(id, ntf);
            else self_strand().post(() => lock_pess_shared_(id, ntf));
        }

        internal void try_lock_shared(long id, Action<bool> ntf)
        {
            if (self_strand().running_in_this_thread()) try_lock_shared_(id, ntf);
            else self_strand().post(() => try_lock_shared_(id, ntf));
        }

        internal void timed_lock_shared(long id, int ms, Action<bool> ntf)
        {
            if (self_strand().running_in_this_thread()) timed_lock_shared_(id, ms, ntf);
            else self_strand().post(() => timed_lock_shared_(id, ms, ntf));
        }

        internal void lock_upgrade(long id, Action ntf)
        {
            if (self_strand().running_in_this_thread()) lock_upgrade_(id, ntf);
            else self_strand().post(() => lock_upgrade_(id, ntf));
        }

        internal void try_lock_upgrade(long id, Action<bool> ntf)
        {
            if (self_strand().running_in_this_thread()) try_lock_upgrade_(id, ntf);
            else self_strand().post(() => try_lock_upgrade_(id, ntf));
        }

        internal void unlock_shared(long id, Action ntf)
        {
            if (self_strand().running_in_this_thread()) unlock_shared_(id, ntf);
            else self_strand().post(() => unlock_shared_(id, ntf));
        }

        internal void unlock_upgrade(long id, Action ntf)
        {
            if (self_strand().running_in_this_thread()) unlock_upgrade_(id, ntf);
            else self_strand().post(() => unlock_upgrade_(id, ntf));
        }

        internal void unlock_and_lock_shared(long id, Action ntf)
        {
            if (self_strand().running_in_this_thread()) unlock_and_lock_shared_(id, ntf);
            else self_strand().post(() => unlock_and_lock_shared_(id, ntf));
        }

        internal void unlock_and_lock_upgrade(long id, Action ntf)
        {
            if (self_strand().running_in_this_thread()) unlock_and_lock_upgrade_(id, ntf);
            else self_strand().post(() => unlock_and_lock_upgrade_(id, ntf));
        }

        internal void unlock_upgrade_and_lock(long id, Action ntf)
        {
            if (self_strand().running_in_this_thread()) unlock_upgrade_and_lock_(id, ntf);
            else self_strand().post(() => unlock_upgrade_and_lock_(id, ntf));
        }

        internal void unlock_shared_and_lock(long id, Action ntf)
        {
            if (self_strand().running_in_this_thread()) unlock_shared_and_lock_(id, ntf);
            else self_strand().post(() => unlock_shared_and_lock_(id, ntf));
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

        public condition_variable() : this(shared_strand.default_strand()) { }

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
                    if (ms >= 0)
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
                        _waitQueue.AddLast(new tuple<long, mutex, Action>(id, mutex, delegate ()
                        {
                            mutex.Lock(id, () => ntf(true));
                        }));
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
