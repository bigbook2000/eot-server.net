namespace cn.eobject.iot.Server.Log
{
    /// <summary>
    /// 日志均为每天单独的目录
    /// </summary>
    public enum em_log_type
    {
        None = 0,
        /// <summary>
        /// 每天放在一个文件中
        /// </summary>
        All,
        /// <summary>
        /// 按小时创建一个文件
        /// </summary>
        Hour,
        /// <summary>
        /// 按名称创建一个文件
        /// </summary>
        Object
    }
}
