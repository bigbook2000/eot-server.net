using System;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using cn.eobject.iot.Server.Core;

namespace cn.eobject.iot.Server.Log
{
    /// <summary>
    /// 日志处理核心对象
    /// </summary>
    public class cls_log_obj
    {
        /// <summary>
        /// 日志文件表
        /// 该表存储了当前使用的日志文件，以文件名为索引。
        /// 暂时只进不出，后期再增加管理。
        /// 由于是多线程，对该表的访问需要加锁lock
        /// 
        /// </summary>
        protected Dictionary<string, cls_log_file> _dic_log_files = new();

        public string _name = "";
        /// <summary>
        /// 类别
        /// </summary>
        public em_log_type _type;
        /// <summary>
        /// 日志文件名
        /// </summary>
        public string _prefix = "";
        /// <summary>
        /// 保存的天数
        /// </summary>
        public int _date_count = 0;

        /// <summary>
        /// 构造函数，构造一个日志对象
        /// </summary>
        /// <param name="name"></param>
        /// <param name="prefix"></param>
        /// <param name="logType"></param>
        /// <param name="dateCount"></param>
        public cls_log_obj(string name, string prefix, em_log_type logType, int dateCount)
        {
            _name = name;
            _prefix = prefix;
            _type = logType;
            _date_count = dateCount;
        }

        /// <summary>
        /// 日志函数带格式化，如果日志内容中包含{}格式符，则会出现报错。
        /// 可以手动调用此方法替换转义字符，避免日志输出错误。
        /// </summary>
        /// <param name="s">格式化字符串</param>
        /// <returns></returns>
        public static string fixed_(string s)
        {
            return s.Replace("{", "{{").Replace("}", "}}");
        }

        /// <summary>
        /// 格式化对齐日志，目的是使得日志看起来更清晰，对于运维调试非常重要，在一大堆杂乱的信息中快速查找自己需要的内容。
        /// 包含了时间、线程、代码文件、所在行
        /// 只能在此对象中调用，指示的堆栈为该方法上两级。
        /// </summary>
        /// <param name="timeString"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private string get_log_string_(string timeString, string format, params object[] args)
        {
            string sMsg = "";

            // 日志作为核心避免崩溃，即使出错，也要返回
            try
            {
                sMsg = string.Format(format, args);
            }
            catch (Exception ex)
            {
                sMsg = format + " | " + ex.ToString();
            }

            try
            {                
                string sClass = "XXXXXX";
                string sNum = "_____";
                string sThread = Environment.CurrentManagedThreadId.ToString().PadRight(6, '_');

                StackTrace trace = new(true);
                StackFrame[] frames = trace.GetFrames();

                int i = 0;
                foreach (StackFrame f in frames)
                {
                    string? s = f.GetMethod()?.Name;
                    if (s == null) continue;

                    if (i == 0)
                    {
                        if (s == "get_log_string_") i = 1;
                    }
                    else if (i == 2)
                    {
                        s = f.GetFileName();
                        // 去掉.cs
                        if (s != null)
                        {
                            s = "XXXXXX" + s;
                            sClass = s.Substring(s.Length - 9, 6);
                        }

                        sNum = f.GetFileLineNumber().ToString().PadLeft(5, '_');
                        break;
                    }
                    else
                    {
                        ++i;
                    }
                }

                return "[" + timeString + "_" + sThread + sClass + sNum + "] " + sMsg;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
#if DEBUG
                Debug.WriteLine(ex.ToString());
#endif
                return format + " | " + ex.ToString();
            }
        }

        /// <summary>
        /// 获取指定的日志文件。
        /// 根据规则返回日志文件的文件名，并检查路径是否有效
        /// </summary>
        /// <param name="objectName">日志对象名</param>
        /// <param name="dateString">时间字符串</param>
        /// <returns></returns>
        private string get_log_file_(string objectName, string dateString)
        {
            string sPath = cls_core.base_path_() + "/logs/" + dateString[..10];

            if (!Directory.Exists(sPath))
            {
                Directory.CreateDirectory(sPath);
            }

            return _type switch
            {
                em_log_type.Object => sPath + "/" + _prefix + objectName + ".txt",
                em_log_type.Hour => sPath + "/" + _prefix + "_" + dateString[11..13] + ".txt",
                _ => sPath + "/" + _prefix + ".txt",
            };
        }

        /// <summary>
        /// 写日志文件
        /// </summary>
        /// <param name="filePath">日志文件名</param>
        /// <param name="logString">日志内容</param>
        private void write_log_file_(string filePath, string logString)
        {
            cls_log_file logFile;

            try
            {
                // 先取得一个锁
                lock (_dic_log_files)
                {
                    if (_dic_log_files.ContainsKey(filePath))
                    {
                        logFile = _dic_log_files[filePath];
                    }
                    else
                    {
                        logFile = new(filePath);
                        _dic_log_files.Add(filePath, logFile);
                    }
                }

                // 写文件有单独的锁，不需要放在文件表锁中
                // 这样避免双重锁带来的风险，而且不会影响日志输出。
                logFile.write_log_file_(logString);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
#if DEBUG
                Debug.WriteLine(ex.ToString());
#endif
            }
        }

        /// <summary>
        /// 写入日志，同时显示控制台
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void T_(string objectName, string format, params object[] args)
        {
            try
            {
                string sNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string sFile = get_log_file_(objectName, sNow);
                string sLog = get_log_string_(sNow[11..], format, args);

                Console.WriteLine(sLog);
#if DEBUG
                Debug.WriteLine(sLog);
#endif
                write_log_file_(sFile, sLog);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
#if DEBUG
                Debug.WriteLine(ex.ToString());
#endif
            }
        }

        /// <summary>
        /// 只写入日志，不显示控制台
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void F_(string objectName, string format, params object[] args)
        {
            try
            {
                string sNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string sFile = get_log_file_(objectName, sNow);
                string sLog = get_log_string_(sNow[11..], format, args);

                write_log_file_(sFile, sLog);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
#if DEBUG
                Debug.WriteLine(ex.ToString());
#endif
            }
        }


        /// <summary>
        /// 只显示控制台
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void V_(string objectName, string format, params object[] args)
        {
            try
            {
                string sNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string sLog = get_log_string_(sNow[11..], format, args);

                Console.WriteLine(sLog);
#if DEBUG
                Debug.WriteLine(sLog);
#endif
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
