using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace cn.eobject.iot.Server.Core
{
    public class cls_result_obj : Dictionary<string, object?>
    {
        public cls_result_obj()
        {
        }

        public string? get_string_(string key)
        {
            if (!ContainsKey(key)) return null;

            object? val = this[key];
            if (val == null) return null;

            if (val is not string) return null;

            return (string)val;
        }
        public int? get_int_(string key)
        {
            if (!ContainsKey(key)) return null;

            object? val = this[key];
            if (val == null) return null;

            if (val is not int) return null;

            return (int)val;
        }
        public double? get_double_(string key)
        {
            if (!ContainsKey(key)) return null;

            object? val = this[key];
            if (val == null) return null;

            if (val is not double) return null;

            return (double)val;
        }
        public DateTime? get_date_(string key)
        {
            if (!ContainsKey(key)) return null;

            object? val = this[key];
            if (val == null) return null;

            if (val is not DateTime) return null;

            return (DateTime)val;
        }
        public string to_string_(string key)
        {
            if (!ContainsKey(key)) return "";

            string? val = this[key]?.ToString();
            if (val == null) return "";

            return val;
        }
        public int to_int_(string key)
        {
            if (!ContainsKey(key)) return 0;

            string? str = this[key]?.ToString();
            if (int.TryParse(str, out int val)) return val;

            return 0;
        }
        public double to_double_(string key)
        {
            if (!ContainsKey(key)) return 0.0;

            string? str = this[key]?.ToString();
            if (double.TryParse(str, out double val)) return val;

            return 0.0;
        }
        public DateTime to_date_(string key)
        {
            if (!ContainsKey(key)) return new(1970, 1, 1, 0, 0, 0, 0);

            string? str = this[key]?.ToString();
            if (DateTime.TryParse(str, out DateTime val)) return val;

            return new(1970, 1, 1, 0, 0, 0, 0);
        }

        public cls_result_obj to_object_(string key)
        {
            if (!ContainsKey(key)) return new cls_result_obj();

            cls_result_obj? obj = (cls_result_obj?) this[key];
            if (obj == null) return new cls_result_obj();

            return obj;
        }

        /// <summary>
        /// 转换动态对象
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public cls_result_obj to_dynamic_(string key)
        {
            if (!ContainsKey(key)) return new cls_result_obj();

            cls_result_obj cObj = new();

            object? obj = this[key];
            if (obj == null) return cObj;

            PropertyInfo[] pis = obj.GetType().GetProperties();
            foreach (PropertyInfo pi in pis)
            {
                cObj.Add(pi.Name, pi.GetValue(obj, null));
            }
            
            return cObj;
        }

        public cls_result_obj to_json_(string key)
        {
            if (!ContainsKey(key)) return new cls_result_obj();

            cls_result_obj cObj = new();

            JsonElement? json = (JsonElement?)this[key];
            if (json == null) return cObj;

            foreach (JsonProperty p in json.Value.EnumerateObject())
            {
                JsonElement el = p.Value;
                if (el.ValueKind == JsonValueKind.String)
                    cObj.Add(p.Name, el.GetString());
                else
                    cObj.Add(p.Name, el.GetRawText());
            }

            return cObj;
        }


        public List<cls_result_obj> to_list_(string key)
        {
            if (!ContainsKey(key)) return new List<cls_result_obj>();

            List<cls_result_obj>? list = (List<cls_result_obj>?)this[key];
            if (list == null) return new List<cls_result_obj>();

            return list;
        }
    }
}
