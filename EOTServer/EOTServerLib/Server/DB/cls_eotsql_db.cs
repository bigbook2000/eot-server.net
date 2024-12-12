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
    /// 数据库操作基类
    /// 保证最小占用连接池，实现IDisposable，使用using保证及时释放。ADO.NET自行管理连接池。
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

        /// <summary>
        /// 连接参数构造
        /// </summary>
        /// <param name="connStr">连接参数字符串</param>
        public cls_eotsql_db(string connStr)
        {
            _connect_string = connStr;
            open_(connStr);
        }
        /// <summary>
        /// 释放连接
        /// </summary>
        public void Dispose()
        {
            close_();
        }
        /// <summary>
        /// 打开连接
        /// 虚函数，由实际派生类实现
        /// </summary>
        /// <param name="connStr">连接参数字符串</param>
        public abstract void open_(string connStr);
        /// <summary>
        /// 关闭连接
        /// 虚函数，由实际派生类实现
        /// </summary>
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

        /// <summary>
        /// 转换数量查询语句，将SELECT * FROM转换为SELECT COUNT(*) FROM
        /// 只处理单一语句，主要用于分页
        /// </summary>
        /// <param name="sql">源 SELECT 语句</param>
        /// <returns>数量语句</returns>
        public abstract string get_sql_count_(string sql);
        /// <summary>
        /// 转换分页查询语句，只处理单一SELECT语句
        /// </summary>
        /// <param name="sql">源 SELECT 语句</param>
        /// <param name="rowIndex">分页偏移</param>
        /// <param name="rowCount">分页数量</param>
        /// <returns>分页语句</returns>
        public abstract string get_sql_page_(string sql, int rowIndex, int rowCount);

        /// <summary>
        /// 执行返回单值
        /// 包装接口，调用实际数据库派生类方法
        /// </summary>
        /// <param name="result">操作结果对象</param>
        /// <param name="sql">执行语句</param>
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

        /// <summary>
        /// 执行返回结果集
        /// 包装接口，调用实际数据库派生类方法
        /// </summary>
        /// <param name="result">操作结果对象</param>
        /// <param name="sql">执行语句</param>
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
        /// <summary>
        /// 执行无结果集更新
        /// 包装接口，调用实际数据库派生类方法
        /// </summary>
        /// <param name="result">操作结果对象</param>
        /// <param name="sql">执行语句</param>
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
        /// 包装接口，调用实际数据库派生类方法
        /// </summary>
        /// <param name="result">操作结果对象</param>
        /// <param name="sql">执行语句</param>
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
