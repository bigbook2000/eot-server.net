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

namespace cn.eobject.iot.Server.DB
{
    public class cls_mysql
    {
        /// <summary>
        /// 监测慢查询 毫秒
        /// </summary>
        protected long _slow_delay = 10000L;
        protected string _dbstring = "";

        public cls_mysql()
        {
        }

        public cls_mysql(string dbString, long slowDelay)
        {
            _dbstring = dbString;
            _slow_delay = slowDelay;
        }

        public void set_dbstring_(string dbString)
        {
            _dbstring = dbString;
        }
        public void set_slow_delay_(long slowDelay)
        {
            _slow_delay = slowDelay;
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

        public cls_result exec_value_(string sql)
        {
            cls_result cResult = new();

            try
            {
                Stopwatch stopWatch = new();
                stopWatch.Start();

                using MySqlConnection conn = new(_dbstring);

                conn.Open();

                MySqlCommand cmd = conn.CreateCommand();

                cmd.CommandText = sql;
                cmd.CommandType = CommandType.Text;

                object obj = cmd.ExecuteScalar();
                cResult.set_scalar(obj);

                conn.Close();
                conn.Dispose();

                cResult.set_success_();

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

        public cls_result exec_query_(string sql)
        {
            cls_result cResult = new();

            try
            {
                Stopwatch stopWatch = new();
                stopWatch.Start();

                using MySqlConnection conn = new(_dbstring);

                conn.Open();

                MySqlCommand cmd = conn.CreateCommand();

                cmd.CommandText = sql;
                cmd.CommandType = CommandType.Text;

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

                conn.Close();
                conn.Dispose();

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

        public cls_result exec_update_(string sql)
        {
            cls_result cResult = new();

            try
            {
                Stopwatch stopWatch = new();
                stopWatch.Start();

                using MySqlConnection conn = new(_dbstring);

                conn.Open();

                MySqlCommand cmd = conn.CreateCommand();

                cmd.CommandText = sql;
                cmd.CommandType = CommandType.Text;

                cmd.ExecuteNonQuery();

                conn.Close();
                conn.Dispose();

                cResult.set_success_();

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

        public cls_result call_value_(string procName, Dictionary<string, object> procArgs)
        {
            cls_result cResult = new();

            try
            {
                Stopwatch stopWatch = new();
                stopWatch.Start();

                using MySqlConnection conn = new(_dbstring);

                conn.Open();

                MySqlCommand cmd = conn.CreateCommand();

                cmd.CommandText = procName;
                cmd.CommandType = CommandType.StoredProcedure;

                foreach (KeyValuePair<string, object> kvp in procArgs)
                {
                    cmd.Parameters.AddWithValue(kvp.Key, kvp.Value);
                }

                object obj = cmd.ExecuteScalar();
                cResult.set_scalar(obj);

                conn.Close();
                conn.Dispose();

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
                Stopwatch stopWatch = new();
                stopWatch.Start();

                using MySqlConnection conn = new(_dbstring);

                conn.Open();

                MySqlCommand cmd = conn.CreateCommand();

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

                conn.Close();
                conn.Dispose();

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
                Stopwatch stopWatch = new();
                stopWatch.Start();

                using MySqlConnection conn = new(_dbstring);

                conn.Open();

                MySqlCommand cmd = conn.CreateCommand();

                cmd.CommandText = procName;
                cmd.CommandType = CommandType.StoredProcedure;

                foreach (KeyValuePair<string, object> kvp in procArgs)
                {
                    cmd.Parameters.AddWithValue(kvp.Key, kvp.Value);
                }

                cmd.ExecuteNonQuery();

                conn.Close();
                conn.Dispose();

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
