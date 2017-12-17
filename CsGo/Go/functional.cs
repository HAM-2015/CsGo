using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Go
{
    public class placeholder { }
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
    }

    public abstract class tuple
    {
        static public tuple<T1> make<T1>(T1 p1) { return new tuple<T1>(p1); }
        static public tuple<T1, T2> make<T1, T2>(T1 p1, T2 p2) { return new tuple<T1, T2>(p1, p2); }
        static public tuple<T1, T2, T3> make<T1, T2, T3>(T1 p1, T2 p2, T3 p3) { return new tuple<T1, T2, T3>(p1, p2, p3); }
        static public tuple<T1, T2, T3, T4> make<T1, T2, T3, T4>(T1 p1, T2 p2, T3 p3, T4 p4) { return new tuple<T1, T2, T3, T4>(p1, p2, p3, p4); }
        static public tuple<T1, T2, T3, T4, T5> make<T1, T2, T3, T4, T5>(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5) { return new tuple<T1, T2, T3, T4, T5>(p1, p2, p3, p4, p5); }
    }

    public delegate void SameAction(params object[] args);
    public delegate R SameFunc<R>(params object[] args);

    public abstract class nil_action { static public readonly Action action = () => { }; }
    public abstract class nil_action<T1> { static public readonly Action<T1> action = (T1 p1) => { }; }
    public abstract class nil_action<T1, T2> { static public readonly Action<T1, T2> action = (T1 p1, T2 p2) => { }; }
    public abstract class nil_action<T1, T2, T3> { static public readonly Action<T1, T2, T3> action = (T1 p1, T2 p2, T3 p3) => { }; }
    public abstract class any_action { static public readonly SameAction action = (object[] args) => { }; }

    public class functional
    {
        public static readonly placeholder _ = new placeholder();

        public static Action cast(SameAction handler)
        {
            return () => handler();
        }

        public static Action<T1> cast<T1>(SameAction handler)
        {
            return (T1 p1) => handler(p1);
        }

        public static Action<T1, T2> cast<T1, T2>(SameAction handler)
        {
            return (T1 p1, T2 p2) => handler(p1, p2);
        }

        public static Action<T1, T2, T3> cast<T1, T2, T3>(SameAction handler)
        {
            return (T1 p1, T2 p2, T3 p3) => handler(p1, p2, p3);
        }

        public static SameAction cast(Action handler)
        {
            return (object[] args) => handler();
        }

        public static SameAction cast<T1>(Action<T1> handler)
        {
            return (object[] args) => handler((T1)args[0]);
        }

        public static SameAction cast<T1, T2>(Action<T1, T2> handler)
        {
            return (object[] args) => handler((T1)args[0], (T2)args[1]);
        }

        public static SameAction cast<T1, T2, T3>(Action<T1, T2, T3> handler)
        {
            return (object[] args) => handler((T1)args[0], (T2)args[1], (T3)args[2]);
        }

        public static Func<R> cast<R>(SameFunc<R> handler)
        {
            return () => handler();
        }

        public static Func<T1, R> cast<R, T1>(SameFunc<R> handler)
        {
            return (T1 p1) => handler(p1);
        }

        public static Func<T1, T2, R> cast<R, T1, T2>(SameFunc<R> handler)
        {
            return (T1 p1, T2 p2) => handler(p1, p2);
        }

        public static Func<T1, T2, T3, R> cast<R, T1, T2, T3>(SameFunc<R> handler)
        {
            return (T1 p1, T2 p2, T3 p3) => handler(p1, p2, p3);
        }

        public static SameFunc<R> cast<R>(Func<R> handler)
        {
            return (object[] args) => handler();
        }

        public static SameFunc<R> cast<R, T1>(Func<T1, R> handler)
        {
            return (object[] args) => handler((T1)args[0]);
        }

        public static SameFunc<R> cast<R, T1, T2>(Func<T1, T2, R> handler)
        {
            return (object[] args) => handler((T1)args[0], (T2)args[1]);
        }

        public static SameFunc<R> cast<R, T1, T2, T3>(Func<T1, T2, T3, R> handler)
        {
            return (object[] args) => handler((T1)args[0], (T2)args[1], (T3)args[2]);
        }

        public static SameAction bind_same<P1, T1>(Action<P1> handler, T1 p1)
        {
            return delegate (object[] args)
            {
                handler((P1)(functional._ == (object)p1 ? args[0] : p1));
            };
        }

        public static SameAction bind_same<P1, P2, T1, T2>(Action<P1, P2> handler, T1 p1, T2 p2)
        {
            return delegate (object[] args)
            {
                int i = 0;
                handler((P1)(functional._ == (object)p1 ? args[i++] : p1),
                    (P2)(functional._ == (object)p2 ? args[i++] : p2));
            };
        }

        public static SameAction bind_same<P1, P2, P3, T1, T2, T3>(Action<P1, P2, P3> handler, T1 p1, T2 p2, T3 p3)
        {
            return delegate (object[] args)
            {
                int i = 0;
                handler((P1)(functional._ == (object)p1 ? args[i++] : p1),
                    (P2)(functional._ == (object)p2 ? args[i++] : p2),
                    (P3)(functional._ == (object)p3 ? args[i++] : p3));
            };
        }

        public static SameFunc<R> bind_same<R, P1, T1>(Func<P1, R> handler, T1 p1)
        {
            return delegate (object[] args)
            {
                return handler((P1)(functional._ == (object)p1 ? args[0] : p1));
            };
        }

        public static SameFunc<R> bind_same<R, P1, P2, T1, T2>(Func<P1, P2, R> handler, T1 p1, T2 p2)
        {
            return delegate (object[] args)
            {
                int i = 0;
                return handler((P1)(functional._ == (object)p1 ? args[i++] : p1),
                    (P2)(functional._ == (object)p2 ? args[i++] : p2));
            };
        }

        public static SameFunc<R> bind_same<R, P1, P2, P3, T1, T2, T3>(Func<P1, P2, P3, R> handler, T1 p1, T2 p2, T3 p3)
        {
            return delegate (object[] args)
            {
                int i = 0;
                return handler((P1)(functional._ == (object)p1 ? args[i++] : p1),
                    (P2)(functional._ == (object)p2 ? args[i++] : p2),
                    (P3)(functional._ == (object)p3 ? args[i++] : p3));
            };
        }

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
    }
}
