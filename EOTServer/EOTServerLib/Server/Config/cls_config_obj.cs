using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.eobject.iot.Server.Config
{
    /// <summary>
    /// 配置节点对象
    /// 使用双列表一保证顺序，二提高检索效率
    /// 解析yaml
    /// 只支持单行注释
    /// 不支持内容分块--- ...
    /// 不支持json花括号
    /// </summary>
    public class cls_config_obj
    {
        /// <summary>
        /// 最大层级
        /// </summary>
        public const int MAX_LEVEL = 16;
        /// <summary>
        /// 用于给节点编号
        /// </summary>
        public static int GConfigObjID = 0;

        /// <summary>
        /// 层级，-1为注释
        /// </summary>
        public int _level = 0;
        /// <summary>
        /// 配置项目名
        /// </summary>
        public string _key = "";
        /// <summary>
        /// 配置项目值
        /// </summary>
        public string _val = "";

        /// <summary>
        /// 保持添加顺序
        /// </summary>
        public List<cls_config_obj> _list = new();
        /// <summary>
        /// 使用dic查找更快
        /// </summary>
        private Dictionary<string, cls_config_obj> _dic = new();

        /// <summary>
        /// 父节点
        /// </summary>
        public cls_config_obj? _parent = null;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public cls_config_obj()
        {
        }
        /// <summary>
        /// 使用键值对初始化构造
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public cls_config_obj(string key, string val)
        {
            _key = key;
            _val = val;
        }
        /// <summary>
        /// 通过key=value字符串行初始化构造
        /// </summary>
        /// <param name="lineString"></param>
        public cls_config_obj(string lineString)
        {
            parse_(lineString);
        }
        /// <summary>
        /// 是否是注释
        /// </summary>
        /// <returns></returns>
        public bool is_comment_()
        {
            return (_level <= 0);
        }
        /// <summary>
        /// 设置父节点，同时将自己添加到父节点中
        /// </summary>
        /// <param name="parent"></param>
        public void set_parent_(cls_config_obj? parent)
        {
            _parent = parent;
            if (_parent != null)
            {
                _parent._list.Add(this);
                _parent._dic.Add(_key, this);
            }
        }

        /// <summary>
        /// 获取指定层级的父节点
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public cls_config_obj? get_parent_(int level)
        {
            cls_config_obj? parentObj = this;

            int i;
            for (i=0; i< MAX_LEVEL; i++)
            {
                if (parentObj == null) return this;
                if (parentObj._level == level) return parentObj._parent;

                parentObj = parentObj._parent;
            }

            return this;
        }
        /// <summary>
        /// 根据名称获取指定的节点
        /// </summary>
        /// <param name="key">配置项目名称</param>
        /// <returns></returns>
        public cls_config_obj? get_(string key)
        {
            if (!_dic.ContainsKey(key)) return null;

            return _dic[key];
        }
        /// <summary>
        /// 根据名称查找所有子项
        /// </summary>
        /// <param name="key">配置项目名称</param>
        /// <returns></returns>
        public cls_config_obj? find_(string key)
        {
            cls_config_obj? cFind;

            if (_dic.ContainsKey(key)) return _dic[key];

            foreach (cls_config_obj obj in _list)
            {
                cFind = obj.find_(key);
                if (cFind != null) return cFind;
            }

            return null;
        }
        /// <summary>
        /// 解析单行字符串，创建节点
        /// 只支持单行注释
        /// 不支持内容分块--- ...
        /// 不支持json花括号
        /// </summary>
        /// <param name="lineString">字符串行</param>
        /// <returns>返回层级</returns>
        public int parse_(string lineString)
        {
            _level = 0;
            foreach (char c in lineString)
            {
                _level++;

                if (c != ' ')
                {
                    if (c == '#')
                    {
                        ++GConfigObjID;
                        _key = "_comment_" + GConfigObjID;
                        _val = lineString;
                        _level = 0;

                        return 0;
                    }

                    break;
                }
            }

            int nPos = lineString.IndexOf(':');
            if (nPos == -1) return -1;

            _key = lineString[..nPos].Trim();
            _val = lineString[(nPos + 1)..].Trim();

            return _level;
        }
    }
}
