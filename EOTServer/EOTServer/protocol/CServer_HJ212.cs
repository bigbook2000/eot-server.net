using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
using cn.eobject.iot.Server.Net;
using EOIotServer.common;
using EOIotServer.protocol.hj212;
using Google.Protobuf.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Org.BouncyCastle.Utilities;
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

        /// <summary>
        /// 按CN标识处理不同的消息包，分发到不同的线程，提高处理能力
        /// </summary>
        protected Dictionary<string, CPackageList_HJ212> DicPackageList = new();

        /// <summary>
        /// 用于处理发送的命令
        /// 串行处理，即每次只有一个MN对应的设备可以发送命令
        /// 需要处理超时
        /// </summary>
        protected Dictionary<string, CPackage_HJ212> DicPackageCommand = new();

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
                config.get_int32_("db/slow_delay"),
                config.get_string_("db/sql_test"));
            DBSql.load_(cls_core.base_path_(config.get_string_("db/sql_path")));
            DBSql.add_db_(
                config.get_string_("db/name"),
                config.get_string_("db/db_string"));

            // 初始化状态管理
            DeviceManager = new(TickTimeout);

            // 默认的消息队列
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
        /// 发送数据
        /// </summary>
        /// <param name="mn"></param>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="info">日志</param>
        public void SendData(string mn, byte[] bytes, int offset, int count, string info)
        {
            try
            {
                // 同一个设备编号可能存在多个连接，每个都发送
                List<string> keyList = DeviceManager.FindConnects(mn);

                int nCount = 0;
                CConnect? connect;
                foreach (string key in keyList)
                {
                    connect = (CConnect?)ServerHandler.get_connect(key);
                    if (connect == null) continue;

                    cls_log.get_default_().T_("", "[" + key + "]>" + info);
                    connect.send_(bytes, offset, count);

                    nCount++;
                }

                if (nCount == 0)
                {
                    cls_log.get_default_().T_("", "未找到对应的设备 " + mn);
                }
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        /// <summary>
        /// 发送命令
        /// </summary>
        /// <param name="pack"></param>
        public void SendPackage(CPackage_HJ212 pack)
        {
            try
            {
                byte[] bytes = cls_core.str2bytes_(pack.PackString + "\r\n");
                SendData(pack.MN, bytes, 0, bytes.Length, pack.PackString);
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }


        /// <summary>
        /// 判断串行控制指令是否正在进行
        /// </summary>
        /// <param name="packSend"></param>
        /// <returns></returns>
        private bool CheckCommandPackage(CPackage_HJ212 packSend)
        {
            try
            {
                lock (DicPackageCommand)
                {
                    if (DicPackageCommand.ContainsKey(packSend.MN))
                    {
                        CPackage_HJ212 pack = DicPackageCommand[packSend.MN];
                        long dt = (DateTime.Now.Ticks - pack.LastTick) / 10000;
                        if (dt > 60000)
                        {
                            // 超过60秒移除旧的
                            DicPackageCommand.Remove(packSend.MN);
                        }
                        else
                        {
                            return true;
                        }
                    }

                    // 加入到队列中            
                    DicPackageCommand.Add(packSend.MN, packSend);
                }
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }

            return false;
        }

        private void SetCommandRecv(CPackage_HJ212 packRecv)
        {
            try
            {
                lock (DicPackageCommand)
                {
                    if (!DicPackageCommand.ContainsKey(packRecv.MN)) return;

                    // 查找对应的发送命令
                    CPackage_HJ212 packSend = DicPackageCommand[packRecv.MN];
                    if (packSend.CN == packRecv.CN)
                    {
                        packSend.Tag = packRecv;
                    }

                    DicPackageCommand.Remove(packRecv.MN);
                }
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        /// <summary>
        /// 自定义HJ212命令
        /// 3101 获取设备参数
        /// 3102 上传设备参数
        /// 3103 获取传感器状态
        /// 3104 控制传感器状态
        /// 3109 固件更新
        /// </summary>
        /// <param name="mn"></param>
        /// <param name="st"></param>
        /// <param name="cn"></param>
        /// <param name="configData"></param>
        public async Task<string?> SendCommand(string mn, string st, string cn, string configData)
        {
            string sMsg;

            try
            {
                CPackage_HJ212 packSend = new("");
                packSend.Encode(mn, st, cn, CPackage_HJ212.PASSWORD_DEFAULT, configData);
                packSend.LastTick = DateTime.Now.Ticks;

                if (CheckCommandPackage(packSend))
                {
                    sMsg = "命令已在队列中 " + mn + ", cn=" + cn;
                    cls_log.get_default_().T_("", sMsg);
                    return sMsg;
                }

                SendPackage(packSend);

                CPackage_HJ212? packRecv = null;
                // 同步等待
                for (int i = 0; i < 0x7F; i++)
                {
                    await Task.Delay(100);
                    if (packSend.Tag != null)
                    {
                        packRecv = (CPackage_HJ212?)packSend.Tag;
                        break;
                    }
                }

                return packRecv?.ParseCP();
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }

            return null;
        }

        /// <summary>
        /// 升级设备版本
        /// </summary>
        /// <param name="mn">设备编号</param>
        /// <param name="type">类型</param>
        /// <param name="version">版本</param>
        /// <param name="total">文件大小</param>
        /// <param name="crc">校验码</param>
        /// <param name="url">文件地址（远程）</param>
        /// <returns></returns>
        public async Task<string?> SendVersionUpdate(string mn, string type, string version, int total, string crc, string url)
        {
            string sMsg;

            try
            {
                CPackage_HJ212 packSend = new("");
                string updateInfo = "UType=" + type +
                    ";UVersion=" + version +
                    ";UTotal=" + total +
                    ";UCrc=" + crc;
                packSend.Encode(mn, "00", CPackage_HJ212.CN_BIN_UPDATE, CPackage_HJ212.PASSWORD_DEFAULT, updateInfo);
                packSend.LastTick = DateTime.Now.Ticks;

                if (CheckCommandPackage(packSend))
                {
                    sMsg = "升级命令已在队列中 " + mn;
                    cls_log.get_default_().T_("", sMsg);
                    return sMsg;
                }

                string dirs = cls_core.base_path_("cache/update");
                Directory.CreateDirectory(dirs);

                // 下载到缓存，web和server分离时
                string file = dirs + "/" + crc + ".bin";
                FileInfo fi = new(file);
                if (!fi.Exists)
                {                    
                    using HttpClient httpClient = new();
                    var response = await httpClient.GetAsync(url);
                    if (response == null || !response.IsSuccessStatusCode)
                    {
                        sMsg = "版本文件不存在 " + url;
                        cls_log.get_default_().T_("", sMsg);
                        return sMsg;
                    }

                    using var fs = File.Create(file);
                    await response.Content.CopyToAsync(fs);
                    fs.Close();
                    fs.Dispose();
                }

                SendPackage(packSend);
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
                return ex.ToString();
            }

            return null;
        }

        private void SendCommandConfigGet(CConnect connect, string mn, string st)
        {
            try
            { 
                CPackage_HJ212 packSend = new("");
                packSend.Encode(mn, st, CPackage_HJ212.CN_CONFIG_GET, CPackage_HJ212.PASSWORD_DEFAULT, "");

                cls_log.get_default_().T_("", "[" + connect.get_key_() + "]>" + packSend.PackString);

                byte[] bytes = cls_core.str2bytes_(packSend.PackString + "\r\n");
                connect.send_(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        /// <summary>
        /// 处理其他CN命令消息
        /// 2011,2021,2031,2041,2051,2061数据包由专门的队列处理，其他的则转到此函数处理
        /// </summary>
        /// <param name="list"></param>
        private void DoPackageDefault(List<CPackage_HJ212> list)
        {
            try
            {
                foreach (CPackage_HJ212 packRecv in list)
                {
                    SetCommandRecv(packRecv);

                    switch (packRecv.CN)
                    {
                        // 处理配置信息
                        case CPackage_HJ212.CN_CONFIG_GET:
                            DBSql.DBUpdateDeviceConfig(packRecv);
                            break;
                        // 处理升级发送文件
                        case CPackage_HJ212.CN_BIN_UPDATE:
                            DoPackageUpdateBin(packRecv);
                            break;
                        case CPackage_HJ212.CN_CFG_UPDATE:
                            DoPackageUpdateConfig(packRecv);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        /// <summary>
        /// 升级，实际发送文件
        /// </summary>
        /// <param name="pack"></param>
        private void DoPackageUpdateBin(CPackage_HJ212 pack)
        {
            try
            {
                string crc = pack.GetCPString("UCrc");
                int pos = pack.GetCPInt32("UPos");
                int size = pack.GetCPInt32("USize");

                string file = cls_core.base_path_("cache/update/" + crc + ".bin");
                FileInfo fi = new(file);

                if (!fi.Exists)
                {
                    // 出现错误，版本文件不存在
                    cls_log.get_default_().T_("", "升级错误，版本文件不存在" + crc);
                    return;
                }

                byte[] buffer = new byte[size];
                int read;
                using FileStream fs = new(file, FileMode.Open, FileAccess.Read);

                // 文件大小不会超过32个字节
                if (pos < (int)fs.Length)
                {
                    fs.Seek(pos, SeekOrigin.Begin);
                    read = fs.Read(buffer, 0, size);

                    // 发送
                    SendData(pack.MN, buffer, 0, read, "Buffer: " + read);
                }

                fs.Close();
                fs.Dispose();
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        /// <summary>
        /// 发送升级配置
        /// </summary>
        /// <param name="pack"></param>
        private void DoPackageUpdateConfig(CPackage_HJ212 pack)
        {
            try
            {
                string crc = pack.GetCPString("UCrc");
                string configData = DBSql.DBLoadVersionConfig(crc);

                // 将通用的MN替换
                configData = configData.Replace("@@DeviceId", pack.MN);

                // 发送
                CPackage_HJ212 packSend = new("");
                packSend.Encode(pack.MN, "99", 
                    CPackage_HJ212.CN_CFG_UPDATE, CPackage_HJ212.PASSWORD_DEFAULT, configData);

                SendPackage(packSend);
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        /// <summary>
        /// 状态处理
        /// </summary>
        private void OnThreadStatus(object? tag)
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
        private void OnThreadDataRtUpdate(object? tag)
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
        private void OnThreadPackageTask(object? tag)
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
        private void OnThreadPackageData(object? state)
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
                        sb.Append(pack.MN).Append(',');
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

                                // 发送命令获取配置参数
                                SendCommandConfigGet(tconnect, pack.MN, pack.ST);
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
                        //ServerHandler.close_(pack.ConnectKey, "设备未注册" + pack.MN);
                        return;
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
                    default:
                        DoPackageDefault(list);
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
