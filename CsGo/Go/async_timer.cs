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
        [DllImport("NtDll.dll")]
        private static extern int NtQueryTimerResolution(out uint MaximumTime, out uint MinimumTime, out uint CurrentTime);
        [DllImport("NtDll.dll")]
        private static extern int NtSetTimerResolution(uint DesiredTime, uint SetResolution, out uint ActualTime);

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

            uint MaximumTime = 0, MinimumTime = 0, CurrentTime = 0, ActualTime = 0;
            if (0 == NtQueryTimerResolution(out MaximumTime, out MinimumTime, out CurrentTime))
            {
                NtSetTimerResolution(MinimumTime, 1, out ActualTime);
            }
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
        shared_strand _strand;
        functional.func _handler;
        Timer _timer;
        int _lastTimeout;
        int _timerCount;
        long _beginTick;
        bool _isInterval;
        bool _onTopCall;

        public async_timer(shared_strand strand)
        {
            _strand = strand;
            _timer = null;
            _lastTimeout = 0;
            _timerCount = 0;
            _beginTick = 0;
            _isInterval = false;
            _onTopCall = false;
        }

        public shared_strand self_strand()
        {
            return _strand;
        }

        private void begin_timer(long tick, long dueTime, int period)
        {
            if (null == _timer)
            {
                _strand.hold_work();
            }
            _timer = new Timer(delegate (object state)
            {
                _strand.post(delegate ()
                {
                    if ((int)state == _timerCount)
                    {
                        _beginTick = 0;
                        _onTopCall = true;
                        if (_isInterval)
                        {
                            functional.func handler = _handler;
                            handler();
                            if ((int)state == _timerCount)
                            {
                                tick += period;
                                dueTime = tick - system_tick.get_tick_ms();
                                begin_timer(tick, dueTime > 0 ? dueTime : 0, period);
                            }
                        }
                        else
                        {
                            functional.func handler = _handler;
                            _handler = null;
                            _timer = null;
                            _strand.release_work();
                            handler();
                        }
                        _onTopCall = false;
                    }
                });
            }, _timerCount, dueTime, 0);
        }

        public void timeout(int ms, functional.func handler)
        {
#if DEBUG
            Trace.Assert(_strand.running_in_this_thread() && null == _timer && null != handler);
#endif
            _isInterval = false;
            _handler = handler;
            _lastTimeout = ms;
            _timerCount++;
            _beginTick = system_tick.get_tick_ms();
            begin_timer(_beginTick, ms, 0);
        }

        public void deadline(long ms, functional.func handler)
        {
#if DEBUG
            Trace.Assert(_strand.running_in_this_thread() && null == _timer && null != handler);
#endif
            _isInterval = false;
            _handler = handler;
            _timerCount++;
            _beginTick = system_tick.get_tick_ms();
            if (ms > _beginTick)
            {
                _lastTimeout = (int)(ms - _beginTick);
                begin_timer(_beginTick, _lastTimeout, 0);
            }
            else
            {
                _lastTimeout = 0;
                begin_timer(_beginTick, 0, 0);
            }
        }

        public void interval(int ms, functional.func handler, bool immed = false)
        {
#if DEBUG
            Trace.Assert(_strand.running_in_this_thread() && null == _timer && null != handler);
#endif
            _isInterval = true;
            _handler = handler;
            _lastTimeout = ms;
            _timerCount++;
            _beginTick = system_tick.get_tick_ms();
            begin_timer(_beginTick, ms, ms);
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
            if (null != _timer)
            {
                _timerCount++;
                _timer.Change(0, 0);
                _beginTick = system_tick.get_tick_ms();
                begin_timer(_beginTick, _lastTimeout, _isInterval ? _lastTimeout : 0);
            }
            return false;
        }

        public bool advance()
        {
#if DEBUG
            Trace.Assert(_strand.running_in_this_thread());
#endif
            if (null != _timer)
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
                    functional.func handler = _handler;
                    handler();
                    return true;
                }
            }
            return false;
        }

        public long cancel()
        {
            if (null != _timer)
            {
                _timerCount++;
                long lastBegin = _beginTick;
                _beginTick = 0;
                _timer.Change(0, 0);
                _timer = null;
                _handler = null;
                _strand.release_work();
                return lastBegin;
            }
            return 0;
        }
    }
}
