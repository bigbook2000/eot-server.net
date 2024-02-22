using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Net.NetworkInformation;
using System.Text;
using WAIotServer.Logic;

namespace WAIotServer.Platform
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class deptController : ControllerBase
    {
        /// <summary>
        /// 增加部门索引
        /// </summary>
        /// <param name="deptId"></param>
        /// <param name="parentId"></param>
        private void AddDeptIndex(int deptId, int parentId)
        {
            // 更新索引，只需将父节点的索引都添加到新加的节点即可
            cls_result cQuery = CGlobal.IotDB.exec_query_(
                "SELECT * FROM `eox_dept_index` WHERE `dept_id`=" + parentId);

            // 自己也要加入索引
            _ = CGlobal.IotDB.exec_update_(
                "INSERT INTO `eox_dept_index`(`dept_id`,`dept_pid`) VALUES(" + deptId + "," + deptId + ")");

            foreach (cls_result_obj d in cQuery._list)
            {
                parentId = d.to_int_("dept_pid");
                _ = CGlobal.IotDB.exec_update_(
                    "INSERT INTO `eox_dept_index`(`dept_id`,`dept_pid`) VALUES(" + deptId + "," + parentId + ")");
            }
        }

        [HttpPost]
        public JsonResult list(cls_result args)
        {
            cls_result_obj data = args.default_();
            cls_result cResult = CGlobal.IotDB.call_query_("eopx_dept_list", new()
            {
                { "v_dept_pid", data.to_int_("dept_pid") },
            });

            return new JsonResult(cResult);
        }

        [HttpPost]
        public JsonResult upd(cls_result args)
        {
            cls_result cResult = new ();

            try
            {
                cls_result_obj data = args.default_();
                int deptId = data.to_int_("dept_id");
                int deptPid = data.to_int_("dept_pid");

                cResult = CGlobal.IotDB.call_query_("eopx_dept_upd", new()
                {
                    { "v_dept_id", deptId },
                    { "v_dept_pid", deptPid },
                    { "v_level", data.to_int_("level") },
                    { "v_name", data.to_string_("name") },
                    { "v_address", data.to_string_("address") },
                    { "v_contact", data.to_string_("contact") },
                    { "v_phone", data.to_string_("phone") },
                    { "v_note", data.to_string_("note") },
                    { "v_status", data.to_int_("status") },
                });

                if (!cResult.is_success_())
                {
                    return new JsonResult(cResult);
                }

                if (deptId <= 0)
                {
                    // 新增
                    deptId = cResult.default_().to_int_("dept_id");
                    if (deptId > 0)
                    {
                        AddDeptIndex(deptId, deptPid);
                    }
                }
            } 
            catch (Exception ex)
            {
                cResult.set_except_(ex);
                cls_log.get_default_().T_("", ex.ToString());
            }

            return new JsonResult(cResult);
        }

        [HttpPost]
        public JsonResult del(cls_result args)
        {
            cls_result cResult = args;

            try
            {
                cls_result_obj data = args.default_();
                int deptId = data.to_int_("dept_id");

                cls_result cQuery = CGlobal.IotDB.exec_query_(
                    "SELECT * FROM `eox_dept_index` WHERE `dept_pid`=" + deptId);

                foreach (cls_result_obj d in cQuery._list)
                {
                    deptId = d.to_int_("dept_id");

                    // 移除节点
                    _ = CGlobal.IotDB.call_query_("eopx_dept_del", new()
                    {
                        { "v_dept_id", deptId },
                    });

                    // 移除节点下所有的索引
                    _ = CGlobal.IotDB.exec_update_(
                        "DELETE FROM `eox_dept_index` WHERE `dept_id`=" + deptId + " OR `dept_pid`=" + deptId);
                }

                cResult.set_success_();
            }
            catch (Exception ex)
            {
                cResult.set_except_(ex);
                cls_log.get_default_().T_("", ex.ToString());
            }

            return new JsonResult(cResult);
        }

        /// <summary>
        /// 变更父节点
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult parent(cls_result args)
        {
            cls_result cResult = args;

            try
            {
                cls_result_obj data = args.default_();
                int deptId = data.to_int_("dept_id");
                //int parentIdOld = data.to_int_("dept_pid_old");
                int parentIdNew = data.to_int_("dept_pid_new");

                cls_result cQuery = CGlobal.IotDB.exec_query_(
                    "SELECT * FROM `eox_dept_index` WHERE `dept_id`=" + parentIdNew + " AND `dept_pid`=" + deptId);
                if (cQuery._list.Any())
                {
                    cResult.set_error_("无法移动到子节点");
                    return new JsonResult(cResult);
                }

                cResult = CGlobal.IotDB.call_query_("eopx_dept_parent", new()
                {
                    { "v_dept_id", deptId },
                    { "v_dept_pid", parentIdNew },
                    { "v_level", data.to_int_("level") },
                });

                // 删除旧索引，仅仅向上删除
                _ = CGlobal.IotDB.exec_update_(
                    "DELETE FROM `eox_dept_index` WHERE `dept_id`=" + deptId);

                // 添加节点
                AddDeptIndex(deptId, parentIdNew);
            }
            catch (Exception ex)
            {
                cResult.set_except_(ex);
                cls_log.get_default_().T_("", ex.ToString());
            }

            return new JsonResult(cResult);
        }

        /// <summary>
        /// 重建索引
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult index(cls_result args)
        {
            cls_result cResult = args;

            try
            {
                cls_result cQuery = CGlobal.IotDB.call_query_("eopx_dept_list", new()
                {
                    { "v_dept_pid", 0 },
                });

                if (!cQuery.is_success_())
                {
                    return new JsonResult(cQuery);
                }

                foreach (cls_result_obj d1 in cQuery._list)
                {
                    d1.Add("parent", null);
                    d1.Add("children", new List<cls_result_obj>());
                }

                foreach (cls_result_obj d1 in cQuery._list)
                {
                    foreach (cls_result_obj d2 in cQuery._list)
                    {
                        if (d1.to_int_("dept_pid") == d2.to_int_("dept_id"))
                        {
                            d1["parent"] = d2;

                            List<cls_result_obj>? children = (List<cls_result_obj>?)d2["children"];
                            if (children == null)
                            {
                                children = new() { d1 };
                                d2.Add("children", children);
                            }
                            else
                            {
                                children.Add(d1);
                            }

                            break;
                        }
                    }
                }

                // 删除全部索引
                _ = CGlobal.IotDB.exec_update_(
                    "DELETE FROM `eox_dept_index` WHERE `dept_index_id`>0");

                int i;
                int deptId, deptPid;
                cls_result_obj? dp;
                foreach (cls_result_obj d1 in cQuery._list)
                {
                    deptId = d1.to_int_("dept_id");
                    _ = CGlobal.IotDB.exec_update_(
                        "INSERT INTO `eox_dept_index`(`dept_id`,`dept_pid`) VALUES(" + deptId + "," + deptId + ")");

                    dp = d1;
                    for (i = 0; i < 8; i++)
                    {
                        dp = (cls_result_obj?)dp["parent"];
                        if (dp == null) break;

                        deptPid = dp.to_int_("dept_id");
                        _ = CGlobal.IotDB.exec_update_(
                            "INSERT INTO `eox_dept_index`(`dept_id`,`dept_pid`) VALUES(" + deptId + "," + deptPid + ")");
                    }
                }

                cResult.set_success_();
            }
            catch (Exception ex)
            {
                cResult.set_except_(ex);
                cls_log.get_default_().T_("", ex.ToString());
            }

            return new JsonResult(cResult);
        }
    }
}
