using cn.eobject.iot.Server.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOIotServer.common
{
    public class CThread
    {
        protected Thread ThreadHandler;
        protected bool IsRuning = false;
        protected ParameterizedThreadStart ThreadCallback;

        /// <summary>
        /// 附加对象
        /// </summary>
        public object? Tag;

        protected int DelayWait;

        public CThread(int delayWait, ParameterizedThreadStart threadCallback, object? tag)
        {
            DelayWait = delayWait;
            if (DelayWait <= 0) DelayWait = 1;

            ThreadCallback = threadCallback;
            Tag = tag;

            ThreadHandler = new Thread(new ThreadStart(OnTreadProc));
            IsRuning = true;
            ThreadHandler.Start();            
        }

        protected void OnTreadProc()
        {
            while (IsRuning)
            {
                try
                {
                    ThreadCallback(this);
                }
                catch (Exception ex)
                {
                    cls_log.get_default_().T_("", ex.ToString());
                }

                Thread.SpinWait(DelayWait);
            }
        }

        public void Stop() 
        {
            IsRuning = false;
        }
    }
}
