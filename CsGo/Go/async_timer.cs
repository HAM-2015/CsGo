using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Go
{
    public class system_tick
    {
        [DllImport("NtDll.dll")]
        private static extern int NtQueryTimerResolution(out uint MaximumTime, out uint MinimumTime, out uint CurrentTime);
        [DllImport("NtDll.dll")]
        private static extern int NtSetTimerResolution(uint DesiredTime, uint SetResolution, out uint ActualTime);
        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);
        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long frequency);

        private static system_tick _pcCycle = new system_tick();
#if DEBUG
        private static volatile bool _checkStepDebugSign = false;
#endif

        private double _sCycle;
        private double _msCycle;
        private double _usCycle;

        private system_tick()
        {
            long freq;
            if (!QueryPerformanceFrequency(out freq))
            {
                throw new Exception("QueryPerformanceFrequency 初始化失败!");
            }
            _sCycle = 1.0 / (double)freq;
            _msCycle = 1000.0 / (double)freq;
            _usCycle = 1000000.0 / (double)freq;
#if DEBUG
            Thread checkStepDebug = new Thread(delegate ()
            {
                long checkTick = get_tick_ms();
                while (true)
                {
                    Thread.Sleep(80);
                    long oldTick = checkTick;
                    checkTick = get_tick_ms();
                    _checkStepDebugSign = (checkTick - oldTick) > 100;
                }
            });
            checkStepDebug.Priority = ThreadPriority.Highest;
            checkStepDebug.IsBackground = true;
            checkStepDebug.Name = "单步调试检测";
            checkStepDebug.Start();
#endif
        }

        public static void high_resolution()
        {
            uint MaximumTime = 0, MinimumTime = 0, CurrentTime = 0, ActualTime = 0;
            if (0 == NtQueryTimerResolution(out MaximumTime, out MinimumTime, out CurrentTime))
            {
                NtSetTimerResolution(MinimumTime, 1, out ActualTime);
            }
        }

        public static long get_tick()
        {
            long quadPart;
            QueryPerformanceCounter(out quadPart);
            return quadPart;
        }

        public static long get_tick_us()
        {
            long quadPart;
            QueryPerformanceCounter(out quadPart);
            return (long)((double)quadPart * _pcCycle._usCycle);
        }

        public static long get_tick_ms()
        {
            long quadPart;
            QueryPerformanceCounter(out quadPart);
            return (long)((double)quadPart * _pcCycle._msCycle);
        }

        public static long get_tick_s()
        {
            long quadPart;
            QueryPerformanceCounter(out quadPart);
            return (long)((double)quadPart * _pcCycle._sCycle);
        }

        public static long us
        {
            get
            {
                return get_tick_us();
            }
        }

        public static long ms
        {
            get
            {
                return get_tick_ms();
            }
        }

        public static long s
        {
            get
            {
                return get_tick_s();
            }
        }
#if DEBUG
        public static bool check_step_debugging()
        {
            return _checkStepDebugSign;
        }
#endif
    }

    public static class utc_tick
    {
        [DllImport("kernel32.dll")]
        private static extern void GetSystemTimeAsFileTime(out long time);

        public const long fileTimeOffset = 504911232000000000L;

        public static long get_tick()
        {
            long tm;
            GetSystemTimeAsFileTime(out tm);
            return tm;
        }

        public static long get_tick_us()
        {
            long tm;
            GetSystemTimeAsFileTime(out tm);
            return tm / 10;
        }

        public static long get_tick_ms()
        {
            long tm;
            GetSystemTimeAsFileTime(out tm);
            return tm / 10000;
        }

        public static long get_tick_s()
        {
            long tm;
            GetSystemTimeAsFileTime(out tm);
            return tm / 10000000;
        }

        public static long us
        {
            get
            {
                return get_tick_us();
            }
        }

        public static long ms
        {
            get
            {
                return get_tick_ms();
            }
        }

        public static long s
        {
            get
            {
                return get_tick_s();
            }
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

        internal class steady_timer
        {
            struct waitable_event_handle
            {
                public int id;
                public steady_timer steadyTimer;
            }

            class waitable_timer
            {
                [DllImport("kernel32.dll")]
                private static extern int CreateWaitableTimer(int lpTimerAttributes, int bManualReset, int lpTimerName);
                [DllImport("kernel32.dll")]
                private static extern int SetWaitableTimer(int hTimer, ref long pDueTime, int lPeriod, int pfnCompletionRoutine, int lpArgToCompletionRoutine, int fResume);
                [DllImport("kernel32.dll")]
                private static extern int CancelWaitableTimer(int hTimer);
                [DllImport("kernel32.dll")]
                private static extern int CloseHandle(int hObject);
                [DllImport("kernel32.dll")]
                private static extern int WaitForSingleObject(int hHandle, int dwMilliseconds);

                static public readonly waitable_timer sysTimer = new waitable_timer(false);
                static public readonly waitable_timer utcTimer = new waitable_timer(true);

                bool _utcMode;
                bool _exited;
                int _timerHandle;
                long _expireTime;
                Thread _timerThread;
                work_engine _workEngine;
                Map<long, waitable_event_handle> _eventsQueue;

                waitable_timer(bool utcMode)
                {
                    _utcMode = utcMode;
                    _exited = false;
                    _expireTime = long.MaxValue;
                    _eventsQueue = new Map<long, waitable_event_handle>(true);
                    _timerHandle = CreateWaitableTimer(0, 0, 0);
                    _workEngine = new work_engine();
                    _timerThread = new Thread(timer_thread);
                    _timerThread.Priority = ThreadPriority.Highest;
                    _timerThread.IsBackground = true;
                    _timerThread.Name = _utcMode ? "UTC定时器" : "系统定时器";
                    _workEngine.run(1, ThreadPriority.Highest, true, _utcMode ? "UTC定时器调度" : "系统定时器调度");
                    _timerThread.Start();
                }

                public void close()
                {
                    _workEngine.service.post(delegate ()
                    {
                        _exited = true;
                        long sleepTime = 0;
                        SetWaitableTimer(_timerHandle, ref sleepTime, 0, 0, 0, 0);
                    });
                    _timerThread.Join();
                    _workEngine.stop();
                    CloseHandle(_timerHandle);
                }

                public void append_event(long absus, waitable_event_handle eventHandle)
                {
                    _workEngine.service.post(delegate ()
                    {
                        if (_exited) return;
                        eventHandle.steadyTimer._waitableNode = _eventsQueue.Insert(absus, eventHandle);
                        if (absus < _expireTime)
                        {
                            _expireTime = absus;
                            if (_utcMode)
                            {
                                long sleepTime = absus * 10;
                                sleepTime = sleepTime > 0 ? sleepTime : 0;
                                SetWaitableTimer(_timerHandle, ref sleepTime, 0, 0, 0, 0);
                            }
                            else
                            {
                                long sleepTime = -(absus - system_tick.get_tick_us()) * 10;
                                sleepTime = sleepTime < 0 ? sleepTime : 0;
                                SetWaitableTimer(_timerHandle, ref sleepTime, 0, 0, 0, 0);
                            }
                        }
                    });
                }

                public void remove_event(steady_timer steadyTime)
                {
                    _workEngine.service.post(delegate ()
                    {
                        if (_exited) return;
                        if (null != steadyTime._waitableNode)
                        {
                            long lastAbsus = steadyTime._waitableNode.Key;
                            _eventsQueue.Remove(steadyTime._waitableNode);
                            steadyTime._waitableNode = null;
                            if (0 == _eventsQueue.Count)
                            {
                                _expireTime = long.MaxValue;
                                CancelWaitableTimer(_timerHandle);
                            }
                            else if (lastAbsus == _expireTime)
                            {
                                _expireTime = _eventsQueue.First.Key;
                                if (_utcMode)
                                {
                                    long sleepTime = _expireTime * 10;
                                    sleepTime = sleepTime > 0 ? sleepTime : 0;
                                    SetWaitableTimer(_timerHandle, ref sleepTime, 0, 0, 0, 0);
                                }
                                else
                                {
                                    long sleepTime = -(_expireTime - system_tick.get_tick_us()) * 10;
                                    sleepTime = sleepTime < 0 ? sleepTime : 0;
                                    SetWaitableTimer(_timerHandle, ref sleepTime, 0, 0, 0, 0);
                                }
                            }
                        }
                    });
                }

                public void update_event(long absus, waitable_event_handle eventHandle)
                {
                    _workEngine.service.post(delegate ()
                    {
                        if (_exited) return;
                        if (null != eventHandle.steadyTimer._waitableNode)
                        {
                            _eventsQueue.Insert(_eventsQueue.ReNewNode(eventHandle.steadyTimer._waitableNode, absus, eventHandle));
                        }
                        else
                        {
                            eventHandle.steadyTimer._waitableNode = _eventsQueue.Insert(absus, eventHandle);
                        }
                        long newAbsus = _eventsQueue.First.Key;
                        if (newAbsus < _expireTime)
                        {
                            _expireTime = newAbsus;
                            if (_utcMode)
                            {
                                long sleepTime = newAbsus * 10;
                                sleepTime = sleepTime > 0 ? sleepTime : 0;
                                SetWaitableTimer(_timerHandle, ref sleepTime, 0, 0, 0, 0);
                            }
                            else
                            {
                                long sleepTime = -(newAbsus - system_tick.get_tick_us()) * 10;
                                sleepTime = sleepTime < 0 ? sleepTime : 0;
                                SetWaitableTimer(_timerHandle, ref sleepTime, 0, 0, 0, 0);
                            }
                        }
                    });
                }

                private void timer_thread()
                {
                    Action timerHandler = timer_handler;
                    while (0 == WaitForSingleObject(_timerHandle, -1) && !_exited)
                    {
                        _workEngine.service.post(timerHandler);
                    }
                }

                private void timer_handler()
                {
                    _expireTime = long.MaxValue;
                    while (0 != _eventsQueue.Count)
                    {
                        MapNode<long, waitable_event_handle> first = _eventsQueue.First;
                        long absus = first.Key;
                        long ct = _utcMode ? utc_tick.get_tick_us() : system_tick.get_tick_us();
                        if (absus > ct)
                        {
                            _expireTime = absus;
                            if (_utcMode)
                            {
                                long sleepTime = absus * 10;
                                SetWaitableTimer(_timerHandle, ref sleepTime, 0, 0, 0, 0);
                            }
                            else
                            {
                                long sleepTime = -(absus - ct) * 10;
                                SetWaitableTimer(_timerHandle, ref sleepTime, 0, 0, 0, 0);
                            }
                            break;
                        }
                        first.Value.steadyTimer._waitableNode = null;
                        first.Value.steadyTimer.timer_handler(first.Value.id);
                        _eventsQueue.Remove(first);
                    }
                }
            }

            bool _utcMode;
            bool _looping;
            int _timerCount;
            long _expireTime;
            shared_strand _strand;
            MapNode<long, waitable_event_handle> _waitableNode;
            Map<long, async_timer> _timerQueue;

            public steady_timer(shared_strand strand, bool utcMode)
            {
                _utcMode = utcMode;
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
                    _expireTime = absus;
                    timer_reloop(absus);
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
                        if (_utcMode)
                        {
                            waitable_timer.utcTimer.remove_event(this);
                        }
                        else
                        {
                            waitable_timer.sysTimer.remove_event(this);
                        }
                    }
                    else if (asyncTimer._timerHandle.absus == _expireTime)
                    {
                        _expireTime = _timerQueue.First.Key;
                        timer_reloop(_expireTime);
                    }
                }
            }

            public void re_timeout(async_timer asyncTimer)
            {
                long absus = asyncTimer._timerHandle.absus;
                if (null != asyncTimer._timerHandle.node)
                {
                    _timerQueue.Insert(_timerQueue.ReNewNode(asyncTimer._timerHandle.node, absus, asyncTimer));
                }
                else
                {
                    asyncTimer._timerHandle.node = _timerQueue.Insert(absus, asyncTimer);
                }
                long newAbsus = _timerQueue.First.Key;
                if (!_looping)
                {
                    _looping = true;
                    _expireTime = newAbsus;
                    timer_loop(newAbsus);
                }
                else if (newAbsus < _expireTime)
                {
                    _expireTime = newAbsus;
                    timer_reloop(newAbsus);
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
                        _expireTime = long.MinValue;
                        while (0 != _timerQueue.Count)
                        {
                            MapNode<long, async_timer> first = _timerQueue.First;
                            if (first.Key > (_utcMode ? utc_tick.get_tick_us() : system_tick.get_tick_us()))
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
                if (_utcMode)
                {
                    waitable_timer.utcTimer.append_event(absus, new waitable_event_handle { id = ++_timerCount, steadyTimer = this });
                }
                else
                {
                    waitable_timer.sysTimer.append_event(absus, new waitable_event_handle { id = ++_timerCount, steadyTimer = this });
                }
            }

            void timer_reloop(long absus)
            {
                if (_utcMode)
                {
                    waitable_timer.utcTimer.update_event(absus, new waitable_event_handle { id = ++_timerCount, steadyTimer = this });
                }
                else
                {
                    waitable_timer.sysTimer.update_event(absus, new waitable_event_handle { id = ++_timerCount, steadyTimer = this });
                }
            }
        }

        shared_strand _strand;
        Action _handler;
        steady_timer_handle _timerHandle;
        int _timerCount;
        long _beginTick;
        bool _isInterval;
        bool _onTopCall;
        bool _utcMode;

        public async_timer(shared_strand strand, bool utcMode = false)
        {
            _strand = strand;
            _timerCount = 0;
            _beginTick = 0;
            _isInterval = false;
            _onTopCall = false;
            _utcMode = utcMode;
        }

        public async_timer(bool utcMode = false) : this(shared_strand.default_strand(), utcMode) { }

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
                functional.catch_invoke(_handler);
                if (lastTc == _timerCount)
                {
                    begin_timer(_beginTick, _timerHandle.absus += _timerHandle.period, _timerHandle.period);
                }
            }
            else
            {
                Action handler = _handler;
                _handler = null;
                _strand.release_work();
                functional.catch_invoke(handler);
            }
            _onTopCall = false;
        }

        private void begin_timer(long nowus, long absus, long period)
        {
            _beginTick = nowus;
            if (nowus < absus)
            {
                _timerCount++;
                _timerHandle.absus = absus;
                _timerHandle.period = period;
                if (_utcMode)
                {
                    _strand._utcTimer.timeout(this);
                }
                else
                {
                    _strand._sysTimer.timeout(this);
                }
            }
            else
            {
                int tmId = ++_timerCount;
                _timerHandle.absus = absus;
                _timerHandle.period = period;
                _strand.post(delegate ()
                {
                    if (tmId == _timerCount)
                    {
                        timer_handler();
                    }
                });
            }
        }

        private void re_begin_timer(long nowus, long absus, long period)
        {
            _beginTick = nowus;
            if (nowus < absus)
            {
                _timerCount++;
                _timerHandle.absus = absus;
                _timerHandle.period = period;
                if (_utcMode)
                {
                    _strand._utcTimer.re_timeout(this);
                }
                else
                {
                    _strand._sysTimer.re_timeout(this);
                }
            }
            else
            {
                if (_utcMode)
                {
                    _strand._utcTimer.cancel(this);
                }
                else
                {
                    _strand._sysTimer.cancel(this);
                }
                int tmId = ++_timerCount;
                _timerHandle.absus = absus;
                _timerHandle.period = period;
                _strand.post(delegate ()
                {
                    if (tmId == _timerCount)
                    {
                        timer_handler();
                    }
                });
            }
        }

        public long timeout_us(long us, Action handler)
        {
            Debug.Assert(_strand.running_in_this_thread() && null == _handler && null != handler, "不正确的 timeout_us 调用!");
            _isInterval = false;
            _handler = handler;
            _strand.hold_work();
            long nowus = _utcMode ? utc_tick.get_tick_us() : system_tick.get_tick_us();
            begin_timer(nowus, us > 0 ? nowus + us : nowus, us > 0 ? us : 0);
            return nowus;
        }

        public long deadline_us(long us, Action handler)
        {
            Debug.Assert(_strand.running_in_this_thread() && null == _handler && null != handler, "不正确的 deadline_us 调用!");
            _isInterval = false;
            _handler = handler;
            _strand.hold_work();
            long nowus = _utcMode ? utc_tick.get_tick_us() : system_tick.get_tick_us();
            begin_timer(nowus, us, us > nowus ? us - nowus : 0);
            return nowus;
        }

        public long deadline(DateTime date, Action handler)
        {
            if (_utcMode)
            {
                if (DateTimeKind.Utc == date.Kind)
                {
                    return deadline_us((date.Ticks - utc_tick.fileTimeOffset) / 10, handler);
                }
                return deadline_us((date.Ticks - TimeZoneInfo.Local.BaseUtcOffset.Ticks - utc_tick.fileTimeOffset) / 10, handler);
            }
            else
            {
                if (DateTimeKind.Utc == date.Kind)
                {
                    return timeout_us((date.Ticks - DateTime.UtcNow.Ticks) / 10, handler);
                }
                return timeout_us((date.Ticks - DateTime.Now.Ticks) / 10, handler);
            }
        }

        public long timeout(int ms, Action handler)
        {
            return timeout_us((long)ms * 1000, handler);
        }

        public long deadline(long ms, Action handler)
        {
            return deadline_us(ms * 1000, handler);
        }

        public long interval(int ms, Action handler, bool immed = false)
        {
            return interval_us((long)ms * 1000, handler, immed);
        }

        public long interval2(int ms1, int ms2, Action handler, bool immed = false)
        {
            return interval2_us((long)ms1 * 1000, (long)ms2 * 1000, handler, immed);
        }

        public long interval_us(long us, Action handler, bool immed = false)
        {
            return interval2_us(us, us, handler, immed);
        }

        public long interval2_us(long us1, long us2, Action handler, bool immed = false)
        {
            Debug.Assert(_strand.running_in_this_thread() && null == _handler && null != handler, "不正确的 interval2_us 调用!");
            _isInterval = true;
            _handler = handler;
            _strand.hold_work();
            long nowus = _utcMode ? utc_tick.get_tick_us() : system_tick.get_tick_us();
            begin_timer(nowus, us1 > 0 ? nowus + us1 : nowus, us2 > 0 ? us2 : 0);
            if (immed)
            {
                functional.catch_invoke(_handler);
            }
            return nowus;
        }

        public bool restart(int ms = -1)
        {
            return restart_us((long)ms * 1000);
        }

        public bool restart_us(long us = -1)
        {
            Debug.Assert(_strand.running_in_this_thread(), "不正确的 restart_us 调用!");
            if (null != _handler)
            {
                long nowus = _utcMode ? utc_tick.get_tick_us() : system_tick.get_tick_us();
                re_begin_timer(nowus, us < 0 ? nowus + _timerHandle.period : nowus + us, _timerHandle.period);
                return true;
            }
            return false;
        }

        public bool advance()
        {
            Debug.Assert(_strand.running_in_this_thread(), "不正确的 advance 调用!");
            if (null != _handler)
            {
                if (!_isInterval)
                {
                    _timerCount++;
                    if (_utcMode)
                    {
                        _strand._utcTimer.cancel(this);
                    }
                    else
                    {
                        _strand._sysTimer.cancel(this);
                    }
                    _beginTick = 0;
                    Action handler = _handler;
                    _handler = null;
                    functional.catch_invoke(handler);
                    _strand.release_work();
                    return true;
                }
                else if (!_onTopCall)
                {
                    functional.catch_invoke(_handler);
                    return true;
                }
            }
            return false;
        }

        public long cancel()
        {
            Debug.Assert(_strand.running_in_this_thread(), "不正确的 cancel 调用!");
            if (null != _handler)
            {
                _timerCount++;
                if (_utcMode)
                {
                    _strand._utcTimer.cancel(this);
                }
                else
                {
                    _strand._sysTimer.cancel(this);
                }
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
