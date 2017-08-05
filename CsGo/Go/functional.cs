using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Go
{
    public class placeholder { }
    public class void_type { public static void_type val = new void_type(); }

    public class delay_result<T>
    {
        public T value;

        public delay_result(T p)
        {
            value = p;
        }
    }

    public class default_value<T>
    {
        static public readonly T value = default(T);
    }

    public class functional
    {
        public static readonly placeholder _ = new placeholder();

        public delegate void same_func(params object[] args);
        public delegate R same_func<R>(params object[] args);
        public delegate void func();
        public delegate void func<T1>(T1 p1);
        public delegate void func<T1, T2>(T1 p1, T2 p2);
        public delegate void func<T1, T2, T3>(T1 p1, T2 p2, T3 p3);
        public delegate R func_res<R>();
        public delegate R func_res<R, T1>(T1 p1);
        public delegate R func_res<R, T1, T2>(T1 p1, T2 p2);
        public delegate R func_res<R, T1, T2, T3>(T1 p1, T2 p2, T3 p3);
        static public readonly func nil_handler = () => { };
        static public readonly same_func any_handler = (object[] args) => { };
        static public readonly Action nil_action = () => { };

        private static same_func _wrap(same_func handler, params object[] bindArgs)
        {
            return delegate (object[] args)
            {
                for (int i = 0, j = 0; i < bindArgs.Count(); i++)
                {
                    if (functional._ == bindArgs[i])
                    {
                        bindArgs[i] = args[j++];
                    }
                }
                handler(bindArgs);
            };
        }

        private static same_func<R> _wrap_res<R>(same_func<R> handler, params object[] bindArgs)
        {
            return delegate (object[] args)
            {
                for (int i = 0, j = 0; i < bindArgs.Count(); i++)
                {
                    if (functional._ == bindArgs[i])
                    {
                        bindArgs[i] = args[j++];
                    }
                }
                return handler(bindArgs);
            };
        }

        public static object[] wrap_params(params object[] args)
        {
            return args;
        }

        public static func cast(same_func handler)
        {
            return () => handler();
        }

        public static func<T1> cast<T1>(same_func handler)
        {
            return (T1 p1) => handler(p1);
        }

        public static func<T1, T2> cast<T1, T2>(same_func handler)
        {
            return (T1 p1, T2 p2) => handler(p1, p2);
        }

        public static func<T1, T2, T3> cast<T1, T2, T3>(same_func handler)
        {
            return (T1 p1, T2 p2, T3 p3) => handler(p1, p2, p3);
        }

        public static same_func cast(func handler)
        {
            return (object[] args) => handler();
        }

        public static same_func cast<T1>(func<T1> handler)
        {
            return (object[] args) => handler((T1)args[0]);
        }

        public static same_func cast<T1, T2>(func<T1, T2> handler)
        {
            return (object[] args) => handler((T1)args[0], (T2)args[1]);
        }

        public static same_func cast<T1, T2, T3>(func<T1, T2, T3> handler)
        {
            return (object[] args) => handler((T1)args[0], (T2)args[1], (T3)args[2]);
        }

        public static func_res<R> cast_res<R>(same_func<R> handler)
        {
            return () => handler();
        }

        public static func_res<R, T1> cast<R, T1>(same_func<R> handler)
        {
            return (T1 p1) => handler(p1);
        }

        public static func_res<R, T1, T2> cast<R, T1, T2>(same_func<R> handler)
        {
            return (T1 p1, T2 p2) => handler(p1, p2);
        }

        public static func_res<R, T1, T2, T3> cast<R, T1, T2, T3>(same_func<R> handler)
        {
            return (T1 p1, T2 p2, T3 p3) => handler(p1, p2, p3);
        }

        public static same_func<R> cast<R>(func_res<R> handler)
        {
            return (object[] args) => handler();
        }

        public static same_func<R> cast<R, T1>(func_res<R, T1> handler)
        {
            return (object[] args) => handler((T1)args[0]);
        }

        public static same_func<R> cast<R, T1, T2>(func_res<R, T1, T2> handler)
        {
            return (object[] args) => handler((T1)args[0], (T2)args[1]);
        }

        public static same_func<R> cast<R, T1, T2, T3>(func_res<R, T1, T2, T3> handler)
        {
            return (object[] args) => handler((T1)args[0], (T2)args[1], (T3)args[2]);
        }

        public static same_func bind(func handler)
        {
            return (object[] args) => handler.DynamicInvoke(args);
        }

        public static same_func bind<P1, T1>(func<P1> handler, T1 p1)
        {
            return _wrap((object[] args) => handler.DynamicInvoke(args), p1);
        }

        public static same_func bind<P1, P2, T1, T2>(func<P1, P2> handler, T1 p1, T2 p2)
        {
            return _wrap((object[] args) => handler.DynamicInvoke(args), p1, p2);
        }

        public static same_func bind<P1, P2, P3, T1, T2, T3>(func<P1, P2, P3> handler, T1 p1, T2 p2, T3 p3)
        {
            return _wrap((object[] args) => handler.DynamicInvoke(args), p1, p2, p3);
        }

        public static same_func<R> bind<R>(func_res<R> handler)
        {
            return (object[] args) => (R)handler.DynamicInvoke(args);
        }

        public static same_func<R> bind<R, P1, T1>(func_res<R, P1> handler, T1 p1)
        {
            return _wrap_res((object[] args) => (R)handler.DynamicInvoke(args), p1);
        }

        public static same_func<R> bind<R, P1, P2, T1, T2>(func_res<R, P1, P2> handler, T1 p1, T2 p2)
        {
            return _wrap_res((object[] args) => (R)handler.DynamicInvoke(args), p1, p2);
        }

        public static same_func<R> bind<R, P1, P2, P3, T1, T2, T3>(func_res<R, P1, P2, P3> handler, T1 p1, T2 p2, T3 p3)
        {
            return _wrap_res((object[] args) => (R)handler.DynamicInvoke(args), p1, p2, p3);
        }

        public static void catch_invoke(func handler)
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

        public static void catch_invoke<T1>(func<T1> handler, T1 p1)
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

        public static void catch_invoke<T1, T2>(func<T1, T2> handler, T1 p1, T2 p2)
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

        public static void catch_invoke<T1, T2, T3>(func<T1, T2, T3> handler, T1 p1, T2 p2, T3 p3)
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
