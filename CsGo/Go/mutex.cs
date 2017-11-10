using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Go
{
    public abstract class mutex_base
    {
        public abstract void Lock(long id, functional.func ntf);
        public abstract void try_lock(long id, functional.func<chan_async_state> ntf);
        public abstract void timed_lock(long id, int ms, functional.func<chan_async_state> ntf);
        public abstract void unlock(long id, functional.func ntf);
        public abstract shared_strand self_strand();
    }

    public class mutex : mutex_base
    {
        class wait_node
        {
            public functional.func<chan_async_state> _ntf;
            public long _id;

            public wait_node(functional.func<chan_async_state> ntf, long id)
            {
                _ntf = ntf;
                _id = id;
            }
        }

        shared_strand _strand;
        LinkedList<wait_node> _waitQueue;
        public long _lockID;
        public int _recCount;

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

        public override void Lock(long id, functional.func ntf)
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
                    _waitQueue.AddLast(new wait_node((chan_async_state) => ntf(), id));
                }
            });
        }

        public override void try_lock(long id, functional.func<chan_async_state> ntf)
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

        public override void timed_lock(long id, int ms, functional.func<chan_async_state> ntf)
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
                    LinkedListNode<wait_node>  it = _waitQueue.AddLast(new wait_node(delegate (chan_async_state state)
                    {
                        timer.cancel();
                        ntf(state);
                    }, id));
                    timer.timeout(ms, delegate ()
                    {
                        functional.func<chan_async_state> waitNtf = it.Value._ntf;
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

        public override void unlock(long id, functional.func ntf)
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

        public override shared_strand self_strand()
        {
            return _strand;
        }
    }

    public class shared_mutex : mutex_base
    {
        enum lock_status
        {
            st_shared,
            st_unique,
            st_upgrade
        };

        class wait_node
        {
            public functional.func<chan_async_state> _ntf;
            public long _waitHostID;
            public lock_status _status;

            public wait_node(functional.func<chan_async_state> ntf, long id, lock_status st)
            {
                _ntf = ntf;
                _waitHostID = id;
                _status = st;
            }
        };

        class shared_count
        {
            public int _count = 0;
        };

        mutex _upgradeMutex;
        LinkedList<wait_node> _waitQueue;
        SortedDictionary<long, shared_count> _sharedMap;

        public shared_mutex(shared_strand strand)
        {
            init(strand);
        }

        public shared_mutex()
        {
            shared_strand strand = generator.self_strand();
            init(null != strand ? strand : new shared_strand());
        }

        private void init(shared_strand strand)
        {
            _upgradeMutex = new mutex(strand);
            _waitQueue = new LinkedList<wait_node>();
            _sharedMap = new SortedDictionary<long, shared_count>();
        }

        public override shared_strand self_strand()
        {
            return _upgradeMutex.self_strand();
        }

        public override void Lock(long id, functional.func ntf)
        {
            self_strand().distribute(delegate ()
            {
                if (0 == _sharedMap.Count && (0 == _upgradeMutex._lockID || id == _upgradeMutex._lockID))
                {
                    _upgradeMutex._lockID = id;
                    _upgradeMutex._recCount++;
                    ntf();
                }
                else
                {
                    _waitQueue.AddLast(new wait_node((chan_async_state) => ntf(), id, lock_status.st_unique));
                }
            });
        }

        public override void try_lock(long id, functional.func<chan_async_state> ntf)
        {
            self_strand().distribute(delegate ()
            {
                if (0 == _sharedMap.Count && (0 == _upgradeMutex._lockID || id == _upgradeMutex._lockID))
                {
                    _upgradeMutex._lockID = id;
                    _upgradeMutex._recCount++;
                    ntf(chan_async_state.async_ok);
                }
                else
                {
                    ntf(chan_async_state.async_fail);
                }
            });
        }

        public override void timed_lock(long id, int ms, functional.func<chan_async_state> ntf)
        {
            self_strand().distribute(delegate ()
            {
                if (0 == _sharedMap.Count && (0 == _upgradeMutex._lockID || id == _upgradeMutex._lockID))
                {
                    _upgradeMutex._lockID = id;
                    _upgradeMutex._recCount++;
                    ntf(chan_async_state.async_ok);
                }
                else if (ms > 0)
                {
                    async_timer timer = new async_timer(self_strand());
                    LinkedListNode<wait_node>  it = _waitQueue.AddLast(new wait_node(delegate (chan_async_state state)
                    {
                        timer.cancel();
                        ntf(state);
                    }, id, lock_status.st_unique));
                    timer.timeout(ms, delegate ()
                    {
                        functional.func<chan_async_state> waitNtf = it.Value._ntf;
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

        public void lock_shared(long id, functional.func ntf)
        {
            self_strand().distribute(delegate ()
            {
                if (0 != _sharedMap.Count || 0 == _upgradeMutex._lockID)
                {
                    find_map(id)._count++;
                    ntf();
                }
                else
                {
                    _waitQueue.AddLast(new wait_node((chan_async_state) => ntf(), id, lock_status.st_shared));
                }
            });
        }

        public void lock_pess_shared(long id, functional.func ntf)
        {
            self_strand().distribute(delegate ()
            {
                if (0 == _waitQueue.Count && (0 != _sharedMap.Count || 0 == _upgradeMutex._lockID))
                {
                    find_map(id)._count++;
                    ntf();
                }
                else
                {
                    _waitQueue.AddLast(new wait_node((chan_async_state) => ntf(), id, lock_status.st_shared));
                }
            });
        }

        public void try_lock_shared(long id, functional.func<chan_async_state> ntf)
        {
            self_strand().distribute(delegate ()
            {
                if (0 != _sharedMap.Count || 0 == _upgradeMutex._lockID)
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

        public void timed_lock_shared(long id, int ms, functional.func<chan_async_state> ntf)
        {
            self_strand().distribute(delegate ()
            {
                if (0 != _sharedMap.Count || 0 == _upgradeMutex._lockID)
                {
                    find_map(id)._count++;
                    ntf(chan_async_state.async_ok);
                }
                else if (ms > 0)
                {
                    async_timer timer = new async_timer(self_strand());
                    LinkedListNode<wait_node> it = _waitQueue.AddLast(new wait_node(delegate (chan_async_state state)
                    {
                        timer.cancel();
                        ntf(state);
                    }, id, lock_status.st_shared));
                    timer.timeout(ms, delegate ()
                    {
                        functional.func<chan_async_state> waitNtf = it.Value._ntf;
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

        public void lock_upgrade(long id, functional.func ntf)
        {
            _upgradeMutex.Lock(id, ntf);
        }

        public void try_lock_upgrade(long id, functional.func<chan_async_state> ntf)
        {
            _upgradeMutex.try_lock(id, ntf);
        }

        public override void unlock(long id, functional.func ntf)
        {
            self_strand().distribute(delegate ()
            {
                if (0 == --_upgradeMutex._recCount && 0 != _waitQueue.Count)
                {
                    LinkedList<functional.func<chan_async_state>> ntfs = new LinkedList<functional.func<chan_async_state>>();
                    wait_node queueFront = _waitQueue.First.Value;
                    _waitQueue.RemoveFirst();
                    ntfs.AddLast(queueFront._ntf);
                    if (lock_status.st_shared == queueFront._status)
                    {
                        _upgradeMutex._lockID = 0;
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
                        _upgradeMutex._lockID = queueFront._waitHostID;
                        _upgradeMutex._recCount++;
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

        public void unlock_shared(long id, functional.func ntf)
        {
            self_strand().distribute(delegate ()
            {
                if (0 == --find_map(id)._count)
                {
                    _sharedMap.Remove(id);
                    if (0 == _sharedMap.Count && 0 != _waitQueue.Count)
                    {
                        LinkedList<functional.func<chan_async_state>> ntfs = new LinkedList<functional.func<chan_async_state>>();
                        wait_node queueFront = _waitQueue.First.Value;
                        _waitQueue.RemoveFirst();
                        ntfs.AddLast(queueFront._ntf);
                        if (lock_status.st_shared == queueFront._status)
                        {
                            _upgradeMutex._lockID = 0;
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
                            _upgradeMutex._lockID = queueFront._waitHostID;
                            _upgradeMutex._recCount++;
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

        public void unlock_upgrade(long id, functional.func ntf)
        {
            _upgradeMutex.unlock(id, ntf);
        }

        public void unlock_and_lock_shared(long id, functional.func ntf)
        {
            unlock(id, () => lock_shared(id, ntf));
        }

        public void unlock_and_lock_upgrade(long id, functional.func ntf)
        {
            unlock_and_lock_shared(id, () => lock_upgrade(id, ntf));
        }

        public void unlock_upgrade_and_lock(long id, functional.func ntf)
        {
            unlock_upgrade(id, () => unlock_shared(id, () => Lock(id, ntf)));
        }

        public void unlock_shared_and_lock(long id, functional.func ntf)
        {
            unlock_shared(id, () => Lock(id, ntf));
        }
    }

    public class condition_variable
    {
        shared_strand _strand;
        LinkedList<functional.func> _waitQueue;

        public condition_variable(shared_strand strand)
        {
            _strand = strand;
            _waitQueue = new LinkedList<functional.func>();
        }

        public condition_variable()
        {
            shared_strand strand = generator.self_strand();
            init(null != strand ? strand : new shared_strand());
        }

        private void init(shared_strand strand)
        {
            _strand = strand;
            _waitQueue = new LinkedList<functional.func>();
        }

        public void wait(long id, mutex_base mutex, functional.func ntf)
        {
            _strand.distribute(delegate ()
            {
                _waitQueue.AddLast(delegate ()
                {
                    mutex.Lock(id, ntf);
                });
            });
        }

        public void timed_wait(long id, int ms, mutex_base mutex, functional.func<chan_async_state> ntf)
        {
            _strand.distribute(delegate ()
            {
                if (ms > 0)
                {
                    async_timer timer = new async_timer(_strand);
                    LinkedListNode<functional.func> node = _waitQueue.AddLast(delegate ()
                    {
                        timer.cancel();
                        mutex.Lock(id, delegate ()
                        {
                            ntf(chan_async_state.async_ok);
                        });
                    });
                    timer.timeout(ms, delegate ()
                    {
                        _waitQueue.Remove(node);
                        ntf(chan_async_state.async_overtime);
                    });
                }
                else
                {
                    ntf(chan_async_state.async_overtime);
                }
            });
        }

        public void notify_one()
        {
            _strand.distribute(delegate ()
            {
                if (_waitQueue.Count > 0)
                {
                    functional.func ntf = _waitQueue.First.Value;
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
                    LinkedList<functional.func> waitQueue = _waitQueue;
                    _waitQueue = new LinkedList<functional.func>();
                    while (waitQueue.Count > 0)
                    {
                        waitQueue.First.Value.Invoke();
                        waitQueue.RemoveFirst();
                    }
                }
            });
        }
    }
}
