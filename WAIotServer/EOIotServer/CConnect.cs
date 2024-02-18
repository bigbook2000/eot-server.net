using cn.eobject.iot.Server.Log;
using cn.eobject.iot.Server.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOIotServer
{
    public class CConnect : cls_connect
    {
        /// <summary>
        /// 设备编号
        /// </summary>
        public int DeviceId = 0;

        /// <summary>
        /// 标记MN
        /// </summary>
        public string ConnectMN = "";

        public CConnect(cls_server server, string connectKey, int bufferRecvMax, int bufferSendMax) : 
            base(server, connectKey, bufferRecvMax, bufferSendMax)
        {

        }
    }
}
