using cn.eobject.iot.Server.Log;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace cn.eobject.iot.Server.DB
{
    /// <summary>
    /// 脚本拼接语句
    /// </summary>
    public class cls_eotsql_obj_ext
    {
        public static string[] SQL_KEYWORDS =
{
            "<", ">", "=", "(", ")", ";", "!",
            "or", "and",
            "update", "insert", "delete", "create", "drop", "alter", "truncate"
        };

        public const int IF_NONE = 0;
        /// <summary>
        /// =0
        /// </summary>
        public const int IF_E = 11;
        /// <summary>
        /// >0
        /// </summary>
        public const int IF_G = 12;
        /// <summary>
        /// >=0
        /// </summary>
        public const int IF_EG = 13;
        /// <summary>
        /// <0
        /// </summary>
        public const int IF_L = 14;
        /// <summary>
        /// <=0
        /// </summary>
        public const int IF_EL = 15;
        /// <summary>
        /// <>0
        /// </summary>
        public const int IF_NE = 21;
        /// <summary>
        /// for循环
        /// </summary>
        public const int IF_FOR = 90;


        public const int IF_TYPE_NONE = 0;
        public const int IF_TYPE_NUMBER = 1;
        public const int IF_TYPE_STRING = 2;


        public List<string> _ext_params = new();
        public int _ext_flag = IF_NONE;
        /// <summary>
        /// 分开处理，主要是为了效率，放弃更为优雅的扩展方式
        /// </summary>
        public int _ext_type = IF_TYPE_NONE;
        public string _ext_string = "";
        public decimal _ext_number = 0;

        public StringBuilder _string_sql = new();

        private string _string_params = "";


        /// <summary>
        /// 避免拼接SQL注入，根据实际情况扩展
        /// </summary>
        /// <param name="valString"></param>
        /// <returns></returns>
        public static string check_sql_value_(string valString)
        {
            foreach (var key in SQL_KEYWORDS)
            {
                //if (valString.Contains(key)) return key;
                // 加入干扰代码
                valString = valString.Replace(key, "&#" + key + "&#");
            }

            return "";
        }

        public static string get_sql_value_(string valString)
        {
            return valString.Replace("&#", "");
        }

        /// <summary>
        /// 替换参数
        /// </summary>
        public static string update_value_(StringBuilder sb, Dictionary<string, object> args)
        {
            string sInfo = "";
            // 替换值
            string? sVal;
            foreach (var arg in args)
            {
                sVal = arg.Value.ToString();
                sVal ??= "";

                if (check_sql_value_(sVal) != "")
                {
                    sInfo = "无法输入 " + arg.Key + " -> " + sVal;
                    cls_log.get_db_().T_("", sInfo);
                }
                else
                {
                    sb.Replace("#" + arg.Key, sVal);
                }
            }

            return sInfo;
        }

        public static int get_if_flag(string s)
        {
            return (s switch
            {
                "=" => IF_E,
                ">" => IF_G,
                ">=" => IF_EG,
                "<" => IF_L,
                "<=" => IF_EL,
                "<>" => IF_NE,
                "for" => IF_FOR,
                _ => IF_NONE,
            });
        }



        /// <summary>
        /// 追加一行数据，空格分开，避免粘连
        /// </summary>
        /// <param name="line"></param>
        public void push_(string line)
        {
            _string_sql.Append(' ').Append(line);
        }

        /// <summary>
        /// -- inc #v_data_field_id
        /// </summary>
        /// <param name="name"></param>
        /// <param name="log"></param>
        /// <param name="args"></param>
        public void var_params_(string name, string log, string[] args, int pos, int len)
        {            
            if (len <= 0)
            {
                len = args.Length;
            }
            else
            {
                len += pos;
                if (len > args.Length) len = args.Length;
            }

            string sVal;
            for (int i = pos; i < len; i++)
            {
                sVal = args[i].Trim();
                if (sVal[0] != '#')
                {
                    cls_log.get_db_().T_("", "语句错误 <{0}> {1}", name, log);
                    return;
                }
                sVal = sVal[1..];

                _ext_params.Add(sVal);
                _string_params += sVal;
            }

            _string_sql.Clear();
        }

        /// <summary>
        /// 参数 -- iff > 0 #var1 #var2 ... #varN
        /// </summary>
        /// <param name="name"></param>
        /// <param name="log"></param>
        /// <param name="args"></param>
        public void ext_params_(string name, string log, string[] args)
        {
            if (_ext_flag != IF_NONE)
            {
                cls_log.get_db_().T_("", "语句错误 <{0}> {1}", name, log);
                return;
            }

            _ext_flag = get_if_flag(args[2].Trim());
            if (_ext_flag == IF_NONE)
            {
                cls_log.get_db_().T_("", $"语句错误 <{name}> {log}");
                return;
            }

            // 判断是比较数字还是比较字符串
            _ext_type = IF_TYPE_NONE;
            string sVal = args[3].Trim();
            if (sVal.Length >= 2)
            {
                if (sVal[0] == '\'' && sVal[^1] == '\'')
                {
                    _ext_type = IF_TYPE_STRING;
                    _ext_string = sVal[1..^1];
                }
            }            
            if (_ext_type == IF_TYPE_NONE)
            {
                if (decimal.TryParse(sVal, out decimal d))
                {
                    _ext_type = IF_TYPE_NUMBER;
                    _ext_number = d;
                }
            }

            if (_ext_type == IF_TYPE_NONE)
            {
                cls_log.get_db_().T_("", "语句错误 <{0}> {1}", name, log);
                return;
            }

            var_params_(name, log, args, 4, -1);
        }
        
        public void set_ext_string_(StringBuilder sb)
        {
            sb.Append(' ').Append('$').Append(_string_params).Append('$');
        }

        public bool check_if_number_(Dictionary<string, object> args)
        {
            foreach (var p in _ext_params)
            {
                if (!args.ContainsKey(p))
                {
                    cls_log.get_db_().T_("", "执行错误 {0}", p);
                    return false;
                }

                object tValue = args[p];

                decimal d = Convert.ToDecimal(tValue);
                bool bReplace = _ext_flag switch
                {
                    IF_E => (d == _ext_number),
                    IF_G => (d > _ext_number),
                    IF_EG => (d >= _ext_number),
                    IF_L => (d < _ext_number),
                    IF_EL => (d <= _ext_number),
                    IF_NE => (d != _ext_number),
                    _ => false
                };

                if (bReplace) return true;
            }

            return false;
        }
        public bool check_if_string_(Dictionary<string, object> args)
        {
            foreach (var p in _ext_params)
            {
                if (!args.ContainsKey(p))
                {
                    cls_log.get_db_().T_("", "执行错误 {0}", p);
                    return false;
                }

                object tValue = args[p];

                string? s = tValue.ToString();
                bool bReplace = _ext_string.Equals(s);
                if (_ext_flag == IF_NE) bReplace = !bReplace;

                if (bReplace) return true;
            }

            return false;
        }
        /// <summary>
        /// 判断参数是否成立
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool check_if_params_(Dictionary<string, object> args)
        {
            if (_ext_type == IF_TYPE_NUMBER)
            {
                return check_if_number_(args);
            }
            else if (_ext_type == IF_TYPE_STRING)
            {
                return check_if_string_(args);
            }
            else
            {
                cls_log.get_db_().T_("", "执行错误");
                return false;
            }
        }

        public void check_for_params_(StringBuilder sb, Dictionary<string, object> args)
        {
            if (_ext_params.Count == 0)
            {
                cls_log.get_db_().T_("", "执行错误 {0}", sb);
                return;
            }
            string sParam = _ext_params[0];
            if (!args.ContainsKey(sParam))
            {
                cls_log.get_db_().T_("", $"无参数 {0}", sb);
                return;
            }

            StringBuilder sbFor = new();

            List<Dictionary<string, object>> list = (List<Dictionary<string, object>>)args[sParam];
            StringBuilder sbLine = new();
            foreach (var p in list)
            {
                sbLine.Clear().Append(_string_sql.ToString());

                // 替换值
                update_value_(sbLine, p);

                sbFor.Append(sbLine).Append(_ext_string);
            }
            // 去掉末尾的分割
            sbFor.Remove(sbFor.Length - _ext_string.Length, _ext_string.Length);

            sb.Replace('$' + _string_params + '$', sbFor.ToString());
        }

        public void update_ext_(StringBuilder sb, Dictionary<string, object> args)
        {
            if (_ext_flag == IF_FOR)
            {
                check_for_params_(sb, args);
            }
            else
            {
                bool bReplace = check_if_params_(args);
                if (bReplace)
                {
                    sb.Replace('$' + _string_params + '$', _string_sql.ToString());
                }
                else
                {
                    sb.Replace('$' + _string_params + '$', "");
                }
            }
        }
    }
}
