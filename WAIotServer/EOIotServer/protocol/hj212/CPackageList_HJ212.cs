using EOIotServer.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOIotServer.protocol.hj212
{
    public class CPackageList_HJ212 : CConcurrentList<CPackage_HJ212>
    {
        public string CN = "";

        public long Delay = 0L;

        /// <summary>
        /// 用于延时，批量处理提高效率
        /// </summary>
        protected long LastTick = 0L;

        public CPackageList_HJ212(string cn)
        {
            CN = cn;
        }

        public CPackageList_HJ212(string cn, long delay)
        {
            CN = cn;
            Delay = delay;
        }

        public bool CheckDelay(long tick, long delay)
        {
            if (delay <= 0L) return true;

            if ((tick - LastTick) > delay)
            {
                LastTick = tick;
                return true;
            }

            return false;
        }

        public bool CheckDelay(long tick)
        {
            if (Delay <= 0L) return true;

            if ((tick - LastTick) > Delay)
            {
                LastTick = tick;
                return true;
            }

            return false;
        }
    }
}
