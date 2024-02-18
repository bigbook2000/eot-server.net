using cn.eobject.iot.Server.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOIotServer
{
    /// <summary>
    /// 按MN在线状态管理
    /// </summary>
    public class CDeviceManager
    {
        protected Dictionary<string, CDevice> DicDevices = new();

        /// <summary>
        /// 100纳秒
        /// </summary>
        protected long TickTimeout = 2000000000L;

        public CDeviceManager(long timeout)
        {
            TickTimeout = timeout * 10000L;
        }

        /// <summary>
        /// 根据MN查找对应的连接
        /// </summary>
        /// <param name="mn"></param>
        /// <returns></returns>
        public List<string> FindConnects(string mn)
        {
            List<string> list = new();

            try
            {
                lock (DicDevices)
                {                    
                    if (DicDevices.ContainsKey(mn))
                    {
                        CDevice device = DicDevices[mn];
                        list = device.GetConnectList();
                    }
                }
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }

            return list;
        }

        public void PushConnect(string mn, string connectKey, long tick)
        {
            try
            {
                lock (DicDevices)
                {
                    CDevice device;
                    if (DicDevices.ContainsKey(mn))
                    {
                        device = DicDevices[mn];
                    }
                    else
                    {
                        device = new CDevice(mn);
                        DicDevices.Add(mn, device);
                    }

                    device.PushConnect(connectKey, tick);
                }
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        public void PopConnect(string mn, string connectKey)
        {
            try
            {
                lock (DicDevices)
                {
                    CDevice device;
                    if (DicDevices.ContainsKey(mn))
                    {
                        device = DicDevices[mn];
                        device.PopConnect(connectKey);
                    }
                    else
                    {
                        cls_log.get_default_().T_("", "[{0}]设备已经离线: {1}", connectKey, mn);
                    }
                }
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        public void CheckConnect(long tick)
        {
            try
            {
                List<string> listRemove = new();

                lock (DicDevices)
                {
                    foreach (KeyValuePair<string, CDevice> kvp in DicDevices)
                    {
                        CDevice device = kvp.Value;
                        if (!device.CheckConnect(tick, TickTimeout))
                        {
                            listRemove.Add(device.MN);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }
    }
}
