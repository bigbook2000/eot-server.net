using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.DB;
using cn.eobject.iot.Server.Log;
using EOIotServer.protocol.hj212;
using Microsoft.Extensions.Primitives;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EOIotServer.protocol
{
    /// <summary>
    /// 直接使用特殊SQL语句
    /// </summary>
    public class CDBSql_HJ212 : cls_mysql
    {

        public CDBSql_HJ212(string dbString, long slowDelay, string sqlTest) : base(dbString, slowDelay)
        {
            if (sqlTest.Length > 0)
            {
                // 测试数据库
                cls_log.get_default_().T_("", "测试数据库" + sqlTest);
                exec_update_(sqlTest);
            }
        }

        /// <summary>
        /// 根据mn批量获取id
        /// </summary>
        /// <param name="mns"></param>
        public Dictionary<string, int> DBLoadDeviceIds(string mns)
        {
            Dictionary<string, int> dic = new();

            try
            {
                string sql = "SELECT `device_id`,`mn` FROM `n_device` WHERE `mn` IN(" + mns + ")";
                cls_result queryResult = exec_query_(sql);

                foreach (cls_result_obj obj in queryResult._list)
                {
                    dic.TryAdd(obj.to_string_("mn"), obj.to_int_("device_id"));
                }
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }

            return dic;
        }

        /// <summary>
        /// 查询未建立数据表的记录
        /// </summary>
        /// <param name="mns"></param>
        /// <returns></returns>
        public void DBDataRtInit(string mns)
        {
            try
            { 
                string sql = "INSERT INTO `n_data_rt`(`device_id`," +
                    "`status`,`dtime`,`st`,`qn`,`datatime`,`data`,`dflag`," +
                    "`A01`,`A02`,`A03`,`A04`,`A05`," +
                    "`A06`,`A07`,`A08`,`A09`,`A10`," +
                    "`A11`,`A12`,`A13`,`A14`,`A15`," +
                    "`A16`,`A17`,`A18`,`A19`,`A20`) " +
                    "SELECT `n_device`.`device_id`," +
                    "0,now(),0,'1970-01-01 00:00:00','1970-01-01 00:00:00','',0," +
                    "0.0,0.0,0.0,0.0,0.0," +
                    "0.0,0.0,0.0,0.0,0.0," +
                    "0.0,0.0,0.0,0.0,0.0," +
                    "0.0,0.0,0.0,0.0,0.0 " +
                    "FROM `n_device` LEFT JOIN `n_data_rt` " +
                    "ON (`n_device`.`device_id`=`n_data_rt`.`device_id`) " +
                    "WHERE `n_device`.`mn` IN(" + mns + ") " +
                    "AND `n_data_rt`.`data_rt_id` is null";
                exec_update_(sql);
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        /// <summary>
        /// 批量插入实时数据缓存，提高效率
        /// </summary>
        /// <param name="listPackage"></param>
        public void DBInsertDataRt(List<CPackage_HJ212> listPackage)
        {
            try
            {
                if (listPackage.Count <= 0) return;

                StringBuilder sb = new();

                sb.Append("INSERT INTO `n_data_rt_cache`(`device_id`," +
                    "`status`,`dtime`," +
                    "`st`,`qn`,`datatime`,`data`,`dflag`," +
                    "`A01`,`A02`,`A03`,`A04`,`A05`," +
                    "`A06`,`A07`,`A08`,`A09`,`A10`," +
                    "`A11`,`A12`,`A13`,`A14`,`A15`," +
                    "`A16`,`A17`,`A18`,`A19`,`A20`) VALUES");

                foreach (CPackage_HJ212 pack in listPackage)
                {
                    sb.Append('(').Append(pack.DeviceId)
                        .Append(",1,now(),'")
                        .Append(pack.ST).Append("','")
                        .Append(pack.QN.ToString("yyyy-MM-dd HH:mm:ss")).Append("','")
                        .Append(pack.DataTime.ToString("yyyy-MM-dd HH:mm:ss")).Append("','")
                        .Append(pack.FormatDataJson()).Append("',0,")
                        .Append(
                        "0.0,0.0,0.0,0.0,0.0," +
                        "0.0,0.0,0.0,0.0,0.0," +
                        "0.0,0.0,0.0,0.0,0.0," +
                        "0.0,0.0,0.0,0.0,0.0),");
                }

                sb.Remove(sb.Length - 1, 1);
                exec_update_(sb.ToString());
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        /// <summary>
        /// 更新缓存中的实时数据
        /// 
        /// </summary>
        public void DBUpdateDataRt()
        {
            try
            {
                // 第一步先变更标记
                // 再更新缓存数据，不用考虑重复
                // 删除缓存

                // 多条同时执行
                string sql = "UPDATE `n_data_rt_cache` SET `dflag`=1 WHERE `data_rt_id`>0;" +
                    "UPDATE `n_data_rt` INNER JOIN `n_data_rt_cache` ON (`n_data_rt`.`device_id`=`n_data_rt_cache`.`device_id`) " +
                    "SET `n_data_rt`.`status`=`n_data_rt_cache`.`status`," +
                    "`n_data_rt`.`dtime`=`n_data_rt_cache`.`dtime`," +
                    "`n_data_rt`.`st`=`n_data_rt_cache`.`st`," +
                    "`n_data_rt`.`qn`=`n_data_rt_cache`.`qn`," +
                    "`n_data_rt`.`datatime`=`n_data_rt_cache`.`datatime`," +
                    "`n_data_rt`.`data`=`n_data_rt_cache`.`data`," +
                    "`n_data_rt`.`A01`=`n_data_rt_cache`.`A01`," +
                    "`n_data_rt`.`A02`=`n_data_rt_cache`.`A02`," +
                    "`n_data_rt`.`A03`=`n_data_rt_cache`.`A03`," +
                    "`n_data_rt`.`A04`=`n_data_rt_cache`.`A04`," +
                    "`n_data_rt`.`A05`=`n_data_rt_cache`.`A05`," +
                    "`n_data_rt`.`A06`=`n_data_rt_cache`.`A06`," +
                    "`n_data_rt`.`A07`=`n_data_rt_cache`.`A07`," +
                    "`n_data_rt`.`A08`=`n_data_rt_cache`.`A08`," +
                    "`n_data_rt`.`A09`=`n_data_rt_cache`.`A09`," +
                    "`n_data_rt`.`A10`=`n_data_rt_cache`.`A10`," +
                    "`n_data_rt`.`A11`=`n_data_rt_cache`.`A11`," +
                    "`n_data_rt`.`A12`=`n_data_rt_cache`.`A12`," +
                    "`n_data_rt`.`A13`=`n_data_rt_cache`.`A13`," +
                    "`n_data_rt`.`A14`=`n_data_rt_cache`.`A14`," +
                    "`n_data_rt`.`A15`=`n_data_rt_cache`.`A15`," +
                    "`n_data_rt`.`A16`=`n_data_rt_cache`.`A16`," +
                    "`n_data_rt`.`A17`=`n_data_rt_cache`.`A17`," +
                    "`n_data_rt`.`A18`=`n_data_rt_cache`.`A18`," +
                    "`n_data_rt`.`A19`=`n_data_rt_cache`.`A19`," +
                    "`n_data_rt`.`A20`=`n_data_rt_cache`.`A20` " +
                    "WHERE `n_data_rt_cache`.`dflag`=1;" +
                    "DELETE FROM `n_data_rt_cache` WHERE `data_rt_id`>0;";
                exec_update_(sql);
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        public void DBInsertHis(string tableName, List<CPackage_HJ212> listPackage)
        {
            try
            {
                if (listPackage.Count <= 0) return;

                StringBuilder sb = new();

                sb.Append("INSERT INTO `").Append(tableName).Append("`(`device_id`," +
                    "`dtime`,`st`,`qn`,`datatime`,`data`,`dflag`) VALUES");

                foreach (CPackage_HJ212 pack in listPackage)
                {
                    sb.Append('(').Append(pack.DeviceId)
                        .Append(",now(),'")
                        .Append(pack.ST).Append("','")
                        .Append(pack.QN.ToString("yyyy-MM-dd HH:mm:ss")).Append("','")
                        .Append(pack.DataTime.ToString("yyyy-MM-dd HH:mm:ss")).Append("','")
                        .Append(pack.FormatDataJson()).Append("',0),");
                }

                sb.Remove(sb.Length - 1, 1);
                exec_update_(sb.ToString());
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }
    }
}
