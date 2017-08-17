using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Go
{
    public class socket_result
    {
        public bool ok = false;
        public int s = 0;
        public string message;
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
            }
        }

        public void async_connect(string ip, int port, functional.func<socket_result> cb)
        {
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.BeginConnect(IPAddress.Parse(ip), port, delegate (IAsyncResult ar)
                {
                    socket_result res = new socket_result();
                    try
                    {
                        _socket.EndConnect(ar);
                        _socket.NoDelay = true;
                        res.ok = true;
                    }
                    catch (System.Exception ec)
                    {
                        res.message = ec.Message;
                    }
                    cb(res);
                }, null);
            }
            catch (System.Exception)
            {
                close();
                cb(new socket_result());
            }
        }

        public void async_read_same(IList<ArraySegment<byte>> buffs, functional.func<socket_result> cb)
        {
            try
            {
                _socket.BeginReceive(buffs, 0, delegate (IAsyncResult ar)
                {
                    socket_result res = new socket_result();
                    try
                    {
                        res.s = _socket.EndReceive(ar);
                        res.ok = true;
                    }
                    catch (System.Exception ec)
                    {
                        res.message = ec.Message;
                    }
                    cb(res);
                }, null);
            }
            catch (System.Exception) { cb(new socket_result()); }
        }

        public void async_read_same(ArraySegment<byte> buff, functional.func<socket_result> cb)
        {
            IList<ArraySegment<byte>> buffs = new List<ArraySegment<byte>>();
            buffs.Add(buff);
            async_read_same(buffs, cb);
        }

        public void async_write_same(IList<ArraySegment<byte>> buffs, functional.func<socket_result> cb)
        {
            try
            {
                _socket.BeginSend(buffs, 0, delegate (IAsyncResult ar)
                {
                    socket_result res = new socket_result();
                    try
                    {
                        res.s = _socket.EndSend(ar);
                        res.ok = true;
                    }
                    catch (System.Exception ec)
                    {
                        res.message = ec.Message;
                    }
                    cb(res);
                }, null);
            }
            catch (System.Exception) { cb(new socket_result()); }
        }

        public void async_write_same(ArraySegment<byte> buff, functional.func<socket_result> cb)
        {
            IList<ArraySegment<byte>> buffs = new List<ArraySegment<byte>>();
            buffs.Add(buff);
            async_write_same(buffs, cb);
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
                        socket_result result = new socket_result();
                        result.s = currTotal;
                        result.ok = true;
                        cb(result);
                    }
                    else
                    {
                        _async_read(currTotal, new ArraySegment<byte>(buff.Array, buff.Offset + tempRes.s, buff.Count - tempRes.s), cb);
                    }
                }
                else
                {
                    socket_result res = new socket_result();
                    res.s = currTotal;
                    res.message = tempRes.message;
                    cb(res);
                }
            });
        }

        void _async_read(int index, int currTotal, IList<ArraySegment<byte>> buffs, functional.func<socket_result> cb)
        {
            async_read(buffs[index], delegate (socket_result tempRes)
            {
                if (tempRes.ok)
                {
                    index++;
                    currTotal += tempRes.s;
                    if (index == buffs.Count)
                    {
                        socket_result result = new socket_result();
                        result.s = currTotal;
                        result.ok = true;
                        cb(result);
                    }
                    else
                    {
                        _async_read(index, currTotal, buffs, cb);
                    }
                }
                else
                {
                    socket_result res = new socket_result();
                    res.s = currTotal;
                    res.message = tempRes.message;
                    cb(res);
                }
            });
        }

        public void async_read(ArraySegment<byte> buff, functional.func<socket_result> cb)
        {
            _async_read(0, buff, cb);
        }

        public void async_read(IList<ArraySegment<byte>> buffs, functional.func<socket_result> cb)
        {
            _async_read(0, 0, buffs, cb);
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
                        socket_result result = new socket_result();
                        result.s = buff.Count;
                        result.ok = true;
                        cb(result);
                    }
                    else
                    {
                        _async_write(currTotal, new ArraySegment<byte>(buff.Array, buff.Offset + tempRes.s, buff.Count - tempRes.s), cb);
                    }
                }
                else
                {
                    socket_result res = new socket_result();
                    res.s = currTotal;
                    res.message = tempRes.message;
                    cb(res);
                }
            });
        }

        void _async_write(int index, int currTotal, IList<ArraySegment<byte>> buffs, functional.func<socket_result> cb)
        {
            async_write(buffs[index], delegate (socket_result tempRes)
            {
                if (tempRes.ok)
                {
                    index++;
                    currTotal += tempRes.s;
                    if (index == buffs.Count)
                    {
                        socket_result result = new socket_result();
                        result.s = currTotal;
                        result.ok = true;
                        cb(result);
                    }
                    else
                    {
                        _async_write(index, currTotal, buffs, cb);
                    }
                }
                else
                {
                    socket_result res = new socket_result();
                    res.s = currTotal;
                    res.message = tempRes.message;
                    cb(res);
                }
            });
        }

        public void async_write(ArraySegment<byte> buff, functional.func<socket_result> cb)
        {
            _async_write(0, buff, cb);
        }

        public void async_write(IList<ArraySegment<byte>> buffs, functional.func<socket_result> cb)
        {
            _async_write(0, 0, buffs, cb);
        }

        public async Task<socket_result> connect(string ip, int port)
        {
            generator host = generator.self;
            return await host.wait_result((async_result_wrap<socket_result> res) => async_connect(ip, port, host.async_result(res)));
        }

        public async Task<socket_result> read_same(IList<ArraySegment<byte>> buffs)
        {
            generator host = generator.self;
            return await host.wait_result((async_result_wrap<socket_result> res) => async_read_same(buffs, host.async_result(res)));
        }

        public async Task<socket_result> read_same(ArraySegment<byte> buff)
        {
            IList<ArraySegment<byte>> buffs = new List<ArraySegment<byte>>();
            buffs.Add(buff);
            return await read_same(buffs);
        }

        public async Task<socket_result> read(IList<ArraySegment<byte>> buffs)
        {
            generator host = generator.self;
            return await host.wait_result((async_result_wrap<socket_result> res) => async_read(buffs, host.async_result(res)));
        }

        public async Task<socket_result> read(ArraySegment<byte> buff)
        {
            IList<ArraySegment<byte>> buffs = new List<ArraySegment<byte>>();
            buffs.Add(buff);
            return await read(buffs);
        }

        public async Task<socket_result> write_same(IList<ArraySegment<byte>> buff)
        {
            generator host = generator.self;
            return await host.wait_result((async_result_wrap<socket_result> res) => async_write_same(buff, host.async_result(res)));
        }

        public async Task<socket_result> write_same(ArraySegment<byte> buff)
        {
            IList<ArraySegment<byte>> buffs = new List<ArraySegment<byte>>();
            buffs.Add(buff);
            return await write_same(buffs);
        }

        public async Task<socket_result> write(IList<ArraySegment<byte>> buff)
        {
            generator host = generator.self;
            return await host.wait_result((async_result_wrap<socket_result> res) => async_write(buff, host.async_result(res)));
        }

        public async Task<socket_result> write(ArraySegment<byte> buff)
        {
            IList<ArraySegment<byte>> buffs = new List<ArraySegment<byte>>();
            buffs.Add(buff);
            return await write(buffs);
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
                    socket_result res = new socket_result();
                    try
                    {
                        sck._socket = _socket.EndAccept(ar);
                        sck._socket.NoDelay = true;
                        res.ok = true;
                    }
                    catch (System.Exception ec)
                    {
                        res.message = ec.Message;
                    }
                    cb(res);
                }, null);
            }
            catch (System.Exception)
            {
                close();
                cb(new socket_result());
            }
        }

        public async Task<socket_result> accept(socket_tcp sck)
        {
            generator host = generator.self;
            return await host.wait_result((async_result_wrap<socket_result> res) => async_accept(sck, host.async_result(res)));
        }
    }
}
