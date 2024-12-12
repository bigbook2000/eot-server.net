using System.Collections;
using System.Text;
using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;

namespace cn.eobject.iot.Server.Config
{
    /// <summary>
    /// yaml简化格式，utf8
    /// </summary>
    public class cls_config
    {
        /// <summary>
        /// 配置文件路径
        /// </summary>
        public string _file_path = "";
        /// <summary>
        /// 配置根节点
        /// </summary>
        public cls_config_obj _root = new ("_root_", "");

        /// <summary>
        /// 根节点（空节点）
        /// </summary>
        /// <returns></returns>
        public cls_config_obj root_()
        {
            return _root;
        }
        /// <summary>
        /// 加载一个yml文件
        /// </summary>
        /// <param name="filePath"></param>
        public void load_(string filePath)
        {
            string? sLine;

            cls_config_obj lastObject = _root;
            cls_config_obj newObject;
            cls_config_obj? parentObject;

            List<cls_config_obj> listComment = new();

            try
            {
                _file_path = filePath;

                using StreamReader sr = new(filePath, Encoding.UTF8);

                while (!sr.EndOfStream)
                {
                    sLine = sr.ReadLine();
                    if (sLine == null) break;

                    newObject = new cls_config_obj();
                    if (newObject.parse_(sLine) < 0) continue;

                    if (newObject.is_comment_())
                    {
                        listComment.Add(newObject);
                    }
                    else
                    {
                        parentObject = lastObject.get_parent_(newObject._level);

                        // 处理注释，添加到下一节点之前
                        foreach (cls_config_obj obj in listComment)
                        {
                            obj.set_parent_(parentObject);
                        }
                        listComment.Clear();

                        newObject.set_parent_(parentObject);
                        lastObject = newObject;
                    }
                }

                // 后面的注释添加到最后
                foreach (cls_config_obj obj in listComment)
                {
                    obj.set_parent_(_root);
                }

                sr.Close();
                sr.Dispose();
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }
        /// <summary>
        /// 写入一行配置项
        /// </summary>
        /// <param name="sw">文件流</param>
        /// <param name="configObj">配置节点对象</param>
        private void write_config_(StreamWriter sw, cls_config_obj configObj)
        {
            string sLine;
            // 根节点不写入
            if (configObj._parent != null)
            {
                if (configObj.is_comment_())
                {
                    sw.WriteLine(configObj._val);
                    return;
                }

                sLine = new string(' ', configObj._level - 1) + configObj._key + ": " + configObj._val;
                sw.WriteLine(sLine);
            }

            foreach (cls_config_obj obj in configObj._list)
            {
                write_config_(sw, obj);
            }
        }
        /// <summary>
        /// 保存yml配置文件
        /// 可以保留注释
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public void save_(string filePath)
        {
            try
            {
                if (filePath.Length <= 0) filePath = _file_path;

                // 先备份
                if (File.Exists(filePath))
                {
                    Random rnd = new();
                    string fileCopy = _file_path +
                        DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") +
                        rnd.Next(1000).ToString("000") +
                        ".txt";
                    File.Move(filePath, fileCopy);
                }

                using StreamWriter sw = new(filePath, false, Encoding.UTF8);

                write_config_(sw, _root);

                sw.Close();
                sw.Dispose();
            }
            catch (Exception ex)
            {
                cls_log.get_default_().T_("", ex.ToString());
            }
        }

        /// <summary>
        /// 多层级获取string
        /// </summary>
        /// <param name="keyPath">多级节点项目名 xxx/../xxx </param>
        /// <returns></returns>
        public string get_string_(string keyPath)
        {
            string[] keyList = keyPath.Split('/');

            cls_config_obj? configObj = _root;
            foreach (string key in keyList)
            {
                configObj = configObj.get_(key);
                if (configObj == null) return "";
            }

            return configObj._val;
        }
        /// <summary>
        /// 设置多层级配置项string
        /// </summary>
        /// <param name="keyPath">多级节点项目名 xxx/../xxx </param>
        /// <param name="val">项目值</param>
        /// <returns></returns>
        public bool set_string_(string keyPath, string val)
        {
            string[] keyList = keyPath.Split('/');

            cls_config_obj? configObj = _root;
            foreach (string key in keyList)
            {
                configObj = configObj.get_(key);
                if (configObj == null) return false;
            }

            configObj._val = val;

            return true;
        }
        /// <summary>
        /// 多层级获取int
        /// </summary>
        /// <param name="keyPath">多级节点项目名 xxx/../xxx </param>
        /// <returns></returns>
        public int get_int32_(string keyPath)
        {
            if (int.TryParse(get_string_(keyPath), out var val))
            {
                return val;
            }

            return 0;
        }
        /// <summary>
        /// 设置多层级配置项int
        /// </summary>
        /// <param name="keyPath">多级节点项目名 xxx/../xxx </param>
        /// <param name="val">项目值</param>
        /// <returns></returns>
        public bool set_int32_(string keyPath, int val)
        {            
            return set_string_(keyPath, val.ToString());
        }
        /// <summary>
        /// 多层级获取double
        /// </summary>
        /// <param name="keyPath">多级节点项目名 xxx/../xxx </param>
        /// <returns></returns>
        public double get_double_(string keyPath)
        {
            if (double.TryParse(get_string_(keyPath), out var val))
            {
                return val;
            }

            return 0.0;
        }
        /// <summary>
        /// 设置多层级配置项double
        /// </summary>
        /// <param name="keyPath">多级节点项目名 xxx/../xxx </param>
        /// <param name="val">项目值</param>
        /// <returns></returns>
        public bool set_double_(string keyPath, int val)
        {
            return set_string_(keyPath, val.ToString());
        }

        /// <summary>
        /// 多层级获取所有子节点
        /// </summary>
        /// <param name="keyPath">多级节点项目名 xxx/../xxx </param>
        /// <returns></returns>
        public List<cls_config_obj> get_childs_(string keyPath)
        {
            string[] keyList = keyPath.Split('/');

            cls_config_obj? configObj = _root;
            foreach (string key in keyList)
            {
                configObj = configObj.get_(key);
                if (configObj == null) return new List<cls_config_obj>();
            }

            return configObj._list;
        }
    }
}
