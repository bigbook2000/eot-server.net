namespace WAIotServer.Common
{
    public class eow_session_item
    {
        /// <summary>
        /// 会话编号 32字节字符串
        /// </summary>
        public string _session_id = "";

        /// <summary>
        /// 用户编号
        /// </summary>
        public int _user_id = 0;

        /// <summary>
        /// 部门编号
        /// </summary>
        public int _dept_id = 0;

        /// <summary>
        /// 用户挂载数据
        /// </summary>
        public object? _user_data;

        /// <summary>
        /// 生成时间，100纳秒
        /// </summary>
        public long _tick_create = 0L;

        /// <summary>
        /// 更新时间，100纳秒
        /// </summary>
        public long _tick_update = 0L;

        /// <summary>
        /// 持续时长，100纳秒
        /// </summary>
        public long _delay = 0L;

        /// <summary>
        /// 检测是否超时
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        public bool check_timeout_(long tick)
        {
            return ((tick - _tick_update) > _delay);
        }
    }
}
