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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Data.Json;
using System.Linq;

namespace io.nodekit.NKScripting
{
    public abstract class NKScriptContextBase
    {
        protected int _id;

        protected NKScriptContextBase()
        {
            _id = NKScriptContextFactory.sequenceNumber++;
         }

        public int NKid
        {
            get { return _id; }
        }

     
        public virtual string NKserialize(object obj)
        {
            IFormatProvider invariant = System.Globalization.CultureInfo.InvariantCulture;
            Type t = obj.GetType();

            if (obj == null)
                return "undefined";

            if (obj is DBNull)
                return "null";

            if (obj != null && obj.GetType().GetTypeInfo().IsPrimitive)
            {
                // The primitive types are Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single.
                if (obj is Char)
                    return "'" + Convert.ToString(((Char)obj), invariant) + "'";

                if (obj is Boolean)
                    return (Boolean)obj ? "true" : "false";
      
                return Convert.ToString(obj, System.Globalization.CultureInfo.InvariantCulture);
            }

            if (obj is NKScriptValue)
                return ((NKScriptValue)obj).ns;

            if (obj is NKScriptExport)
            {
                var scriptValueObject = obj.getNKScriptValue();
                if (scriptValueObject != null)
                    return scriptValueObject.ns;
                var newScriptValueObject = new NKScriptValueNative(obj, (NKScriptContext)this);
                obj.setNKScriptValue(newScriptValueObject);
                return newScriptValueObject.ns;
            }

            if (obj is string)
            {
                var str = JsonValue.CreateStringValue((string)obj).Stringify();
                return str; //  str.Substring(1, str.Length - 2);
            }

            if (obj is DateTime)
                return "\"" + ((DateTime)obj).ToString("u") + "\"";

            if (typeof(IEnumerable).IsAssignableFrom(t))
                return "[" + ((IEnumerable<dynamic>)obj).Select(o => NKserialize(o)).Aggregate("", (prod, next) => prod + ", " + next) + "]";

            if (typeof(IDictionary).IsAssignableFrom(t) && typeof(string).IsAssignableFrom(t.GetGenericArguments()[0]))
            {
                var dict = (IDictionary<string, dynamic>)obj;
                return "{" + dict.Keys.Select(k => "\"" + k + "\":" + NKserialize(dict[k])).Aggregate("", (prod, next) => prod + ", " + next) + "}";
            }

            return Convert.ToString(obj, System.Globalization.CultureInfo.InvariantCulture);
        }

        public virtual object NKDeserialize(string json)
        {
            var j = JsonValue.Parse(json);
            return _NKDeserialize_Convert(j);
        }

        private object _NKDeserialize_Convert(IJsonValue json)
        {
            object obj = null;
            switch (json.ValueType)
            {
                case JsonValueType.Array:
                    JsonArray jsonArray = json.GetArray();
                    object[] objArray = new object[jsonArray.Count];
                    for (int i1 = 0; i1 < jsonArray.Count; i1++)
                    {
                        objArray[i1] = _NKDeserialize_Convert(jsonArray[i1]);
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
                        d.Add(keys[i2], _NKDeserialize_Convert(item));
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

       protected List<NKScriptValue> _injectedPlugins = new List<NKScriptValue>();
       protected void LoadPluginBase(object plugin, string ns, ref Dictionary<string, object> options, out bool handled)
        {
            if (options == null)
                options = new Dictionary<string, object>();

            bool mainThread;
            if (options.ContainsKey("MainThread"))
                mainThread = (bool)options["MainThread"];
            else
            {
                mainThread = false;
                options["MainThread"] = mainThread;
            }

            NKScriptExportType bridge;
            if (options.ContainsKey("PluginBridge"))
                bridge = (NKScriptExportType)options["PluginBridge"];
            else if (plugin == null)
            {
                bridge = NKScriptExportType.WinRT;
                options["PluginBridge"] = bridge;
            }
            else
            {
                bridge = NKScriptExportType.NKScriptExport;
                options["PluginBridge"] = bridge;
            }

         
            switch (bridge)
            {
                case NKScriptExportType.NKScriptExport:
                    NKScriptChannel channel;
                    if (mainThread)
                        channel = new NKScriptChannel((NKScriptContext)this, TaskScheduler.FromCurrentSynchronizationContext());
                    else
                        channel = new NKScriptChannel((NKScriptContext)this);


                    channel.userContentController = (NKScriptContentController)this;

                    channel.bindPlugin(plugin, ns).ContinueWith(t =>
                    {
                        if (!t.IsFaulted)
                        {
                            _injectedPlugins.Add(t.Result);
                            NKLogging.log("+NKScripting Plugin loaded at " + ns);
                        }
                        else
                            NKLogging.log(t.Exception);

                   
                    });
                     handled = true;
                    break;
                default:
                    handled = false;
                    break;
            }
        }
    }

    public abstract class NKScriptContextAsyncBase : NKScriptContextBase, NKScriptContext, NKScriptContentController
    {

        private TaskScheduler _async_queue;
        private int _thread_id;
    
        protected NKScriptContextAsyncBase() : base()
        {
            _async_queue = TaskScheduler.FromCurrentSynchronizationContext();
            _thread_id = Environment.CurrentManagedThreadId;
        }

        // Abstract Must Inherit Items, All Called Thread Safe on Main JS Thread (whatever thread with which the context instance is created )
        protected abstract Task<object> RunScript(string javaScriptString, string filename);
        protected abstract Task LoadPlugin(object plugin, string ns, Dictionary<string, object> options);
        public abstract void NKaddScriptMessageHandler(NKScriptMessageHandler scriptMessageHandler, string name);
        public abstract void NKremoveScriptMessageHandlerForName(string name);

        public Task<object> NKevaluateJavaScript(string javaScriptString, string filename)
        {
            return ensureOnEngineThread(() =>
            {
                if (filename == null)
                    filename = "";

                return RunScript(javaScriptString, filename);
            }).Unwrap();
        }

       public Task NKloadPlugin(object plugin, string ns, Dictionary<string, object> options)
        {
            return ensureOnEngineThread(() =>
            {
                bool handled;
                LoadPluginBase(plugin, ns, ref options, out handled);

                if (options == null)
                    options = new Dictionary<string, object>();

                Task t;
                t = LoadPlugin(plugin, ns, options);

                if (!handled)
                {
                    // set t
                }

                plugin = null;
                ns = null;
                options = null;
                return t;
            }).Unwrap();
        }

        protected Task<Task<object>> ensureOnEngineThread(Func<Task<object>> t)
        {
            if (_thread_id != Environment.CurrentManagedThreadId)
                return Task.Factory.StartNew(t,
                         System.Threading.CancellationToken.None, TaskCreationOptions.DenyChildAttach, _async_queue);
            else
                return Task.FromResult<Task<object>>(t.Invoke());
        }

        protected Task<Task> ensureOnEngineThread(Func<Task> t)
        {
            if (_thread_id != Environment.CurrentManagedThreadId)
                return Task.Factory.StartNew(t,
                         System.Threading.CancellationToken.None, TaskCreationOptions.DenyChildAttach, _async_queue);
            else
                return Task.FromResult<Task>(t.Invoke());
        }

    }

    public abstract class NKScriptContextSyncBase : NKScriptContextBase, NKScriptContext, NKScriptContentController
    {
        private TaskScheduler _async_queue;
        private int _thread_id;

        protected NKScriptContextSyncBase() : base()
        {
            _async_queue = TaskScheduler.FromCurrentSynchronizationContext();
            _thread_id = Environment.CurrentManagedThreadId;
        }

        // Abstract Must Inherit Items, All Called Thread Safe on Main JS Thread (whatever thread with which the context instance is created )
        protected abstract object RunScript(string javaScriptString, string filename);
        protected abstract void LoadPlugin(ref object plugin, string ns, Dictionary<string, object> options);
        public abstract void NKaddScriptMessageHandler(NKScriptMessageHandler scriptMessageHandler, string name);
        public abstract void NKremoveScriptMessageHandlerForName(string name);

        public Task<object> NKevaluateJavaScript(string javaScriptString, string filename)
        {
            return ensureOnEngineThread(() =>
            {
                if (filename == null)
                    filename = "";

                return RunScript(javaScriptString, filename);
            });
        }

        public Task NKloadPlugin(object plugin, string ns, Dictionary<string, object> options)
        {
            return ensureOnEngineThread(() =>
            {
                bool handled;
                LoadPluginBase(plugin, ns, ref options, out handled);

                if (!handled)
                {
                    bool mainThread = (bool)options["MainThread"];
                    NKScriptExportType bridge = (NKScriptExportType)options["PluginBridge"];
                    LoadPlugin(ref plugin, ns, options);
                    plugin = null;
                    ns = null;
                    options = null;
                }

            });
        }

        protected Task<object> ensureOnEngineThread(Func<object> t)
        {
            if (_thread_id != Environment.CurrentManagedThreadId)
                return Task.Factory.StartNew(t,
                         System.Threading.CancellationToken.None, TaskCreationOptions.DenyChildAttach, _async_queue);
            else
                return Task.FromResult<object>(t.Invoke());
        }

        protected Task ensureOnEngineThread(Action t)
        {
            if (_thread_id != Environment.CurrentManagedThreadId)
                return Task.Factory.StartNew(t,
                         System.Threading.CancellationToken.None, TaskCreationOptions.DenyChildAttach, _async_queue);
            else
            {   t.Invoke();
                return Task.FromResult<object>(null);
            }
        }
    }
    
}
