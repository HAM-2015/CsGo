using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Go
{
    public struct void_type { }

    [Serializable]
    public struct tuple<T1>
    {
        public readonly T1 value1;
        public tuple(T1 v1) { value1 = v1; }

        public override string ToString()
        {
            return string.Format("({0})", value1);
        }
#if NETCORE
        public static implicit operator tuple<T1>(ValueTuple<T1> rval)
        {
            return new tuple<T1>(rval.Item1);
        }

        public static implicit operator ValueTuple<T1>(tuple<T1> rval)
        {
            return new ValueTuple<T1>(rval.value1);
        }
#endif
    }

    [Serializable]
    public struct tuple<T1, T2>
    {
        public readonly T1 value1; public readonly T2 value2;
        public tuple(T1 v1, T2 v2) { value1 = v1; value2 = v2; }

        public override string ToString()
        {
            return string.Format("({0},{1})", value1, value2);
        }
#if NETCORE
        public static implicit operator tuple<T1, T2>(ValueTuple<T1, T2> rval)
        {
            return new tuple<T1, T2>(rval.Item1, rval.Item2);
        }

        public static implicit operator ValueTuple<T1, T2>(tuple<T1, T2> rval)
        {
            return new ValueTuple<T1, T2>(rval.value1, rval.value2);
        }
#endif
    }

    [Serializable]
    public struct tuple<T1, T2, T3>
    {
        public readonly T1 value1; public readonly T2 value2; public readonly T3 value3;
        public tuple(T1 v1, T2 v2, T3 v3) { value1 = v1; value2 = v2; value3 = v3; }

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", value1, value2, value3);
        }
#if NETCORE
        public static implicit operator tuple<T1, T2, T3>(ValueTuple<T1, T2, T3> rval)
        {
            return new tuple<T1, T2, T3>(rval.Item1, rval.Item2, rval.Item3);
        }

        public static implicit operator ValueTuple<T1, T2, T3>(tuple<T1, T2, T3> rval)
        {
            return new ValueTuple<T1, T2, T3>(rval.value1, rval.value2, rval.value3);
        }
#endif
    }

    [Serializable]
    public struct tuple<T1, T2, T3, T4>
    {
        public readonly T1 value1; public readonly T2 value2; public readonly T3 value3; public readonly T4 value4;
        public tuple(T1 v1, T2 v2, T3 v3, T4 v4) { value1 = v1; value2 = v2; value3 = v3; value4 = v4; }

        public override string ToString()
        {
            return string.Format("({0},{1},{2},{3})", value1, value2, value3, value4);
        }
#if NETCORE
        public static implicit operator tuple<T1, T2, T3, T4>(ValueTuple<T1, T2, T3, T4> rval)
        {
            return new tuple<T1, T2, T3, T4>(rval.Item1, rval.Item2, rval.Item3, rval.Item4);
        }

        public static implicit operator ValueTuple<T1, T2, T3, T4>(tuple<T1, T2, T3, T4> rval)
        {
            return new ValueTuple<T1, T2, T3, T4>(rval.value1, rval.value2, rval.value3, rval.value4);
        }
#endif
    }

    [Serializable]
    public struct tuple<T1, T2, T3, T4, T5>
    {
        public readonly T1 value1; public readonly T2 value2; public readonly T3 value3; public readonly T4 value4; public readonly T5 value5;
        public tuple(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5) { value1 = v1; value2 = v2; value3 = v3; value4 = v4; value5 = v5; }

        public override string ToString()
        {
            return string.Format("({0},{1},{2},{3},{4})", value1, value2, value3, value4, value5);
        }
#if NETCORE
        public static implicit operator tuple<T1, T2, T3, T4, T5>(ValueTuple<T1, T2, T3, T4, T5> rval)
        {
            return new tuple<T1, T2, T3, T4, T5>(rval.Item1, rval.Item2, rval.Item3, rval.Item4, rval.Item5);
        }

        public static implicit operator ValueTuple<T1, T2, T3, T4, T5>(tuple<T1, T2, T3, T4, T5> rval)
        {
            return new ValueTuple<T1, T2, T3, T4, T5>(rval.value1, rval.value2, rval.value3, rval.value4, rval.value5);
        }
#endif
    }

    public static class tuple
    {
        static public tuple<T1> make<T1>(T1 p1) { return new tuple<T1>(p1); }
        static public tuple<T1, T2> make<T1, T2>(T1 p1, T2 p2) { return new tuple<T1, T2>(p1, p2); }
        static public tuple<T1, T2, T3> make<T1, T2, T3>(T1 p1, T2 p2, T3 p3) { return new tuple<T1, T2, T3>(p1, p2, p3); }
        static public tuple<T1, T2, T3, T4> make<T1, T2, T3, T4>(T1 p1, T2 p2, T3 p3, T4 p4) { return new tuple<T1, T2, T3, T4>(p1, p2, p3, p4); }
        static public tuple<T1, T2, T3, T4, T5> make<T1, T2, T3, T4, T5>(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5) { return new tuple<T1, T2, T3, T4, T5>(p1, p2, p3, p4, p5); }
    }

    public static class nil_action { static public readonly Action action = () => { }; }
    public static class nil_action<T1> { static public readonly Action<T1> action = (T1 p1) => { }; }
    public static class nil_action<T1, T2> { static public readonly Action<T1, T2> action = (T1 p1, T2 p2) => { }; }
    public static class nil_action<T1, T2, T3> { static public readonly Action<T1, T2, T3> action = (T1 p1, T2 p2, T3 p3) => { }; }

    public static class nil_func { static public readonly Func<Task> func = () => generator.non_async(); }
    public static class nil_func<T1> { static public readonly Func<T1, Task> func = (T1 p1) => generator.non_async(); }
    public static class nil_func<T1, T2> { static public readonly Func<T1, T2, Task> func = (T1 p1, T2 p2) => generator.non_async(); }
    public static class nil_func<T1, T2, T3> { static public readonly Func<T1, T2, T3, Task> func = (T1 p1, T2 p2, T3 p3) => generator.non_async(); }

    public static class functional
    {
        public class placeholder { internal placeholder() { } }
        public static readonly placeholder _ = new placeholder();

        public static Action bind<T1>(Action<T1> handler, T1 p1)
        {
            return () => handler(p1);
        }

        public static Action bind<T1, T2>(Action<T1, T2> handler, T1 p1, T2 p2)
        {
            return () => handler(p1, p2);
        }

        public static Action bind<T1, T2, T3>(Action<T1, T2, T3> handler, T1 p1, T2 p2, T3 p3)
        {
            return () => handler(p1, p2, p3);
        }

        public static Action<T1> bind<T1, T2>(Action<T1, T2> handler, placeholder p1, T2 p2)
        {
            return (T1 _1) => handler(_1, p2);
        }

        public static Action<T2> bind<T1, T2>(Action<T1, T2> handler, T1 p1, placeholder p2)
        {
            return (T2 _2) => handler(p1, _2);
        }

        public static Action<T1> bind<T1, T2, T3>(Action<T1, T2, T3> handler, placeholder p1, T2 p2, T3 p3)
        {
            return (T1 _1) => handler(_1, p2, p3);
        }

        public static Action<T2> bind<T1, T2, T3>(Action<T1, T2, T3> handler, T1 p1, placeholder p2, T3 p3)
        {
            return (T2 _2) => handler(p1, _2, p3);
        }

        public static Action<T3> bind<T1, T2, T3>(Action<T1, T2, T3> handler, T1 p1, T2 p2, placeholder p3)
        {
            return (T3 _3) => handler(p1, p2, _3);
        }

        public static Action<T1, T2> bind<T1, T2, T3>(Action<T1, T2, T3> handler, placeholder p1, placeholder p2, T3 p3)
        {
            return (T1 _1, T2 _2) => handler(_1, _2, p3);
        }

        public static Action<T1, T3> bind<T1, T2, T3>(Action<T1, T2, T3> handler, placeholder p1, T2 p2, placeholder p3)
        {
            return (T1 _1, T3 _3) => handler(_1, p2, _3);
        }

        public static Action<T2, T3> bind<T1, T2, T3>(Action<T1, T2, T3> handler, T1 p1, placeholder p2, placeholder p3)
        {
            return (T2 _2, T3 _3) => handler(p1, _2, _3);
        }

        public static Func<R> bind<R, T1>(Func<T1, R> handler, T1 p1)
        {
            return () => handler(p1);
        }

        public static Func<R> bind<R, T1, T2>(Func<T1, T2, R> handler, T1 p1, T2 p2)
        {
            return () => handler(p1, p2);
        }

        public static Func<R> bind<R, T1, T2, T3>(Func<T1, T2, T3, R> handler, T1 p1, T2 p2, T3 p3)
        {
            return () => handler(p1, p2, p3);
        }

        public static Func<T1, R> bind<R, T1, T2>(Func<T1, T2, R> handler, placeholder p1, T2 p2)
        {
            return (T1 _1) => handler(_1, p2);
        }

        public static Func<T2, R> bind<R, T1, T2>(Func<T1, T2, R> handler, T1 p1, placeholder p2)
        {
            return (T2 _2) => handler(p1, _2);
        }

        public static Func<T1, R> bind<R, T1, T2, T3>(Func<T1, T2, T3, R> handler, placeholder p1, T2 p2, T3 p3)
        {
            return (T1 _1) => handler(_1, p2, p3);
        }

        public static Func<T2, R> bind<R, T1, T2, T3>(Func<T1, T2, T3, R> handler, T1 p1, placeholder p2, T3 p3)
        {
            return (T2 _2) => handler(p1, _2, p3);
        }

        public static Func<T3, R> bind<R, T1, T2, T3>(Func<T1, T2, T3, R> handler, T1 p1, T2 p2, placeholder p3)
        {
            return (T3 _3) => handler(p1, p2, _3);
        }

        public static Func<T1, T2, R> bind<R, T1, T2, T3>(Func<T1, T2, T3, R> handler, placeholder p1, placeholder p2, T3 p3)
        {
            return (T1 _1, T2 _2) => handler(_1, _2, p3);
        }

        public static Func<T1, T3, R> bind<R, T1, T2, T3>(Func<T1, T2, T3, R> handler, placeholder p1, T2 p2, placeholder p3)
        {
            return (T1 _1, T3 _3) => handler(_1, p2, _3);
        }

        public static Func<T2, T3, R> bind<R, T1, T2, T3>(Func<T1, T2, T3, R> handler, T1 p1, placeholder p2, placeholder p3)
        {
            return (T2 _2, T3 _3) => handler(p1, _2, _3);
        }

        public static generator.action bind<T1>(Func<T1, Task> handler, T1 p1)
        {
            return () => handler(p1);
        }

        public static generator.action bind<T1, T2>(Func<T1, T2, Task> handler, T1 p1, T2 p2)
        {
            return () => handler(p1, p2);
        }

        public static generator.action bind<T1, T2, T3>(Func<T1, T2, T3, Task> handler, T1 p1, T2 p2, T3 p3)
        {
            return () => handler(p1, p2, p3);
        }

        public static void catch_invoke(Action handler)
        {
            try
            {
                handler?.Invoke();
            }
            catch (System.Exception ec)
            {
                Trace.Fail(ec.Message, ec.StackTrace);
            }
        }

        public static void catch_invoke<T1>(Action<T1> handler, T1 p1)
        {
            try
            {
                handler?.Invoke(p1);
            }
            catch (System.Exception ec)
            {
                Trace.Fail(ec.Message, ec.StackTrace);
            }
        }

        public static void catch_invoke<T1, T2>(Action<T1, T2> handler, T1 p1, T2 p2)
        {
            try
            {
                handler?.Invoke(p1, p2);
            }
            catch (System.Exception ec)
            {
                Trace.Fail(ec.Message, ec.StackTrace);
            }
        }

        public static void catch_invoke<T1, T2, T3>(Action<T1, T2, T3> handler, T1 p1, T2 p2, T3 p3)
        {
            try
            {
                handler?.Invoke(p1, p2, p3);
            }
            catch (System.Exception ec)
            {
                Trace.Fail(ec.Message, ec.StackTrace);
            }
        }

        public static R init<R>(Func<R> action)
        {
            return action.Invoke();
        }

#if NETCORE
        public static Func<ValueTask<R>> acry<R>(Func<ValueTask<R>> handler)
        {
            return handler;
        }

        public static Func<T1, ValueTask<R>> acry<R, T1>(Func<T1, ValueTask<R>> handler)
        {
            return handler;
        }

        public static Func<T1, T2, ValueTask<R>> acry<R, T1, T2>(Func<T1, T2, ValueTask<R>> handler)
        {
            return handler;
        }

        public static Func<T1, T2, T3, ValueTask<R>> acry<R, T1, T2, T3>(Func<T1, T2, T3, ValueTask<R>> handler)
        {
            return handler;
        }
#else
        public static Func<Task<R>> acry<R>(Func<Task<R>> handler)
        {
            return handler;
        }

        public static Func<T1, Task<R>> acry<R, T1>(Func<T1, Task<R>> handler)
        {
            return handler;
        }

        public static Func<T1, T2, Task<R>> acry<R, T1, T2>(Func<T1, T2, Task<R>> handler)
        {
            return handler;
        }

        public static Func<T1, T2, T3, Task<R>> acry<R, T1, T2, T3>(Func<T1, T2, T3, Task<R>> handler)
        {
            return handler;
        }
#endif
    }
}
