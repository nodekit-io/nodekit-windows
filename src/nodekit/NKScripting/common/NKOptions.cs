using System;
using System.Collections.Generic;
using System.Text;

namespace io.nodekit
{
    public static class NKOptions
    {
        public static T itemOrDefault<T>(this IDictionary<String, object> options, string key, T defaultValue = default(T))
        {
            if (options.ContainsKey(key))
                return (T)options[key];
            else
                return defaultValue;
        }
    }
}
