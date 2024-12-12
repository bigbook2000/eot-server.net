using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.eobject.iot.Server.Net
{
    /// <summary>
    /// 缓存对象
    /// 用来处理网络字节流或者管理内存，该对象使用了一个先进先出FIFO的方式。
    /// 具有线程安全性。
    /// 固定长度的缓存，之所以不自动增加，避免由于逻辑错误造成内存消耗过大，正常情况下我们设计的负载能力和网络包的大小是可预知的。
    /// </summary>
    public class cls_buffer
    {
        /// <summary>
        /// 使用一个只读对象锁
        /// </summary>
        private readonly object _lock_flag = new();
        /// <summary>
        /// 字节流缓存
        /// </summary>
        protected byte[] _buffer;
        /// <summary>
        /// 字节流长度
        /// </summary>
        protected int _length;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="bufferMax">固定长度</param>
        public cls_buffer(int bufferMax) 
        {
            _buffer = new byte[bufferMax];
        }
        /// <summary>
        /// 获取长度
        /// </summary>
        /// <returns></returns>
        public int get_length_() 
        {
            lock (_lock_flag)
            {
                return _length;
            }
        }

        public void clear_()
        {
            lock (_lock_flag)
            {
                _length = 0;
            }
        }

        /// <summary>
        /// 缓存数据，如果插入的数据过多，则全部清除。
        /// 长度固定，如果超出则表示逻辑出现异常。
        /// </summary>
        /// <param name="bytes">源字节流数据</param>
        /// <param name="offset">偏移量</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        public int push_(byte[] bytes, int offset, int length)
        {
            if (length <= 0) return _length;

            lock (_lock_flag)
            {
                if ((_length + length) > _buffer.Length)
                {
                    // 溢出，清空防止阻塞
                    _length = 0;
                    return -1;
                }

                Array.Copy(bytes, offset, _buffer, _length, length);
                _length += length;

                //cls_log.get_default_().T_("", "T[{0}/{1}] {2}", length, _length, cls_core.log_bytes_(_buffer, 0, _length));
            }

            return _length;
        }

        /// <summary>
        /// 取出数据
        /// </summary>
        /// <param name="bytes">目标字节流数据</param>
        /// <param name="offset">偏移量</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        public int pop_(byte[] bytes, int offset, int length)
        {
            lock (_lock_flag)
            {
                // 如果超出范围，全部拷贝（截断）
                if (length < 0 || length > _length) length = _length;

                // 从0开始
                Array.Copy(_buffer, 0, bytes, offset, length);
                _length -= length;

                //cls_log.get_default_().T_("", "P[{0}/{1}] {2}", length, _length, cls_core.log_bytes_(_buffer, 0, _length));
            }

            return length;
        }
        /// <summary>
        /// 移除指定长度的数据
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public int pop_(int length)
        {
            if (length <= 0) return _length;

            lock (_lock_flag)
            {
                // 如果超出范围，清空
                if (length < 0 || length > _length)
                {
                    _length = 0;
                }
                else
                {
                    // 尾部的数据前移
                    Array.Copy(_buffer, length, _buffer, 0, _length - length);
                    _length -= length;
                }

                //cls_log.get_default_().T_("", "P[{0}/{1}] {2}", length, _length, cls_core.log_bytes_(_buffer, 0, _length));
            }

            return _length;
        }

        /// <summary>
        /// 创建一个副本，可以避免冲突锁
        /// </summary>
        /// <returns></returns>
        public byte[] clone_()
        {
            byte[] bytes;
            lock (_lock_flag)
            {
                bytes = new byte[_length];
                if (_length > 0)
                {                    
                    Array.Copy(_buffer, 0, bytes, 0, _length);
                }

                //cls_log.get_default_().T_("", "K[{0}/{1}] {2}", 0, _length, cls_core.log_bytes_(_buffer, 0, _length));
            }

            return bytes;
        }
    }
}
