using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace cn.eobject.iot.Server.Net
{
    /// <summary>
    /// 服务类
    /// </summary>
    public class cls_server
    {
        public const int MAX_BUFFER = 4096;

        private Socket? _socket_server;
        protected int _port;

        //AutoResetEvent _event_accept = new AutoResetEvent(false);

        /// <summary>
        /// 避免内存问题
        /// </summary>
        private readonly Dictionary<string, cls_connect> _connect_map = new();

        protected evt_server _event_server;

        public cls_server(evt_server eventServer)
        {
            _event_server = eventServer;
        }

        public cls_connect? get_connect(string connectKey)
        {
            lock (_connect_map)
            {
                if (_connect_map.ContainsKey(connectKey))
                    return _connect_map[connectKey];
            }

            return null;
        }

        public int get_connect_count()
        {
            lock (_connect_map)
            {
                return _connect_map.Count;
            }
        }

        /// <summary>
        /// 复制一个列表
        /// </summary>
        /// <returns></returns>
        public List<cls_connect> get_connect_list_()
        {
            List<cls_connect> list = new();

            lock (_connect_map)
            {
                foreach (KeyValuePair<string, cls_connect> kvp in _connect_map)
                {
                    list.Add(kvp.Value);
                }
            }

            return list;
        }

        /// <summary>
        /// 心跳超时监测
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public void check_timeout_(long tick, long timeout)
        {
            List<cls_connect> connects = new();

            lock (_connect_map)
            {
                foreach (KeyValuePair<string, cls_connect> kvp in _connect_map)
                {
                    cls_connect connect = kvp.Value;
                    if (connect.check_timeout_(tick, timeout))
                    {
                        connects.Add(connect);
                    }
                }
            }

            foreach (cls_connect connect in connects)
            {
                _event_server.on_close(connect);

                connect.do_close_("超时");
                lock (_connect_map)
                {
                    _connect_map.Remove(connect.get_key_());
                }
            }
        }

        public void start_(int port)
        {
            stop_();

            try
            {
                _port = port;

                _socket_server = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                IPEndPoint endPoint = new(IPAddress.Any, _port);
                _socket_server.Bind(endPoint);
                _socket_server.Listen();

                _socket_server.BeginAccept(new(on_accept), null);

                cls_log.get_default_().T_("", "服务启动，开始监听 {0}", _port);
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        public void stop_()
        {
            try
            {
                if (_socket_server == null) return;
                _socket_server.Close();
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }

            _socket_server = null;
        }

        public void close_(string connectKey, string info)
        {
            try
            {
                cls_connect connect;
                lock (_connect_map)
                {
                    connect = _connect_map[connectKey];
                }

                _event_server.on_close(connect);

                connect.do_close_(info);
                lock (_connect_map)
                {
                    _connect_map.Remove(connectKey);
                }
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        private void on_accept(IAsyncResult asyncResult)
        {
            try
            {
                if (_socket_server == null) return;

                if (asyncResult.IsCompleted)
                {
                    Socket socket = _socket_server.EndAccept(asyncResult);

                    IPEndPoint? endPoint = socket.RemoteEndPoint as IPEndPoint;
                    if (endPoint == null)
                    {
                        return;
                    }

                    byte[] bytes = endPoint.Address.GetAddressBytes();                    

                    string connectKey = cls_core.format_ip_(bytes, endPoint.Port);
                    cls_log.get_default_().T_("", "[{0}]终端连接", connectKey);
                    // 由外部接口实现连接对象创建
                    cls_connect? connect = _event_server.on_connect(connectKey);
                    if (connect == null)
                    {
                        // 拒绝连接
                        socket.Close();
                    }
                    else
                    {
                        connect.do_accept_(socket);

                        lock (_connect_map)
                        {
                            _connect_map.Add(connect.get_key_(), connect);
                        }
                    }

                    _socket_server.BeginAccept(new(on_accept), null);
                }
                else
                {
                    start_(_port);
                }
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        internal int do_recv_(cls_connect connect, byte[] bytes)
        {
            try
            {
                // 调用外部事件
                return _event_server.on_recv(connect, bytes);
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }

            return -1;
        }
    }
}
