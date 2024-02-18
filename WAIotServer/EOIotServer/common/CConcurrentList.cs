using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOIotServer.common
{
    public class CConcurrentList<T> : List<T>
    {
        private readonly object LockFlag = new();

        public int GetLength()
        {
            lock (LockFlag)
            {
                return Count;
            }
        }

        public void Push(T item) 
        {
            lock (LockFlag)
            {
                Add(item);
            }
        }
        public void PushAll(IEnumerable<T> collection)
        {
            lock (LockFlag)
            {
                AddRange(collection);
            }
        }

        /// <summary>
        /// 提取指定数量的元素
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<T> Pop(int count)
        {
            List<T> list = new();

            lock (LockFlag)
            {
                // 如果超出索引，提取全部
                if (count <= 0 || count > Count) count = Count;

                if (count > 0)
                {
                    list.AddRange(GetRange(0, count));
                    RemoveRange(0, count);
                }
            }

            return list;
        }

        public List<T> Clone()
        {
            lock (LockFlag)
            {
                return new List<T>(this);
            }
        }
    }
}
