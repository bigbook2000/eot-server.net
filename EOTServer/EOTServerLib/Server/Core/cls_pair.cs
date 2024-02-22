using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.eobject.iot.Server.Core
{
    /// <summary>
    /// 键值对
    /// </summary>
    public class cls_pair<TKey, TValue>
    {
        public TKey _key;
        public TValue _val;
        public cls_pair()
        {
            _key = Activator.CreateInstance<TKey>();
            _val = Activator.CreateInstance<TValue>();
        }
        public cls_pair(TKey key, TValue val)
        {
            _key = key;
            _val = val;
        }
    }
}
