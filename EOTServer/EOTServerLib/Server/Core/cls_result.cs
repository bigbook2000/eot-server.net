using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace cn.eobject.iot.Server.Core
{
    /// <summary>
    /// 返回结果集
    /// </summary>
    public class cls_result
    {
        /// <summary>
        /// 成功
        /// </summary>
        public const int RESULT_SUCCESS = 0;
        /// <summary>
        /// 一般错误
        /// </summary>
        public const int RESULT_ERROR = -1;
        /// <summary>
        /// 未执行
        /// </summary>
        public const int RESULT_UNKNOWN = -10;
        /// <summary>
        /// 会话超时
        /// </summary>
        public const int RESULT_SESSION = -50;        
        /// <summary>
        /// 异常
        /// </summary>
        public const int RESULT_EXCEPT = -100;
        /// <summary>
        /// 超时
        /// </summary>
        public const int RESULT_TIMEOUT = -110;
        /// <summary>
        /// 无权限
        /// </summary>
        public const int RESULT_PERMIT = -120;
        /// <summary>
        /// 本地
        /// </summary>
        public const int RESULT_LOCAL = -1000;

        public const string KEY_SCALAR = "_SCALAR";

        /// <summary>
        /// 状态码
        /// </summary>
        public int _d { get; set; } = RESULT_SUCCESS;
        /// <summary>
        /// 描述
        /// </summary>
        public string _s { get; set; } = "";
        /// <summary>
        /// 令牌token
        /// </summary>
        public string _k { get; set; } = "";
        /// <summary>
        /// 时间戳
        /// </summary>
        public long _t { get; set; } = 0L;

        public List<cls_result_obj> _list { get; set; } = new();

        public cls_result()
        {
            //Environment.TickCount
            _t = cls_core.handle_().now_1970ms_();
        }

        public void set_token_(string token)
        {
            _k = token;
        }

        /// <summary>
        /// 默认第一项，且避免null
        /// </summary>
        /// <returns></returns>
        public cls_result_obj default_()
        {
            if (_list.Count <= 0) return new();
            return _list[0];
        }

        public int count_()
        {
            return _list.Count;
        }

        public bool is_success_()
        {
            return (_d == RESULT_SUCCESS);
        }

        public void set_(int d, string s)
        {
            _d = d;
            _s = s;
        }

        public void set_success_()
        {
            _d = RESULT_SUCCESS;
            _s = "";
        }

        public void set_error_(string errMsg)
        {
            _d = RESULT_ERROR;
            _s = errMsg;
        }

        public void set_except_(Exception ex)
        {
            _d = RESULT_EXCEPT;
            _s = ex.Message;
        }

        /// <summary>
        /// 挂载单值
        /// </summary>
        /// <param name="data"></param>
        public void set_scalar(object data)
        {
            _list.Clear();
            _list.Add(new()
            {
                { KEY_SCALAR, data }
            });
        }

        /// <summary>
        /// 获取单值
        /// </summary>
        /// <param name="data"></param>
        public object? get_scalar()
        {
            if (!_list.Any()) return null;

            cls_result_obj obj = _list[0];
            return obj.First().Value;
        }

        public void add_(cls_result_obj obj)
        {
            _list.Add(obj);
        }

        public void reset_()
        {
            _d = RESULT_SUCCESS;
            _s = "";

            _list.Clear();
        }
    }
}
