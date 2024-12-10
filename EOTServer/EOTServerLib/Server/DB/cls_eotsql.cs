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
        private string _path = "";
        public Dictionary<string, cls_eotsql_file> _dic_script = new();

        /// <summary>
        /// 多数据库参数
        /// </summary>
        private Dictionary<string, string> _dic_db_string = new();
        private Dictionary<string, Type> _dic_db_type = new();

        public cls_eotsql(long slowDelay)
        {
            cls_eotsql_db._slow_delay = slowDelay;

            // 注册数据库类型
            _dic_db_type.Add("mysql", typeof(cls_db_mysql));
        }

        /// <summary>
        /// 数据库参数
        /// </summary>
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

        public cls_eotsql_db get_db_(string dbType, string dbName)
        {
            string sConn = "";
            if (_dic_db_string.ContainsKey(dbName))
            {
                sConn = _dic_db_string[dbName];
            }
            else
            {
                sConn = _dic_db_string.First().Value;
            }

            return dbType switch
            {
                _ => new cls_db_mysql(sConn),
            };
        }

        /// <summary>
        /// 动态反射更优雅，效率太低
        /// </summary>
        /// <param name="dbType"></param>
        /// <param name="dbName"></param>
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

            string sConn = "";
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
        /// 加载所有SQL语句
        /// </summary>
        /// <param name="path"></param>
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

        public void reload_()
        {
            load_(_path);
        }

        /// <summary>
        /// 分页仅用于SELECT，整个脚本只能有唯一SELECT
        /// </summary>
        /// <param name="result"></param>
        /// <param name="sqlObj"></param>
        /// <param name="args"></param>
        /// <param name="rowIndex"></param>
        /// <param name="rowCount"></param>
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
                string? sqlCount = cls_eotsql_obj.get_count_(sql);
                if (sqlCount != null)
                {
                    cls_result cQuery = new();
                    sqlDb.exec_value_(cQuery, sqlCount);
                    // 错误中断执行
                    if (!cQuery.is_success_()) return false;
                    nRowTotal = cls_core.o2int_(cQuery.get_scalar());
                }

                nRowIndexQuery = 0;
            }

            if (rowCount > 0)
            {
                // 语句后面不要跟;
                sql += " limit " + nRowIndexQuery + "," + rowCount;
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
        /// <param name="result"></param>
        /// <param name="sqlObj"></param>
        /// <param name="args"></param>
        /// <param name="rowIndex"></param>
        /// <param name="rowCount"></param>
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
        /// <param name="result"></param>
        /// <param name="sqlObj"></param>
        /// <param name="args"></param>
        /// <param name="rowIndex"></param>
        /// <param name="rowCount"></param>
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
        /// <param name="result"></param>
        /// <param name="sqlObj"></param>
        /// <param name="args"></param>
        /// <param name="rowIndex"></param>
        /// <param name="rowCount"></param>
        /// <returns></returns>
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
        /// 
        /// 执行脚本
        /// 
        /// </summary>
        /// <param name="result"></param>
        /// <param name="scriptName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
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

        public cls_result call_query_(string procName, Dictionary<string, object> procArgs)
        {
            return new cls_result();
        }
        public cls_result call_update_(string procName, Dictionary<string, object> procArgs)
        {
            return new cls_result();
        }
        public cls_result call_value_(string procName, Dictionary<string, object> procArgs)
        {
            return new cls_result();
        }
    }
}
