using cn.eobject.iot.Server.Config;
using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.DB;
using cn.eobject.iot.Server.Log;
using EOIotServer.protocol;
using System.Security.Cryptography;
using System.Text;

namespace WAIotServer.Logic
{
    public class CGlobal : cls_config
    {
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        private static CGlobal gHandle;
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        public static CGlobal GCHandle()
        {
            return gHandle;
        }

        public static string AppToken = "!__EOService@2023*";
        public static string DefaultPassword = "Asdf@4321";

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        
        /// <summary>
        /// 数据库对象
        /// </summary>
        public static cls_eotsql DBScript;
        /// <summary>
        /// 物联网数据服务
        /// </summary>
        public static CServer_HJ212 IotServer;

#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。

        public static int SessionTimeout = 600000;
        public static int RootId = 1;

        public static Dictionary<string, string> DBProcList = new();


        public CGlobal()
        {
            gHandle = this;

            load_(cls_core.base_path_("web.yml"));
            
            SessionTimeout = get_int32_("web/session_timeout");
            RootId = get_int32_("web/root_id");
            DefaultPassword = get_string_("web/default_password");

            DBScript = new(get_int32_("db/slow_delay"));
            DBScript.load_(cls_core.base_path_(get_string_("db/sql_path")));
            DBScript.add_db_(get_string_("db/name"), get_string_("db/db_string"));

            // 启动一个数据服务
            IotServer = new CServer_HJ212();

            // 初始化数据接口，只有配置的数据接口才能被调用
            LoadDBProcList();
        }

        public void LoadDBProcList()
        {
            DBProcList.Clear();

            List<cls_config_obj> list = get_childs_("dbproclist");
            foreach (cls_config_obj obj in list)
            {
                if (obj.is_comment_()) continue;

                // 如果是 none 不计权限
                if ("none".Equals(obj._val, StringComparison.OrdinalIgnoreCase))
                    DBProcList.Add(obj._key, "");
                else
                    DBProcList.Add(obj._key, obj._val);
            }

            int cnt = DBProcList.Count;
            if (cnt > 0)
                cls_log.get_default_().T_("", "加载数据接口: {0}", cnt);
            else
                cls_log.get_default_().T_("", cls_log.WARNING_ + "无任何数据接口");
        }

        /// <summary>
        /// 加密密码
        /// </summary>
        /// <param name="data"></param>
        /// <param name="salt"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static string EncryptPassword(string data, string salt, bool origin)
        {
            MD5 md5 = MD5.Create();

            byte[] bytes;

            if (origin)
            {
                bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(data));
                data = Convert.ToHexString(bytes).ToLower();
            }

            bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(salt + CGlobal.AppToken + data));
            return Convert.ToHexString(bytes).ToLower();
        }

        /// <summary>
        /// 分解文件路径和扩展名，
        /// </summary>
        /// <param name="path"></param>
        /// <returns>返回固定长度3的字符串数组，分别为路径，文件名，扩展名</returns>
        public static string[] GetFileInfo(string path)
        {
            string[] ss = new string[3];
            ss[0] = "";
            ss[1] = path;
            ss[2] = "";

            int pos1 = path.LastIndexOf('/');
            int pos2 = path.LastIndexOf('.');

            // .在路径中
            if (pos1 > pos2) pos2 = -1;

            if (pos1 != -1)
            {
                ss[0] = path.Substring(0, pos1);
                ss[1] = path.Substring(pos1 + 1);
            }

            if (pos2 != -1)
            {
                string s = ss[1];
                ss[1] = s.Substring(0, pos2);
                ss[2] = s.Substring(pos2 + 1);
            }

            return ss;
        }
    }
}
