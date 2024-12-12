using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.eobject.iot.Server.Net
{
    /// <summary>
    /// 网络服务事件接口
    /// </summary>
    public interface evt_server
    {
        /// <summary>
        /// 客户端连接事件
        /// 由外部应用返回一个cls_connect连接对象，主要是为了系统架构的优雅性。
        /// 外部应用可以通过派生cls_connect来扩展自己的连接对象，实现逻辑上的方法，避免二次管理。
        /// 如果逻辑上拒绝连接，返回null
        /// </summary>
        /// <param name="connectKey">连接对象标识，IP+端口，固定长度</param>
        /// <returns>返回一个新的连接对象</returns>
        cls_connect? on_connect(string connectKey);
        /// <summary>
        /// 接收数据事件
        /// 返回长度表示当前处理的字节，逻辑接收缓存会将指定长度的字节移除，保留剩下的，避免分包粘包现象。
        /// 比如接收到100个字节的数据，其中80个字节是一个完整的包，剩下的20个字节并不完整。
        /// 返回80，逻辑缓存会移除80个字节，将剩下的20个字节保留等下一次数据到来一起拼接返回。
        /// </summary>
        /// <param name="connect">连接对象</param>
        /// <param name="bytes">接收数据</param>
        /// <returns>返回值为解析处理的数据长度</returns>
        int on_recv(cls_connect connect, byte[] bytes);
        /// <summary>
        /// 处理客户端端口事件
        /// 任何原因的关闭都会调用这个接口，包括手动关闭，因此不要在此接口中再次调用任何关闭客户端的方法
        /// </summary>
        /// <param name="connect"></param>
        void on_close(cls_connect connect);
    }
}
