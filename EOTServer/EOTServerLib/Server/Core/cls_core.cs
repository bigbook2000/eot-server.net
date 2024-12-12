using cn.eobject.iot.Server.Log;
using System.Reflection;
using System.Text;

namespace cn.eobject.iot.Server.Core
{
    /// <summary>
    /// 
    /// dotnet tool install -g docfx
    /// docfx init -y
    /// 
    /// 全局性的通用模块
    /// 主要是简化代码，提供一些常用的例如日期、类型转换方法，核心的变量、全局路径等
    /// 
    /// 其中类型转换非常重要，虽然一般的语言都带有类型转换方法，但往往并不处理异常，
    /// 为了应对网络中各种可能出现的情况并让逻辑不出错误的继续下去，我们需要做一些特有的处理，使得无论什么状况都返回正确合理的值。
    /// </summary>
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
        /// 记录1970年毫秒数。
        /// 日期转换成数字的好处是可以快速运算，提高效率。
        /// 这是老程序员习惯，可能对于现代语言来说日期函数非常丰富且运算速度非常快，有时候代表的是一种方式。
        /// 转换到1970年unix时间戳，进一步降低数字大小提高时间精度。
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

        /// <summary>
        /// 构造函数
        /// 通过应用程序集可以获取一些有关的环境参数，便于在程序中使用
        /// </summary>
        /// <param name="assemblyType">应用程序集</param>
        /// <exception cref="Exception"></exception>
        public cls_core(Type assemblyType) 
        {
            if (__handle != null)
            {
                throw new Exception("cls_core 重复创建");
            }

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

        /// <summary>
        /// 转换到string
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string o2str_(object? obj)
        {
            string? str = obj?.ToString();
            if (str == null) return "";
            return str;
        }
        /// <summary>
        /// 转换到int
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int o2int_(object? obj)
        {
            string? str = obj?.ToString();
            if (int.TryParse(str, out int val)) return val;

            return 0;
        }
        /// <summary>
        /// 转换到double
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static double o2double_(object? obj)
        {
            string? str = obj?.ToString();
            if (double.TryParse(str, out double val)) return val;

            return 0.0;
        }
        /// <summary>
        /// 转换到date
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static DateTime o2date_(object? obj)
        {
            string? str = obj?.ToString();
            if (DateTime.TryParse(str, out DateTime val)) return val;

            return new(1970, 1, 1, 0, 0, 0, 0);
        }
        /// <summary>
        /// 将字节流转换成可视字符串，由于物联网单片机缺少字库，使用代价最小的GB2312字符集。
        /// 我们需要进行特别的转换
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string bytes2str_(byte[] bytes, int index, int length)
        {
            // BitConverter.ToString()
            // 需安装引用 System.Text.Encodings.CodePages
            return Encoding.GetEncoding("GB2312").GetString(bytes, index, length);
        }
        /// <summary>
        /// 字符串转换成字节流
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] str2bytes_(string str)
        {
            // BitConverter.ToString()
            // 需安装引用 System.Text.Encodings.CodePages
            return Encoding.GetEncoding("GB2312").GetBytes(str);
        }
        /// <summary>
        /// 格式化字节流，和log_bytes_相比中间无空格，主要用于一些特殊的字符串转换
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 用于日志显示字节流
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <returns></returns>
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
 
        /// <summary>
        /// 格式化网络地址，主要用于日志显示
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="port"></param>
        /// <returns></returns>
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
