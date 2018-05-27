using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO.Ports;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;

namespace Go
{
    internal class TypeReflection
    {
        public static readonly Assembly _systemAss = Assembly.LoadFrom(RuntimeEnvironment.GetRuntimeDirectory() + "System.dll");
        public static readonly Assembly _mscorlibAss = Assembly.LoadFrom(RuntimeEnvironment.GetRuntimeDirectory() + "mscorlib.dll");

        public static MethodInfo GetMethod(Type type, string name, Type[] parmsType = null)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == name)
                {
                    if (null == parmsType)
                    {
                        return methods[i];
                    }
                    else
                    {
                        ParameterInfo[] parameters = methods[i].GetParameters();
                        if (parameters.Length == parmsType.Length)
                        {
                            int j = 0;
                            for (; j < parmsType.Length && parameters[j].ParameterType == parmsType[j]; j++) { }
                            if (j == parmsType.Length)
                            {
                                return methods[i];
                            }
                        }
                    }
                }
            }
            return null;
        }

        public static FieldInfo GetField(Type type, string name)
        {
            FieldInfo[] members = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i].Name == name)
                {
                    return members[i];
                }
            }
            return null;
        }
    }

    public struct socket_result
    {
        public bool ok;
        public int s;
        public string message;

        public socket_result(bool resOk = false, int resSize = 0, string resMsg = null)
        {
            ok = resOk;
            s = resSize;
            message = resMsg;
        }

        static public implicit operator bool(socket_result src)
        {
            return src.ok;
        }

        static public implicit operator int(socket_result src)
        {
            return src.s;
        }
    }

    struct socket_same_handler
    {
        public object pinnedObj;
        public Action<socket_result> cb;
        public AsyncCallback handler;
    }

    struct socket_handler
    {
        public int currTotal;
        public IntPtr ptr;
        public int offset;
        public int size;
        public object pinnedObj;
        public Action<socket_result> handler;
        public Action<socket_result> cb;
    }

    struct accept_handler
    {
        public socket_tcp sck;
        public AsyncCallback handler;
        public Action<socket_result> cb;
    }

    public abstract class socket
    {
        [DllImport("kernel32.dll")]
        static extern int WriteFile(SafeHandle handle, IntPtr bytes, int numBytesToWrite, out int numBytesWritten, IntPtr mustBeZero);
        [DllImport("kernel32.dll")]
        static extern int ReadFile(SafeHandle handle, IntPtr bytes, int numBytesToRead, out int numBytesRead, IntPtr mustBeZero);
        [DllImport("kernel32.dll")]
        static extern int CancelIoEx(SafeHandle handle, IntPtr mustBeZero);

        static readonly Type _memoryAccessorType = TypeReflection._mscorlibAss.GetType("System.IO.UnmanagedMemoryAccessor");
        static readonly Type _memoryStreamType = TypeReflection._mscorlibAss.GetType("System.IO.UnmanagedMemoryStream");
        static readonly Type _safeHandleType = TypeReflection._mscorlibAss.GetType("System.Runtime.InteropServices.SafeHandle");
        static readonly FieldInfo _memoryAccessorSafeBuffer = TypeReflection.GetField(_memoryAccessorType, "_buffer");
        static readonly FieldInfo _memoryStreamSafeBuffer = TypeReflection.GetField(_memoryStreamType, "_buffer");
        static readonly FieldInfo _safeBufferHandle = TypeReflection.GetField(_safeHandleType, "handle");

        static public tuple<bool, int> sync_write(SafeHandle handle, IntPtr ptr, int offset, int size)
        {
            int num = 0;
            return new tuple<bool, int>(1 == WriteFile(handle, ptr + offset, size, out num, IntPtr.Zero), num);
        }

        static public tuple<bool, int> sync_read(SafeHandle handle, IntPtr ptr, int offset, int size)
        {
            int num = 0;
            return new tuple<bool, int>(1 == ReadFile(handle, ptr + offset, size, out num, IntPtr.Zero), num);
        }

        static public tuple<bool, int> sync_write(SafeHandle handle, byte[] bytes, int offset, int size)
        {
            return sync_write(handle, Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0), offset, size);
        }

        static public tuple<bool, int> sync_read(SafeHandle handle, byte[] bytes, int offset, int size)
        {
            return sync_read(handle, Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0), offset, size);
        }

        static public tuple<bool, int> sync_write(SafeHandle handle, byte[] bytes)
        {
            return sync_write(handle, bytes, 0, bytes.Length);
        }

        static public tuple<bool, int> sync_read(SafeHandle handle, byte[] bytes)
        {
            return sync_read(handle, bytes, 0, bytes.Length);
        }

        static public bool cancel_io(SafeHandle handle)
        {
            return 1 == CancelIoEx(handle, IntPtr.Zero);
        }

        static public IntPtr get_mmview_ptr(MemoryMappedViewAccessor mmv)
        {
            return (IntPtr)_safeBufferHandle.GetValue(_memoryAccessorSafeBuffer.GetValue(mmv));
        }

        static public IntPtr get_mmview_ptr(MemoryMappedViewStream mmv)
        {
            return (IntPtr)_safeBufferHandle.GetValue(_memoryStreamSafeBuffer.GetValue(mmv));
        }

        public abstract void async_read_same(ArraySegment<byte> buff, Action<socket_result> cb);
        public abstract void async_write_same(ArraySegment<byte> buff, Action<socket_result> cb);
        public abstract void close();

        public void async_read_same(byte[] buff, Action<socket_result> cb)
        {
            async_read_same(new ArraySegment<byte>(buff), cb);
        }

        public void async_write_same(byte[] buff, Action<socket_result> cb)
        {
            async_write_same(new ArraySegment<byte>(buff), cb);
        }

        void _async_reads(int currTotal, int currIndex, IList<ArraySegment<byte>> buff, Action<socket_result> cb)
        {
            if (buff.Count == currIndex)
            {
                functional.catch_invoke(cb, new socket_result(true, currTotal));
            }
            else
            {
                async_read(buff[currIndex], delegate (socket_result tempRes)
                {
                    if (tempRes.ok)
                    {
                        _async_reads(currTotal + tempRes.s, currIndex + 1, buff, cb);
                    }
                    else
                    {
                        functional.catch_invoke(cb, new socket_result(false, currTotal, tempRes.message));
                    }
                });
            }
        }

        void _async_reads(int currTotal, int currIndex, IList<byte[]> buff, Action<socket_result> cb)
        {
            if (buff.Count == currIndex)
            {
                functional.catch_invoke(cb, new socket_result(true, currTotal));
            }
            else
            {
                async_read(buff[currIndex], delegate (socket_result tempRes)
                {
                    if (tempRes.ok)
                    {
                        _async_reads(currTotal + tempRes.s, currIndex + 1, buff, cb);
                    }
                    else
                    {
                        functional.catch_invoke(cb, new socket_result(false, currTotal, tempRes.message));
                    }
                });
            }
        }

        void _async_read(int currTotal, ArraySegment<byte> buff, Action<socket_result> cb)
        {
            async_read_same(buff, delegate (socket_result tempRes)
            {
                if (tempRes.ok)
                {
                    currTotal += tempRes.s;
                    if (buff.Count == tempRes.s)
                    {
                        functional.catch_invoke(cb, new socket_result(true, currTotal));
                    }
                    else
                    {
                        _async_read(currTotal, new ArraySegment<byte>(buff.Array, buff.Offset + tempRes.s, buff.Count - tempRes.s), cb);
                    }
                }
                else
                {
                    functional.catch_invoke(cb, new socket_result(false, currTotal, tempRes.message));
                }
            });
        }

        public void async_read(ArraySegment<byte> buff, Action<socket_result> cb)
        {
            _async_read(0, buff, cb);
        }

        public void async_read(byte[] buff, Action<socket_result> cb)
        {
            _async_read(0, new ArraySegment<byte>(buff), cb);
        }

        void _async_writes(int currTotal, int currIndex, IList<ArraySegment<byte>> buff, Action<socket_result> cb)
        {
            if (buff.Count == currIndex)
            {
                functional.catch_invoke(cb, new socket_result(true, currTotal));
            }
            else
            {
                async_write(buff[currIndex], delegate (socket_result tempRes)
                {
                    if (tempRes.ok)
                    {
                        _async_writes(currTotal + tempRes.s, currIndex + 1, buff, cb);
                    }
                    else
                    {
                        functional.catch_invoke(cb, new socket_result(false, currTotal, tempRes.message));
                    }
                });
            }
        }

        void _async_writes(int currTotal, int currIndex, IList<byte[]> buff, Action<socket_result> cb)
        {
            if (buff.Count == currIndex)
            {
                functional.catch_invoke(cb, new socket_result(true, currTotal));
            }
            else
            {
                async_write(buff[currIndex], delegate (socket_result tempRes)
                {
                    if (tempRes.ok)
                    {
                        _async_writes(currTotal + tempRes.s, currIndex + 1, buff, cb);
                    }
                    else
                    {
                        functional.catch_invoke(cb, new socket_result(false, currTotal, tempRes.message));
                    }
                });
            }
        }

        void _async_write(int currTotal, ArraySegment<byte> buff, Action<socket_result> cb)
        {
            async_write_same(buff, delegate (socket_result tempRes)
            {
                if (tempRes.ok)
                {
                    currTotal += tempRes.s;
                    if (buff.Count == tempRes.s)
                    {
                        functional.catch_invoke(cb, new socket_result(true, currTotal));
                    }
                    else
                    {
                        _async_write(currTotal, new ArraySegment<byte>(buff.Array, buff.Offset + tempRes.s, buff.Count - tempRes.s), cb);
                    }
                }
                else
                {
                    functional.catch_invoke(cb, new socket_result(false, currTotal, tempRes.message));
                }
            });
        }

        public void async_write(ArraySegment<byte> buff, Action<socket_result> cb)
        {
            _async_write(0, buff, cb);
        }

        public void async_write(byte[] buff, Action<socket_result> cb)
        {
            _async_write(0, new ArraySegment<byte>(buff), cb);
        }

        public ValueTask<socket_result> read_same(ArraySegment<byte> buff)
        {
            return generator.async_call((Action<socket_result> cb) => async_read_same(buff, cb));
        }

        public ValueTask<socket_result> read_same(byte[] buff)
        {
            return generator.async_call((Action<socket_result> cb) => async_read_same(buff, cb));
        }

        public ValueTask<socket_result> read(ArraySegment<byte> buff)
        {
            return generator.async_call((Action<socket_result> cb) => async_read(buff, cb));
        }

        public ValueTask<socket_result> read(byte[] buff)
        {
            return generator.async_call((Action<socket_result> cb) => async_read(buff, cb));
        }

        public ValueTask<socket_result> reads(IList<byte[]> buff)
        {
            return generator.async_call((Action<socket_result> cb) => _async_reads(0, 0, buff, cb));
        }

        public ValueTask<socket_result> reads(IList<ArraySegment<byte>> buff)
        {
            return generator.async_call((Action<socket_result> cb) => _async_reads(0, 0, buff, cb));
        }

        public ValueTask<socket_result> reads(params byte[][] buff)
        {
            return generator.async_call((Action<socket_result> cb) => _async_reads(0, 0, buff, cb));
        }

        public ValueTask<socket_result> reads(params ArraySegment<byte>[] buff)
        {
            return generator.async_call((Action<socket_result> cb) => _async_reads(0, 0, buff, cb));
        }

        public ValueTask<socket_result> write_same(ArraySegment<byte> buff)
        {
            return generator.async_call((Action<socket_result> cb) => async_write_same(buff, cb));
        }

        public ValueTask<socket_result> write_same(byte[] buff)
        {
            return generator.async_call((Action<socket_result> cb) => async_write_same(buff, cb));
        }

        public ValueTask<socket_result> write(ArraySegment<byte> buff)
        {
            return generator.async_call((Action<socket_result> cb) => async_write(buff, cb));
        }

        public ValueTask<socket_result> write(byte[] buff)
        {
            return generator.async_call((Action<socket_result> cb) => async_write(buff, cb));
        }

        public ValueTask<socket_result> writes(IList<byte[]> buff)
        {
            return generator.async_call((Action<socket_result> cb) => _async_writes(0, 0, buff, cb));
        }

        public ValueTask<socket_result> writes(IList<ArraySegment<byte>> buff)
        {
            return generator.async_call((Action<socket_result> cb) => _async_writes(0, 0, buff, cb));
        }

        public ValueTask<socket_result> writes(params byte[][] buff)
        {
            return generator.async_call((Action<socket_result> cb) => _async_writes(0, 0, buff, cb));
        }

        public ValueTask<socket_result> writes(params ArraySegment<byte>[] buff)
        {
            return generator.async_call((Action<socket_result> cb) => _async_writes(0, 0, buff, cb));
        }

        public Task unsafe_read_same(async_result_wrap<socket_result> res, ArraySegment<byte> buff)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_read_same(buff, cb));
        }

        public Task unsafe_read_same(async_result_wrap<socket_result> res, byte[] buff)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_read_same(buff, cb));
        }

        public Task unsafe_read(async_result_wrap<socket_result> res, ArraySegment<byte> buff)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_read(buff, cb));
        }

        public Task unsafe_read(async_result_wrap<socket_result> res, byte[] buff)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_read(buff, cb));
        }

        public Task unsafe_reads(async_result_wrap<socket_result> res, IList<byte[]> buff)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => _async_reads(0, 0, buff, cb));
        }

        public Task unsafe_reads(async_result_wrap<socket_result> res, IList<ArraySegment<byte>> buff)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => _async_reads(0, 0, buff, cb));
        }

        public Task unsafe_reads(async_result_wrap<socket_result> res, params byte[][] buff)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => _async_reads(0, 0, buff, cb));
        }

        public Task unsafe_reads(async_result_wrap<socket_result> res, params ArraySegment<byte>[] buff)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => _async_reads(0, 0, buff, cb));
        }

        public Task unsafe_write_same(async_result_wrap<socket_result> res, ArraySegment<byte> buff)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_write_same(buff, cb));
        }

        public Task unsafe_write_same(async_result_wrap<socket_result> res, byte[] buff)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_write_same(buff, cb));
        }

        public Task unsafe_write(async_result_wrap<socket_result> res, ArraySegment<byte> buff)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_write(buff, cb));
        }

        public Task unsafe_write(async_result_wrap<socket_result> res, byte[] buff)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_write(buff, cb));
        }

        public Task unsafe_writes(async_result_wrap<socket_result> res, IList<byte[]> buff)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => _async_writes(0, 0, buff, cb));
        }

        public Task unsafe_writes(async_result_wrap<socket_result> res, IList<ArraySegment<byte>> buff)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => _async_writes(0, 0, buff, cb));
        }

        public Task unsafe_writes(async_result_wrap<socket_result> res, params byte[][] buff)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => _async_writes(0, 0, buff, cb));
        }

        public Task unsafe_writes(async_result_wrap<socket_result> res, params ArraySegment<byte>[] buff)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => _async_writes(0, 0, buff, cb));
        }
    }

    public class socket_tcp : socket
    {
        class SocketAsyncIo
        {
            [DllImport("Ws2_32.dll", CharSet = CharSet.None, ExactSpelling = false, SetLastError = true)]
            static extern SocketError WSASend(IntPtr socketHandle, IntPtr buffer, int bufferCount, out int bytesTransferred, SocketFlags socketFlags, SafeHandle overlapped, IntPtr completionRoutine);
            [DllImport("Ws2_32.dll", CharSet = CharSet.None, ExactSpelling = false, SetLastError = true)]
            static extern SocketError WSARecv(IntPtr socketHandle, IntPtr buffer, int bufferCount, out int bytesTransferred, ref SocketFlags socketFlags, SafeHandle overlapped, IntPtr completionRoutine);
            [DllImport("Kernel32.dll", CharSet = CharSet.None, ExactSpelling = false, SetLastError = true)]
            static extern int SetFilePointerEx(SafeHandle fileHandle, long liDistanceToMove, out long lpNewFilePointer, int dwMoveMethod);
            [DllImport("Kernel32.dll", CharSet = CharSet.None, ExactSpelling = false, SetLastError = true)]
            static extern int GetFileSizeEx(SafeHandle fileHandle, out long lpFileSize);
            [DllImport("Mswsock.dll", CharSet = CharSet.None, ExactSpelling = false, SetLastError = true)]
            static extern int TransmitFile(IntPtr socketHandle, SafeHandle fileHandle, int nNumberOfBytesToWrite, int nNumberOfBytesPerSend, SafeHandle overlapped, IntPtr mustBeZero, int dwFlags);

            static readonly Type _overlappedType = TypeReflection._systemAss.GetType("System.Net.Sockets.OverlappedAsyncResult");
            static readonly Type _cacheSetType = TypeReflection._systemAss.GetType("System.Net.Sockets.Socket+CacheSet");
            static readonly Type _callbackClosureType = TypeReflection._systemAss.GetType("System.Net.CallbackClosure");
            static readonly Type _callbackClosureRefType = TypeReflection._systemAss.GetType("System.Net.CallbackClosure&");
            static readonly Type _overlappedCacheRefType = TypeReflection._systemAss.GetType("System.Net.Sockets.OverlappedCache&");
            static readonly Type _WSABufferType = TypeReflection._systemAss.GetType("System.Net.WSABuffer");
            static readonly MethodInfo _overlappedStartPostingAsyncOp = TypeReflection.GetMethod(_overlappedType, "StartPostingAsyncOp", new Type[] { typeof(bool) });
            static readonly MethodInfo _overlappedFinishPostingAsyncOp = TypeReflection.GetMethod(_overlappedType, "FinishPostingAsyncOp", new Type[] { _callbackClosureRefType });
            static readonly MethodInfo _overlappedSetupCache = TypeReflection.GetMethod(_overlappedType, "SetupCache", new Type[] { _overlappedCacheRefType });
            static readonly MethodInfo _overlappedExtractCache = TypeReflection.GetMethod(_overlappedType, "ExtractCache", new Type[] { _overlappedCacheRefType });
            static readonly MethodInfo _overlappedSetUnmanagedStructures = TypeReflection.GetMethod(_overlappedType, "SetUnmanagedStructures", new Type[] { typeof(object) });
            static readonly MethodInfo _overlappedOverlappedHandle = TypeReflection.GetMethod(_overlappedType, "get_OverlappedHandle");
            static readonly MethodInfo _overlappedCheckAsyncCallOverlappedResult = TypeReflection.GetMethod(_overlappedType, "CheckAsyncCallOverlappedResult", new Type[] { typeof(SocketError) });
            static readonly MethodInfo _overlappedInvokeCallback = TypeReflection.GetMethod(_overlappedType, "InvokeCallback", new Type[] { typeof(object) });
            static readonly MethodInfo _socketCaches = TypeReflection.GetMethod(typeof(Socket), "get_Caches");
            static readonly MethodInfo _sockektUpdateStatusAfterSocketError = TypeReflection.GetMethod(typeof(Socket), "UpdateStatusAfterSocketError", new Type[] { typeof(SocketError) });
            static readonly FieldInfo _overlappedmSingleBuffer = TypeReflection.GetField(_overlappedType, "m_SingleBuffer");
            static readonly FieldInfo _WSABufferLength = TypeReflection.GetField(_WSABufferType, "Length");
            static readonly FieldInfo _WSABufferPointer = TypeReflection.GetField(_WSABufferType, "Pointer");
            static readonly FieldInfo _cacheSendClosureCache = TypeReflection.GetField(_cacheSetType, "SendClosureCache");
            static readonly FieldInfo _cacheSendOverlappedCache = TypeReflection.GetField(_cacheSetType, "SendOverlappedCache");
            static readonly FieldInfo _cacheReceiveClosureCache = TypeReflection.GetField(_cacheSetType, "ReceiveClosureCache");
            static readonly FieldInfo _cacheReceiveOverlappedCache = TypeReflection.GetField(_cacheSetType, "ReceiveOverlappedCache");

            static private IAsyncResult MakeOverlappedAsyncResult(Socket sck, AsyncCallback callback)
            {
                return (IAsyncResult)TypeReflection._systemAss.CreateInstance(_overlappedType.FullName, true,
                    BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new object[] { sck, null, callback }, null, null);
            }

            static readonly object[] startPostingAsyncOpParam = new object[] { false };
            static private void StartPostingAsyncOp(IAsyncResult overlapped)
            {
                _overlappedStartPostingAsyncOp.Invoke(overlapped, startPostingAsyncOpParam);
            }

            static private void FinishPostingAsyncSendOp(Socket sck, IAsyncResult overlapped, object cacheSet)
            {
                object oldClosure = _cacheSendClosureCache.GetValue(cacheSet);
                object[] closure = new object[] { oldClosure };
                _overlappedFinishPostingAsyncOp.Invoke(overlapped, closure);
                if (oldClosure != closure[0])
                {
                    _cacheSendClosureCache.SetValue(cacheSet, closure[0]);
                }
            }

            static private void FinishPostingAsyncRecvOp(Socket sck, IAsyncResult overlapped, object cacheSet)
            {
                object oldClosure = _cacheReceiveClosureCache.GetValue(cacheSet);
                object[] closure = new object[] { oldClosure };
                _overlappedFinishPostingAsyncOp.Invoke(overlapped, closure);
                if (oldClosure != closure[0])
                {
                    _cacheReceiveClosureCache.SetValue(cacheSet, closure[0]);
                }
            }

            static readonly object[] nullPinnedObj = new object[] { null };
            static private void SetUnmanagedStructures(IAsyncResult overlapped, object pinnedObj)
            {
                _overlappedSetUnmanagedStructures.Invoke(overlapped, null == pinnedObj ? nullPinnedObj : new object[] { pinnedObj });
            }

            static private IntPtr SetSendPointer(Socket sck, IAsyncResult overlapped, IntPtr ptr, int offset, int size, object cacheSet, object pinnedObj)
            {
                object oldCache = _cacheSendOverlappedCache.GetValue(cacheSet);
                object[] overlappedCache = new object[] { oldCache };
                _overlappedSetupCache.Invoke(overlapped, overlappedCache);
                if (oldCache != overlappedCache[0])
                {
                    _cacheSendOverlappedCache.SetValue(cacheSet, overlappedCache[0]);
                }
                SetUnmanagedStructures(overlapped, pinnedObj);
                object WSABuffer = _overlappedmSingleBuffer.GetValue(overlapped);
                _WSABufferPointer.SetValue(WSABuffer, ptr + offset);
                _WSABufferLength.SetValue(WSABuffer, size);
                _overlappedmSingleBuffer.SetValue(overlapped, WSABuffer);
                return GCHandle.Alloc(WSABuffer, GCHandleType.Pinned).AddrOfPinnedObject();
            }

            static private IntPtr SetRecvPointer(Socket sck, IAsyncResult overlapped, IntPtr ptr, int offset, int size, object cacheSet, object pinnedObj)
            {
                object oldCache = _cacheReceiveOverlappedCache.GetValue(cacheSet);
                object[] overlappedCache = new object[] { oldCache };
                _overlappedSetupCache.Invoke(overlapped, overlappedCache);
                if (oldCache != overlappedCache[0])
                {
                    _cacheReceiveOverlappedCache.SetValue(cacheSet, overlappedCache[0]);
                }
                SetUnmanagedStructures(overlapped, pinnedObj);
                object WSABuffer = _overlappedmSingleBuffer.GetValue(overlapped);
                _WSABufferPointer.SetValue(WSABuffer, ptr + offset);
                _WSABufferLength.SetValue(WSABuffer, size);
                _overlappedmSingleBuffer.SetValue(overlapped, WSABuffer);
                return GCHandle.Alloc(WSABuffer, GCHandleType.Pinned).AddrOfPinnedObject();
            }

            static public SocketError Send(Socket sck, IntPtr ptr, int offset, int size, SocketFlags socketFlags, AsyncCallback callback)
            {
                IAsyncResult overlapped = MakeOverlappedAsyncResult(sck, callback);
                StartPostingAsyncOp(overlapped);
                object cacheSet = _socketCaches.Invoke(sck, null);
                IntPtr WSABuffer = SetSendPointer(sck, overlapped, ptr, offset, size, cacheSet, null);
                int num = 0;
                SafeHandle overlappedHandle = (SafeHandle)_overlappedOverlappedHandle.Invoke(overlapped, null);
                SocketError lastWin32Error = WSASend(sck.Handle, WSABuffer, 1, out num, socketFlags, overlappedHandle, IntPtr.Zero);
                if (SocketError.Success != lastWin32Error)
                {
                    lastWin32Error = (SocketError)Marshal.GetLastWin32Error();
                }
                if (SocketError.Success == lastWin32Error || SocketError.IOPending == lastWin32Error)
                {
                    FinishPostingAsyncSendOp(sck, overlapped, cacheSet);
                    return SocketError.Success;
                }
                lastWin32Error = (SocketError)_overlappedCheckAsyncCallOverlappedResult.Invoke(overlapped, new object[] { lastWin32Error });
                object oldCache = _cacheSendOverlappedCache.GetValue(cacheSet);
                object[] overlappedCache = new object[] { oldCache };
                _overlappedExtractCache.Invoke(overlapped, overlappedCache);
                if (oldCache != overlappedCache[0])
                {
                    _cacheSendOverlappedCache.SetValue(cacheSet, overlappedCache[0]);
                }
                _sockektUpdateStatusAfterSocketError.Invoke(sck, new object[] { lastWin32Error });
                return lastWin32Error;
            }

            static public SocketError Recv(Socket sck, IntPtr ptr, int offset, int size, SocketFlags socketFlags, AsyncCallback callback)
            {
                IAsyncResult overlapped = MakeOverlappedAsyncResult(sck, callback);
                StartPostingAsyncOp(overlapped);
                object cacheSet = _socketCaches.Invoke(sck, null);
                IntPtr WSABuffer = SetRecvPointer(sck, overlapped, ptr, offset, size, cacheSet, null);
                int num = 0;
                SafeHandle overlappedHandle = (SafeHandle)_overlappedOverlappedHandle.Invoke(overlapped, null);
                SocketError lastWin32Error = WSARecv(sck.Handle, WSABuffer, 1, out num, ref socketFlags, overlappedHandle, IntPtr.Zero);
                if (SocketError.Success != lastWin32Error)
                {
                    lastWin32Error = (SocketError)Marshal.GetLastWin32Error();
                }
                if (SocketError.Success == lastWin32Error || SocketError.IOPending == lastWin32Error)
                {
                    FinishPostingAsyncRecvOp(sck, overlapped, cacheSet);
                    return SocketError.Success;
                }
                lastWin32Error = (SocketError)_overlappedCheckAsyncCallOverlappedResult.Invoke(overlapped, new object[] { lastWin32Error });
                object oldCache = _cacheReceiveOverlappedCache.GetValue(cacheSet);
                object[] overlappedCache = new object[] { oldCache };
                _overlappedExtractCache.Invoke(overlapped, overlappedCache);
                if (oldCache != overlappedCache[0])
                {
                    _cacheReceiveOverlappedCache.SetValue(cacheSet, overlappedCache[0]);
                }
                _sockektUpdateStatusAfterSocketError.Invoke(sck, new object[] { lastWin32Error });
                _overlappedInvokeCallback.Invoke(overlapped, new object[] { new SocketException((int)lastWin32Error) });
                return lastWin32Error;
            }

            static public SocketError SendFile(Socket sck, SafeHandle fileHandle, long offset, int size, AsyncCallback callback)
            {
                IAsyncResult overlapped = MakeOverlappedAsyncResult(sck, callback);
                StartPostingAsyncOp(overlapped);
                object cacheSet = _socketCaches.Invoke(sck, null);
                SocketError lastWin32Error = SocketError.SocketError;
                do
                {
                    if (offset >= 0)
                    {
                        long newOffset = 0;
                        if (0 == SetFilePointerEx(fileHandle, offset, out newOffset, 0) || offset != newOffset)
                        {
                            break;
                        }
                    }
                    lastWin32Error = SocketError.Success;
                    SetSendPointer(sck, overlapped, IntPtr.Zero, 0, 0, cacheSet, null);
                    SafeHandle overlappedHandle = (SafeHandle)_overlappedOverlappedHandle.Invoke(overlapped, null);
                    if (0 == TransmitFile(sck.Handle, fileHandle, size, 0, overlappedHandle, IntPtr.Zero, 0x20 | 0x04))
                    {
                        lastWin32Error = (SocketError)Marshal.GetLastWin32Error();
                    }
                    if (SocketError.Success == lastWin32Error || SocketError.IOPending == lastWin32Error)
                    {
                        FinishPostingAsyncSendOp(sck, overlapped, cacheSet);
                        return SocketError.Success;
                    }
                } while (false);
                lastWin32Error = (SocketError)_overlappedCheckAsyncCallOverlappedResult.Invoke(overlapped, new object[] { lastWin32Error });
                object oldCache = _cacheSendOverlappedCache.GetValue(cacheSet);
                object[] overlappedCache = new object[] { oldCache };
                _overlappedExtractCache.Invoke(overlapped, overlappedCache);
                if (oldCache != overlappedCache[0])
                {
                    _cacheSendOverlappedCache.SetValue(cacheSet, overlappedCache[0]);
                }
                _sockektUpdateStatusAfterSocketError.Invoke(sck, new object[] { lastWin32Error });
                return lastWin32Error;
            }
        }

        Socket _socket;
        socket_same_handler _readSameHandler;
        socket_same_handler _writeSameHandler;
        socket_handler _readHandler;
        socket_handler _writeHandler;

        public socket_tcp()
        {
            _readSameHandler.pinnedObj = null;
            _readSameHandler.cb = null;
            _readSameHandler.handler = delegate (IAsyncResult ar)
            {
                object pinnedObj = _readSameHandler.pinnedObj;
                Action<socket_result> cb = _readSameHandler.cb;
                _readSameHandler.pinnedObj = null;
                _readSameHandler.cb = null;
                try
                {
                    int s = _socket.EndReceive(ar);
                    functional.catch_invoke(cb, new socket_result(0 != s, s));
                }
                catch (System.Exception ec)
                {
                    functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
                }
            };
            _writeSameHandler.pinnedObj = null;
            _writeSameHandler.cb = null;
            _writeSameHandler.handler = delegate (IAsyncResult ar)
            {
                object pinnedObj = _writeSameHandler.pinnedObj;
                Action<socket_result> cb = _writeSameHandler.cb;
                _writeSameHandler.pinnedObj = null;
                _writeSameHandler.cb = null;
                try
                {
                    int s = _socket.EndSend(ar);
                    functional.catch_invoke(cb, new socket_result(0 != s, s));
                }
                catch (System.Exception ec)
                {
                    functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
                }
            };
            _readHandler.pinnedObj = null;
            _readHandler.cb = null;
            _readHandler.handler = delegate (socket_result tempRes)
            {
                if (tempRes.ok)
                {
                    _readHandler.currTotal += tempRes.s;
                    if (_readHandler.size == tempRes.s)
                    {
                        object pinnedObj = _readHandler.pinnedObj;
                        Action<socket_result> cb = _readHandler.cb;
                        _readHandler.pinnedObj = null;
                        _readHandler.cb = null;
                        functional.catch_invoke(cb, new socket_result(true, _readHandler.currTotal));
                    }
                    else
                    {
                        _async_read(_readHandler.currTotal, _readHandler.ptr, _readHandler.offset + tempRes.s, _readHandler.size - tempRes.s, _readHandler.cb, _readHandler.pinnedObj);
                    }
                }
                else
                {
                    object pinnedObj = _readHandler.pinnedObj;
                    Action<socket_result> cb = _readHandler.cb;
                    _readHandler.pinnedObj = null;
                    _readHandler.cb = null;
                    functional.catch_invoke(cb, new socket_result(false, _readHandler.currTotal, tempRes.message));
                }
            };
            _writeHandler.pinnedObj = null;
            _writeHandler.cb = null;
            _writeHandler.handler = delegate (socket_result tempRes)
            {
                if (tempRes.ok)
                {
                    _writeHandler.currTotal += tempRes.s;
                    if (_writeHandler.size == tempRes.s)
                    {
                        object pinnedObj = _writeHandler.pinnedObj;
                        Action<socket_result> cb = _writeHandler.cb;
                        _writeHandler.pinnedObj = null;
                        _writeHandler.cb = null;
                        functional.catch_invoke(cb, new socket_result(true, _writeHandler.currTotal));
                    }
                    else
                    {
                        _async_write(_writeHandler.currTotal, _writeHandler.ptr, _writeHandler.offset + tempRes.s, _writeHandler.size - tempRes.s, _writeHandler.cb, _writeHandler.pinnedObj);
                    }
                }
                else
                {
                    object pinnedObj = _writeHandler.pinnedObj;
                    Action<socket_result> cb = _writeHandler.cb;
                    _writeHandler.pinnedObj = null;
                    _writeHandler.cb = null;
                    functional.catch_invoke(cb, new socket_result(false, _writeHandler.currTotal, tempRes.message));
                }
            };
        }

        public Socket socket
        {
            get
            {
                return _socket;
            }
        }

        public override void close()
        {
            try
            {
                _socket.Close();
            }
            catch (System.Exception) { }
        }

        public void async_connect(string ip, int port, Action<socket_result> cb)
        {
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.BeginConnect(IPAddress.Parse(ip), port, delegate (IAsyncResult ar)
                {
                    try
                    {
                        _socket.EndConnect(ar);
                        _socket.NoDelay = true;
                        functional.catch_invoke(cb, new socket_result(true));
                    }
                    catch (System.Exception ec)
                    {
                        functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
                    }
                }, null);
            }
            catch (System.Exception ec)
            {
                close();
                functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
            }
        }

        public void async_disconnect(bool reuseSocket, Action<socket_result> cb)
        {
            try
            {
                _socket.BeginDisconnect(reuseSocket, delegate (IAsyncResult ar)
                {
                    try
                    {
                        _socket.EndDisconnect(ar);
                        functional.catch_invoke(cb, new socket_result(true));
                    }
                    catch (System.Exception ec)
                    {
                        functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
                    }
                }, null);
            }
            catch (System.Exception ec)
            {
                close();
                functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
            }
        }

        public override void async_read_same(ArraySegment<byte> buff, Action<socket_result> cb)
        {
            try
            {
                _readSameHandler.cb = cb;
                _socket.BeginReceive(buff.Array, buff.Offset, buff.Count, 0, _readSameHandler.handler, null);
            }
            catch (System.Exception ec)
            {
                close();
                functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
            }
        }

        public override void async_write_same(ArraySegment<byte> buff, Action<socket_result> cb)
        {
            try
            {
                _writeSameHandler.cb = cb;
                _socket.BeginSend(buff.Array, buff.Offset, buff.Count, 0, _writeSameHandler.handler, null);
            }
            catch (System.Exception ec)
            {
                close();
                functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
            }
        }

        public void async_send_file(SafeHandle fileHandle, long offset, int size, Action<socket_result> cb, object pinnedObj = null)
        {
            try
            {
                _writeSameHandler.pinnedObj = pinnedObj;
                _writeSameHandler.cb = cb;
                SocketError lastWin32Error = SocketAsyncIo.SendFile(_socket, fileHandle, offset, size, _writeSameHandler.handler);
                if (SocketError.Success != lastWin32Error)
                {
                    close();
                    functional.catch_invoke(cb, new socket_result(false, (int)lastWin32Error));
                }
            }
            catch (System.Exception ec)
            {
                close();
                functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
            }
        }

        public void async_read_same(IntPtr ptr, int offset, int size, Action<socket_result> cb, object pinnedObj = null)
        {
            try
            {
                _readSameHandler.pinnedObj = pinnedObj;
                _readSameHandler.cb = cb;
                SocketError lastWin32Error = SocketAsyncIo.Recv(_socket, ptr, offset, size, 0, _readSameHandler.handler);
                if (SocketError.Success != lastWin32Error)
                {
                    close();
                    functional.catch_invoke(cb, new socket_result(false, (int)lastWin32Error));
                }
            }
            catch (System.Exception ec)
            {
                close();
                functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
            }
        }

        public void async_write_same(IntPtr ptr, int offset, int size, Action<socket_result> cb, object pinnedObj = null)
        {
            try
            {
                _writeSameHandler.pinnedObj = pinnedObj;
                _writeSameHandler.cb = cb;
                SocketError lastWin32Error = SocketAsyncIo.Send(_socket, ptr, offset, size, 0, _writeSameHandler.handler);
                if (SocketError.Success != lastWin32Error)
                {
                    close();
                    functional.catch_invoke(cb, new socket_result(false, (int)lastWin32Error));
                }
            }
            catch (System.Exception ec)
            {
                close();
                functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
            }
        }

        void _async_read(int currTotal, IntPtr ptr, int offset, int size, Action<socket_result> cb, object pinnedObj)
        {
            _readHandler.currTotal = currTotal;
            _readHandler.ptr = ptr;
            _readHandler.offset = offset;
            _readHandler.size = size;
            _readHandler.pinnedObj = pinnedObj;
            async_read_same(ptr, offset, size, _readHandler.handler);
        }

        void _async_write(int currTotal, IntPtr ptr, int offset, int size, Action<socket_result> cb, object pinnedObj)
        {
            _writeHandler.currTotal = currTotal;
            _writeHandler.ptr = ptr;
            _writeHandler.offset = offset;
            _writeHandler.size = size;
            _writeHandler.pinnedObj = pinnedObj;
            async_write_same(ptr, offset, size, _writeHandler.handler);
        }

        public void async_read(IntPtr ptr, int offset, int size, Action<socket_result> cb, object pinnedObj = null)
        {
            _async_read(0, ptr, offset, size, cb, pinnedObj);
        }

        public void async_write(IntPtr ptr, int offset, int size, Action<socket_result> cb, object pinnedObj = null)
        {
            _async_write(0, ptr, offset, size, cb, pinnedObj);
        }

        public ValueTask<socket_result> read_same(IntPtr ptr, int offset, int size, object pinnedObj = null)
        {
            return generator.async_call((Action<socket_result> cb) => async_read_same(ptr, offset, size, cb, pinnedObj));
        }

        public ValueTask<socket_result> write_same(IntPtr ptr, int offset, int size, object pinnedObj = null)
        {
            return generator.async_call((Action<socket_result> cb) => async_write_same(ptr, offset, size, cb, pinnedObj));
        }

        public ValueTask<socket_result> read(IntPtr ptr, int offset, int size, object pinnedObj = null)
        {
            return generator.async_call((Action<socket_result> cb) => async_read(ptr, offset, size, cb, pinnedObj));
        }

        public ValueTask<socket_result> write(IntPtr ptr, int offset, int size, object pinnedObj = null)
        {
            return generator.async_call((Action<socket_result> cb) => async_write(ptr, offset, size, cb, pinnedObj));
        }

        public ValueTask<socket_result> send_file(SafeHandle fileHandle, long offset = 0, int size = 0, object pinnedObj = null)
        {
            return generator.async_call((Action<socket_result> cb) => async_send_file(fileHandle, offset, size, cb, pinnedObj));
        }

        public ValueTask<socket_result> send_file(System.IO.FileStream file)
        {
            return generator.async_call((Action<socket_result> cb) => async_send_file(file.SafeFileHandle, -1, 0, cb, file));
        }

        public ValueTask<socket_result> connect(string ip, int port)
        {
            return generator.async_call((Action<socket_result> cb) => async_connect(ip, port, cb));
        }

        public ValueTask<socket_result> disconnect(bool reuseSocket)
        {
            return generator.async_call((Action<socket_result> cb) => async_disconnect(reuseSocket, cb));
        }

        public Task unsafe_read_same(async_result_wrap<socket_result> res, IntPtr ptr, int offset, int size, object pinnedObj = null)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_read_same(ptr, offset, size, cb, pinnedObj));
        }

        public Task unsafe_write_same(async_result_wrap<socket_result> res, IntPtr ptr, int offset, int size, object pinnedObj = null)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_write_same(ptr, offset, size, cb, pinnedObj));
        }

        public Task unsafe_read(async_result_wrap<socket_result> res, IntPtr ptr, int offset, int size, object pinnedObj = null)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_read(ptr, offset, size, cb, pinnedObj));
        }

        public Task unsafe_write(async_result_wrap<socket_result> res, IntPtr ptr, int offset, int size, object pinnedObj = null)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_write(ptr, offset, size, cb, pinnedObj));
        }

        public Task unsafe_send_file(async_result_wrap<socket_result> res, SafeHandle fileHandle, long offset = 0, int size = 0, object pinnedObj = null)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_send_file(fileHandle, offset, size, cb, pinnedObj));
        }

        public Task unsafe_send_file(async_result_wrap<socket_result> res, System.IO.FileStream file)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_send_file(file.SafeFileHandle, -1, 0, cb, file));
        }

        public Task unsafe_connect(async_result_wrap<socket_result> res, string ip, int port)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_connect(ip, port, cb));
        }

        public Task unsafe_disconnect(async_result_wrap<socket_result> res, bool reuseSocket)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_disconnect(reuseSocket, cb));
        }

        public class acceptor
        {
            Socket _socket;
            accept_handler _acceptHandler;

            public acceptor()
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _acceptHandler.sck = null;
                _acceptHandler.cb = null;
                _acceptHandler.handler = delegate (IAsyncResult ar)
                {
                    socket_tcp sck = _acceptHandler.sck;
                    Action<socket_result> cb = _acceptHandler.cb;
                    _acceptHandler.sck = null;
                    _acceptHandler.cb = null;
                    try
                    {
                        sck._socket = _socket.EndAccept(ar);
                        sck._socket.NoDelay = true;
                        functional.catch_invoke(cb, new socket_result(true));
                    }
                    catch (System.Exception ec)
                    {
                        functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
                    }
                };
            }

            public Socket socket
            {
                get
                {
                    return _socket;
                }
            }

            public bool resue
            {
                set
                {
                    try
                    {
                        _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, value);
                    }
                    catch (System.Exception) { }
                }
            }

            public bool bind(string ip, int port)
            {
                try
                {
                    _socket.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
                    _socket.Listen(1);
                    return true;

                }
                catch (System.Exception) { }
                return false;
            }

            public void close()
            {
                try
                {
                    _socket.Close();
                }
                catch (System.Exception) { }
            }

            public void async_accept(socket_tcp sck, Action<socket_result> cb)
            {
                try
                {
                    sck.close();
                    _acceptHandler.sck = sck;
                    _acceptHandler.cb = cb;
                    _socket.BeginAccept(_acceptHandler.handler, null);
                }
                catch (System.Exception ec)
                {
                    close();
                    functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
                }
            }

            public ValueTask<socket_result> accept(socket_tcp sck)
            {
                return generator.async_call((Action<socket_result> cb) => async_accept(sck, cb));
            }

            public Task unsafe_accept(async_result_wrap<socket_result> res, socket_tcp sck)
            {
                return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_accept(sck, cb));
            }
        }
    }

    struct pipe_same_handler
    {
        public ArraySegment<byte> buff;
        public object pinnedObj;
        public Action<socket_result> cb;
        public AsyncCallback handler;
    }

#if NETCORE
#else
    public class socket_serial : socket
    {
        SerialPort _socket;
        pipe_same_handler _readSameHandler;
        pipe_same_handler _writeSameHandler;

        public socket_serial(string portName, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            _socket = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            _readSameHandler.pinnedObj = null;
            _readSameHandler.cb = null;
            _readSameHandler.handler = delegate (IAsyncResult ar)
            {
                Action<socket_result> cb = _readSameHandler.cb;
                _readSameHandler.cb = null;
                try
                {
                    int s = _socket.BaseStream.EndRead(ar);
                    functional.catch_invoke(cb, new socket_result(0 != s, s));
                }
                catch (System.Exception ec)
                {
                    functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
                }
            };
            _writeSameHandler.pinnedObj = null;
            _writeSameHandler.cb = null;
            _writeSameHandler.handler = delegate (IAsyncResult ar)
            {
                ArraySegment<byte> buff = _writeSameHandler.buff;
                Action<socket_result> cb = _writeSameHandler.cb;
                _writeSameHandler.buff = default(ArraySegment<byte>);
                _writeSameHandler.cb = null;
                try
                {
                    _socket.BaseStream.EndWrite(ar);
                    functional.catch_invoke(cb, new socket_result(true, buff.Count));
                }
                catch (System.Exception ec)
                {
                    functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
                }
            };
        }

        public SerialPort socket
        {
            get
            {
                return _socket;
            }
        }

        public bool open()
        {
            try
            {
                _socket.Open();
                return true;
            }
            catch (System.Exception) { }
            return false;
        }

        public override void close()
        {
            try
            {
                _socket.Close();
            }
            catch (System.Exception) { }
        }

        public override void async_read_same(ArraySegment<byte> buff, Action<socket_result> cb)
        {
            try
            {
                _readSameHandler.cb = cb;
                _socket.BaseStream.BeginRead(buff.Array, buff.Offset, buff.Count, _readSameHandler.handler, null);
            }
            catch (System.Exception ec)
            {
                close();
                functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
            }
        }

        public override void async_write_same(ArraySegment<byte> buff, Action<socket_result> cb)
        {
            try
            {
                _writeSameHandler.buff = buff;
                _writeSameHandler.cb = cb;
                _socket.BaseStream.BeginWrite(buff.Array, buff.Offset, buff.Count, _writeSameHandler.handler, null);
            }
            catch (System.Exception ec)
            {
                close();
                functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
            }
        }

        public void clear_in_buffer()
        {
            try
            {
                _socket.DiscardInBuffer();
            }
            catch (System.Exception) { }
        }

        public void clear_out_buffer()
        {
            try
            {
                _socket.DiscardOutBuffer();
            }
            catch (System.Exception) { }
        }
    }
#endif

    public abstract class socket_pipe : socket
    {
        protected PipeStream _socket;
        pipe_same_handler _readSameHandler;
        pipe_same_handler _writeSameHandler;
        socket_handler _readHandler;
        socket_handler _writeHandler;

        protected socket_pipe()
        {
            _readSameHandler.pinnedObj = null;
            _readSameHandler.cb = null;
            _readSameHandler.handler = delegate (IAsyncResult ar)
            {
                Action<socket_result> cb = _readSameHandler.cb;
                _readSameHandler.cb = null;
                try
                {
                    int s = _socket.EndRead(ar);
                    functional.catch_invoke(cb, new socket_result(0 != s, s));
                }
                catch (System.Exception ec)
                {
                    functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
                }
            };
            _writeSameHandler.pinnedObj = null;
            _writeSameHandler.cb = null;
            _writeSameHandler.handler = delegate (IAsyncResult ar)
            {
                ArraySegment<byte> buff = _writeSameHandler.buff;
                Action<socket_result> cb = _writeSameHandler.cb;
                _writeSameHandler.buff = default(ArraySegment<byte>);
                _writeSameHandler.cb = null;
                try
                {
                    _socket.EndWrite(ar);
                    functional.catch_invoke(cb, new socket_result(true, buff.Count));
                }
                catch (System.Exception ec)
                {
                    functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
                }
            };
            _readHandler.pinnedObj = null;
            _readHandler.cb = null;
            _readHandler.handler = delegate (socket_result tempRes)
            {
                if (tempRes.ok)
                {
                    _readHandler.currTotal += tempRes.s;
                    if (_readHandler.size == tempRes.s)
                    {
                        object pinnedObj = _readHandler.pinnedObj;
                        Action<socket_result> cb = _readHandler.cb;
                        _readHandler.pinnedObj = null;
                        _readHandler.cb = null;
                        functional.catch_invoke(cb, new socket_result(true, _readHandler.currTotal));
                    }
                    else
                    {
                        _async_read(_readHandler.currTotal, _readHandler.ptr, _readHandler.offset + tempRes.s, _readHandler.size - tempRes.s, _readHandler.cb, _readHandler.pinnedObj);
                    }
                }
                else
                {
                    object pinnedObj = _readHandler.pinnedObj;
                    Action<socket_result> cb = _readHandler.cb;
                    _readHandler.pinnedObj = null;
                    _readHandler.cb = null;
                    functional.catch_invoke(cb, new socket_result(false, _readHandler.currTotal, tempRes.message));
                }
            };
            _writeHandler.pinnedObj = null;
            _writeHandler.cb = null;
            _writeHandler.handler = delegate (socket_result tempRes)
            {
                if (tempRes.ok)
                {
                    _writeHandler.currTotal += tempRes.s;
                    if (_writeHandler.size == tempRes.s)
                    {
                        object pinnedObj = _writeHandler.pinnedObj;
                        Action<socket_result> cb = _writeHandler.cb;
                        _writeHandler.pinnedObj = null;
                        _writeHandler.cb = null;
                        functional.catch_invoke(cb, new socket_result(true, _writeHandler.currTotal));
                    }
                    else
                    {
                        _async_write(_writeHandler.currTotal, _writeHandler.ptr, _writeHandler.offset + tempRes.s, _writeHandler.size - tempRes.s, _writeHandler.cb, _writeHandler.pinnedObj);
                    }
                }
                else
                {
                    object pinnedObj = _writeHandler.pinnedObj;
                    Action<socket_result> cb = _writeHandler.cb;
                    _writeHandler.pinnedObj = null;
                    _writeHandler.cb = null;
                    functional.catch_invoke(cb, new socket_result(false, _writeHandler.currTotal, tempRes.message));
                }
            };
        }

        public override void async_read_same(ArraySegment<byte> buff, Action<socket_result> cb)
        {
            try
            {
                _readSameHandler.cb = cb;
                _socket.BeginRead(buff.Array, buff.Offset, buff.Count, _readSameHandler.handler, null);
            }
            catch (System.Exception ec)
            {
                close();
                functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
            }
        }

        public override void async_write_same(ArraySegment<byte> buff, Action<socket_result> cb)
        {
            try
            {
                _writeSameHandler.buff = buff;
                _writeSameHandler.cb = cb;
                _socket.BeginWrite(buff.Array, buff.Offset, buff.Count, _writeSameHandler.handler, null);
            }
            catch (System.Exception ec)
            {
                close();
                functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
            }
        }

        public void async_read_same(IntPtr ptr, int offset, int size, Action<socket_result> cb, object pinnedObj = null)
        {
            Task.Run(delegate ()
            {
                if (_socket.IsConnected)
                {
                    try
                    {
                        object holdPin = pinnedObj;
                        tuple<bool, int> res = sync_read(_socket.SafePipeHandle, ptr, offset, size);
                        functional.catch_invoke(cb, new socket_result(res.value1, res.value2));
                        return;
                    }
                    catch (System.Exception) { }
                }
                functional.catch_invoke(cb, new socket_result(false, 0));
            });
        }

        public void async_write_same(IntPtr ptr, int offset, int size, Action<socket_result> cb, object pinnedObj = null)
        {
            Task.Run(delegate ()
            {
                if (_socket.IsConnected)
                {
                    try
                    {
                        object holdPin = pinnedObj;
                        tuple<bool, int> res = sync_write(_socket.SafePipeHandle, ptr, offset, size);
                        functional.catch_invoke(cb, new socket_result(res.value1, res.value2));
                        return;
                    }
                    catch (System.Exception) { }
                }
                functional.catch_invoke(cb, new socket_result(false, 0));
            });
        }

        void _async_read(int currTotal, IntPtr ptr, int offset, int size, Action<socket_result> cb, object pinnedObj)
        {
            _readHandler.currTotal = currTotal;
            _readHandler.ptr = ptr;
            _readHandler.offset = offset;
            _readHandler.size = size;
            _readHandler.pinnedObj = pinnedObj;
            _readHandler.cb = cb;
            async_read_same(ptr, offset, size, _readHandler.handler);
        }

        void _async_write(int currTotal, IntPtr ptr, int offset, int size, Action<socket_result> cb, object pinnedObj)
        {
            _writeHandler.currTotal = currTotal;
            _writeHandler.ptr = ptr;
            _writeHandler.offset = offset;
            _writeHandler.size = size;
            _writeHandler.pinnedObj = pinnedObj;
            _writeHandler.cb = cb;
            async_write_same(ptr, offset, size, _writeHandler.handler);
        }

        public void async_read(IntPtr ptr, int offset, int size, Action<socket_result> cb, object pinnedObj = null)
        {
            _async_read(0, ptr, offset, size, cb, pinnedObj);
        }

        public void async_write(IntPtr ptr, int offset, int size, Action<socket_result> cb, object pinnedObj = null)
        {
            _async_write(0, ptr, offset, size, cb, pinnedObj);
        }

        public ValueTask<socket_result> read_same(IntPtr ptr, int offset, int size, object pinnedObj = null)
        {
            return generator.async_call((Action<socket_result> cb) => async_read_same(ptr, offset, size, cb, pinnedObj));
        }

        public ValueTask<socket_result> write_same(IntPtr ptr, int offset, int size, object pinnedObj = null)
        {
            return generator.async_call((Action<socket_result> cb) => async_write_same(ptr, offset, size, cb, pinnedObj));
        }

        public ValueTask<socket_result> read(IntPtr ptr, int offset, int size, object pinnedObj = null)
        {
            return generator.async_call((Action<socket_result> cb) => async_read(ptr, offset, size, cb, pinnedObj));
        }

        public ValueTask<socket_result> write(IntPtr ptr, int offset, int size, object pinnedObj = null)
        {
            return generator.async_call((Action<socket_result> cb) => async_write(ptr, offset, size, cb, pinnedObj));
        }

        public Task unsafe_read_same(async_result_wrap<socket_result> res, IntPtr ptr, int offset, int size, object pinnedObj = null)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_read_same(ptr, offset, size, cb, pinnedObj));
        }

        public Task unsafe_write_same(async_result_wrap<socket_result> res, IntPtr ptr, int offset, int size, object pinnedObj = null)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_write_same(ptr, offset, size, cb, pinnedObj));
        }

        public Task unsafe_read(async_result_wrap<socket_result> res, IntPtr ptr, int offset, int size, object pinnedObj = null)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_read(ptr, offset, size, cb, pinnedObj));
        }

        public Task unsafe_write(async_result_wrap<socket_result> res, IntPtr ptr, int offset, int size, object pinnedObj = null)
        {
            return generator.unsafe_async_call(res, (Action<socket_result> cb) => async_write(ptr, offset, size, cb, pinnedObj));
        }
    }

    public class socket_pipe_server : socket_pipe
    {
        readonly string _pipeName;

        public socket_pipe_server(string pipeName, int inBufferSize = 4 * 1024, int outBufferSize = 4 * 1024)
        {
            _pipeName = pipeName;
            _socket = new NamedPipeServerStream("CsGo_" + pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, inBufferSize, outBufferSize);
        }

        public NamedPipeServerStream socket
        {
            get
            {
                return (NamedPipeServerStream)_socket;
            }
        }

        public override void close()
        {
            if (_socket.IsConnected)
            {
                try
                {
                    ((NamedPipeServerStream)_socket).Disconnect();
                }
                catch (System.Exception) { }
            }
            try
            {
                _socket.Close();
            }
            catch (System.Exception) { }
        }

        public async Task<bool> wait_connection(int ms = -1)
        {
            bool overtime = false;
            async_timer waitTimeout = null;
            try
            {
                if (ms >= 0)
                {
                    waitTimeout = new async_timer(generator.self_strand());
                    waitTimeout.timeout(ms, delegate ()
                    {
                        overtime = true;
                        try
                        {
                            NamedPipeClientStream timedPipe = new NamedPipeClientStream(".", "CsGo_" + _pipeName);
                            timedPipe.Connect(0);
                            timedPipe.Close();
                        }
                        catch (System.Exception) { }
                    });
                }
                await generator.send_task(((NamedPipeServerStream)_socket).WaitForConnection);
                if (overtime)
                {
                    close();
                }
                waitTimeout?.cancel();
            }
            catch (System.Exception)
            {
                waitTimeout?.advance();
                close();
                throw;
            }
            return !overtime;
        }
    }

    public class socket_pipe_client : socket_pipe
    {
        public socket_pipe_client(string pipeName, string serverName = ".")
        {
            _socket = new NamedPipeClientStream(serverName, "CsGo_" + pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        }

        public NamedPipeClientStream socket
        {
            get
            {
                return (NamedPipeClientStream)_socket;
            }
        }

        public override void close()
        {
            if (_socket.IsConnected)
            {
                try
                {
                    cancel_io(_socket.SafePipeHandle);
                }
                catch (System.Exception) { }
            }
            try
            {
                _socket.Close();
            }
            catch (System.Exception) { }
        }

        public bool try_connect()
        {
            try
            {
                ((NamedPipeClientStream)_socket).Connect(0);
                return true;
            }
            catch (System.Exception) { }
            return false;
        }

        public async Task<bool> connect(int ms = -1)
        {
            if (0 == ms)
            {
                return try_connect();
            }
            long beginTick = system_tick.get_tick_us();
            while (!try_connect())
            {
                await generator.sleep(1);
                if (ms >= 0 && system_tick.get_tick_us() - beginTick >= (long)ms * 1000)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
