using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO.Ports;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Go
{
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
    }

    public abstract class socket
    {
        public abstract void async_read_same(ArraySegment<byte> buff, functional.func<socket_result> cb);
        public abstract void async_write_same(ArraySegment<byte> buff, functional.func<socket_result> cb);
        public abstract void close();

        public void async_read_same(byte[] buff, functional.func<socket_result> cb)
        {
            async_read_same(new ArraySegment<byte>(buff), cb);
        }

        public void async_write_same(byte[] buff, functional.func<socket_result> cb)
        {
            async_write_same(new ArraySegment<byte>(buff), cb);
        }

        void _async_reads(int currTotal, int currIndex, IList<ArraySegment<byte>> buff, functional.func<socket_result> cb)
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

        void _async_reads(int currTotal, int currIndex, IList<byte[]> buff, functional.func<socket_result> cb)
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

        void _async_read(int currTotal, ArraySegment<byte> buff, functional.func<socket_result> cb)
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

        public void async_read(ArraySegment<byte> buff, functional.func<socket_result> cb)
        {
            _async_read(0, buff, cb);
        }

        public void async_read(byte[] buff, functional.func<socket_result> cb)
        {
            _async_read(0, new ArraySegment<byte>(buff), cb);
        }

        void _async_writes(int currTotal, int currIndex, IList<ArraySegment<byte>> buff, functional.func<socket_result> cb)
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

        void _async_writes(int currTotal, int currIndex, IList<byte[]> buff, functional.func<socket_result> cb)
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

        void _async_write(int currTotal, ArraySegment<byte> buff, functional.func<socket_result> cb)
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

        public void async_write(ArraySegment<byte> buff, functional.func<socket_result> cb)
        {
            _async_write(0, buff, cb);
        }

        public void async_write(byte[] buff, functional.func<socket_result> cb)
        {
            _async_write(0, new ArraySegment<byte>(buff), cb);
        }

        public Task<socket_result> read_same(ArraySegment<byte> buff)
        {
            return generator.async_call((functional.func<socket_result> cb) => async_read_same(buff, cb));
        }

        public Task<socket_result> read_same(byte[] buff)
        {
            return generator.async_call((functional.func<socket_result> cb) => async_read_same(buff, cb));
        }

        public Task<socket_result> read(ArraySegment<byte> buff)
        {
            return generator.async_call((functional.func<socket_result> cb) => async_read(buff, cb));
        }

        public Task<socket_result> read(byte[] buff)
        {
            return generator.async_call((functional.func<socket_result> cb) => async_read(buff, cb));
        }

        public Task<socket_result> reads(IList<byte[]> buff)
        {
            return generator.async_call((functional.func<socket_result> cb) => _async_reads(0, 0, buff, cb));
        }

        public Task<socket_result> reads(IList<ArraySegment<byte>> buff)
        {
            return generator.async_call((functional.func<socket_result> cb) => _async_reads(0, 0, buff, cb));
        }

        public Task<socket_result> reads(params byte[][] buff)
        {
            return generator.async_call((functional.func<socket_result> cb) => _async_reads(0, 0, buff, cb));
        }

        public Task<socket_result> reads(params ArraySegment<byte>[] buff)
        {
            return generator.async_call((functional.func<socket_result> cb) => _async_reads(0, 0, buff, cb));
        }

        public Task<socket_result> write_same(ArraySegment<byte> buff)
        {
            return generator.async_call((functional.func<socket_result> cb) => async_write_same(buff, cb));
        }

        public Task<socket_result> write_same(byte[] buff)
        {
            return generator.async_call((functional.func<socket_result> cb) => async_write_same(buff, cb));
        }

        public Task<socket_result> write(ArraySegment<byte> buff)
        {
            return generator.async_call((functional.func<socket_result> cb) => async_write(buff, cb));
        }

        public Task<socket_result> write(byte[] buff)
        {
            return generator.async_call((functional.func<socket_result> cb) => async_write(buff, cb));
        }

        public Task<socket_result> writes(IList<byte[]> buff)
        {
            return generator.async_call((functional.func<socket_result> cb) => _async_writes(0, 0, buff, cb));
        }

        public Task<socket_result> writes(IList<ArraySegment<byte>> buff)
        {
            return generator.async_call((functional.func<socket_result> cb) => _async_writes(0, 0, buff, cb));
        }

        public Task<socket_result> writes(params byte[][] buff)
        {
            return generator.async_call((functional.func<socket_result> cb) => _async_writes(0, 0, buff, cb));
        }

        public Task<socket_result> writes(params ArraySegment<byte>[] buff)
        {
            return generator.async_call((functional.func<socket_result> cb) => _async_writes(0, 0, buff, cb));
        }
    }

    public class socket_tcp : socket
    {
        class SocketAsyncIo
        {
            static MethodInfo GetMethod(Type type, string name, Type[] parmsType = null)
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

            static FieldInfo GetField(Type type, string name)
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

            [DllImport("Ws2_32.dll", CharSet = CharSet.None, ExactSpelling = false, SetLastError = true)]
            internal static extern SocketError WSASend(IntPtr socketHandle, IntPtr buffer, int bufferCount, out int bytesTransferred, SocketFlags socketFlags, SafeHandle overlapped, IntPtr completionRoutine);
            [DllImport("Ws2_32.dll", CharSet = CharSet.None, ExactSpelling = false, SetLastError = true)]
            internal static extern SocketError WSARecv(IntPtr socketHandle, IntPtr buffer, int bufferCount, out int bytesTransferred, ref SocketFlags socketFlags, SafeHandle overlapped, IntPtr completionRoutine);

            static Assembly _systemAss = Assembly.LoadFrom(RuntimeEnvironment.GetRuntimeDirectory() + "System.dll");
            static Type _overlappedType = _systemAss.GetType("System.Net.Sockets.OverlappedAsyncResult");
            static Type _cacheSetType = _systemAss.GetType("System.Net.Sockets.Socket+CacheSet");
            static Type _callbackClosureType = _systemAss.GetType("System.Net.CallbackClosure");
            static Type _callbackClosureRefType = _systemAss.GetType("System.Net.CallbackClosure&");
            static Type _overlappedCacheRefType = _systemAss.GetType("System.Net.Sockets.OverlappedCache&");
            static Type _WSABufferType = _systemAss.GetType("System.Net.WSABuffer");
            static MethodInfo _overlappedStartPostingAsyncOp = GetMethod(_overlappedType, "StartPostingAsyncOp", new Type[] { typeof(bool) });
            static MethodInfo _overlappedFinishPostingAsyncOp = GetMethod(_overlappedType, "FinishPostingAsyncOp", new Type[] { _callbackClosureRefType });
            static MethodInfo _overlappedSetupCache = GetMethod(_overlappedType, "SetupCache", new Type[] { _overlappedCacheRefType });
            static MethodInfo _overlappedExtractCache = GetMethod(_overlappedType, "ExtractCache", new Type[] { _overlappedCacheRefType });
            static MethodInfo _overlappedSetUnmanagedStructures = GetMethod(_overlappedType, "SetUnmanagedStructures", new Type[] { typeof(object) });
            static MethodInfo _overlappedOverlappedHandle = GetMethod(_overlappedType, "get_OverlappedHandle");
            static MethodInfo _overlappedCheckAsyncCallOverlappedResult = GetMethod(_overlappedType, "CheckAsyncCallOverlappedResult", new Type[] { typeof(SocketError) });
            static MethodInfo _overlappedInvokeCallback = GetMethod(_overlappedType, "InvokeCallback", new Type[] { typeof(object) });
            static MethodInfo _socketCaches = GetMethod(typeof(Socket), "get_Caches");
            static MethodInfo _sockektUpdateStatusAfterSocketError = GetMethod(typeof(Socket), "UpdateStatusAfterSocketError", new Type[] { typeof(SocketError) });
            static FieldInfo _overlappedmSingleBuffer = GetField(_overlappedType, "m_SingleBuffer");
            static FieldInfo _WSABufferLength = GetField(_WSABufferType, "Length");
            static FieldInfo _WSABufferPointer = GetField(_WSABufferType, "Pointer");
            static FieldInfo _cacheSendClosureCache = GetField(_cacheSetType, "SendClosureCache");
            static FieldInfo _cacheSendOverlappedCache = GetField(_cacheSetType, "SendOverlappedCache");
            static FieldInfo _cacheReceiveClosureCache = GetField(_cacheSetType, "ReceiveClosureCache");
            static FieldInfo _cacheReceiveOverlappedCache = GetField(_cacheSetType, "ReceiveOverlappedCache");

            static private IAsyncResult MakeOverlappedAsyncResult(Socket sck, AsyncCallback callback)
            {
                return (IAsyncResult)_systemAss.CreateInstance(_overlappedType.FullName, true,
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

            static readonly object[] setUnmanagedStructuresParam = new object[] { null };
            static private void SetUnmanagedStructures(IAsyncResult overlapped)
            {
                _overlappedSetUnmanagedStructures.Invoke(overlapped, setUnmanagedStructuresParam);
            }

            static private IntPtr SetSendPointer(Socket sck, IAsyncResult overlapped, IntPtr ptr, int offset, int size, object cacheSet)
            {
                object oldCache = _cacheSendOverlappedCache.GetValue(cacheSet);
                object[] overlappedCache = new object[] { oldCache };
                _overlappedSetupCache.Invoke(overlapped, overlappedCache);
                if (oldCache != overlappedCache[0])
                {
                    _cacheSendOverlappedCache.SetValue(cacheSet, overlappedCache[0]);
                }
                SetUnmanagedStructures(overlapped);
                object WSABuffer = _overlappedmSingleBuffer.GetValue(overlapped);
                _WSABufferPointer.SetValue(WSABuffer, ptr + offset);
                _WSABufferLength.SetValue(WSABuffer, size);
                _overlappedmSingleBuffer.SetValue(overlapped, WSABuffer);
                return GCHandle.Alloc(WSABuffer, GCHandleType.Pinned).AddrOfPinnedObject();
            }

            static private IntPtr SetRecvPointer(Socket sck, IAsyncResult overlapped, IntPtr ptr, int offset, int size, object cacheSet)
            {
                object oldCache = _cacheReceiveOverlappedCache.GetValue(cacheSet);
                object[] overlappedCache = new object[] { oldCache };
                _overlappedSetupCache.Invoke(overlapped, overlappedCache);
                if (oldCache != overlappedCache[0])
                {
                    _cacheReceiveOverlappedCache.SetValue(cacheSet, overlappedCache[0]);
                }
                SetUnmanagedStructures(overlapped);
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
                IntPtr WSABuffer = SetSendPointer(sck, overlapped, ptr, offset, size, cacheSet);
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
                IntPtr WSABuffer = SetRecvPointer(sck, overlapped, ptr, offset, size, cacheSet);
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
        }

        Socket _socket;

        public socket_tcp()
        {
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

        public void async_connect(string ip, int port, functional.func<socket_result> cb)
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

        public void async_disconnect(bool reuseSocket, functional.func<socket_result> cb)
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

        public override void async_read_same(ArraySegment<byte> buff, functional.func<socket_result> cb)
        {
            try
            {
                _socket.BeginReceive(buff.Array, buff.Offset, buff.Count, 0, delegate (IAsyncResult ar)
                {
                    try
                    {
                        functional.catch_invoke(cb, new socket_result(true, _socket.EndReceive(ar)));
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

        public override void async_write_same(ArraySegment<byte> buff, functional.func<socket_result> cb)
        {
            try
            {
                _socket.BeginSend(buff.Array, buff.Offset, buff.Count, 0, delegate (IAsyncResult ar)
                {
                    try
                    {
                        functional.catch_invoke(cb, new socket_result(true, _socket.EndSend(ar)));
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

        public void async_send_file(string fileName, byte[] preBuffer, byte[] postBuffer, functional.func<socket_result> cb)
        {
            try
            {
                _socket.BeginSendFile(fileName, preBuffer, postBuffer, 0, delegate (IAsyncResult ar)
                {
                    try
                    {
                        _socket.EndSendFile(ar);
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

        public void async_send_file(string fileName, functional.func<socket_result> cb)
        {
            async_send_file(fileName, null, null, cb);
        }

        public void async_read_same(IntPtr ptr, int offset, int size, functional.func<socket_result> cb)
        {
            try
            {
                SocketError lastWin32Error = SocketAsyncIo.Recv(_socket, ptr, offset, size, 0, delegate (IAsyncResult ar)
                {
                    try
                    {
                        functional.catch_invoke(cb, new socket_result(true, _socket.EndReceive(ar)));
                    }
                    catch (System.Exception ec)
                    {
                        functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
                    }
                });
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

        public void async_write_same(IntPtr ptr, int offset, int size, functional.func<socket_result> cb)
        {
            try
            {
                SocketError lastWin32Error = SocketAsyncIo.Send(_socket, ptr, offset, size, 0, delegate (IAsyncResult ar)
                {
                    try
                    {
                        functional.catch_invoke(cb, new socket_result(true, _socket.EndSend(ar)));
                    }
                    catch (System.Exception ec)
                    {
                        functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
                    }
                });
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

        void _async_read(int currTotal, IntPtr ptr, int offset, int size, functional.func<socket_result> cb)
        {
            async_read_same(ptr, offset, size, delegate (socket_result tempRes)
            {
                if (tempRes.ok)
                {
                    currTotal += tempRes.s;
                    if (size == tempRes.s)
                    {
                        functional.catch_invoke(cb, new socket_result(true, currTotal));
                    }
                    else
                    {
                        _async_read(currTotal, ptr, offset + tempRes.s, size - tempRes.s, cb);
                    }
                }
                else
                {
                    functional.catch_invoke(cb, new socket_result(false, currTotal, tempRes.message));
                }
            });
        }

        void _async_write(int currTotal, IntPtr ptr, int offset, int size, functional.func<socket_result> cb)
        {
            async_write_same(ptr, offset, size, delegate (socket_result tempRes)
            {
                if (tempRes.ok)
                {
                    currTotal += tempRes.s;
                    if (size == tempRes.s)
                    {
                        functional.catch_invoke(cb, new socket_result(true, currTotal));
                    }
                    else
                    {
                        _async_write(currTotal, ptr, offset + tempRes.s, size - tempRes.s, cb);
                    }
                }
                else
                {
                    functional.catch_invoke(cb, new socket_result(false, currTotal, tempRes.message));
                }
            });
        }

        public void async_read(IntPtr ptr, int offset, int size, functional.func<socket_result> cb)
        {
            _async_read(0, ptr, offset, size, cb);
        }

        public void async_write(IntPtr ptr, int offset, int size, functional.func<socket_result> cb)
        {
            _async_write(0, ptr, offset, size, cb);
        }

        public Task<socket_result> read_same(IntPtr ptr, int offset, int size)
        {
            return generator.async_call((functional.func<socket_result> cb) => async_read_same(ptr, offset, size, cb));
        }

        public Task<socket_result> write_same(IntPtr ptr, int offset, int size)
        {
            return generator.async_call((functional.func<socket_result> cb) => async_write_same(ptr, offset, size, cb));
        }

        public Task<socket_result> read(IntPtr ptr, int offset, int size)
        {
            return generator.async_call((functional.func<socket_result> cb) => async_read(ptr, offset, size, cb));
        }

        public Task<socket_result> write(IntPtr ptr, int offset, int size)
        {
            return generator.async_call((functional.func<socket_result> cb) => async_write(ptr, offset, size, cb));
        }

        public Task<socket_result> send_file(string fileName, byte[] preBuffer = null, byte[] postBuffer = null)
        {
            return generator.async_call((functional.func<socket_result> cb) => async_send_file(fileName, preBuffer, postBuffer, cb));
        }

        public Task<socket_result> connect(string ip, int port)
        {
            return generator.async_call((functional.func<socket_result> cb) => async_connect(ip, port, cb));
        }

        public Task<socket_result> disconnect(bool reuseSocket)
        {
            return generator.async_call((functional.func<socket_result> cb) => async_disconnect(reuseSocket, cb));
        }

        public class acceptor
        {
            Socket _socket;

            public acceptor()
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }

            public Socket socket
            {
                get
                {
                    return _socket;
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

            public void async_accept(socket_tcp sck, functional.func<socket_result> cb)
            {
                try
                {
                    sck.close();
                    _socket.BeginAccept(delegate (IAsyncResult ar)
                    {
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
                    }, null);
                }
                catch (System.Exception ec)
                {
                    close();
                    functional.catch_invoke(cb, new socket_result(false, 0, ec.Message));
                }
            }

            public Task<socket_result> accept(socket_tcp sck)
            {
                return generator.async_call((functional.func<socket_result> cb) => async_accept(sck, cb));
            }
        }
    }

    public class socket_serial : socket
    {
        SerialPort _socket;

        public socket_serial(string portName, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            _socket = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
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

        public override void async_read_same(ArraySegment<byte> buff, functional.func<socket_result> cb)
        {
            try
            {
                _socket.BaseStream.BeginRead(buff.Array, buff.Offset, buff.Count, delegate (IAsyncResult ar)
                {
                    try
                    {
                        functional.catch_invoke(cb, new socket_result(true, _socket.BaseStream.EndRead(ar)));
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

        public override void async_write_same(ArraySegment<byte> buff, functional.func<socket_result> cb)
        {
            try
            {
                _socket.BaseStream.BeginWrite(buff.Array, buff.Offset, buff.Count, delegate (IAsyncResult ar)
                {
                    try
                    {
                        _socket.BaseStream.EndWrite(ar);
                        functional.catch_invoke(cb, new socket_result(true, buff.Count));
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

    public class socket_pipe_server : socket
    {
        NamedPipeServerStream _socket;

        public socket_pipe_server(string pipeName, int maxNumberOfServerInstances = 1, int inBufferSize = 4 * 1024, int outBufferSize = 4 * 1024)
        {
            _socket = new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxNumberOfServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, inBufferSize, outBufferSize);
        }

        public NamedPipeServerStream socket
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

        public void disconnect()
        {
            try
            {
                _socket.Disconnect();
            }
            catch (System.Exception) { }
        }

        public void async_wait_connection(functional.func<socket_result> cb)
        {
            try
            {
                _socket.BeginWaitForConnection(delegate (IAsyncResult ar)
                {
                    try
                    {
                        _socket.EndWaitForConnection(ar);
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

        public override void async_read_same(ArraySegment<byte> buff, functional.func<socket_result> cb)
        {
            try
            {
                _socket.BeginRead(buff.Array, buff.Offset, buff.Count, delegate (IAsyncResult ar)
                {
                    try
                    {
                        functional.catch_invoke(cb, new socket_result(true, _socket.EndRead(ar)));
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

        public override void async_write_same(ArraySegment<byte> buff, functional.func<socket_result> cb)
        {
            try
            {
                _socket.BeginWrite(buff.Array, buff.Offset, buff.Count, delegate (IAsyncResult ar)
                {
                    try
                    {
                        _socket.EndWrite(ar);
                        functional.catch_invoke(cb, new socket_result(true, buff.Count));
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

        public Task<socket_result> wait_connection()
        {
            return generator.async_call((functional.func<socket_result> cb) => async_wait_connection(cb));
        }
    }

    public class socket_pipe_client : socket
    {
        NamedPipeClientStream _socket;

        public socket_pipe_client(string pipeName, string serverName = ".")
        {
            _socket = new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        }

        public NamedPipeClientStream socket
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

        public bool connect()
        {
            try
            {
                _socket.Connect(0);
                return true;
            }
            catch (System.Exception) { }
            return false;
        }

        public override void async_read_same(ArraySegment<byte> buff, functional.func<socket_result> cb)
        {
            try
            {
                _socket.BeginRead(buff.Array, buff.Offset, buff.Count, delegate (IAsyncResult ar)
                {
                    try
                    {
                        functional.catch_invoke(cb, new socket_result(true, _socket.EndRead(ar)));
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

        public override void async_write_same(ArraySegment<byte> buff, functional.func<socket_result> cb)
        {
            try
            {
                _socket.BeginWrite(buff.Array, buff.Offset, buff.Count, delegate (IAsyncResult ar)
                {
                    try
                    {
                        _socket.EndWrite(ar);
                        functional.catch_invoke(cb, new socket_result(true, buff.Count));
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
    }
}
