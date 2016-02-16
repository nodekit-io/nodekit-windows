/*
* nodekit.io
*
* Copyright (c) 2016 OffGrid Networks. All Rights Reserved.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

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
