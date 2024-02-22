using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;

namespace cn.eobject.iot.Server.Net
{
    public class cls_connect
    {
        /// <summary>
        /// 清空数据次数
        /// </summary>
        private const int POP_PACK_MAX = 8;

        private cls_server? _server;
        private Socket? _socket;

        private byte[] _recv_bytes;
        private byte[] _send_bytes;

        private cls_buffer _buffer_recv;
        private cls_buffer _buffer_send;

        protected string _key = "_";

        /// <summary>
        /// 记录连接时间
        /// </summary>
        protected DateTime _start_time = DateTime.Now;

        /// <summary>
        /// 最新获取数据的时刻
        /// </summary>
        protected long _last_tick = 0L;

        public cls_connect(cls_server server, string key, int bufferRecvMax, int bufferSendMax)
        {
            _server = server;

            _key = key;

            _recv_bytes = new byte[bufferRecvMax];
            _send_bytes = new byte[bufferSendMax];

            _buffer_recv = new(bufferRecvMax);
            _buffer_send = new(bufferSendMax);
        }

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

        internal int do_accept_(Socket socket)
        {
            // 仅关闭，不清除
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

                cls_log.get_default_().T_("", "[" + get_key_() + "]直接发送 {0} / {1}", ret, len);

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
