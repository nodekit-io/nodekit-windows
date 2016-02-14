using System.Collections.Generic;
using Windows.Data.Json;

namespace io.nodekit.NKScripting
{
    public class NKData
    {
        public static string jsonSerialize(string obj)
        {
            return JsonValue.CreateStringValue((string)obj).Stringify();
        }

        public static object jsonDeserialize(string json)
        {
            var j = JsonValue.Parse(json);
            return _jsonDeserialize_convert(j);
        }

        private static object _jsonDeserialize_convert(IJsonValue json)
        {
            object obj = null;
            switch (json.ValueType)
            {
                case JsonValueType.Array:
                    JsonArray jsonArray = json.GetArray();
                    object[] objArray = new object[jsonArray.Count];
                    for (int i1 = 0; i1 < jsonArray.Count; i1++)
                    {
                        objArray[i1] = _jsonDeserialize_convert(jsonArray[i1]);
                    }
                    obj = objArray;
                    break;
                case JsonValueType.Boolean:
                    obj = json.GetBoolean();
                    break;
                case JsonValueType.Null:
                    obj = null;
                    break;
                case JsonValueType.Number:
                    obj = json.GetNumber();
                    break;
                case JsonValueType.Object:
                    JsonObject jsonObject = json.GetObject();

                    Dictionary<string, object> d = new Dictionary<string, object>();

                    List<string> keys = new List<string>();
                    foreach (var key in jsonObject.Keys)
                    {
                        keys.Add(key);
                    }

                    int i2 = 0;
                    foreach (var item in jsonObject.Values)
                    {
                        d.Add(keys[i2], _jsonDeserialize_convert(item));
                        i2++;
                    }
                    obj = d;
                    break;
                case JsonValueType.String:
                    obj = json.GetString();
                    break;
            }
            return obj;
        }

    }
}
