using cn.eobject.iot.Server.Log;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace cn.eobject.iot.Server.DB
{
    /// <summary>
    /// 
    /// SQL脚本对象
    /// 
    /// 识别标记以 -- 双中划线开始，空格分割，注释以 ---- 开始
    /// 参数以 # 开头，系统参数以 ## 开头，例如 ##now
    /// 
    /// 所有语句均 end 结束
    /// 
    /// 普通语句以 set 开始
    ///     -- set
    ///     update n_data_field set _update_flag = -1 where f_data_field_id =#v_data_field_id;
    ///     --end
    ///     
    /// 变量以 var 格式进行字符串替换
    /// 	-- var
    /// 	select f_user_id as t_user_id from eox_user where f_user_id=#v_user_id and f_login_psw=#v_login_psw_old;
    /// 	-- end
    /// 
    /// 拼接语句以 add 开始，支持逻辑运算和for循环
    ///     -- add &lt;&gt; '' #v_dkey
    ///     -- add for ',' #v_pack
    /// 
    /// 支持 iff 复合语句
    ///     -- iff &gt; 0 #v_user_id
    /// 
    /// </summary>
    public class cls_eotsql_obj : cls_eotsql_obj_ext
    {
        /// <summary>
        /// 未知脚本
        /// </summary>
        public const int SQL_TYPE_NONE = 0;
        /// <summary>
        /// select 语句
        /// </summary>
        public const int SQL_TYPE_SELECT = 1;
        /// <summary>
        /// update 语句
        /// </summary>
        public const int SQL_TYPE_UPDATE = 2;
        /// <summary>
        /// insert 语句，不返回自增编号
        /// </summary>
        public const int SQL_TYPE_INSERT = 3;
        /// <summary>
        /// delete 语句
        /// </summary>
        public const int SQL_TYPE_DELETE = 4;
        /// <summary>
        /// var 语句
        /// </summary>
        public const int SQL_TYPE_VAR = 11;
        /// <summary>
        /// 带自增id的 insert 语句
        /// </summary>
        public const int SQL_TYPE_INC = 12;
        /// <summary>
        /// 复合语句
        /// </summary>
        public const int SQL_TYPE_IFF = 21;


        /// <summary>
        /// 数据库脚本名称，就是文件名
        /// </summary>
        public string _name = "";
        /// <summary>
        /// 数据库脚本对象SQL语句类型
        /// </summary>
        public int _type = SQL_TYPE_NONE;

        /// <summary>
        /// 拼接语句缓存
        /// </summary>
        protected cls_eotsql_obj_ext _sql_ext = new();
        /// <summary>
        /// 拼接语句列表
        /// </summary>
        protected List<cls_eotsql_obj_ext> _list_ext = new();
        /// <summary>
        /// 父节点
        /// </summary>
        public cls_eotsql_obj? _parent = null;
        /// <summary>
        /// 子节点列表
        /// </summary>
        public List<cls_eotsql_obj> _childs = new();


        /// <summary>
        /// 判断SQL语句类型
        /// </summary>
        /// <param name="str"></param>
        public static int check_sql_type_(string str)
        {
            str = str.Trim();
            if (str.Length < 6) return SQL_TYPE_NONE;

            string sType = str[..6];
            int nType = sType switch
            {
                "select" => SQL_TYPE_SELECT,
                "update" => SQL_TYPE_UPDATE,
                "insert" => SQL_TYPE_INSERT,
                "delete" => SQL_TYPE_DELETE,
                _ => SQL_TYPE_NONE
            };

            return nType;
        }
        /// <summary>
        /// 处理拼接语句 end 结束标识
        /// </summary>
        private void set_ext_end_()
        {
            // 条件拼接                
            _list_ext.Add(_sql_ext);

            // 进行标识
            _sql_ext.set_ext_string_(_string_sql);

            // 重置
            _sql_ext = new();
        }
        /// <summary>
        /// 解析行
        /// </summary>
        /// <param name="str">行字符串</param>
        public void push_line_(string str)
        {
            // 增加一个空格分割
            if (_sql_ext._ext_flag != IF_NONE)
            {
                _sql_ext.push_(str);
            }
            else
            {
                push_(str);
            }
        }

        /// <summary>
        /// 创建新的SQL语句子脚本对象
        /// </summary>
        /// <param name="name">语句名称</param>
        /// <param name="log">用于日志</param>
        /// <param name="args">脚本执行参数</param>
        /// <returns>返回新创建脚本对象</returns>
        public cls_eotsql_obj push_set_(string name, string log, string[] args)
        {
            cls_eotsql_obj newObj = new()
            {
                _name = name,
                _type = SQL_TYPE_NONE,
                _parent = this
            };

            _childs.Add(newObj);
            return newObj;
        }

        /// <summary>
        /// 创建新的自增insert语句子脚本对象
        /// </summary>
        /// <param name="name">语句名称</param>
        /// <param name="log">用于日志</param>
        /// <param name="args">脚本执行参数</param>
        /// <returns>返回新创建脚本对象</returns>
        public cls_eotsql_obj push_inc_(string name, string log, string[] args)
        {
            cls_eotsql_obj newObj = new()
            {
                _name = name,
                _type = SQL_TYPE_INC,
                _parent = this
            };

            if (args.Length < 3)
            {
                cls_log.get_db_().T_("", "语句错误 <{0}> {1}", name, log);
                return this;
            }

            newObj.var_params_(name, log, args, 2, -1);

            _childs.Add(newObj);
            return newObj;
        }

        /// <summary>
        /// 创建新的var参数子脚本对象
        /// </summary>
        /// <param name="name">语句名称</param>
        /// <param name="log">用于日志</param>
        /// <param name="args">脚本执行参数</param>
        /// <returns>返回新创建脚本对象</returns>
        public cls_eotsql_obj push_var_(string name, string log, string[] args)
        {
            cls_eotsql_obj newObj = new()
            {
                _name = name,
                _type = SQL_TYPE_VAR,
                _parent = this
            };

            _childs.Add(newObj);
            return newObj;
        }
        /// <summary>
        /// 添加新的拼接语句
        /// </summary>
        /// <param name="name">语句名称</param>
        /// <param name="log">用于日志</param>
        /// <param name="args">脚本执行参数</param>
        /// <returns>返回本对象</returns>
        public cls_eotsql_obj push_ext_(string name, string log, string[] args)
        {
            if (args.Length < 5)
            {
                cls_log.get_db_().T_("", "语句错误 <{0}> {1}", name, log);
                return this;
            }

            // 处理拼接语句条件
            _sql_ext.ext_params_(name, log, args);

            return this;
        }
        /// <summary>
        /// 创建新的iff复合语句子脚本对象
        /// </summary>
        /// <param name="name">语句名称</param>
        /// <param name="log">用于日志</param>
        /// <param name="args">脚本执行参数</param>
        /// <returns>返回新创建脚本对象</returns>
        public cls_eotsql_obj push_iff_(string name, string log, string[] args)
        {
            cls_eotsql_obj newObj = new()
            {
                _name = name,
                _type = SQL_TYPE_IFF,
                _parent = this
            };

            // 处理条件复合语句参数
            newObj.ext_params_(name, log, args);

            _childs.Add(newObj);
            return newObj;
        }



        /// <summary>
        /// 语句过程结束
        /// 返回上一级
        /// </summary>
        /// <param name="name">语句名称</param>
        /// <param name="log">用于日志</param>
        /// <returns>如果时新创建的语句，返回上一级，如果时拼接语句返回当前语句</returns>
        public cls_eotsql_obj push_end_(string name, string log)
        {
            // 拼接语句
            if (_sql_ext._ext_flag != IF_NONE)
            {
                set_ext_end_();
            }
            else
            {
                // 如果未标记流程，判断SQL语句类型
                if (_type == SQL_TYPE_NONE)
                {
                    _type = check_sql_type_(_string_sql.ToString());
                }
                else if (_type == SQL_TYPE_IFF)
                {
                    if (_childs.Count == 0)
                    {
                        cls_log.get_db_().T_("", "语句错误 <{0}> {1}", name, log);
                    }
                }

                // 返回上一级
                if (_parent != null)
                {
                    return _parent;
                }
            }

            return this;
        }

        /// <summary>
        /// 将语句中的变量替换成值
        /// </summary>
        /// <param name="args">脚本执行参数</param>
        /// <returns></returns>
        public StringBuilder get_sql_(Dictionary<string, object> args)
        {
            StringBuilder sb = new(_string_sql.ToString());

            // 处理拼接
            foreach (var item in _list_ext)
            {
                item.update_ext_(sb, args);
            }

            // 替换值
            update_value_(sb, args);

            // 替换系统变量
            string dts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            sb.Replace("##now", "'" + dts + "'");

            cls_log.get_db_().T_("", "{0}> {1}", _name, sb);

            return sb;
        }
    }
}
