using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
using Org.BouncyCastle.Ocsp;
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;

namespace cn.eobject.iot.Server.Net
{
    /// <summary>
    /// 客户端连接对象
    /// 每个网络终端对应一个连接对象，发送和接收都采用异步IO模型，提高并发承载能力。
    /// 由系统处理网络接收，完成之后通知应用进行下一步操作。
    /// </summary>
    public class cls_connect
    {
        /// <summary>
        /// 清空数据次数
        /// </summary>
        private const int POP_PACK_MAX = 8;

        /// <summary>
        /// 服务器对象
        /// </summary>
        private cls_server? _server;
        /// <summary>
        /// Socket对象
        /// </summary>
        private Socket? _socket;
        /// <summary>
        /// Socket网络接收地址（实际）
        /// </summary>
        private byte[] _recv_bytes;
        /// <summary>
        /// Socket网络发送地址（实际）
        /// </summary>
        private byte[] _send_bytes;

        /// <summary>
        /// 逻辑接收缓存，由于TCP协议存在分包粘包现象，通过首尾相接的逻辑缓存保证协议的完整性。
        /// </summary>
        private cls_buffer _buffer_recv;
        /// <summary>
        /// 逻辑发送缓存，将逻辑应用和网络分开，使用发送缓存避免阻塞，提高并发效率。
        /// </summary>
        private cls_buffer _buffer_send;

        /// <summary>
        /// 连接对象标识，使用IP+端口，固定长度
        /// </summary>
        protected string _key = "_";

        /// <summary>
        /// 记录连接时间
        /// </summary>
        protected DateTime _start_time = DateTime.Now;

        /// <summary>
        /// 最新获取数据的时刻
        /// </summary>
        protected long _last_tick = 0L;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="server">父节点</param>
        /// <param name="key">连接对象标识，使用IP+端口，固定长度</param>
        /// <param name="bufferRecvMax">接收缓存大小</param>
        /// <param name="bufferSendMax">发送缓存大小</param>
        public cls_connect(cls_server server, string key, int bufferRecvMax, int bufferSendMax)
        {
            _server = server;

            _key = key;

            // 正常情况下我们应将逻辑缓存设计的更大，但这会带来内存的消耗。
            // 这里使用相同的大小，简化设计。

            _recv_bytes = new byte[bufferRecvMax];
            _send_bytes = new byte[bufferSendMax];

            _buffer_recv = new(bufferRecvMax);
            _buffer_send = new(bufferSendMax);
        }

        /// <summary>
        /// 获取连接标识
        /// </summary>
        /// <returns></returns>
        public string get_key_()
        {
            return _key;
        }

        /// <summary>
        /// 获取开始连接的时间
        /// </summary>
        /// <returns></returns>
        public DateTime get_start_time()
        {
            return _start_time;
        }

        /// <summary>
        /// 获取逻辑接收缓存长度
        /// </summary>
        /// <returns></returns>
        public int get_recv_length()
        {
            return _buffer_recv.get_length_();
        }

        /// <summary>
        /// 判断是否超时
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="timeout"></param>
        public bool check_timeout_(long tick, long timeout)
        {
            return (tick - _last_tick) > timeout;
        }

        /// <summary>
        /// 处理监听连接
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        internal int do_accept_(Socket socket)
        {
            // 仅关闭，不清除
            // 防御性代码，初始化
            do_close_(null);

            try
            {
                _socket = socket;

                _start_time = DateTime.Now;
                _last_tick = _start_time.Ticks;

                // 接收数据
                do_recv();
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", "[" + get_key_() + "]" + ex);
            }

            return 0;
        }

        /// <summary>
        /// 被父管理节点调用，不要再次处理
        /// </summary>
        /// <param name="info"></param>
        internal void do_close_(string? info)
        {
            try
            {
                if (_socket != null ) 
                {
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Close();
                }
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", "[" + get_key_() + "]" + ex);
            }

            // 如果是主动关闭，则释放
            if (info != null)
            {
                cls_log.get_default_().T_("", "[" + get_key_() + "]断开终端 " + info);

                _socket = null;
                _server = null;
            }
        }
        /// <summary>
        /// 向客户端发送数据，这里并不实际调用网络发送函数，仅仅将数据插入到逻辑缓存中，由专门的线程来调度实际发送，提高并发能力
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public void send_(byte[] bytes, int offset, int length)
        {
            try
            {
                // 并不直接发送
                _buffer_send.push_(bytes, offset, length);
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", "[" + get_key_() + "]" + ex);
            }

            do_send();
        }
        /// <summary>
        /// 处理接收数据
        /// </summary>
        protected void do_recv()
        {
            try
            {
                _socket?.BeginReceive(
                    _recv_bytes, 0, _recv_bytes.Length,
                    SocketFlags.None, new AsyncCallback(on_recv), this);
            } 
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", "[" + get_key_() + "]" + ex);

                // 不要直接调用关闭函数 do_close
                _server?.close_(_key, ex.Message);
            }
        }
        /// <summary>
        /// 实际发送数据
        /// </summary>
        protected void do_send()
        {
            try
            {
                int len = _buffer_send.get_length_();
                if (len <= 0) return;

                if (len > _send_bytes.Length) len = _send_bytes.Length;

                // 取出剩余数据
                int ret = _buffer_send.pop_(_send_bytes, 0, len);
                if (ret <= 0) return;

                cls_log.get_default_().T_("", "[" + get_key_() + "]发送开始 {0} / {1}", ret, len);

                // 继续发送
                _socket?.BeginSend(
                    _send_bytes, 0, ret,
                    SocketFlags.None, new AsyncCallback(on_send), this);
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", "[" + get_key_() + "]" + ex);

                // 不要直接调用关闭函数 do_close
                _server?.close_(_key, ex.Message);
            }
        }
        /// <summary>
        /// 当网络发送数据完成后回调
        /// </summary>
        /// <param name="result"></param>
        protected void on_send(IAsyncResult result)
        {
            try
            {
                if (!result.IsCompleted)
                {
                    // 发送未完成
                    cls_log.get_default_().T_("", "[" + get_key_() + "]" + cls_log.WARNING_ + "Send not completed");

                    result.AsyncWaitHandle.Close();

                    // 不要直接调用关闭函数 do_close
                    _server?.close_(_key, "Send not completed");

                    return;
                }

                int? count = _socket?.EndSend(result);
                result.AsyncWaitHandle.Close();

                count ??= -1;
                cls_log.get_default_().T_("", "[" + get_key_() + "]发送结束 {0}", count.Value);

                if (count > 0)
                {
                    _buffer_send.pop_((int)count);
                    // 继续发送
                    do_send();
                }
                else
                {
                    // 不要直接调用关闭函数 do_close
                    _server?.close_(_key, "Send null");
                }
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", "[" + get_key_() + "]" + ex);

                // 不要直接调用关闭函数 do_close
                _server?.close_(_key, ex.Message);
            }
        }
        /// <summary>
        /// 当网络接收数据后回调
        /// </summary>
        /// <param name="result"></param>
        protected void on_recv(IAsyncResult result)
        {
            try
            {
                if (!result.IsCompleted)
                {
                    // 读取未完成
                    cls_log.get_default_().T_("", "[" + get_key_() + "]" + cls_log.WARNING_ + "Recv not completed");

                    result.AsyncWaitHandle.Close();

                    // 不要直接调用关闭函数 do_close
                    _server?.close_(_key, "Recv not completed");

                    return;
                }

                int? count = _socket?.EndReceive(result);
                result.AsyncWaitHandle.Close();

                count ??= 0;

                // 快速处理数据
                if (count > 0)
                {
                    // 更新时刻
                    _last_tick = DateTime.Now.Ticks;

                    _buffer_recv.push_(_recv_bytes, 0, (int)count);

                    // 调用事件
                    // 使用一个复制避免数据锁

                    // 反复调用，清空数据
                    for (int i = 0; i < POP_PACK_MAX; i++)
                    {
                        byte[] cloneBytes = _buffer_recv.clone_();
                        if (cloneBytes.Length <= 0) break;

                        int? popCount = _server?.do_recv_(this, cloneBytes);

                        if (popCount == null) break;
                        if (popCount <= 0) break;
                        
                        if (_buffer_recv.pop_(popCount.Value) <= 0) break;
                    }                    

                    // 继续读取
                    do_recv();
                }
                else
                {
                    // 不要直接调用关闭函数 do_close
                    _server?.close_(_key, "Recv null");
                }
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", "[" + get_key_() + "]" + ex);

                // 不要直接调用关闭函数 do_close
                _server?.close_(_key, ex.Message);
            }
        }
    }
}
