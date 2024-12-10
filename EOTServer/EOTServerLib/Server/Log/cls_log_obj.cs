using System;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using cn.eobject.iot.Server.Core;

namespace cn.eobject.iot.Server.Log
{
    public class cls_log_obj
    {
        /// <summary>
        /// 防止文件冲突锁
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

        public cls_log_obj(string name, string prefix, em_log_type logType, int dateCount)
        {
            _name = name;
            _prefix = prefix;
            _type = logType;
            _date_count = dateCount;
        }

        public static string fixed_(string s)
        {
            return s.Replace("{", "{{").Replace("}", "}}");
        }

        /// <summary>
        /// 只能在此对象中调用，指示的堆栈为该方法上两级
        /// </summary>
        /// <param name="timeString"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private string get_log_string_(string timeString, string format, params object[] args)
        {
            try
            {                
                string sMsg = string.Format(format, args);

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

        private string get_log_file_(string objectName, string dateString)
        {
            string sPath = cls_core.base_path_() + "/logs/" + dateString;

            if (!Directory.Exists(sPath))
            {
                Directory.CreateDirectory(sPath);
            }

            if (_type != em_log_type.Object)
                return sPath + "/" + _prefix + ".txt";

            return sPath + "/" + _prefix + objectName + ".txt";
        }

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

                logFile.write_log_file_(filePath, logString);
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
                string nowString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string filePath = get_log_file_(objectName, nowString.Substring(0, 10));
                string logString = get_log_string_(nowString[11..], format, args);

                Console.WriteLine(logString);
#if DEBUG
                Debug.WriteLine(logString);
#endif
                write_log_file_(filePath, logString);
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
                string nowString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string filePath = get_log_file_(objectName, nowString.Substring(0, 10));
                string logString = get_log_string_(nowString.Substring(11), format, args);

                write_log_file_(filePath, logString);
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
                string sLog = get_log_string_(sNow.Substring(11), format, args);

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
