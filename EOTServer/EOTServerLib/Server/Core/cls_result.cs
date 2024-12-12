using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace cn.eobject.iot.Server.Core
{
    /// <summary>
    /// 
    /// 结果集
    /// 对于http协议，需要传递一些参数，如果每个接口都定义一个参数对象，代码工作将变得非常大。
    /// 为了减少Coding的量，我们使用键值对表来处理所有的http接口，同时可以用于其他内部方法之间的参数传递。
    /// cls_result定义了一个整型_d和字符串_s，分别用来表示调用的状态码和信息描述。
    /// _t用来管理调用的时间，_k为调用令牌。数据则存储在_list键值对表数组中。
    /// 
    /// 最终在前后端交互中都转化成json字符串，目前大多数Web框架都自动支持该方式，具有很强的扩展性。
    /// 
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
        /// 状态码，0表示成功，否则返回错误代码
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

        /// <summary>
        /// 键值对表数组，参数传递的具体数据
        /// </summary>
        public List<cls_result_obj> _list { get; set; } = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        public cls_result()
        {
            //Environment.TickCount
            _t = cls_core.handle_().now_1970ms_();
        }

        /// <summary>
        /// 设置令牌
        /// </summary>
        /// <param name="token"></param>
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

        /// <summary>
        /// 数组长度
        /// </summary>
        /// <returns></returns>
        public int count_()
        {
            return _list.Count;
        }
        /// <summary>
        /// 返回是否成功
        /// </summary>
        /// <returns></returns>
        public bool is_success_()
        {
            return (_d == RESULT_SUCCESS);
        }
        /// <summary>
        /// 设置返回状态
        /// </summary>
        /// <param name="d"></param>
        /// <param name="s"></param>
        public void set_(int d, string s)
        {
            _d = d;
            _s = s;
        }
        /// <summary>
        /// 标记成功
        /// </summary>
        public void set_success_()
        {
            _d = RESULT_SUCCESS;
            _s = "";
        }
        /// <summary>
        /// 标记错误
        /// </summary>
        /// <param name="errMsg"></param>
        public void set_error_(string errMsg)
        {
            _d = RESULT_ERROR;
            _s = errMsg;
        }
        /// <summary>
        /// 标记异常
        /// </summary>
        /// <param name="ex"></param>
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
        /// <summary>
        /// 追加一个键值对表项数据
        /// </summary>
        /// <param name="obj"></param>
        public void add_(cls_result_obj obj)
        {
            _list.Add(obj);
        }
        /// <summary>
        /// 重置所有状态，清空数据
        /// </summary>
        public void reset_()
        {
            _d = RESULT_SUCCESS;
            _s = "";

            _list.Clear();
        }
    }
}
