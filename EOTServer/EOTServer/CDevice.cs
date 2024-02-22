using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOIotServer
{
    /// <summary>
    /// MN对应的设备
    /// </summary>
    public class CDevice
    {
        public string MN = "";

        // 每个设备可能存在多个连接
        public Dictionary<string, long> ConnectList = new();

        public CDevice(string mn) 
        { 
            MN = mn;
        }

        /// <summary>
        /// 获取一个副本
        /// </summary>
        /// <returns></returns>
        public List<string> GetConnectList()
        {
            List<string> list = new();

            lock (ConnectList)
            {
                foreach (KeyValuePair<string, long> kvp in ConnectList)
                {
                    list.Add(kvp.Key);
                }
            }

            return list;
        }

        public void PushConnect(string connectKey, long tick)
        {
            lock (ConnectList)
            {
                if (ConnectList.ContainsKey(connectKey))
                {
                    ConnectList[connectKey] = tick;
                }
                else
                {
                    ConnectList.Add(connectKey, tick);
                }
            }
        }

        public void PopConnect(string connectKey)
        {
            lock (ConnectList)
            {
                ConnectList.Remove(connectKey);
            }
        }

        public bool CheckConnect(long tick, long delay)
        {
            lock (ConnectList)
            {
                List<string> listRemove = new();

                // 使用超时，避免死数据
                foreach (KeyValuePair<string, long> kvp in ConnectList)
                {
                    if ((tick - kvp.Value) > delay)
                    {
                        listRemove.Add(kvp.Key);
                    }
                }

                foreach (string s in listRemove)
                {
                    ConnectList.Remove(s);
                }

                // 如果一个连接都没有，设备离线
                if (ConnectList.Count <= 0) return false;
            }

            return true;
        }
    }
}
