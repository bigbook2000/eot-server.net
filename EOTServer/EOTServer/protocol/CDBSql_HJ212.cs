using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.DB;
using cn.eobject.iot.Server.DB.Ext;
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
    public class CDBSql_HJ212 : cls_eotsql
    {

        public CDBSql_HJ212(long slowDelay, string sqlTest) : base(slowDelay)
        {
            if (sqlTest.Length > 0)
            {
                // 测试数据库
                //cls_log.get_default_().T_("", "测试数据库" + sqlTest);
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
                cls_result queryResult = new();
                script_(queryResult, "db_load_device_ids", new()
                {
                    { "v_mns", mns },
                });

                foreach (cls_result_obj obj in queryResult._list)
                {
                    dic.TryAdd(obj.to_string_("f_mn"), obj.to_int_("f_device_id"));
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
                cls_result queryResult = new();
                script_(queryResult, "db_data_rt_insert", new()
                {
                    { "v_mns", mns },
                });
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }


        /// <summary>
        /// 从数据库中读取设备配置
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public string DBLoadDeviceConfig(int deviceId)
        {
            try
            {
                cls_result queryResult = new();
                script_(queryResult, "db_load_device_config", new()
                {
                    { "v_device_id",  deviceId},
                });

                if (!queryResult.is_success_()) return "";
                string? str = (string?)queryResult.get_scalar();
                str ??= "";

                return str;
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }

            return "";
        }
        /// <summary>
        /// 从数据库中读取版本配置
        /// </summary>
        /// <param name="versionSign"></param>
        /// <returns></returns>
        public string DBLoadVersionConfig(string versionSign)
        {
            try
            {
                cls_result queryResult = new();
                script_(queryResult, "db_load_version_config", new()
                {
                    { "v_sign",  versionSign},
                }); 
                
                if (!queryResult.is_success_()) return "";
                string? str = (string?)queryResult.get_scalar();
                str ??= "";

                return str;
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }

            return "";
        }

        /// <summary>
        /// 更新设备配置
        /// </summary>
        /// <param name="pack"></param>
        public void DBUpdateDeviceConfig(CPackage_HJ212 pack)
        {
            try
            {
                string? cpStr = pack.ParseCP();
                if (cpStr == null) cpStr = "";

                //StringBuilder sb = new();

                //// 先更新设备信息
                //sb.Append("UPDATE `n_device` SET ")
                //    .Append("`dkey`='").Append(pack.GetCPString("DKey"))
                //    .Append("',`dtype`='").Append(pack.GetCPString("DType"))
                //    .Append("',`dversion`='").Append(pack.GetCPString("DVersion"))
                //    .Append("' WHERE `device_id`=").Append(pack.DeviceId);

                //sb.Append(";DELETE FROM `n_config` WHERE `device_id`=").Append(pack.DeviceId);

                //// 再存储配置信息
                //sb.Append(";INSERT INTO `n_config`(`device_id`,`config_data`,`ctime`) VALUES(")
                //    .Append(pack.DeviceId).Append(",'").Append(cpStr).Append("',now());");



                cls_result queryResult = new();
                script_(queryResult, "db_save_device_config", new()
                {
                    { "v_device_id",  pack.DeviceId },
                    { "v_dkey",  pack.GetCPString("DKey") },
                    { "v_dtype",  pack.GetCPString("DType") },
                    { "v_dversion",  pack.GetCPString("DVersion") },
                    { "v_config_data",  cpStr},
                });
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

                List<Dictionary<string, object>> listPack = new();
                Dictionary<string, object> dicPack;
                foreach (CPackage_HJ212 pack in listPackage)
                {
                    dicPack = new()
                    {
                        { "v_device_id", pack.DeviceId },
                        { "v_st", pack.ST },
                        { "v_qn", pack.QN.ToString("yyyy-MM-dd HH:mm:ss") },
                        { "v_datatime", pack.DataTime.ToString("yyyy-MM-dd HH:mm:ss") },
                        { "v_data_json", pack.FormatDataJson() }
                    };
                    listPack.Add(dicPack);
                }

                cls_result queryResult = new();
                script_(queryResult, "db_data_rt_cache_insert", new()
                {
                    { "v_pack",  listPack},
                });
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        /// <summary>
        /// 更新缓存中的实时数据
        /// 单线程执行
        /// </summary>
        public void DBUpdateDataRt()
        {
            try
            {
                // 第一步先变更标记
                // 再更新缓存数据，不用考虑重复
                // 删除缓存

                cls_result queryResult = new();
                script_(queryResult, "db_data_rt_update", new());
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

                List<Dictionary<string, object>> listPack = new();
                Dictionary<string, object> dicPack;
                foreach (CPackage_HJ212 pack in listPackage)
                {
                    dicPack = new()
                    {
                        { "v_device_id", pack.DeviceId },
                        { "v_st", pack.ST },
                        { "v_qn", pack.QN.ToString("yyyy-MM-dd HH:mm:ss") },
                        { "v_datatime", pack.DataTime.ToString("yyyy-MM-dd HH:mm:ss") },
                        { "v_data_json", pack.FormatDataJson() }
                    };
                    listPack.Add(dicPack);
                }

                cls_result queryResult = new();
                script_(queryResult, "db_data_his_insert", new()
                {
                    { "v_table", tableName },
                    { "v_pack",  listPack },
                });
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }
    }
}
