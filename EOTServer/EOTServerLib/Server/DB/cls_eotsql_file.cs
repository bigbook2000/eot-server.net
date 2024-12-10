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
    public class cls_eotsql_file
    {
        public string _script_name = "";

        public string _db_type = "";
        public string _db_name = "";

        public cls_eotsql_obj _root_sql = new();
        
        private cls_eotsql_obj? _last_sql = null;

        public cls_eotsql_file()
        {
        }
        public cls_eotsql_file(string name)
        {
            _script_name = name;
            _last_sql = _root_sql;
        }


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
