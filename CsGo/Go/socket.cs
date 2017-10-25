using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

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

    public class socket_tcp
    {
        public Socket _socket;

        public socket_tcp()
        {
        }

        public socket_tcp(Socket sck)
        {
            _socket = sck;
        }

        public void close()
        {
            if (null != _socket)
            {
                try
                {
                    _socket.Close();
                }
                catch (System.Exception) { }
                _socket = null;
            }
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
            catch (System.Exception)
            {
                close();
                cb(new socket_result());
            }
        }

        public void async_read_same(ArraySegment<byte> buff, functional.func<socket_result> cb)
        {
            try
            {
                _socket.BeginReceive(buff.Array, buff.Offset, buff.Count(), 0, delegate (IAsyncResult ar)
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
            catch (System.Exception) { cb(new socket_result()); }
        }
        
        public void async_read_same(byte[] buff, functional.func<socket_result> cb)
        {
            async_read_same(new ArraySegment<byte>(buff), cb);
        }

        public void async_write_same(ArraySegment<byte> buff, functional.func<socket_result> cb)
        {
            try
            {
                _socket.BeginSend(buff.Array, buff.Offset, buff.Count(), 0, delegate (IAsyncResult ar)
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
            catch (System.Exception) { cb(new socket_result()); }
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
                    if (buff.Count == currTotal)
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
                    if (buff.Count == currTotal)
                    {
                        cb(new socket_result(true, buff.Count));
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

        public Task<socket_result> connect(string ip, int port)
        {
            return generator.async_call((functional.func<socket_result> cb) => async_connect(ip, port, cb));
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

    public class socket_accept
    {
        Socket _socket;

        public socket_accept()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
            catch (System.Exception)
            {
                close();
                cb(new socket_result());
            }
        }

        public Task<socket_result> accept(socket_tcp sck)
        {
            return generator.async_call((functional.func<socket_result> cb) => async_accept(sck, cb));
        }
    }
}
