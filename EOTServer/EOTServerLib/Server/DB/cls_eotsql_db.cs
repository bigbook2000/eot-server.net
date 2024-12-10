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
    /// <summary>
    /// 
    /// 数据库操作基类
    /// 
    /// </summary>
    public abstract class cls_eotsql_db : IDisposable
    {
        /// <summary>
        /// 监测慢查询 毫秒
        /// </summary>
        public static long _slow_delay = 10000L;

        /// <summary>
        /// 连接字符串
        /// </summary>
        protected string _connect_string = "";

        public cls_eotsql_db(string connStr)
        {
            _connect_string = connStr;
            open_(connStr);
        }

        public void Dispose()
        {
            close_();
        }

        public abstract void open_(string connStr);
        public abstract void close_();

        /// <summary>
        /// 返回一个单值
        /// </summary>
        /// <param name="result"></param>
        /// <param name="sql"></param>
        protected abstract void exec_value_inner_(cls_result result, string sql);
        /// <summary>
        /// 返回结果集
        /// </summary>
        /// <param name="result"></param>
        /// <param name="sql"></param>
        protected abstract void exec_query_inner_(cls_result result, string sql);
        /// <summary>
        /// 更新不返回
        /// </summary>
        /// <param name="result"></param>
        /// <param name="sql"></param>
        protected abstract void exec_update_inner_(cls_result result, string sql);
        /// <summary>
        /// 插入自增，如果不是自增字段插入，调用 exec_update_
        /// 存放到 _last_id
        /// </summary>
        /// <param name="result"></param>
        /// <param name="sql"></param>
        protected abstract void exec_insert_inner_(cls_result result, string sql);


        public void exec_value_(cls_result result, string sql)
        {
            try
            {
                Stopwatch stopWatch = new();
                stopWatch.Start();

                exec_value_inner_(result, sql);

                long tick = stopWatch.ElapsedMilliseconds;
                if (tick > _slow_delay)
                    cls_log.get_db_().T_("", cls_log.WARNING_ + " [slow:" + tick + "] " + sql);
                stopWatch.Stop();
            }
            catch (Exception ex)
            {
                result.set_(cls_result.RESULT_EXCEPT, ex.Message);
                cls_log.get_db_().T_("", ex.ToString() + "\r\n" + sql);
            }
        }

        public void exec_query_(cls_result result, string sql)
        {
            try
            {
                Stopwatch stopWatch = new();
                stopWatch.Start();

                exec_query_inner_(result, sql);

                long tick = stopWatch.ElapsedMilliseconds;
                if (tick > _slow_delay)
                    cls_log.get_db_().T_("", cls_log.WARNING_ + " [slow:" + tick + "] " + sql);
                stopWatch.Stop();
            }
            catch (Exception ex)
            {
                result.set_(cls_result.RESULT_EXCEPT, ex.Message);
                cls_log.get_db_().T_("", ex.ToString() + "\r\n" + sql);
            }
        }

        public void exec_update_(cls_result result, string sql)
        {
            try
            {
                Stopwatch stopWatch = new();
                stopWatch.Start();

                exec_update_inner_(result, sql);

                long tick = stopWatch.ElapsedMilliseconds;
                if (tick > _slow_delay)
                    cls_log.get_db_().T_("", cls_log.WARNING_ + " [slow:" + tick + "] " + sql);
                stopWatch.Stop();
            }
            catch (Exception ex)
            {
                result.set_(cls_result.RESULT_EXCEPT, ex.Message);
                cls_log.get_db_().T_("", ex.ToString() + "\r\n" + sql);
            }
        }

        /// <summary>
        /// 单独处理自增变量 存放到 _last_id
        /// </summary>
        /// <param name="result"></param>
        /// <param name="sql"></param>
        public void exec_insert_(cls_result result, string sql)
        {
            try
            {
                Stopwatch stopWatch = new();
                stopWatch.Start();

                exec_insert_inner_(result, sql);

                long tick = stopWatch.ElapsedMilliseconds;
                if (tick > _slow_delay)
                    cls_log.get_db_().T_("", cls_log.WARNING_ + " [slow:" + tick + "] " + sql);
                stopWatch.Stop();
            }
            catch (Exception ex)
            {
                result.set_(cls_result.RESULT_EXCEPT, ex.Message);
                cls_log.get_db_().T_("", ex.ToString() + "\r\n" + sql);
            }
        }

    }
}
