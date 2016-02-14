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
using System.Threading.Tasks;
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
                var str = NKData.jsonSerialize((string)obj);
                return str; //  str.Substring(1, str.Length - 2);
            }

            if (obj is DateTime)
                return "\"" + ((DateTime)obj).ToString("u") + "\"";

            if (typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(ti))
                return "[" + ((IEnumerable<dynamic>)obj).Select(o => NKserialize(o)).Aggregate("", (prod, next) => prod + ", " + next) + "]";
           

            if (typeof(IDictionary).GetTypeInfo().IsAssignableFrom(ti))
            {
                var genericKey = ti.GenericTypeParameters[0];
                if (typeof(string).GetTypeInfo().IsAssignableFrom(genericKey.GetTypeInfo()))
                {
                    var dict = (IDictionary<string, dynamic>)obj;
                    return "{" + dict.Keys.Select(k => "\"" + k + "\":" + NKserialize(dict[k])).Aggregate("", (prod, next) => prod + ", " + next) + "}";
                }
            }

            return Convert.ToString(obj, System.Globalization.CultureInfo.InvariantCulture);
        }

        public virtual object NKDeserialize(string json)
        {
            return NKData.jsonDeserialize(json);
        }

        protected List<NKScriptValue> _injectedPlugins = new List<NKScriptValue>();
        protected void LoadPluginBase<T>(T plugin, ref string ns, ref Dictionary<string, object> options, out bool handled)
        {
            if (ns == null)
            {
                if (typeof(T) == typeof(Type))
                    ns = (plugin as Type).Namespace;
                else
                    ns = (typeof(T)).Namespace;
            }

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
                    var ns1 = ns;
                    channel.bindPlugin<T>(plugin, ns).ContinueWith(t =>
                    {
                        if (!t.IsFaulted)
                        {
                            _injectedPlugins.Add(t.Result);
                            NKLogging.log("+NKScripting Plugin loaded at " + ns1);
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
        protected abstract Task LoadPlugin<T>(T plugin, string ns, Dictionary<string, object> options);
        public abstract void NKaddScriptMessageHandler(NKScriptMessageHandler scriptMessageHandler, string name);
        public abstract void NKremoveScriptMessageHandlerForName(string name);
        protected abstract Task<NKScriptValueProtocol> getJavaScriptValue(string key);

        public Task<object> NKevaluateJavaScript(string javaScriptString, string filename)
        {
            return ensureOnEngineThread(() =>
            {
                if (filename == null)
                    filename = "";

                return RunScript(javaScriptString, filename);
            }).Unwrap();
        }

        public Task<NKScriptValueProtocol> NKgetJavaScriptValue(string key)
        {
            return ensureOnEngineThread(() =>
            {
                  return getJavaScriptValue(key);
            }).Unwrap();
        }


        public Task NKloadPlugin<T>(T plugin, string ns, Dictionary<string, object> options = null)
        {
            return ensureOnEngineThread(() =>
            {
                bool handled;
                LoadPluginBase(plugin, ref ns, ref options, out handled);

                if (options == null)
                    options = new Dictionary<string, object>();

                Task t;
                t = LoadPlugin(plugin, ns, options);

                if (!handled)
                {
                    // set t
                }

                plugin = default(T);
                ns = null;
                options = null;
                return t;
            }).Unwrap();
        }


        public Task<Task<object>> ensureOnEngineThread(Func<Task<object>> t)
        {
            if (_thread_id != Environment.CurrentManagedThreadId)
                return Task.Factory.StartNew(t,
                         System.Threading.CancellationToken.None, TaskCreationOptions.DenyChildAttach, _async_queue);
            else
                return Task.FromResult<Task<object>>(t.Invoke());
        }

        public Task<Task<NKScriptValueProtocol>> ensureOnEngineThread(Func<Task<NKScriptValueProtocol>> t)
        {
            if (_thread_id != Environment.CurrentManagedThreadId)
                return Task.Factory.StartNew(t,
                         System.Threading.CancellationToken.None, TaskCreationOptions.DenyChildAttach, _async_queue);
            else
                return Task.FromResult<Task<NKScriptValueProtocol>>(t.Invoke());
        }

        public Task<Task> ensureOnEngineThread(Func<Task> t)
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
        protected abstract void LoadPlugin<T>(T plugin, string ns, Dictionary<string, object> options);
        public abstract void NKaddScriptMessageHandler(NKScriptMessageHandler scriptMessageHandler, string name);
        public abstract void NKremoveScriptMessageHandlerForName(string name);
        protected abstract NKScriptValueProtocol getJavaScriptValue(string key);

        public Task<object> NKevaluateJavaScript(string javaScriptString, string filename)
        {
            return ensureOnEngineThread(() =>
            {
                if (filename == null)
                    filename = "";

                return RunScript(javaScriptString, filename);
            });
        }

        public Task<NKScriptValueProtocol> NKgetJavaScriptValue(string key)
        {
            return ensureOnEngineThread(() =>
            {
                return getJavaScriptValue(key);
            });
        }

        public Task NKloadPlugin<T>(T plugin, string ns = null, Dictionary<string, object> options = null)
        {
            return ensureOnEngineThread(() =>
            {
                bool handled;
                LoadPluginBase(plugin, ref ns, ref options, out handled);

                if (!handled)
                {
                    bool mainThread = (bool)options["MainThread"];
                    NKScriptExportType bridge = (NKScriptExportType)options["PluginBridge"];
                    LoadPlugin(plugin, ns, options);
                    plugin = default(T);
                    ns = null;
                    options = null;
                }

            });
        }
        /*if(EqualityComparer<T>.Default.Equals(obj, default(T))) {
    return obj;
}*/

        public Task<object> ensureOnEngineThread(Func<object> t)
        {
            if (_thread_id != Environment.CurrentManagedThreadId)
                return Task.Factory.StartNew(t,
                         System.Threading.CancellationToken.None, TaskCreationOptions.DenyChildAttach, _async_queue);
            else
                return Task.FromResult<object>(t.Invoke());
        }

        public Task<NKScriptValueProtocol> ensureOnEngineThread(Func<NKScriptValueProtocol> t)
        {
            if (_thread_id != Environment.CurrentManagedThreadId)
                return Task.Factory.StartNew(t,
                         System.Threading.CancellationToken.None, TaskCreationOptions.DenyChildAttach, _async_queue);
            else
                return Task.FromResult<NKScriptValueProtocol>(t.Invoke());
        }

        public Task ensureOnEngineThread(Action t)
        {
            if (_thread_id != Environment.CurrentManagedThreadId)
                return Task.Factory.StartNew(t,
                         System.Threading.CancellationToken.None, TaskCreationOptions.DenyChildAttach, _async_queue);
            else
            {
                t.Invoke();
                return Task.FromResult<object>(null);
            }
        }
    }
}
