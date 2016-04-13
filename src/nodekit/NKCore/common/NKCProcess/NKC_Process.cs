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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using io.nodekit.NKScripting;
using System.Reflection;
using System.Collections;
using System.Linq;

namespace io.nodekit.NKCore
{
    public sealed class NKC_Process
    {
        private NKEventEmitter events = NKEventEmitter.global;

        #region NKScriptExport

        internal static Task attachToContext(NKScriptContext context, Dictionary<string, object> options)
        {
            return context.NKloadPlugin(typeof(NKC_Process), null, options);
        }

        private static string defaultNamespace { get { return "io.nodekit.platform.process"; } }

        private static string rewriteGeneratedStub(string stub, string forKey)
        {
            switch (forKey)
            {
                case ".global":
                    var appjs = NKStorage.getResource(typeof(NKC_Process), "console.js", "lib_core/platform");
                    return "function loadplugin(){\n" + appjs + "\n}\n" + stub + "\n" + "loadplugin();" + "\n";
                default:
                    return stub;
            }
        }
        #endregion

        // PUBLIC FUNCTIONS EXPOSED TO JAVASCRIPT as io.nodekit.process.*
        public void nextTick(NKScriptValue callback)
        {
            Task.Factory.StartNew(() =>
            {
                callback.callWithArguments(new object[] { });
            });
           
        }

        public void emit(string eventType, object data)
        {
            events.emit(eventType, data);
        }

        // PRIVATE FUNCTIONS USED TO SET PROCESS DICTIONARY

        private static string syncProcessDictionary()
        {
            string PLATFORM = "win32";
            string DEVICEFAMILY = "desktop";

            Dictionary<String, object> process = new Dictionary<String, object>();
            process["platform"] = PLATFORM;
            process["devicefamily"] = DEVICEFAMILY;
            process["argv"] = new string[] { "nodekit" };
            process["execPath"] = "/";
            setNodePaths(process);
            string script = "";

            foreach (var pair in process)
                script += string.Format("process['{0}'] = {1};]n", pair.Key, serialize(pair.Value));

            return script;
        }

#if WINDOWS_UWP
        static string root = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
#elif WINDOWS_WIN32
        static string root = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
#endif
        private static void setNodePaths(Dictionary<string, object> process)
        {
            string webPath = "/";
            string appModulePath = "/node_modules";
            string exePath = root;
            string resPaths;

            process["workingDirectory"] = "/";
            resPaths = webPath + ":" + appModulePath + ":" + exePath;
            var env = new Dictionary<string, string>();
            env["NODE_PATH"] = resPaths;
            process["outputDirectory"] = env["OUTPUT_DIRECTORY"] ?? NKC_FileSystem.current.getTempDirectorySync();
            process["env"] = env;
        }

        private static string serialize(object obj)
        {
            IFormatProvider invariant = System.Globalization.CultureInfo.InvariantCulture;
            Type t = obj.GetType();
            TypeInfo ti = t.GetTypeInfo();

            if (obj == null)
                return "undefined";

            if (obj != null && obj.GetType().GetTypeInfo().IsPrimitive)
            {
                // The primitive types are Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single.
                if (obj is Char)
                    return "'" + Convert.ToString(((Char)obj), invariant) + "'";

                if (obj is Boolean)
                    return (Boolean)obj ? "true" : "false";

                return Convert.ToString(obj, System.Globalization.CultureInfo.InvariantCulture);
            }

            if (obj is string)
            {
                var str = NKData.jsonSerialize((string)obj);
                return str; //  str.Substring(1, str.Length - 2);
            }

            if (obj is DateTime)
                return "\"" + ((DateTime)obj).ToString("u") + "\"";

            if (typeof(IDictionary).GetTypeInfo().IsAssignableFrom(ti))
            {
                var genericKey = ti.GenericTypeArguments[0];
                if (typeof(string).GetTypeInfo().IsAssignableFrom(genericKey.GetTypeInfo()))
                {
                    var dict = (IDictionary<string, dynamic>)obj;
                    return "{" + string.Join(", ", dict.Keys.Select(k => "\"" + k + "\":" + serialize(dict[k]))) + "}";
                }
            }

            if (typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(ti))
                return "[" + string.Join(", ", ((IEnumerable<dynamic>)obj).Select(o => serialize(o))) + "]";

            return Convert.ToString(obj, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}