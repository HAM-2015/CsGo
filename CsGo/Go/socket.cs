using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO.Ports;
using System.IO.Pipes;

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

        void _async_read(int currTotal, ArraySegment<byte> buff, functional.func<socket_result> cb)
        {
            async_read_same(buff, delegate (socket_result tempRes)
            {
                if (tempRes.ok)
                {
                    currTotal += tempRes.s;
                    if (buff.Count == tempRes.s)
                    {
                        cb(new socket_result(true, currTotal));
                    }
                    else
                    {
                        _async_read(currTotal, new ArraySegment<byte>(buff.Array, buff.Offset + tempRes.s, buff.Count - tempRes.s), cb);
                    }
                }
                else
                {
                    cb(new socket_result(false, currTotal, tempRes.message));
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

        void _async_write(int currTotal, ArraySegment<byte> buff, functional.func<socket_result> cb)
        {
            async_write_same(buff, delegate (socket_result tempRes)
            {
                if (tempRes.ok)
                {
                    currTotal += tempRes.s;
                    if (buff.Count == tempRes.s)
                    {
                        cb(new socket_result(true, currTotal));
                    }
                    else
                    {
                        _async_write(currTotal, new ArraySegment<byte>(buff.Array, buff.Offset + tempRes.s, buff.Count - tempRes.s), cb);
                    }
                }
                else
                {
                    cb(new socket_result(false, currTotal, tempRes.message));
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
    }

    public class socket_tcp : socket
    {
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
                        cb(new socket_result(true));
                    }
                    catch (System.Exception ec)
                    {
                        cb(new socket_result(false, 0, ec.Message));
                    }
                }, null);
            }
            catch (System.Exception ec)
            {
                close();
                cb(new socket_result(false, 0, ec.Message));
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
                        cb(new socket_result(true));
                    }
                    catch (System.Exception ec)
                    {
                        cb(new socket_result(false, 0, ec.Message));
                    }
                }, null);
            }
            catch (System.Exception ec)
            {
                close();
                cb(new socket_result(false, 0, ec.Message));
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
                        cb(new socket_result(true, _socket.EndReceive(ar)));
                    }
                    catch (System.Exception ec)
                    {
                        cb(new socket_result(false, 0, ec.Message));
                    }
                }, null);
            }
            catch (System.Exception ec)
            {
                close();
                cb(new socket_result(false, 0, ec.Message));
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
                        cb(new socket_result(true, _socket.EndSend(ar)));
                    }
                    catch (System.Exception ec)
                    {
                        cb(new socket_result(false, 0, ec.Message));
                    }
                }, null);
            }
            catch (System.Exception ec)
            {
                close();
                cb(new socket_result(false, 0, ec.Message));
            }
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
                            cb(new socket_result(true));
                        }
                        catch (System.Exception ec)
                        {
                            cb(new socket_result(false, 0, ec.Message));
                        }
                    }, null);
                }
                catch (System.Exception ec)
                {
                    close();
                    cb(new socket_result(false, 0, ec.Message));
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
                        cb(new socket_result(true, _socket.BaseStream.EndRead(ar)));
                    }
                    catch (System.Exception ec)
                    {
                        cb(new socket_result(false, 0, ec.Message));
                    }
                }, null);
            }
            catch (System.Exception ec)
            {
                close();
                cb(new socket_result(false, 0, ec.Message));
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
                        cb(new socket_result(true, buff.Count));
                    }
                    catch (System.Exception ec)
                    {
                        cb(new socket_result(false, 0, ec.Message));
                    }
                }, null);
            }
            catch (System.Exception ec)
            {
                close();
                cb(new socket_result(false, 0, ec.Message));
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
                        cb(new socket_result(true));
                    }
                    catch (System.Exception ec)
                    {
                        cb(new socket_result(false, 0, ec.Message));
                    }
                }, null);
            }
            catch (System.Exception ec)
            {
                close();
                cb(new socket_result(false, 0, ec.Message));
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
                        cb(new socket_result(true, _socket.EndRead(ar)));
                    }
                    catch (System.Exception ec)
                    {
                        cb(new socket_result(false, 0, ec.Message));
                    }
                }, null);
            }
            catch (System.Exception ec)
            {
                close();
                cb(new socket_result(false, 0, ec.Message));
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
                        cb(new socket_result(true, buff.Count));
                    }
                    catch (System.Exception ec)
                    {
                        cb(new socket_result(false, 0, ec.Message));
                    }
                }, null);
            }
            catch (System.Exception ec)
            {
                close();
                cb(new socket_result(false, 0, ec.Message));
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
                        cb(new socket_result(true, _socket.EndRead(ar)));
                    }
                    catch (System.Exception ec)
                    {
                        cb(new socket_result(false, 0, ec.Message));
                    }
                }, null);
            }
            catch (System.Exception ec)
            {
                close();
                cb(new socket_result(false, 0, ec.Message));
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
                        cb(new socket_result(true, buff.Count));
                    }
                    catch (System.Exception ec)
                    {
                        cb(new socket_result(false, 0, ec.Message));
                    }
                }, null);
            }
            catch (System.Exception ec)
            {
                close();
                cb(new socket_result(false, 0, ec.Message));
            }
        }
    }
}
