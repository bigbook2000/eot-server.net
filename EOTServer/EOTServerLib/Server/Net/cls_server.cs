using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace cn.eobject.iot.Server.Net
{
    /// <summary>
    /// 网络服务类
    /// 主要是监听客户端连接事件，管理连接对象。连接成功之后交由cls_connect连接对象处理。
    /// </summary>
    public class cls_server
    {
        public const int MAX_BUFFER = 4096;

        /// <summary>
        /// 监听Socket
        /// </summary>
        private Socket? _socket_server;
        /// <summary>
        /// 服务监听端口
        /// </summary>
        protected int _port;

        //AutoResetEvent _event_accept = new AutoResetEvent(false);

        /// <summary>
        /// 避免内存问题
        /// 使用IP+端口标识，快速检索
        /// 使用锁保证线程安全
        /// </summary>
        private readonly Dictionary<string, cls_connect> _connect_map = new();
        /// <summary>
        /// 处理事件回调，通知外部应用
        /// </summary>
        protected evt_server _event_server;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="eventServer">回调事件</param>
        public cls_server(evt_server eventServer)
        {
            _event_server = eventServer;
        }
        /// <summary>
        /// 根据标识获取连接对象
        /// </summary>
        /// <param name="connectKey">连接对象标识，IP+端口，固定长度</param>
        /// <returns></returns>
        public cls_connect? get_connect(string connectKey)
        {
            lock (_connect_map)
            {
                if (_connect_map.ContainsKey(connectKey))
                    return _connect_map[connectKey];
            }

            return null;
        }
        /// <summary>
        /// 获取连接数量
        /// </summary>
        /// <returns></returns>
        public int get_connect_count()
        {
            lock (_connect_map)
            {
                return _connect_map.Count;
            }
        }

        /// <summary>
        /// 复制一个列表，目的是当外部循环时线程安全，同时使用副本避免在锁内循环
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

        /// <summary>
        /// 启动服务
        /// </summary>
        /// <param name="port"></param>
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
        /// <summary>
        /// 停止服务
        /// </summary>
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
        /// <summary>
        /// 关闭指定的连接对象
        /// </summary>
        /// <param name="connectKey"></param>
        /// <param name="info"></param>
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
        /// <summary>
        /// 处理客户端连接请求
        /// </summary>
        /// <param name="asyncResult"></param>
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
                    // 发现异常，重启服务
                    start_(_port);
                }
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }
        /// <summary>
        /// 传递接收数据事件
        /// 接收数据在cls_connect网络连接对象中进行，通过_event_server事件向外部应用传递，优化系统架构代码
        /// </summary>
        /// <param name="connect"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
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
