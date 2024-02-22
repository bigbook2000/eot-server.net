using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.eobject.iot.Server.Net
{
    public interface evt_server
    {
        /// <summary>
        /// 返回一个新的连接对象
        /// </summary>
        /// <param name="connectKey"></param>
        /// <returns></returns>
        cls_connect? on_connect(string connectKey);

        /// <summary>
        /// 返回值为解析处理的数据长度
        /// </summary>
        /// <param name="connect"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        int on_recv(cls_connect connect, byte[] bytes);

        void on_close(cls_connect connect);
    }
}
