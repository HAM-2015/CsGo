using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Go
{
    public class system_tick
    {
        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);
        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long frequency);

        private static system_tick _pcCycle = new system_tick();

        private double _sCycle;
        private double _msCycle;
        private double _usCycle;

        private system_tick()
        {
            long frep = 0;
            if (!QueryPerformanceFrequency(out frep))
            {
                _sCycle = 0;
                _msCycle = 0;
                _usCycle = 0;
                return;
            }
            _sCycle = 1.0 / (double)frep;
            _msCycle = 1000.0 / (double)frep;
            _usCycle = 1000000.0 / (double)frep;
        }

        public static long get_tick_us()
        {
            long quadPart = 0;
            QueryPerformanceCounter(out quadPart);
            return (long)((double)quadPart * _pcCycle._usCycle);
        }

        public static long get_tick_ms()
        {
            long quadPart = 0;
            QueryPerformanceCounter(out quadPart);
            return (long)((double)quadPart * _pcCycle._msCycle);
        }

        public static int get_tick_s()
        {
            long quadPart = 0;
            QueryPerformanceCounter(out quadPart);
            return (int)((double)quadPart * _pcCycle._sCycle);
        }
    }

    public class async_timer
    {
        struct steady_timer_handle
        {
            public long absus;
            public long period;
            public MapNode<long, async_timer> node;
        }

        public class steady_timer
        {
            struct waitable_event_handle
            {
                public int id;
                public steady_timer steadyTimer;

                public waitable_event_handle(int i, steady_timer h)
                {
                    id = i;
                    steadyTimer = h;
                }
            }

            class waitable_timer
            {
                [DllImport("kernel32.dll")]
                private static extern int CreateWaitableTimer(int lpTimerAttributes, int bManualReset, int lpTimerName);
                [DllImport("kernel32.dll")]
                private static extern int SetWaitableTimer(int hTimer, ref long pDueTime, int lPeriod, int pfnCompletionRoutine, int lpArgToCompletionRoutine, int fResume);
                [DllImport("kernel32.dll")]
                private static extern int CloseHandle(int hObject);
                [DllImport("kernel32.dll")]
                private static extern int WaitForSingleObject(int hHandle, int dwMilliseconds);
                [DllImport("NtDll.dll")]
                private static extern int NtQueryTimerResolution(out uint MaximumTime, out uint MinimumTime, out uint CurrentTime);
                [DllImport("NtDll.dll")]
                private static extern int NtSetTimerResolution(uint DesiredTime, uint SetResolution, out uint ActualTime);

                static public readonly waitable_timer timer = new waitable_timer();

                bool _exited;
                int _timerHandle;
                long _expireTime;
                Thread _timerThread;
                work_engine _workEngine;
                work_strand _workStrand;
                Map<long, waitable_event_handle> _eventsQueue;

                waitable_timer()
                {
                    _exited = false;
                    _expireTime = long.MaxValue;
                    _eventsQueue = new Map<long, waitable_event_handle>(true);
                    _timerHandle = CreateWaitableTimer(0, 0, 0);
                    _workEngine = new work_engine();
                    _workStrand = new work_strand(_workEngine);
                    _timerThread = new Thread(timerThread);
                    _timerThread.Priority = ThreadPriority.Highest;
                    _timerThread.IsBackground = true;
                    _workEngine.run(1, ThreadPriority.Highest, true);
                    _timerThread.Start();
                    uint MaximumTime = 0, MinimumTime = 0, CurrentTime = 0, ActualTime = 0;
                    if (0 == NtQueryTimerResolution(out MaximumTime, out MinimumTime, out CurrentTime))
                    {
                        NtSetTimerResolution(MinimumTime, 1, out ActualTime);
                    }
                }

                ~waitable_timer()
                {
                    /*_workStrand.post(delegate ()
                    {
                        _exited = true;
                        long sleepTime = 0;
                        SetWaitableTimer(_timerHandle, ref sleepTime, 0, 0, 0, 0);
                    });
                    _timerThread.Join();
                    _workEngine.stop();*/
                    CloseHandle(_timerHandle);
                }

                public void appendEvent(long absus, waitable_event_handle eventHandle)
                {
                    _workStrand.post(delegate ()
                    {
                        eventHandle.steadyTimer._waitableNode = _eventsQueue.Insert(absus, eventHandle);
                        if (absus < _expireTime)
                        {
                            _expireTime = absus;
                            long sleepTime = -(absus - system_tick.get_tick_us()) * 10;
                            sleepTime = sleepTime < 0 ? sleepTime : 0;
                            SetWaitableTimer(_timerHandle, ref sleepTime, 0, 0, 0, 0);
                        }
                    });
                }

                public void removeEvent(steady_timer steadyTime)
                {
                    _workStrand.post(delegate ()
                    {
                        if (null != steadyTime._waitableNode)
                        {
                            _eventsQueue.Remove(steadyTime._waitableNode);
                            steadyTime._waitableNode = null;
                            if (0 == _eventsQueue.Count)
                            {
                                _expireTime = long.MaxValue;
                            }
                        }
                    });
                }

                private void timerThread()
                {
                    while (0 == WaitForSingleObject(_timerHandle, -1) && !_exited)
                    {
                        _workStrand.post(timerComplete);
                    }
                }

                private void timerComplete()
                {
                    _expireTime = long.MaxValue;
                    while (0 != _eventsQueue.Count)
                    {
                        MapNode<long, waitable_event_handle> first = _eventsQueue.First;
                        long absus = first.Key;
                        long ct = system_tick.get_tick_us();
                        if (absus > ct)
                        {
                            _expireTime = absus;
                            long sleepTime = -(absus - ct) * 10;
                            SetWaitableTimer(_timerHandle, ref sleepTime, 0, 0, 0, 0);
                            break;
                        }
                        first.Value.steadyTimer._waitableNode = null;
                        first.Value.steadyTimer.timer_handler(first.Value.id);
                        _eventsQueue.Remove(first);
                    }
                }
            }

            bool _looping;
            int _timerCount;
            long _expireTime;
            shared_strand _strand;
            MapNode<long, waitable_event_handle> _waitableNode;
            Map<long, async_timer> _timerQueue;

            public steady_timer(shared_strand strand)
            {
                _timerCount = 0;
                _looping = false;
                _expireTime = long.MaxValue;
                _strand = strand;
                _timerQueue = new Map<long, async_timer>(true);
            }

            public void timeout(async_timer asyncTimer)
            {
                long absus = asyncTimer._timerHandle.absus;
                asyncTimer._timerHandle.node = _timerQueue.Insert(absus, asyncTimer);
                if (!_looping)
                {
                    _looping = true;
                    _expireTime = absus;
                    timer_loop(absus);
                }
                else if (absus < _expireTime)
                {
                    _timerCount++;
                    _expireTime = absus;
                    waitable_timer.timer.removeEvent(this);
                    timer_loop(absus);
                }
            }

            public void cancel(async_timer asyncTimer)
            {
                if (null != asyncTimer._timerHandle.node)
                {
                    _timerQueue.Remove(asyncTimer._timerHandle.node);
                    asyncTimer._timerHandle.node = null;
                    if (0 == _timerQueue.Count)
                    {
                        _timerCount++;
                        _expireTime = 0;
                        _looping = false;
                        waitable_timer.timer.removeEvent(this);
                    }
                }
            }

            public void timer_handler(int id)
            {
                if (id != _timerCount)
                {
                    return;
                }
                _strand.post(delegate ()
                {
                    if (id == _timerCount)
                    {
                        _expireTime = 0;
                        while (0 != _timerQueue.Count)
                        {
                            MapNode<long, async_timer> first = _timerQueue.First;
                            if (first.Key > system_tick.get_tick_us())
                            {
                                _expireTime = first.Key;
                                timer_loop(_expireTime);
                                return;
                            }
                            else
                            {
                                first.Value._timerHandle.node = null;
                                first.Value.timer_handler();
                                _timerQueue.Remove(first);
                            }
                        }
                        _looping = false;
                    }
                });
            }

            void timer_loop(long absus)
            {
                waitable_timer.timer.appendEvent(absus, new waitable_event_handle(++_timerCount, this));
            }
        }

        shared_strand _strand;
        functional.func _handler;
        steady_timer_handle _timerHandle;
        int _timerCount;
        long _beginTick;
        bool _isInterval;
        bool _onTopCall;

        public async_timer(shared_strand strand)
        {
            _strand = strand;
            _timerCount = 0;
            _beginTick = 0;
            _isInterval = false;
            _onTopCall = false;
        }

        public shared_strand self_strand()
        {
            return _strand;
        }

        private void timer_handler()
        {
            _onTopCall = true;
            if (_isInterval)
            {
                int lastTc = _timerCount;
                _handler();
                if (lastTc == _timerCount)
                {
                    begin_timer(_timerHandle.absus += _timerHandle.period, _timerHandle.period);
                }
            }
            else
            {
                functional.func handler = _handler;
                _handler = null;
                _strand.release_work();
                handler();
            }
            _onTopCall = false;
        }

        private void begin_timer(long absus, long period)
        {
            _timerCount++;
            _timerHandle.absus = absus;
            _timerHandle.period = period;
            _strand._timer.timeout(this);
        }

        public void timeout_us(long us, functional.func handler)
        {
#if DEBUG
            Trace.Assert(_strand.running_in_this_thread() && null == _handler && null != handler);
#endif
            _isInterval = false;
            _handler = handler;
            _strand.hold_work();
            _beginTick = system_tick.get_tick_us();
            begin_timer(_beginTick + us, us);
        }

        public void deadline_us(long us, functional.func handler)
        {
#if DEBUG
            Trace.Assert(_strand.running_in_this_thread() && null == _handler && null != handler);
#endif
            _isInterval = false;
            _handler = handler;
            _strand.hold_work();
            _beginTick = system_tick.get_tick_us();
            begin_timer(us, us - _beginTick);
        }

        public void timeout(int ms, functional.func handler)
        {
            timeout_us(ms * 1000, handler);
        }

        public void deadline(long ms, functional.func handler)
        {
            deadline_us(ms * 1000, handler);
        }

        public void interval(int ms, functional.func handler, bool immed = false)
        {
            interval_us(ms * 1000, handler, immed);
        }

        public void interval_us(long us, functional.func handler, bool immed = false)
        {
#if DEBUG
            Trace.Assert(_strand.running_in_this_thread() && null == _handler && null != handler);
#endif
            _isInterval = true;
            _handler = handler;
            _strand.hold_work();
            _beginTick = system_tick.get_tick_us();
            begin_timer(_beginTick + us, us);
            if (immed)
            {
                handler();
            }
        }

        public bool restart()
        {
#if DEBUG
            Trace.Assert(_strand.running_in_this_thread());
#endif
            if (null != _handler)
            {
                _strand._timer.cancel(this);
                _beginTick = system_tick.get_tick_us();
                begin_timer(_beginTick + _timerHandle.period, _timerHandle.period);
                return true;
            }
            return false;
        }

        public bool advance()
        {
#if DEBUG
            Trace.Assert(_strand.running_in_this_thread());
#endif
            if (null != _handler)
            {
                if (!_isInterval)
                {
                    functional.func handler = _handler;
                    cancel();
                    handler();
                    return true;
                }
                else if (!_onTopCall)
                {
                    _handler();
                    return true;
                }
            }
            return false;
        }

        public long cancel()
        {
#if DEBUG
            Trace.Assert(_strand.running_in_this_thread());
#endif
            if (null != _handler)
            {
                _timerCount++;
                _strand._timer.cancel(this);
                long lastBegin = _beginTick;
                _beginTick = 0;
                _handler = null;
                _strand.release_work();
                return lastBegin;
            }
            return 0;
        }
    }
}
