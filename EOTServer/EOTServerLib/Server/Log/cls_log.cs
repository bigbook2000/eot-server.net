using System.Diagnostics;

namespace cn.eobject.iot.Server.Log
{
    /// <summary>
    /// 
    /// 日志全局对象
    /// 
    /// 对于服务端应用来说，大数据和大并发是最重要的特点，表现在很多程序的bug和逻辑缺陷需要在一定的条件下才触发。
    /// 开发此类应用首先需要有一个“趁手”的日志框架。为什么强调趁手，无论是系统自带还是网络开源，不乏优秀的日志框架，但很多时候会有力不从心的感觉，因此按照以往的经验自定义一个日志框架。
    /// 
    /// cls_log用于管理输出和格式化日志，主要目标一是分文件，二是格式化。
    /// 
    /// </summary>
    public sealed class cls_log
    {
        /// <summary>
        /// 重要提醒前缀，用于格式化日志
        /// </summary>
        public const string WARNING_ = "******** E ******** ";

        /// <summary>
        /// 默认日志文件前缀
        /// </summary>
        public const string LOG_DEFAULT_ = "_default_";
        /// <summary>
        /// 调试日志前缀
        /// </summary>
        public const string LOG_DEBUG_ = "_debug_";
        /// <summary>
        /// 数据库日志前缀
        /// </summary>
        public const string LOG_DB_ = "_db_";

        /// <summary>
        /// 使用一个全局句柄，便于在任何地方都可以访问日志系统。
        /// 需要在应用初始化时，创建一个全局对象
        /// </summary>
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        private static cls_log __handle;
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        public static cls_log handle_()
        {
            return __handle;
        }

        /// <summary>
        /// 日志表，通过日志名称访问不同的日志
        /// </summary>
        private Dictionary<string, cls_log_obj> _map_log = new();

        /// <summary>
        /// 日志构造函数，在全局初始化一次即可。
        /// 添加3个日志对象，默认、调试和数据库
        /// </summary>
        public cls_log()
        {
            if (__handle != null)
            {
                throw new Exception("cls_log 重复创建");
            }

            __handle = this;
            add_log_(LOG_DEFAULT_, LOG_DEFAULT_, em_log_type.All, 0);
            add_log_(LOG_DEBUG_, LOG_DEBUG_, em_log_type.All, 0);
            add_log_(LOG_DB_, LOG_DB_, em_log_type.All, 0);
        }

        /// <summary>
        /// 添加一个日志
        /// </summary>
        /// <param name="name">日志名称，根据此进行访问</param>
        /// <param name="prefix">日志文件前缀，用于标识</param>
        /// <param name="logType">日志文件类型</param>
        /// <param name="dateCount">保留天数（暂不支持）</param>
        /// <returns></returns>
        public static cls_log_obj add_log_(string name, string prefix, em_log_type logType, int dateCount)
        {
            cls_log_obj logObj = new cls_log_obj(name, prefix, logType, dateCount);
            __handle._map_log.Add(name, logObj);

            return logObj;
        }
        /// <summary>
        /// 访问默认日志
        /// </summary>
        /// <returns></returns>
        public static cls_log_obj get_default_()
        {
            return __handle._map_log[LOG_DEFAULT_];
        }
        /// <summary>
        /// 访问调试日志
        /// </summary>
        /// <returns></returns>
        public static cls_log_obj get_debug_()
        {
            return __handle._map_log[LOG_DEBUG_];
        }
        /// <summary>
        /// 访问数据库日志
        /// </summary>
        /// <returns></returns>
        public static cls_log_obj get_db_()
        {
            return __handle._map_log[LOG_DB_];
        }
        /// <summary>
        /// 访问指定的日志
        /// </summary>
        /// <param name="name">日志名称</param>
        /// <returns></returns>
        public static cls_log_obj get_log_(string name)
        {
            return __handle._map_log[name];
        }
    }
}
