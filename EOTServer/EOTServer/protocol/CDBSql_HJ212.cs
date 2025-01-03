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
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
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
        /// 将json格式输出为yml格式
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ConfigDataDecode(string str)
        {
            Dictionary<string, object?>? dic = JsonSerializer.Deserialize<Dictionary<string, object?>>(str);
            if (dic == null)
            {
                cls_log.get_default_().T_("", "版本序列化失败 " + str);
                return "";
            }

            StringBuilder sb = new();
            foreach (var pvs in dic)
            {
                // 特殊标识不处理
                if (pvs.Key[0] == '_') continue;

                sb.Append(pvs.Key).Append('=').Append(cls_core.o2str_(pvs.Value)).Append(';');
            }

            return sb.ToString();
        }

        /// <summary>
        /// 将yml格式转换为json，同时输出字典表
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ConfigDataEncode(string str)
        {
            str = str.Replace("\n", "");
            string[] ss = str.Split(';');

            string sKey, sVal;
            string[] ts;

            Dictionary<string, string> dic = new();

            foreach (string s in ss)
            {
                ts = s.Split('=');
                if (ts.Length < 2) continue;
                
                sKey = ts[0].Trim();
                sVal = ts[1].Trim();


                // 特殊标识不序列化
                if (sKey[0] != '_')
                {
                    if (dic.ContainsKey(sKey))
                        dic[sKey] = sVal;
                    else
                        dic.Add(sKey, sVal);
                }
            }

            return JsonSerializer.Serialize(dic, new JsonSerializerOptions()
            {
                //Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }

        /// <summary>
        /// 根据mn批量获取id
        /// </summary>
        /// <param name="mns"></param>
        public Dictionary<string, int> DBLoadDeviceIds(string mns)
        {
            Dictionary<string, int> dic = new();

            // 使用##lstr##
            try
            {
                cls_result queryResult = new();
                script_(queryResult, "db_load_device_ids", new()
                {
                    { "##lstr##v_mns", mns },
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
            // 使用##lstr##
            try
            {
                cls_result queryResult = new();
                script_(queryResult, "db_data_rt_insert", new()
                {
                    { "##lstr##v_mns", mns },
                });
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }


        /// <summary>
        /// 通过设备id从数据库中读取设备配置
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
        /// 通过mn从数据库中读取设备配置
        /// </summary>
        /// <param name="mn"></param>
        /// <returns></returns>
        public string DBLoadDeviceConfigs(string mn)
        {
            try
            {
                cls_result queryResult = new();
                script_(queryResult, "db_load_device_configs", new()
                {
                    { "v_mn",  mn},
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
        /// <param name="deviceId"></param>
        /// <param name="dkey"></param>
        /// <param name="dtype"></param>
        /// <param name="dversion"></param>
        /// <param name="cpStr"></param>
        public void DBUpdateDeviceConfig(int deviceId, string dkey, string dtype, string dversion, string cpStr)
        {
            try
            {
                cls_result_obj dic = new();
                string sJson = ConfigDataEncode(cpStr);
                
                cls_result queryResult = new();
                script_(queryResult, "db_save_device_config", new()
                {
                    { "v_device_id",  deviceId },
                    { "v_dkey",  dkey },
                    { "v_dtype",  dtype },
                    { "v_dversion",  dversion },
                    { "v_config_data",  sJson},
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
