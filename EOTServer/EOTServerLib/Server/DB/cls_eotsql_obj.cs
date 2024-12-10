using cn.eobject.iot.Server.Log;
using System.Text;

namespace cn.eobject.iot.Server.DB
{
    /// <summary>
    /// 
    /// SQL拼接脚本
    /// 变量以 #var 格式进行字符串替换
    /// 支持 if 拼接
    /// 
    /// </summary>
    public class cls_eotsql_obj : cls_eotsql_obj_ext
    {
        public const int SQL_TYPE_NONE = 0;
        public const int SQL_TYPE_SELECT = 1;
        public const int SQL_TYPE_UPDATE = 2;
        public const int SQL_TYPE_INSERT = 3;
        public const int SQL_TYPE_DELETE = 4;

        public const int SQL_TYPE_VAR = 11;
        public const int SQL_TYPE_INC = 12;
        public const int SQL_TYPE_IFF = 21;



        public string _name = "";

        public int _type = SQL_TYPE_NONE;

        protected cls_eotsql_obj_ext _sql_ext = new();
        protected List<cls_eotsql_obj_ext> _list_ext = new();

        public cls_eotsql_obj? _parent = null;
        public List<cls_eotsql_obj> _childs = new();



        /// <summary>
        /// 每条单独的SQL语句
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

        public void add_end_()
        {
            // 条件拼接                
            _list_ext.Add(_sql_ext);

            // 进行标识
            _sql_ext.set_ext_string_(_string_sql);

            // 重置
            _sql_ext = new();
        }

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
        /// SQL语句行
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
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
        /// 自增语句
        /// </summary>
        /// <param name="name"></param>
        /// <param name="log"></param>
        /// <param name="args"></param>
        /// <returns></returns>
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
        /// 通过SELECT语句返回变量
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
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
        /// <returns></returns>
        public cls_eotsql_obj push_end_(string name, string log)
        {
            // 拼接语句
            if (_sql_ext._ext_flag != IF_NONE)
            {
                add_end_();
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
        /// 计数，主要用于分页
        /// 仅支持单一 SELECT 语句
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string? get_count_(string sql)
        {
            sql = sql.Trim();
            if (!sql.StartsWith("SELECT", StringComparison.CurrentCultureIgnoreCase))
            {
                cls_log.get_db_().T_("", "分页仅支持单一语句\r\n{0}", sql);
                return null;
            }

            int p2 = sql.IndexOf("FROM", StringComparison.CurrentCultureIgnoreCase);
            if (p2 < 0)
            {
                cls_log.get_db_().T_("", "分页语句错误\r\n{0}", sql);
                return null;
            }

            sql = "SELECT count(*) " + sql[p2..];

            return sql;
        }


        /// <summary>
        /// 将语句中的变量替换成值
        /// </summary>
        /// <param name="args"></param>
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
