using cn.eobject.iot.Server.Config;
using cn.eobject.iot.Server.Core;
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOIotServer.common
{
    public class CConfig : cls_config
    {
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        private static CConfig gHandle;
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        public static CConfig GCHandle()
        {
            return gHandle;
        }

        public CConfig()
        {
            gHandle = this;

            load_(cls_core.base_path_("server.yml"));
        }
    }
}
