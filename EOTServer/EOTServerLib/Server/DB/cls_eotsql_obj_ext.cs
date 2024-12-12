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
        /// <summary>
        /// IF 逻辑判断运算符
        /// </summary>
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

        /// <summary>
        /// IF 条件值，目前只支持数字和字符串
        /// </summary>
        public const int IF_TYPE_NONE = 0;
        /// <summary>
        /// IF 数字值
        /// </summary>
        public const int IF_TYPE_NUMBER = 1;
        /// <summary>
        /// IF 字符串值
        /// </summary>
        public const int IF_TYPE_STRING = 2;

        /// <summary>
        /// 拼接参数
        /// </summary>
        public List<string> _ext_params = new();
        /// <summary>
        /// IF 运算符
        /// </summary>
        public int _ext_flag = IF_NONE;
        /// <summary>
        /// IF 运算值类型
        /// 分开处理，主要是为了效率，放弃更为优雅的扩展方式
        /// </summary>
        public int _ext_type = IF_TYPE_NONE;
        /// <summary>
        /// 如果是 IF_TYPE_STRING 取该值
        /// </summary>
        public string _ext_string = "";
        /// <summary>
        /// 如果是 IF_TYPE_NUMBER 取该值
        /// </summary>
        public decimal _ext_number = 0;

        /// <summary>
        /// 语句缓存
        /// </summary>
        public StringBuilder _string_sql = new();
        /// <summary>
        /// 拼接名称
        /// </summary>
        private string _ext_name = "";

        /// <summary>
        /// 将语句中的参数标记替换为执行值
        /// </summary>
        /// <param name="sb">sql 语句字符串</param>
        /// <param name="args">语句执行参数值</param>
        public static void update_value_(StringBuilder sb, Dictionary<string, object> args)
        {
            // 替换值
            string sKey;
            string? sVal;
            foreach (var arg in args)
            {
                sKey = arg.Key;

                sVal = arg.Value.ToString();
                sVal ??= "";

                // 处理特殊值
                if (sKey[0] == '#' && sKey.Length > 8)
                {
                    switch (sKey[..8])
                    {
                        case "##lstr##":
                            sVal = "'" + sVal.Replace(",", "','") + "'";
                            break;
                        default:
                            break;
                    }

                    sKey = sKey[8..];
                }

                sb.Replace("#" + sKey, sVal);
            }
        }
        /// <summary>
        /// 将字符串标识转换为数字标识，便于快速比对
        /// </summary>
        /// <param name="s">字符串标识</param>
        /// <returns>数字标识</returns>
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
        /// 解析脚本中定义的参数变量
        /// -- inc #v_data_field_id
        /// </summary>
        /// <param name="name">脚本名称</param>
        /// <param name="log">用户日志</param>
        /// <param name="args">语句执行参数值</param>
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
                _ext_name += sVal;
            }

            _string_sql.Clear();
        }

        /// <summary>
        /// 解析脚本中定义的 iff 条件逻辑运算
        /// 参数 -- iff > 0 #var1 #var2 ... #varN
        /// </summary>
        /// <param name="name">脚本名称</param>
        /// <param name="log">用户日志</param>
        /// <param name="args">语句执行参数值</param>
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

        /// <summary>
        /// 标记一个拼接语句，使用 $_ext_name$ 占位
        /// </summary>
        /// <param name="sb">SQL字符串语句</param>
        public void set_ext_string_(StringBuilder sb)
        {
            sb.Append(' ').Append('$').Append(_ext_name).Append('$');
        }

        /// <summary>
        /// 判断数字值类型变量是否满足条件
        /// </summary>
        /// <param name="args">语句执行参数值</param>
        /// <returns>如果条件符合true，否则false</returns>
        private bool check_if_number_(Dictionary<string, object> args)
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

        /// <summary>
        /// 判断字符串类型变量是否满足条件
        /// </summary>
        /// <param name="args">语句执行参数值</param>
        /// <returns>如果条件符合true，否则false</returns>
        private bool check_if_string_(Dictionary<string, object> args)
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
        /// 判断iff 条件参数是否成立
        /// </summary>
        /// <param name="args">语句执行参数值</param>
        /// <returns>如果条件符合true，否则false</returns>
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
        /// <summary>
        /// 处理for 循环拼接语句
        /// 第3个参数为 for 循环分割字符串
        /// 第4个参数为执行参数名称
        /// -- add for ',' #v_pack
        /// </summary>
        /// <param name="sb">SQL字符串语句</param>
        /// <param name="args">语句执行参数值</param>
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

            sb.Replace('$' + _ext_name + '$', sbFor.ToString());
        }

        /// <summary>
        /// 替换 -- add 拼接语句
        /// </summary>
        /// <param name="sb">SQL字符串语句</param>
        /// <param name="args">语句执行参数值</param>
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
                    sb.Replace('$' + _ext_name + '$', _string_sql.ToString());
                }
                else
                {
                    sb.Replace('$' + _ext_name + '$', "");
                }
            }
        }
    }
}
