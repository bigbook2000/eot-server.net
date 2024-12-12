using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.DB.Ext;
using cn.eobject.iot.Server.Log;

namespace cn.eobject.iot.Server.DB
{
    /// <summary>
    /// 
    /// 数据库脚本系统
    /// 在实际使用数据库过程中，拼接SQL语句具有很强的优势
    /// 首先数据库兼容型更好，目前国产化运动正盛，不同的数据库都支持基本的SQL语句，存储过程依赖性太强
    /// ORM模型虽然优雅，但局限性很多，特别是关联查询和效率优化，对于大规模物联网数据更容易优化
    /// 实际应用中使用的标准语句很少，拼接SQL语句灵活性更强
    /// 
    /// 这里面封装了多层，源代码较为复杂，主要是为了上层开发更为简洁
    /// 
    /// </summary>
    public class cls_eotsql
    {
        /// <summary>
        /// 由于是拼接方式，需要通过过滤来处理注入问题
        /// 虽然不是最优解，但这是数据库兼容性最好的方式
        /// </summary>
        public static string[] SQL_KEYWORDS =
        {
            "'", "<", ">", "=", "(", ")", "!", ";"
        };


        /// <summary>
        /// 避免拼接SQL注入，根据实际情况扩展
        /// </summary>
        /// <param name="valString"></param>
        /// <returns></returns>
        public static bool check_sql_value_(string valString)
        {
            foreach (var key in SQL_KEYWORDS)
            {
                if (valString.Contains(key, StringComparison.CurrentCultureIgnoreCase)) return true;
            }

            return false;
        }
        /// <summary>
        /// 脚本文件路径
        /// </summary>
        private string _path = "";
        /// <summary>
        /// 脚本对象表
        /// </summary>
        public Dictionary<string, cls_eotsql_file> _dic_script = new();

        /// <summary>
        /// 多数据源连接参数字符串表
        /// </summary>
        private Dictionary<string, string> _dic_db_string = new();
        /// <summary>
        /// 数据库类型表，派生类管理
        /// 目前暂对mysql有效
        /// </summary>
        private Dictionary<string, Type> _dic_db_type = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="slowDelay">慢查询时间上限，单位秒</param>
        public cls_eotsql(long slowDelay)
        {
            cls_eotsql_db._slow_delay = slowDelay;

            // 注册数据库类型
            _dic_db_type.Add("mysql", typeof(cls_db_mysql));
        }

        /// <summary>
        /// 添加多数据源
        /// </summary>
        /// <param name="dbName">数据源名称</param>
        /// <param name="connStr">数据库连接参数字符串</param>
        public void add_db_(string dbName, string connStr)
        {
            if (!_dic_db_string.ContainsKey(dbName))
            {
                _dic_db_string.Add(dbName, connStr);
            }
            else
            {
                _dic_db_string[dbName] = connStr;
            }
        }
        /// <summary>
        /// 访问指定的数据源对象
        /// 暂时只支持mysql
        /// </summary>
        /// <param name="dbType">数据类型</param>
        /// <param name="dbName">数据源名称</param>
        /// <returns></returns>
        public cls_eotsql_db get_db_(string dbType, string dbName)
        {
            string sConn;
            if (_dic_db_string.ContainsKey(dbName))
            {
                sConn = _dic_db_string[dbName];
            }
            else
            {
                sConn = _dic_db_string.First().Value;
            }

            // 目前暂支持mysql
            // 后续扩展
            return dbType switch
            {
                _ => new cls_db_mysql(sConn),
            };
        }

        /// <summary>
        /// 动态反射更优雅，效率太低
        /// </summary>
        /// <param name="dbType">数据类型</param>
        /// <param name="dbName">数据源名称</param>
        /// <returns></returns>
        public object? _get_db0(string dbType, string dbName)
        {
            Type tDbType;
            if (_dic_db_type.ContainsKey(dbType))
            {
                tDbType = _dic_db_type[dbType];
            }
            else
            {
                tDbType = _dic_db_type.First().Value;
            }

            string sTypeName = dbType;
            if (tDbType.FullName != null) sTypeName = tDbType.FullName;

            string sConn;
            if (_dic_db_string.ContainsKey(dbName))
            {
                sConn = _dic_db_string[dbName];
            }
            else
            {
                sConn = _dic_db_string.First().Value;
            }

            return tDbType.Assembly.CreateInstance(
                sTypeName,
                true,
                System.Reflection.BindingFlags.Default,
                null,
                new object[] { sConn },
                null,
                null);
        }

        /// <summary>
        /// 加载指定路径下的数据库脚本
        /// </summary>
        /// <param name="path">数据库脚本文件路径</param>
        public void load_(string path)
        {
            string? sLine;
            string? sName;

            try
            {
                _path = path;
                DirectoryInfo dir = new(_path);

                FileInfo[] fs = dir.GetFiles("*.sql", SearchOption.AllDirectories);

                _dic_script.Clear();

                foreach (FileInfo fi in fs)
                {
                    StreamReader sr = new(fi.FullName);
                    sName = fi.Name[..fi.Name.IndexOf('.')];

                    cls_eotsql_file tFileSql = new(sName);
                    _dic_script.Add(sName, tFileSql);

                    while (!sr.EndOfStream)
                    {
                        sLine = sr.ReadLine();
                        if (sLine == null) break;

                        if (!tFileSql.parse_line_(sLine))
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                cls_log.get_db_().T_("", ex.ToString());
            }
        }
        /// <summary>
        /// 重新加载，动态更新
        /// </summary>
        public void reload_()
        {
            load_(_path);
        }

        /// <summary>
        /// 分页仅用于SELECT，整个脚本只能有唯一SELECT
        /// </summary>
        /// <param name="result">执行结果</param>
        /// <param name="sqlDb">数据源</param>
        /// <param name="sqlObj">数据库脚本对象</param>
        /// <param name="args">执行参数</param>
        /// <param name="rowIndex">分页偏移</param>
        /// <param name="rowCount">分页数量</param>
        /// <returns>执行成功返回true，否则false</returns>
        private bool script_select_(cls_result result,
            cls_eotsql_db sqlDb,
            cls_eotsql_obj sqlObj,
            Dictionary<string, object> args, int rowIndex, int rowCount)
        {
            string sql = sqlObj.get_sql_(args).ToString();

            int nRowIndexQuery = rowIndex;
            int nRowTotal = 0;

            // 如果小于0，查询总数
            if (nRowIndexQuery < 0)
            {
                string? sSqlCount = sqlDb.get_sql_count_(sql);
                if (sSqlCount != null)
                {
                    cls_result cQuery = new();
                    sqlDb.exec_value_(cQuery, sSqlCount);
                    // 错误中断执行
                    if (!cQuery.is_success_()) return false;
                    nRowTotal = cls_core.o2int_(cQuery.get_scalar());
                }

                nRowIndexQuery = 0;
            }

            if (rowCount > 0)
            {
                sql = sqlDb.get_sql_page_(sql, nRowIndexQuery, rowCount);
            }

            result.reset_();

            sqlDb.exec_query_(result, sql);
            // 错误中断执行
            if (!result.is_success_()) return false;

            if (result.count_() > 0)
            {
                cls_result_obj obj = result.default_();
                obj.Add("s_page_row_index", rowIndex);
                obj.Add("s_page_row_count", rowCount);
                obj.Add("s_total_count", nRowTotal);
            }

            // 继续执行
            foreach (var child in sqlObj._childs)
            {
                script_obj_(result, sqlDb, child, args, rowIndex, rowCount);
                // 错误中断执行
                if (!result.is_success_()) return false;
            }

            return result.is_success_();
        }

        /// <summary>
        /// 执行条件复合语句
        /// </summary>
        /// <param name="result">执行结果</param>
        /// <param name="sqlDb">数据源</param>
        /// <param name="sqlObj">数据库脚本对象</param>
        /// <param name="args">执行参数</param>
        /// <param name="rowIndex">分页偏移</param>
        /// <param name="rowCount">分页数量</param>
        /// <returns>执行成功返回true，否则false</returns>
        private bool script_iff_(cls_result result,
            cls_eotsql_db sqlDb,
            cls_eotsql_obj sqlObj,
            Dictionary<string, object> args, int rowIndex, int rowCount)
        {
            // 条件不满足
            if (!sqlObj.check_if_params_(args))
            {
                return true;
            }

            // 条件满足继续执行子查询
            foreach (var child in sqlObj._childs)
            {
                script_obj_(result, sqlDb, child, args, rowIndex, rowCount);
                // 错误中断执行
                if (!result.is_success_()) return false;
            }

            return true;
        }

        /// <summary>
        /// 执行变量返回语句，单一SELECT
        /// </summary>
        /// <param name="result">执行结果</param>
        /// <param name="sqlDb">数据源</param>
        /// <param name="sqlObj">数据库脚本对象</param>
        /// <param name="args">执行参数</param>
        /// <param name="rowIndex">分页偏移（本方法保留）</param>
        /// <param name="rowCount">分页数量（本方法保留）</param>
        /// <returns>执行成功返回true，否则false</returns>
        private bool script_var_(cls_result result,
            cls_eotsql_db sqlDb,
            cls_eotsql_obj sqlObj,
            Dictionary<string, object> args, int rowIndex, int rowCount)
        {
            string sql = sqlObj.get_sql_(args).ToString();

            result.reset_();
            sqlDb.exec_query_(result, sql);
            // 错误中断执行
            if (!result.is_success_()) return false;

            cls_result_obj data = result.default_();

            foreach (var d in data)
            {
                if (d.Value != null)
                {
                    if (args.ContainsKey(d.Key))
                    {
                        args[d.Key] = d.Value;
                    }
                    else
                    {
                        args.Add(d.Key, d.Value);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 执行更新命令
        /// </summary>
        /// <param name="result">执行结果</param>
        /// <param name="sqlDb">数据源</param>
        /// <param name="sqlObj">数据库脚本对象</param>
        /// <param name="args">执行参数</param>
        /// <param name="rowIndex">分页偏移（本方法保留）</param>
        /// <param name="rowCount">分页数量（本方法保留）</param>
        /// <returns>执行成功返回true，否则false</returns>
        private bool script_update_(cls_result result,
            cls_eotsql_db sqlDb,
            cls_eotsql_obj sqlObj,
            Dictionary<string, object> args, int rowIndex, int rowCount)
        {
            string sql = sqlObj.get_sql_(args).ToString();

            result.reset_();
            sqlDb.exec_update_(result, sql);

            // 错误中断执行
            if (!result.is_success_()) return false;

            return true;
        }

        /// <summary>
        /// 插入语句，处理自增
        /// 如果不是自增，请调用 update
        /// </summary>
        /// <param name="result">执行结果</param>
        /// <param name="sqlDb">数据源</param>
        /// <param name="sqlObj">数据库脚本对象</param>
        /// <param name="args">执行参数</param>
        /// <param name="rowIndex">分页偏移（本方法保留）</param>
        /// <param name="rowCount">分页数量（本方法保留）</param>
        /// <returns>执行成功返回true，否则false</returns>
        private bool script_inc_(cls_result result,
            cls_eotsql_db sqlDb,
            cls_eotsql_obj sqlObj,
            Dictionary<string, object> args, int rowIndex, int rowCount)
        {
            string sql = sqlObj.get_sql_(args).ToString();

            result.reset_();
            sqlDb.exec_insert_(result, sql);

            int nLastId = result.default_().to_int_("_last_id");
            foreach (var p in sqlObj._ext_params)
            {
                if (args.ContainsKey(p))
                {
                    args[p] = nLastId;
                }
                else
                {
                    args.Add(p, nLastId);
                }
            }

            // 错误中断执行
            if (!result.is_success_()) return false;

            return true;
        }

        /// <summary>
        /// 总脚本对象执行方法
        /// </summary>
        /// <param name="result">执行结果</param>
        /// <param name="sqlDb">数据源</param>
        /// <param name="sqlObj">数据库脚本对象</param>
        /// <param name="args">执行参数</param>
        /// <param name="rowIndex">分页偏移（本方法保留）</param>
        /// <param name="rowCount">分页数量（本方法保留）</param>
        /// <returns>执行成功返回true，否则false</returns>
        private bool script_obj_(cls_result result,
            cls_eotsql_db sqlDb,
            cls_eotsql_obj sqlObj,
            Dictionary<string, object> args, int rowIndex, int rowCount)
        {
            switch (sqlObj._type)
            {
                case cls_eotsql_obj.SQL_TYPE_SELECT:
                    script_select_(result, sqlDb, sqlObj, args, rowIndex, rowCount);
                    break;
                case cls_eotsql_obj.SQL_TYPE_INC:
                    script_inc_(result, sqlDb, sqlObj, args, rowIndex, rowCount);
                    break;
                case cls_eotsql_obj.SQL_TYPE_INSERT:
                case cls_eotsql_obj.SQL_TYPE_UPDATE:
                case cls_eotsql_obj.SQL_TYPE_DELETE:
                    script_update_(result, sqlDb, sqlObj, args, rowIndex, rowCount);
                    break;
                case cls_eotsql_obj.SQL_TYPE_VAR:
                    script_var_(result, sqlDb, sqlObj, args, rowIndex, rowCount);
                    break;
                case cls_eotsql_obj.SQL_TYPE_IFF:
                    script_iff_(result, sqlDb, sqlObj, args, rowIndex, rowCount);
                    break;
                default:
                    {
                        foreach (var child in sqlObj._childs)
                        {
                            script_obj_(result, sqlDb, child, args, rowIndex, rowCount);
                            // 错误中断执行
                            if (!result.is_success_()) return false;
                        }
                    }
                    break;
            }

            // 错误中断执行
            if (!result.is_success_()) return false;

            return true;
        }

        /// <summary>
        /// 外部调用执行脚本
        /// </summary>
        /// <param name="result">执行结果</param>
        /// <param name="scriptName">脚本名称</param>
        /// <param name="args">执行参数</param>
        public void script_(cls_result result, string scriptName, Dictionary<string, object> args)
        {
            try
            {
                string sInfo;
                if (!_dic_script.ContainsKey(scriptName))
                {
                    sInfo = $"语句未定义 {scriptName}";
                    cls_log.get_db_().T_("", sInfo);
                    result.set_error_(sInfo);
                    return;
                }

                // 检查特殊字符，如有需要可动态调整

                string? sVal;
                foreach (var arg in args)
                {
                    if (arg.Value == null) continue;
                    sVal = arg.Value.ToString();
                    if (sVal == null) continue;
                    if (check_sql_value_(sVal))
                    {
                        sInfo = $"含有特殊字符 {scriptName} {sVal}";
                        cls_log.get_db_().T_("", sInfo);
                        result.set_error_(sInfo);
                        return;
                    }
                }                

                cls_eotsql_file scriptFile = _dic_script[scriptName];

                int nRowIndex = 0;
                int nRowCount = 0;

                // 需要分页
                if (args.ContainsKey("s_page_row_index") && args.ContainsKey("s_page_row_count"))
                {
                    nRowIndex = cls_core.o2int_(args["s_page_row_index"]);
                    nRowCount = cls_core.o2int_(args["s_page_row_count"]);
                }

                // 使用同一连接会话，考虑事务扩展                
                using cls_eotsql_db sqlDb = get_db_(scriptFile._db_type, scriptFile._db_name);
                script_obj_(result, sqlDb, scriptFile._root_sql, args, nRowIndex, nRowCount);
                sqlDb.Dispose();
            }
            catch (Exception ex)
            {
                result.set_(cls_result.RESULT_EXCEPT, ex.Message);
                cls_log.get_db_().T_("", ex.ToString());
            }
        }
    }
}
