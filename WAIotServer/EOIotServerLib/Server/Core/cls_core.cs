using cn.eobject.iot.Server.Log;
using System.Reflection;
using System.Text;

namespace cn.eobject.iot.Server.Core
{
    public sealed class cls_core
    {
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        private static cls_core __handle;
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        public static cls_core handle_()
        {
            return __handle;
        }

        /// <summary>
        /// 1970基准时间
        /// </summary>
        public static DateTime _date1970 = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        private static string _base_path = "";
        /// <summary>
        /// 反斜杠路径 / ，末尾不带反斜杠
        /// </summary>
        /// <returns></returns>
        public static string base_path_()
        {
            return _base_path;
        }
        public static string base_path_(string path)
        {
            path = path.Replace("\\", "/");
            if (path[0] != '/') path = "/" + path;

            return _base_path + path;
        }

        /// <summary>
        /// 记录1970年毫秒数
        /// </summary>
        private long _tick_1970ms = 0L;
        public long tick_1970ms_()
        {
            return _tick_1970ms;
        }
        public long tick_1970ms_(long ms)
        {
            return ms - _tick_1970ms;
        }
        public long tick_1970ms_(DateTime dt)
        {
            return dt.Ticks / 10000 - _tick_1970ms;
        }
        public long now_1970ms_()
        {
            return DateTime.Now.Ticks / 10000 - _tick_1970ms;
        }
        public DateTime date_1970ms_(long ms)
        {
            return new DateTime((ms + _tick_1970ms) * 10000);
        }

        public cls_core(Type assemblyType) 
        {
            __handle = this;

            //Console.WriteLine("Environment: " + builder_.Environment.ContentRootPath);

            Assembly assembly = assemblyType.GetTypeInfo().Assembly;
            Console.WriteLine("Assembly: " + assembly.Location);
            
            _base_path = AppContext.BaseDirectory.Replace("\\", "/");

            // 去掉尾部的反斜杠
            if (_base_path.EndsWith('/')) 
            {
                _base_path = _base_path.Substring(0, _base_path.Length - 1);
            }

            Console.WriteLine("AppContext: " + _base_path);

            // 注册GB2312字符集
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
            _tick_1970ms = dt.Ticks / 10000L;

            // 创建日志实例
            _ = new cls_log();
        }

        public static string o2str_(object? obj)
        {
            string? str = obj?.ToString();
            if (str == null) return "";
            return str;
        }
        public static int o2int_(object? obj)
        {
            string? str = obj?.ToString();
            if (int.TryParse(str, out int val)) return val;

            return 0;
        }
        public static double o2double_(object? obj)
        {
            string? str = obj?.ToString();
            if (double.TryParse(str, out double val)) return val;

            return 0.0;
        }
        public static DateTime o2date_(object? obj)
        {
            string? str = obj?.ToString();
            if (DateTime.TryParse(str, out DateTime val)) return val;

            return new(1970, 1, 1, 0, 0, 0, 0);
        }

        public static string bytes2str_(byte[] bytes, int index, int length)
        {
            // BitConverter.ToString()
            // 需安装引用 System.Text.Encodings.CodePages
            return Encoding.GetEncoding("GB2312").GetString(bytes, index, length);
        }

        public static string log_bytes_(byte[] bytes, int index, int length)
        {
            // BitConverter.ToString()
            StringBuilder sb = new StringBuilder();
            int i;
            for (i = index; i < length; i++)
            {
                sb.Append(bytes[i].ToString("X2")).Append(' ');
            }
            return sb.ToString();
        }

        public static string format_bytes_(byte[] bytes, int index, int length)
        {
            // BitConverter.ToString()
            StringBuilder sb = new StringBuilder();
            int i;
            for (i = index; i < length; i++)
            {
                sb.Append(bytes[i].ToString("X2"));
            }
            return sb.ToString();
        }        

        public static string format_ip_(byte[] bytes, int port)
        {
            string s1 = bytes[0].ToString().PadLeft(3, '_');
            string s2 = bytes[1].ToString().PadLeft(3, '_');
            string s3 = bytes[2].ToString().PadLeft(3, '_');
            string s4 = bytes[3].ToString().PadLeft(3, '_');

            string s5 = port.ToString().PadLeft(5, '_');

            return s1 + "." + s2 + "." + s3 + "." + s4 + ":" + s5;
        }
    }
}
