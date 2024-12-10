using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.eobject.iot.Server.DB.Ext
{
    public class cls_db_mysql : cls_eotsql_db
    {
        protected MySqlConnection? _connection;

        public cls_db_mysql(string db) : base(db)
        {
        }

        public override void open_(string connStr)
        {
            try
            {
                _connection = new(connStr);
                _connection.Open();
            }
            catch (Exception ex)
            {
                cls_log.get_db_().T_("", ex.ToString());
            }
        }
        public override void close_()
        {
            try
            {
                if (_connection != null)
                {
                    _connection.Close();
                    _connection.Dispose();
                }

                _connection = null;
            }
            catch (Exception ex)
            {
                cls_log.get_db_().T_("", ex.ToString());
            }
        }

        protected override void exec_value_inner_(cls_result result, string sql)
        {
            if (_connection == null)
            {
                result.set_error_("连接未打开 " + sql);
                return;
            }

            MySqlCommand cmd = _connection.CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;

            object obj = cmd.ExecuteScalar();
            result.set_scalar(obj);
            result.set_success_();
        }
        protected override void exec_query_inner_(cls_result result, string sql)
        {
            if (_connection == null)
            {
                result.set_error_("连接未打开 " + sql);
                return;
            }

            MySqlCommand cmd = _connection.CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;

            cls_result_obj cResultObj;
            int i;
            string sName;

            result.reset_();

            MySqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                cResultObj = new();
                result._list.Add(cResultObj);

                for (i = 0; i < dr.FieldCount; i++)
                {
                    sName = dr.GetName(i);

                    if (sName == "_d")
                    {
                        result._d = dr.GetInt32(i);
                        continue;
                    }
                    else if (sName == "_s")
                    {
                        result._s = dr.GetString(i);
                        continue;
                    }

                    // 如果以 _ 开头表示内部标记，跳过
                    if (sName[0] == '_') continue;

                    cResultObj[sName] = dr.GetValue(i);
                    //cResultObj.Add(dr.GetName(i), dr.GetValue(i));
                }
            }

            dr.Close();
        }
        protected override void exec_update_inner_(cls_result result, string sql)
        {
            if (_connection == null)
            {
                result.set_error_("连接未打开 " + sql);
                return;
            }

            MySqlCommand cmd = _connection.CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;

            cmd.ExecuteNonQuery();

            result.set_success_();
        }
        protected override void exec_insert_inner_(cls_result result, string sql)
        {
            if (_connection == null)
            {
                result.set_error_("连接未打开 " + sql);
                return;
            }
            MySqlCommand cmd = _connection.CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;

            cmd.ExecuteNonQuery();
            long nLastId = cmd.LastInsertedId;

            cls_result_obj cResultObj = new()
                {
                    { "_last_id", nLastId }
                };
            result.add_(cResultObj);
            result.set_success_();
        }

        protected string get_call_string(string procName, Dictionary<string, object> procArgs)
        {
            StringBuilder sb = new();
            sb.Append("call ").Append(procName).Append('(');
            foreach (KeyValuePair<string, object> kvp in procArgs)
            {
                sb.Append(kvp.Key).Append('=').Append(kvp.Value).Append(',');
            }
            sb.Append(')');

            return sb.ToString();
        }


        public cls_result call_value_(string procName, Dictionary<string, object> procArgs)
        {
            cls_result cResult = new();

            try
            {
                if (_connection == null)
                {
                    cResult.set_error_("连接未打开 " + procName);
                    return cResult;
                }

                Stopwatch stopWatch = new();
                stopWatch.Start();

                MySqlCommand cmd = _connection.CreateCommand();

                cmd.CommandText = procName;
                cmd.CommandType = CommandType.StoredProcedure;

                foreach (KeyValuePair<string, object> kvp in procArgs)
                {
                    cmd.Parameters.AddWithValue(kvp.Key, kvp.Value);
                }

                object obj = cmd.ExecuteScalar();
                cResult.set_scalar(obj);

                cResult.set_success_();

                string sql = get_call_string(procName, procArgs);
                long tick = stopWatch.ElapsedMilliseconds;
                if (tick > _slow_delay)
                    cls_log.get_db_().T_("", cls_log.WARNING_ + " [slow:" + tick + "] " + sql);
                stopWatch.Stop();
            }
            catch (Exception ex)
            {
                cResult.set_(cls_result.RESULT_EXCEPT, ex.Message);
                cls_log.get_db_().T_("", ex.ToString());
            }

            return cResult;
        }

        public cls_result call_query_(string procName, Dictionary<string, object> procArgs)
        {
            cls_result cResult = new();

            try
            {                
                if (_connection == null)
                {
                    cResult.set_error_("连接未打开 " + procName);
                    return cResult;
                }                

                Stopwatch stopWatch = new();
                stopWatch.Start();

                MySqlCommand cmd = _connection.CreateCommand();

                cmd.CommandText = procName;
                cmd.CommandType = CommandType.StoredProcedure;

                foreach (KeyValuePair<string, object> kvp in procArgs)
                {
                    cmd.Parameters.AddWithValue(kvp.Key, kvp.Value);
                }

                cls_result_obj cResultObj;
                int i;
                string sName;

                MySqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    cResultObj = new();
                    cResult._list.Add(cResultObj);

                    for (i = 0; i < dr.FieldCount; i++)
                    {
                        sName = dr.GetName(i);

                        if (sName == "_d")
                        {
                            cResult._d = dr.GetInt32(i);
                            continue;
                        }
                        else if (sName == "_s")
                        {
                            cResult._s = dr.GetString(i);
                            continue;
                        }

                        // 如果以 _ 开头表示内部标记，跳过
                        if (sName[0] == '_') continue;

                        cResultObj[sName] = dr.GetValue(i);
                        //cResultObj.Add(dr.GetName(i), dr.GetValue(i));
                    }
                }

                dr.Close();

                string sql = get_call_string(procName, procArgs);
                long tick = stopWatch.ElapsedMilliseconds;
                if (tick > _slow_delay)
                    cls_log.get_db_().T_("", cls_log.WARNING_ + " [slow:" + tick + "] " + sql);
                stopWatch.Stop();
            }
            catch (Exception ex)
            {
                cResult.set_(cls_result.RESULT_EXCEPT, ex.Message);
                cls_log.get_db_().T_("", ex.ToString());
            }

            return cResult;
        }

        public cls_result call_update_(string procName, Dictionary<string, object> procArgs)
        {
            cls_result cResult = new();

            try
            {
                if (_connection == null)
                {
                    cResult.set_error_("连接未打开 " + procName);
                    return cResult;
                }

                Stopwatch stopWatch = new();
                stopWatch.Start();

                MySqlCommand cmd = _connection.CreateCommand();

                cmd.CommandText = procName;
                cmd.CommandType = CommandType.StoredProcedure;

                foreach (KeyValuePair<string, object> kvp in procArgs)
                {
                    cmd.Parameters.AddWithValue(kvp.Key, kvp.Value);
                }

                cmd.ExecuteNonQuery();

                cResult.set_success_();

                string sql = get_call_string(procName, procArgs);
                long tick = stopWatch.ElapsedMilliseconds;
                if (tick > _slow_delay)
                    cls_log.get_db_().T_("", cls_log.WARNING_ + " [slow:" + tick + "] " + sql);
                stopWatch.Stop();
            }
            catch (Exception ex)
            {
                cResult.set_(cls_result.RESULT_EXCEPT, ex.Message);
                cls_log.get_db_().T_("", ex.ToString());
            }

            return cResult;
        }
    }
}
