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
    public class CPackage_HJ212
    {
        public const int MIN_LENGTH = 12;

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

        public CPackage_HJ212(string connectKey) 
        {
            ConnectKey = connectKey;
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
    }
}
