using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.eobject.iot.Server.Log
{
    /// <summary>
    /// 日志文件处理核心
    /// </summary>
    public class cls_log_file
    {
        /// <summary>
        /// 日志写入锁
        /// </summary>
        private readonly object lock_flag_ = new();
        /// <summary>
        /// 日志文件路径
        /// </summary>
        protected string file_path_;

        /// <summary>
        /// 日志文件路径初始化构造
        /// </summary>
        /// <param name="filePath">日志文件路径</param>
        public cls_log_file(string filePath)
        {
            file_path_ = filePath;
        }
        /// <summary>
        /// 获取日志文件路径
        /// </summary>
        /// <returns></returns>
        public string get_file_path_()
        {
            return file_path_;
        }

        /// <summary>
        /// 写入日志到文件中
        /// 
        /// 使用lock保证线程安全。
        /// 使用using保证。
        /// </summary>
        /// <param name="logString"></param>
        public void write_log_file_(string logString)
        {
            try
            {
                lock (lock_flag_)
                {
                    using StreamWriter sw = new(file_path_, true, Encoding.UTF8);
                    sw.WriteLine(logString);
                    sw.Flush();
                    sw.Close();
                    sw.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
#if DEBUG
                Debug.WriteLine(ex.ToString());
#endif
            }
        }
    }
}
