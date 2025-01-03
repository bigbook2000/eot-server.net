using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOIotServer.protocol.hj212
{
    /// <summary>
    /// 
    /// 解析HJ212消息包
    /// 
    /// </summary>
    public class CPackage_HJ212
    {
        /// <summary>
        /// 最小包长度
        /// </summary>
        public const int MIN_LENGTH = 12;

        /// <summary>
        /// 密钥（忽略）
        /// </summary>
        public const string PASSWORD_DEFAULT = "123456";

        /// <summary>
        /// 设备上传唯一标识（注册）
        /// </summary>
        public const string CN_DEVICE_KEY = "3019";
        /// <summary>
        /// 获取设备参数
        /// </summary>
        public const string CN_CONFIG_GET = "3020";
        /// <summary>
        /// 上传设备参数
        /// </summary>
        public const string CN_CONFIG_SET = "3021";

        /// <summary>
        /// 获取传感器状态
        /// </summary>
        public const string CN_CONTROL_GET = "3103";
        /// <summary>
        /// 控制传感器状态
        /// </summary>
        public const string CN_CONTROL_SET = "3104";
        /// <summary>
        /// 固件更新
        /// </summary>
        public const string CN_BIN_UPDATE = "3111";

        public int DeviceId = 0;

        public string MN = "";
        public DateTime QN = cls_core._date1970;
        public string CN = "";
        public string ST = "";

        /// <summary>
        /// 完整的数据包，不包含换行标识
        /// </summary>
        public string PackString = "";

        public string ConnectKey = "___.___.___.___:_____";
        /// <summary>
        /// 标记连接MN
        /// </summary>
        public string ConnectMN = "";

        public Dictionary<string, string> CPList = new();

        /// <summary>
        /// 数据时间
        /// </summary>
        public DateTime DataTime = cls_core._date1970;

        /// <summary>
        /// 时间戳
        /// </summary>
        public long LastTick = 0L;

        /// <summary>
        /// 外挂标识
        /// </summary>
        public object? Tag = null;

        public CPackage_HJ212(string connectKey) 
        {
            ConnectKey = connectKey;
        }

        /// <summary>
        /// HJ212 16位CRC校验码
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string EncodeCRC16(string str)
        {
            byte[] bytes = cls_core.str2bytes_(str);

            int i, cnt;
            
            int crcReg = 0xFFFF;
            int check;;

            cnt = bytes.Length;
            for (i = 0; i < cnt; i++)
            {

                crcReg = (crcReg >> 8) ^ bytes[i];
                for (int j = 0; j < 8; j++)
                {
                    check = crcReg & 0x0001;
                    crcReg >>= 1;
                    if (check == 0x0001)
                    {
                        crcReg ^= 0xa001;
                    }
                }
            }

            return crcReg.ToString("X4");
        }

        public int GetKeyValue(string sp, out string key, out string val)
        {
            int pos = sp.IndexOf('=');
            if (pos <= 0)
            {
                key = "";
                val = "";
                return -1;
            }

            key = sp.Substring(0, pos).Trim();
            val = sp.Substring(pos + 1).Trim();

            return pos;
        }

        public DateTime String2DateTime(string str)
        {
            DateTime dt = cls_core._date1970;

            try
            {
                if (!(str.Length == 14 || str.Length == 17)) return dt;

                _ = int.TryParse(str.AsSpan(0, 4), out int yy);
                _ = int.TryParse(str.AsSpan(4, 2), out int mm);
                _ = int.TryParse(str.AsSpan(6, 2), out int dd);
                _ = int.TryParse(str.AsSpan(8, 2), out int hh);
                _ = int.TryParse(str.AsSpan(10, 2), out int mi);
                _ = int.TryParse(str.AsSpan(12, 2), out int ss);

                int ms = 0;                
                if (str.Length == 17)
                    _ = int.TryParse(str.AsSpan(14, 3), out ms);

                dt = new DateTime(yy, mm, dd, hh, mi, ss, ms);
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }

            return dt;
        }

        /// <summary>
        /// 将CP数据转成Json
        /// </summary>
        /// <returns></returns>
        public string FormatDataJson()
        {
            StringBuilder sb = new();

            try
            {
                sb.Append('{');
                foreach (KeyValuePair<string, string> kvp in CPList)
                {
                    sb.Append('\"') .Append(kvp.Key).Append("\":\"").Append(kvp.Value).Append("\",");
                }
                if (CPList.Count > 0) sb.Remove(sb.Length- 1, 1);
                sb.Append('}');

                sb.Replace("'", "\\'");
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }

            return sb.ToString();
        }

        /// <summary>
        /// 解析CP
        /// </summary>
        /// <returns></returns>
        public string? ParseCP()
        {
            int pos = PackString.IndexOf("CP=");
            if (pos < 6)
            {
                cls_log.get_default_().T_("", "[" + ConnectKey + "]数据包错误CP {0}", PackString);
                return null;
            }

            // 忽略校验长度和CRC
            string sp = PackString.Substring(pos + 3, PackString.Length - pos - 7);
            if (!sp.StartsWith("&&") || !sp.EndsWith("&&"))
            {
                cls_log.get_default_().T_("", "[" + ConnectKey + "]数据包错误&& {0}", PackString);
                return null;
            }

            return sp.Substring(2, sp.Length - 4);
        }

        /// <summary>
        /// 解析包，如果未能解析成功，返回0
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public int Parse(byte[] bytes)
        {
            int len = 0;

            try
            {
                int i, cnt;
                cnt = bytes.Length;

                // 长度不够
                if (cnt < MIN_LENGTH) return 0;

                int startPos = -1;
                int endPos = -1;

                for (i = 1; i < cnt; i++)
                {
                    if (bytes[i - 1] == 0x23 && bytes[i] == 0x23)
                    {
                        startPos = i - 1;
                    }

                    if (bytes[i - 1] == 0x0D && bytes[i] == 0x0A)
                    {
                        endPos = i;

                        // 有一个包即先处理
                        break;
                    }
                }

                // 未解析成功
                if (startPos < 0 || endPos < 0) return 0;

                // 错误的包
                if (startPos > endPos)
                {
                    cls_log.get_default_().T_("", "[" + ConnectKey + "]数据包解析失败 {0} - {1}", startPos, endPos);
                    return cnt;
                }

                len = endPos - startPos + 1;

                // 不包含换行标识
                PackString = cls_core.bytes2str_(bytes, 0, len - 2);

                int pos = PackString.IndexOf("CP=");
                if (pos < 6)
                {
                    cls_log.get_default_().T_("", "[" + ConnectKey + "]数据包错误CP {0}", PackString);
                    return len;
                }

                // 忽略校验长度和CRC
                string sp1 = PackString.Substring(6, pos - 6);
                string sp2 = PackString.Substring(pos + 3, PackString.Length - pos - 7);
                if (!sp2.StartsWith("&&") || !sp2.EndsWith("&&"))
                {
                    cls_log.get_default_().T_("", "[" + ConnectKey + "]数据包错误&& {0}", PackString);
                    return len;
                }
                sp2 = sp2.Substring(2, sp2.Length - 4);

                string sp, key, val;
                string[] ss;
                ss = sp1.Split(';');
                foreach (string s in ss)
                {
                    sp = s.Trim();
                    if (sp.Length <= 0) continue;

                    if (GetKeyValue(sp, out key, out val) <= 0) continue;
                    switch (key)
                    {
                        case "QN": QN = String2DateTime(val); break;
                        case "MN": MN = val; break;
                        case "ST": ST = val; break;
                        case "CN": CN = val; break;
                    }
                }

                sp2 = sp2.Replace(',', ';');
                ss = sp2.Split(';');
                foreach (string s in ss)
                {
                    sp = s.Trim();
                    if (sp.Length <= 0) continue;

                    if (GetKeyValue(sp, out key, out val) <= 0) continue;

                    // 提取数据时间
                    if ("DataTime".Equals(key, StringComparison.CurrentCultureIgnoreCase))
                    {
                        DataTime = String2DateTime(val);
                    }
                    else
                    {
                        if (CPList.ContainsKey(key))
                            CPList[key] = val;
                        else
                            CPList.Add(key, val);
                    }
                }
            }
            catch(Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }

            return len;
        }

        /// <summary>
        /// 编码
        /// 不含\r\n换行符
        /// </summary>
        /// <param name="mn"></param>
        /// <param name="st"></param>
        /// <param name="cn"></param>
        /// <param name="psw"></param>
        /// <param name="cp"></param>
        public string Encode(string mn, string st, string cn, string psw, string cp)
        {
            MN = mn;
            CN = cn;
            ST = st;

            string dts = DateTime.Now.ToString("yyyyMMddHHmmss");
            PackString = string.Format(
                "QN={0}000;ST={1};CN={2};PW={3};MN={4};Flag=5;CP=&&{5}&&",
                dts, st, cn, psw, mn, cp);
            string sLen = PackString.Length.ToString("0000");
            string sCrc = EncodeCRC16(PackString);

            PackString = "##" + sLen + PackString + sCrc;

            return PackString;
        }

        public string GetCPString(string key)
        {
            if (this.CPList.ContainsKey(key))
            {
                return this.CPList[key];
            }

            return "";
        }
        public int GetCPInt32(string key)
        {
            if (this.CPList.ContainsKey(key))
            {
                if (int.TryParse(this.CPList[key], out int val))
                {
                    return val;
                }                
            }

            return 0;
        }
    }
}
