using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
using cn.eobject.iot.Server.Net;
using EOIotServer.common;
using EOIotServer.protocol.hj212;
using Google.Protobuf.Collections;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOIotServer.protocol
{
    public class CServer_HJ212 : CServerIot, evt_server
    {
        protected int RecvMax = MAX_BUFFER;
        protected int SendMax = MAX_BUFFER;

        private const string PACKAGE_DEFAULT = "0";

        /// <summary>
        /// 单个线程最大处理包数量
        /// </summary>
        private const int THREAD_PACKAGE_COUNT = 256;

        protected cls_server ServerHandler;

        protected Dictionary<string, CPackageList_HJ212> DicPackageList = new();

        protected CDBSql_HJ212 DBSql;
        protected CDeviceManager DeviceManager;

        /// <summary>
        /// Tick单位 100纳秒
        /// </summary>
        protected long TickTimeout = 2000000000L;

        public CServer_HJ212()
        {
            ServerHandler = new(this);

            CConfig config = new CConfig();

            RecvMax = config.get_int32_("server/max_recv");
            SendMax = config.get_int32_("server/max_send");

            if (RecvMax <= 0)
            {
                cls_log.get_default_().T_("", cls_log.WARNING_ + "_buffer_recv_max = {0}", RecvMax);
                RecvMax = MAX_BUFFER;                
            }
            if (SendMax <= 0)
            {
                cls_log.get_default_().T_("", cls_log.WARNING_ + "_buffer_send_max = {0}", SendMax);
                SendMax = MAX_BUFFER;                
            }

            // 转换成100纳秒
            TickTimeout = config.get_int32_("server/timeout") * 10000L;

            // 初始化数据库
            DBSql = new(
                config.get_string_("db/db_string"),
                config.get_int32_("db/slow_delay"),
                config.get_string_("db/sql_test"));

            // 初始化状态管理
            DeviceManager = new(TickTimeout);

            DicPackageList.Add(PACKAGE_DEFAULT, new CPackageList_HJ212(PACKAGE_DEFAULT, 0));

            long delay;
            delay = config.get_int32_("package/delay_2011") * 10000L;
            DicPackageList.Add("2011", new CPackageList_HJ212("2011", delay));
            delay = config.get_int32_("package/delay_2051") * 10000L;
            DicPackageList.Add("2051", new CPackageList_HJ212("2051", delay));
            delay = config.get_int32_("package/delay_2061") * 10000L;
            DicPackageList.Add("2061", new CPackageList_HJ212("2061", delay));
            delay = config.get_int32_("package/delay_2031") * 10000L;
            DicPackageList.Add("2031", new CPackageList_HJ212("2031", delay));


            // 创建一个调度线程
            _ = new CThread(0, new ParameterizedThreadStart(OnThreadPackageTask), null);

            // 创建一个实时数据更新线程 大约每分钟执行2次
            _ = new CThread(27153, new ParameterizedThreadStart(OnThreadDataRtUpdate), null);

            // 状态管理（超时） 大约每分钟执行1次
            _ = new CThread(55297, new ParameterizedThreadStart(OnThreadStatus), null);

            ServerHandler.start_(config.get_int32_("server/port"));
        }
        
        /// <summary>
        /// 状态处理
        /// </summary>
        public void OnThreadStatus(object? tag)
{
            try
            {
                long tick = DateTime.Now.Ticks;

                // 处理连接超时
                ServerHandler.check_timeout_(tick, TickTimeout);

                // 处理状态管理
                DeviceManager.CheckConnect(tick);
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        /// <summary>
        /// 实时数据更新线程
        /// </summary>
        public void OnThreadDataRtUpdate(object? tag)
        {
            try
            {
                DBSql.DBUpdateDataRt();
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        /// <summary>
        /// 线程池调度线程
        /// </summary>
        public void OnThreadPackageTask(object? tag)
        {
            try
            {
                long tick = DateTime.Now.Ticks;

                CPackageList_HJ212 listPackage;
                foreach (KeyValuePair<string, CPackageList_HJ212> kvp in DicPackageList)
                {
                    listPackage = kvp.Value;

                    if (!listPackage.CheckDelay(tick)) continue;
                    if (listPackage.GetLength() <= 0) continue;

                    // 调用线程池
                    if (!ThreadPool.QueueUserWorkItem(new WaitCallback(OnThreadPackageData), listPackage))
                    {
                        cls_log.get_default_().T_("", cls_log.WARNING_ + " 线程池不足");
                    }
                }
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        /// <summary>
        /// 调用线程池将数据存储到数据库中
        /// </summary>
        /// <param name="state"></param>
        public void OnThreadPackageData(object? state)
        {
            try
            {
                if (state == null) return;

                CPackageList_HJ212 listPackage = (CPackageList_HJ212)state;

                List<CPackage_HJ212> list = listPackage.Pop(THREAD_PACKAGE_COUNT);
                if (list.Count <= 0) return;

                cls_log.get_default_().T_("", "处理{0} {1}/{2}", listPackage.CN, list.Count, listPackage.GetLength());

                long tick = DateTime.Now.Ticks;

                StringBuilder sb = new();
                List<CPackage_HJ212> listRegister = new();

                foreach (CPackage_HJ212 pack in list)
                {
                    if (pack.DeviceId <= 0)
                    {
                        listRegister.Add(pack);
                        sb.Append('\'').Append(pack.MN).Append('\'').Append(',');
                    }
                }

                if (sb.Length > 0)
                {
                    sb.Remove(sb.Length - 1, 1);
                    string mns = sb.ToString();
                    Dictionary<string, int> dicIds = DBSql.DBLoadDeviceIds(mns);

                    foreach (CPackage_HJ212 pack in listRegister)
                    {
                        if (dicIds.ContainsKey(pack.MN))
                        {
                            pack.DeviceId = dicIds[pack.MN];

                            // 更新连接上的编号
                            CConnect? tconnect = (CConnect?)ServerHandler.get_connect(pack.ConnectKey);
                            if (tconnect != null)
                            {
                                tconnect.DeviceId = pack.DeviceId;
                                tconnect.ConnectMN = pack.MN;
                            }

                            // 进入状态管理
                            DeviceManager.PushConnect(pack.MN, pack.ConnectKey, tick);
                        }                        
                    }

                    // 未避免每次都检查数据表，第一次注册时，检查记录是否存在
                    // 这里忽略并发冲突
                    DBSql.DBDataRtInit(mns);
                }

                listRegister.Clear();
                foreach (CPackage_HJ212 pack in list)
                {
                    if (!string.Equals(pack.ConnectMN, pack.MN, StringComparison.CurrentCultureIgnoreCase))
                    {
                        cls_log.get_default_().T_("", "MN设置: {0} -> {1}", pack.ConnectMN, pack.MN);
                    }

                    if (pack.DeviceId > 0)
                    {
                        listRegister.Add(pack);
                    }
                    else
                    {
                        cls_log.get_default_().T_("", "设备未注册{0}", pack.MN);
                        ServerHandler.close_(pack.ConnectKey, "设备未注册" + pack.MN);
                    }
                }

                // 更新数据
                switch (listPackage.CN)
                {
                    case "2011":
                        DBSql.DBInsertDataRt(list);
                        break;
                    case "2051":
                    case "2061":
                    case "2031":
                        DBSql.DBInsertHis("n_data_" + listPackage.CN, list);
                        break;
                }
                
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        public cls_connect? on_connect(string connectKey)
        {
            CConnect? connect = null;

            try
            {
                connect = new CConnect(ServerHandler, connectKey, RecvMax, SendMax);                
                cls_log.get_default_().T_("", "[" + connect.get_key_() + "]设备连接");
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", "[" + connectKey + "]" + ex.ToString());
            }

            return connect;
        }

        public void on_close(cls_connect connect)
        {
            string connectKey = "-";

            try
            {
                connectKey = connect.get_key_();

                cls_log.get_default_().T_("", 
                    "[" + connect.get_key_() + "]设备断开" + connect.get_start_time());
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", "[" + connectKey + "]" + ex.ToString());
            }
        }

        public int on_recv(cls_connect connect, byte[] bytes)
        {
            int len = 0;
            string connectKey = "-";

            try
            {
                connectKey = connect.get_key_();

                CConnect tconnect = (CConnect)connect;
                CPackage_HJ212 packageHJ212 = new(tconnect.get_key_());

                len = packageHJ212.Parse(bytes);
                if (len <= 0) return len;

                // 赋值设备编号
                packageHJ212.DeviceId = tconnect.DeviceId;
                packageHJ212.ConnectMN = tconnect.ConnectMN;

                cls_log.get_default_().T_("", "[" + connectKey + "]<" + packageHJ212.PackString);

                CConcurrentList<CPackage_HJ212> listPackage;
                if (DicPackageList.ContainsKey(packageHJ212.CN))
                    listPackage = DicPackageList[packageHJ212.CN];
                else
                    listPackage = DicPackageList[PACKAGE_DEFAULT];

                listPackage.Push(packageHJ212);
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", "[" + connectKey + "]" + ex.ToString());
            }

            return len;
        }
    }
}
