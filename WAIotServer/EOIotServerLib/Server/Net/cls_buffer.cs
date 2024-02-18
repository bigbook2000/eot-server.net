using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.eobject.iot.Server.Net
{
    public class cls_buffer
    {
        /// <summary>
        /// 使用一个只读对象锁
        /// </summary>
        private readonly object _lock_flag = new();
            
        protected byte[] _buffer;
        protected int _length;

        public cls_buffer(int bufferMax) 
        {
            _buffer = new byte[bufferMax];
        }

        public int get_length_() 
        {
            lock (_lock_flag)
            {
                return _length;
            }
        }

        /// <summary>
        /// 缓存数据
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
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
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public int pop_(byte[] bytes, int offset, int length)
        {
            lock (_lock_flag)
            {
                // 如果超出范围，全部拷贝
                if (length < 0 || length > _length) length = _length;

                // 从0开始
                Array.Copy(_buffer, 0, bytes, offset, length);
                _length -= length;

                //cls_log.get_default_().T_("", "P[{0}/{1}] {2}", length, _length, cls_core.log_bytes_(_buffer, 0, _length));
            }

            return _length;
        }

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
