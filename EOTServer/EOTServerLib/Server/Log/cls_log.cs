using System.Diagnostics;

namespace cn.eobject.iot.Server.Log
{
    public sealed class cls_log
    {
        /// <summary>
        /// 重要提醒前缀
        /// </summary>
        public const string WARNING_ = "******** E ******** ";

        public const string LOG_DEFAULT_ = "_default_";
        public const string LOG_DEBUG_ = "_debug_";
        public const string LOG_DB_ = "_db_";

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        private static cls_log __handle;
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        public static cls_log handle_()
        {
            return __handle;
        }

        private Dictionary<string, cls_log_obj> _map_log = new();

        public cls_log()
        {
            __handle = this;
            add_log_(LOG_DEFAULT_, LOG_DEFAULT_, em_log_type.All, 0);
            add_log_(LOG_DEBUG_, LOG_DEBUG_, em_log_type.All, 0);
            add_log_(LOG_DB_, LOG_DB_, em_log_type.All, 0);
        }

        public static cls_log_obj add_log_(string name, string prefix, em_log_type logType, int dateCount)
        {
            cls_log_obj logObj = new cls_log_obj(name, prefix, logType, dateCount);
            __handle._map_log.Add(name, logObj);

            return logObj;
        }
        public static cls_log_obj get_default_()
        {
            return __handle._map_log[LOG_DEFAULT_];
        }
        public static cls_log_obj get_debug_()
        {
            return __handle._map_log[LOG_DEBUG_];
        }
        public static cls_log_obj get_db_()
        {
            return __handle._map_log[LOG_DB_];
        }
        public static cls_log_obj get_log_(string name)
        {
            return __handle._map_log[name];
        }
    }
}
