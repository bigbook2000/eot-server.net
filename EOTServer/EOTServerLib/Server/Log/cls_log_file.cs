using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.eobject.iot.Server.Log
{
    public class cls_log_file
    {
        private readonly object lock_flag_ = new();

        protected string file_path_;

        public cls_log_file(string filePath)
        {
            file_path_ = filePath;
        }

        public string get_file_path_()
        {
            return file_path_;
        }

        public void write_log_file_(string filePath, string logString)
        {
            try
            {
                lock (lock_flag_)
                {
                    using StreamWriter sw = new(filePath, true, Encoding.UTF8);
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
