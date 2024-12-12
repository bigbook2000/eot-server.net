using cn.eobject.iot.Server.Log;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace cn.eobject.iot.Server.DB
{
    /// <summary>
    /// SQL脚本文件
    /// 可以使用use指定数据源，暂时不支持，后续扩展
    ///  -- use mysql eotgate
    /// </summary>
    public class cls_eotsql_file
    {
        /// <summary>
        /// 脚本名称
        /// </summary>
        public string _script_name = "";
        /// <summary>
        /// 数据库类型
        /// </summary>
        public string _db_type = "";
        /// <summary>
        /// 数据源名称
        /// </summary>
        public string _db_name = "";

        /// <summary>
        /// 执行脚本根节点
        /// </summary>
        public cls_eotsql_obj _root_sql = new();
        /// <summary>
        /// 用于解析脚本缓存变量
        /// </summary>
        private cls_eotsql_obj? _last_sql = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        public cls_eotsql_file()
        {
        }
        /// <summary>
        /// 使用脚本名构造
        /// </summary>
        /// <param name="name">脚本名</param>
        public cls_eotsql_file(string name)
        {
            _script_name = name;
            _last_sql = _root_sql;
        }

        /// <summary>
        /// 解析单行字符串
        /// </summary>
        /// <param name="line">字符串行</param>
        /// <returns>成功返回true，否则false</returns>
        public bool parse_line_(string line)
        {
            // 不区分大小写，全部小写化
            line = line.Trim().ToLower();

            // 空行
            if (line.Length == 0) return true;

            if (line[0] != '-' || line[1] != '-')
            {
                _last_sql?.push_line_(line);
                return true;
            }

            // 注释，4个-
            if (line[2] == '-' && line[3] == '-') return true;

            string[] ss = line.Split(' ');
            if (ss.Length < 2)
            {
                cls_log.get_db_().T_("", $"语句错误 <{_script_name}> {line}");
                return false;
            }

            string sqlFlag = ss[1].Trim();
            switch (sqlFlag)
            {
                case "use": // 指定数据库
                    {
                        if (ss.Length >= 4)
                        {
                            _db_type = ss[2].Trim();
                            _db_name = ss[3].Trim();
                        }
                    }
                    break;
                case "add": // 拼接语句条件
                    {
                        _last_sql = _last_sql?.push_ext_(_script_name, line, ss);
                    }
                    break;
                case "set": // SQL语句
                    {
                        _last_sql = _last_sql?.push_set_(_script_name, line, ss);
                    }
                    break;
                case "inc": // 自增语句
                    {
                        _last_sql = _last_sql?.push_inc_(_script_name, line, ss);
                    }
                    break;
                case "var": // 返回变量
                    {
                        _last_sql = _last_sql?.push_var_(_script_name, line, ss);
                    }
                    break;
                case "iff": // 条件复合语句
                    {
                        if (ss.Length < 4)
                        {
                            cls_log.get_db_().T_("", $"语句错误 <{_script_name}> {line}");
                            return false;
                        }
                        _last_sql = _last_sql?.push_iff_(_script_name, line, ss);
                    }
                    break;                
                case "end": // 语句结束
                    {
                        _last_sql = _last_sql?.push_end_(_script_name, line);
                    }
                    break;
                default:
                    {
                        cls_log.get_db_().T_("", $"语句错误 <{_script_name}> {line}");
                        return false;
                    }
            }

            return true;
        }
    }
}
