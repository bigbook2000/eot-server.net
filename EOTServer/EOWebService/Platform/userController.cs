using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Ocsp;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using WAIotServer.Common;
using WAIotServer.Logic;
using static Google.Protobuf.Reflection.SourceCodeInfo.Types;

namespace WAIotServer.Platfrom
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class userController : ControllerBase
    {
        private string LoadUserPermits(int userId, string roleIds)
        {
            StringBuilder sb = new ();
            cls_result cQuery = new();

            // 角色不能为空
            if (roleIds.Length == 0) return "";

            // 获取权限
            string perimtIds = "";
            if (CGlobal.RootId != userId)
            {
                CGlobal.DBScript.script_(cQuery, "eopx_user_permits", new()
                {
                    { "v_role_ids", roleIds }
                });

                foreach (cls_result_obj d in cQuery._list)
                {
                    string sp = d.to_string_("f_permits");
                    sb.Append(',').Append(sp);
                }

                if (sb.Length > 0)
                {
                    sb.Remove(0, 1);

                    // 去重
                    string[] ss = sb.ToString().Split(',');
                    Array.Sort(ss);

                    int i;
                    sb.Clear();
                    sb.Append(ss[0]);
                    for (i = 1; i < ss.Length; i++)
                    {
                        if (ss[i] == ss[i - 1]) continue;
                        sb.Append(',').Append(ss[i]);
                    }

                    perimtIds = sb.ToString();
                }

                // 如果是普通用户不允许为空
                if (perimtIds.Length == 0) return "";
            }

            CGlobal.DBScript.script_(cQuery, "eopx_permit_list", new()
            {
                { "v_permit_ids", perimtIds }
            });

            sb.Clear();
            // 前后都加上分割符，便于查找
            sb.Append(',');
            foreach (cls_result_obj d in cQuery._list)
            {
                sb.Append(d.to_string_("f_name")).Append(',');
            }
                
            return sb.ToString();
        }

        // [EnableCors("CORS")]
        [HttpPost]
        public JsonResult login(cls_result args)
        {
            cls_result cResult = new ();

            try
            {
                cls_result_obj data = args.default_();

                string loginId = data.to_string_("login_id");
                string loginPsw = data.to_string_("login_psw");

                loginPsw = CGlobal.EncryptPassword(loginPsw, loginId, false);

                // aaaaa
                // 594f803b380a41396ed63dca39503542
                // __EOApp@2023#
                // root!__EOService@2023*594f803b380a41396ed63dca39503542
                // 5bea4ce7453e8f8237129cb4e0fd7d1a
                CGlobal.DBScript.script_(cResult, "eopx_user_check", new()
                {
                    { "v_login_id",  loginId },
                    { "v_login_psw", loginPsw }
                });
                if (!cResult.is_success_()) return new JsonResult(cResult);

                // 需要校验部门是否存在

                cls_result_obj userData = cResult.default_();
                int userId = userData.to_int_("f_user_id");
                int deptId = userData.to_int_("f_dept_id");

                eow_session_item sessionItem =
                    eow_session.handle_().create_(userId, deptId, userData, CGlobal.SessionTimeout);
                cResult.set_token_(sessionItem._session_id);

                string permits = LoadUserPermits(userId, userData.to_string_("f_role"));
                userData.Add("f_permits", permits);
            }
            catch (Exception ex)
            {
                cResult.set_except_(ex);
                cls_log.get_default_().T_("", ex.ToString());
            }

            return new JsonResult(cResult);
        }

        [HttpPost]
        public JsonResult logout(cls_result args)
        {
            eow_session.handle_().remove_(Request);
            return new JsonResult(new cls_result());
        }

        [HttpPost]
        public JsonResult query(cls_result args)
        {
            cls_result cResult = new ();

            try
            {
                cls_result_obj data = args.default_();

                eow_session_item? sessionItem = eow_session.handle_().get_(Request);
                if (sessionItem == null)
                {
                    cResult.set_error_("验证失效");
                    return new JsonResult(cResult);
                }

                CGlobal.DBScript.script_(cResult, "eopx_user_query", new()
                {
                    { "v_dept_id", sessionItem._dept_id },
                    { "v_login_id", data.to_string_("login_id") },
                    { "v_name", data.to_string_("name") },
                    { "v_phone", data.to_string_("phone") },
                });
            }
            catch (Exception ex)
            {
                cResult.set_except_(ex);
                cls_log.get_default_().T_("", ex.ToString());
            }

            return new JsonResult(cResult);
        }

        /// <summary>
        /// 编辑用户信息
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [eow_permit("platform.user.upd")]
        [HttpPost]
        public JsonResult upd(cls_result args)
        {
            cls_result cResult = new();

            try
            {
                cls_result_obj data = args.default_();

                int userId = data.to_int_("f_user_id");
                if (userId == CGlobal.RootId)
                {
                    cResult.set_error_("无法修改根用户");
                    return new JsonResult(cResult);
                }

                string loginId = data.to_string_("f_login_id");
                string loginPsw = "";

                if (userId <= 0)
                {
                    loginPsw = CGlobal.EncryptPassword(CGlobal.DefaultPassword, loginId, true);
                }

                CGlobal.DBScript.script_(cResult, "eopx_user_upd", new()
                {
                    { "v_user_id", userId },
                    { "v_login_id", loginId },
                    { "v_login_psw", loginPsw },
                    { "v_name", data.to_string_("f_name") },
                    { "v_dept_id", data.to_int_("f_dept_id") },
                    { "v_role", data.to_string_("f_role") },
                    { "v_sex", data.to_string_("f_sex") },
                    { "v_phone", data.to_string_("f_phone") },                    
                    { "v_location", data.to_string_("f_location") },
                    { "v_status", data.to_int_("f_status") },
                    { "v_note", data.to_string_("f_note") },
                    { "v_data_ex", data.to_string_("f_data_ex") },
                });
            }
            catch (Exception ex)
            {
                cResult.set_except_(ex);
                cls_log.get_default_().T_("", ex.ToString());
            }

            return new JsonResult(cResult);
        }

        [eow_permit("platform.user.upd")]
        [HttpPost]
        public JsonResult del(cls_result args)
        {
            cls_result cResult = new();

            try
            {
                cls_result_obj data = args.default_();
                int userId = data.to_int_("user_id");
                if (userId == CGlobal.RootId)
                {
                    cResult.set_error_("无法删除根用户");
                    return new JsonResult(cResult);
                }

                eow_session_item? sessionItem = eow_session.handle_().get_(Request);
                if (sessionItem == null)
                {
                    cResult.set_error_("验证失效");
                    return new JsonResult(cResult);
                }

                if (userId == sessionItem._user_id)
                {
                    cResult.set_error_("无法删除自己");
                    return new JsonResult(cResult);
                }

                CGlobal.DBScript.script_(cResult, "eopx_user_del", new()
                {
                    { "v_user_id", data.to_int_("user_id") },
                });

            }
            catch (Exception ex)
            {
                cResult.set_except_(ex);
                cls_log.get_default_().T_("", ex.ToString());
            }

            return new JsonResult(cResult);
        }

        /// <summary>
        /// 修改自身密码
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [eow_permit("platform.user.upd")]
        [HttpPost]
        public JsonResult password(cls_result args)
        {
            cls_result cResult = new();

            try
            {
                cls_result_obj data = args.default_();
                int userId = data.to_int_("user_id");
                string loginId = data.to_string_("login_id");
                string loginPswOld = data.to_string_("login_psw_old");
                string loginPswNew = data.to_string_("login_psw_new");

                eow_session_item? sessionItem = eow_session.handle_().get_(Request);
                if (sessionItem == null)
                {
                    cResult.set_error_("验证失效");
                    return new JsonResult(cResult);
                }

                if (userId != sessionItem._user_id)
                {
                    cResult.set_error_("无法修改其他用户密码");
                    return new JsonResult(cResult);
                }

                if (loginPswOld.Length <= 0 || loginPswNew.Length <= 0)
                {
                    cResult.set_error_("密码不能为空");
                    return new JsonResult(cResult);
                }

                loginPswOld = CGlobal.EncryptPassword(loginPswOld, loginId, false);
                loginPswNew = CGlobal.EncryptPassword(loginPswNew, loginId, false);

                cResult = CGlobal.DBScript.call_query_("eopx_user_password", new()
                {
                    { "v_user_id", userId },
                    { "v_login_psw_old", loginPswOld },
                    { "v_login_psw_new", loginPswNew },
                });
            }
            catch (Exception ex)
            {
                cResult.set_except_(ex);
                cls_log.get_default_().T_("", ex.ToString());
            }

            return new JsonResult(cResult);
        }


        /// <summary>
        /// 重置他人密码
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [eow_permit("platform.user.upd")]
        [HttpPost]
        public JsonResult reset(cls_result args)
        {
            cls_result cResult = new();

            try
            {
                cls_result_obj data = args.default_();
                int userId = data.to_int_("user_id");
                string loginId = data.to_string_("login_id");

                if (userId == CGlobal.RootId)
                {
                    cResult.set_error_("无法重置根用户密码");
                    return new JsonResult(cResult);
                }

                string loginPswNew = CGlobal.EncryptPassword(CGlobal.DefaultPassword, loginId, false);

                cResult = CGlobal.DBScript.call_query_("eopx_user_password", new()
                {
                    { "v_user_id", userId },
                    { "v_login_psw_old", "" },
                    { "v_login_psw_new", loginPswNew },
                });
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
