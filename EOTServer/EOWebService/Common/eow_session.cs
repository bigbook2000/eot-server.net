using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace WAIotServer.Common
{
    public class eow_session
    {
        public const string SESSION_ID_CODE = "_##EO_SESSION__";
        public const int SESSION_CODE_LENGTH = 32;
        /// <summary>
        /// 会话检测时长，1分钟精度
        /// </summary>
        public const int TIMER_CHECK_DELAY = 60000;

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        private static eow_session __handle;
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        public static eow_session handle_()
        {
            return __handle;
        }

        private ConcurrentDictionary<string, eow_session_item> _map_session = new ();

        public eow_session()
        {
            // 仅仅初始化一次
            if (__handle != null)
            {
                return;
            }

            __handle = this;

            _ = new Timer(new TimerCallback(on_timer_check), null, 0, TIMER_CHECK_DELAY);
        }

        public eow_session_item create_(int userId, int deptId, object userData, int delayMs)
        {
            Random rnd = new ();
            byte[] bytes = new byte[SESSION_CODE_LENGTH];
            rnd.NextBytes(bytes);
            string code = cls_core.format_bytes_(bytes, 0, bytes.Length);

            code = SESSION_ID_CODE + code + Guid.NewGuid().ToString();
            bytes = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(code));

            code = cls_core.format_bytes_(bytes, 0, bytes.Length);

            eow_session_item item = new()
            {
                _session_id = code,
                _user_id = userId,
                _dept_id = deptId,
                _user_data = userData,
                _tick_create = DateTime.Now.Ticks,
                _tick_update = DateTime.Now.Ticks,
                // 100纳秒
                _delay = delayMs * 1000 * 10,
            };

            _map_session.TryAdd(item._session_id, item);

            return item;
        }

        public void remove_(string sessionId)
        {
            _map_session.TryRemove(sessionId, out _);
        }

        public void remove_(HttpRequest request)
        {
            string? sessionId = request.Headers["Authorization"].Single();
            if (sessionId == null) return;

            _map_session.TryRemove(sessionId, out _);
        }

        /// <summary>
        /// 访问session，并更新时间
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public eow_session_item? get_(string sessionId)
        {
            if (_map_session.TryGetValue(sessionId, out var item))
            {
                item._tick_update = DateTime.Now.Ticks;
                return item;
            }

            return null;
        }

        public eow_session_item? get_(HttpRequest request)
        {
            string? sessionId = request.Headers["Authorization"].Single();
            if (sessionId == null) return null;

            if (_map_session.TryGetValue(sessionId, out var item))
            {
                item._tick_update = DateTime.Now.Ticks;
                return item;
            }

            return null;
        }

        private void on_timer_check(object? target)
        {
            try
            {
                //System.Diagnostics
                long tick = DateTime.Now.Ticks;

                List<eow_session_item> listRemove = new ();
                eow_session_item item;
                foreach (KeyValuePair<string, eow_session_item> kvp in _map_session)
                {
                    item = kvp.Value;
                    if (item.check_timeout_(tick)) listRemove.Add(item);
                }

                foreach (eow_session_item t in listRemove)
                {
                    _map_session.TryRemove(t._session_id, out _);
                }
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

    }
}
