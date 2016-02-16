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
using System.IO;

namespace io.nodekit.NKScripting
{
    public abstract class NKScriptContextBase : NKScriptContext
    {
        protected int _id;
        protected int _thread_id;
        protected TaskScheduler _async_queue;

        // Abstract Must Inherit Items, All Called Thread Safe on Main JS Thread (whatever thread with which the context instance is created )
        protected abstract Task LoadPlugin<T>(T plugin, string ns, Dictionary<string, object> options) where T : class;
        protected abstract Task InjectScript(NKScriptSource source);
        protected abstract Task PrepareEnvironment();

        // Abstract Must Inherit Script Handlers
        public abstract void NKaddScriptMessageHandler(NKScriptMessageHandler scriptMessageHandler, string name);
        public abstract void NKremoveScriptMessageHandlerForName(string name);

        // NKContext abstract methods implemented in Async of Sync version of base
        public abstract Task<object> NKevaluateJavaScript(string javaScriptString, string filename = null);

        protected NKScriptContextBase(int id)
        {
            _id = id;
            _async_queue = TaskScheduler.FromCurrentSynchronizationContext();
            _thread_id = Environment.CurrentManagedThreadId;
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

        public virtual object NKdeserialize(string json)
        {
            return NKData.jsonDeserialize(json);
        }

        protected List<NKScriptValue> _injectedPlugins = new List<NKScriptValue>();
        protected virtual bool baseCanHandle<T>(T plugin, ref string ns, ref Dictionary<string, object> options) where T : class
        {
            if (ns == null)
            {
                var export = new NKScriptExportProxy<T>(plugin);
                ns = export.defaultNamespace;
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
                    return true;
                default:
                    return false;
            }
        }

        protected async Task LoadPluginBase<T>(T plugin, string ns, Dictionary<string, object> options) where T : class
        {
            bool mainThread = (bool)options["MainThread"];
            NKScriptExportType bridge = (NKScriptExportType)options["PluginBridge"];

            switch (bridge)
            {
                case NKScriptExportType.NKScriptExport:
                    NKScriptChannel channel;
                    if (mainThread)
                        channel = new NKScriptChannel((NKScriptContext)this, TaskScheduler.FromCurrentSynchronizationContext());
                    else
                        channel = new NKScriptChannel((NKScriptContext)this);
                    var ns1 = ns;
                    var scriptValue = await channel.bindPlugin<T>(plugin, ns);
                    _injectedPlugins.Add(scriptValue);
                    NKLogging.log("+NKScripting Plugin loaded at " + ns1);
                    break;
                default:
                    throw new InvalidOperationException("Load Plugin Base called for non-handled bridge type");
            }
        }

        public Task NKloadPlugin<T>(T plugin, string ns, Dictionary<string, object> options = null) where T : class
        {
            return ensureOnEngineThread(async () =>
            {
                if (baseCanHandle(plugin, ref ns, ref options))
                    await LoadPluginBase(plugin, ns, options);
                else
                    await LoadPlugin(plugin, ns, options);

                plugin = default(T);
                ns = null;
                options = null;
            }).Unwrap();
        }

        private bool preparationComplete = false;
        async public Task<NKScriptContext> completeInitialization()
        {
            if (preparationComplete) return this;
            try
            {
                var source = await NKStorage.getResourceAsync(typeof(NKScriptContext), "nkscripting.js", "lib");
                if (source == null)
                    throw new FileNotFoundException("Could not find file nkscripting.js");

                var script = new NKScriptSource(source, "io.nodekit.scripting/NKScripting/nkscripting.js", "NKScripting", null);
                await this.NKinjectScript(script);

                await PrepareEnvironment();

                preparationComplete = true;
            }
            catch
            {
                preparationComplete = false;
            }

            if (preparationComplete)
                NKLogging.log(String.Format("+E{0} JavaScript Engine is ready for loading plugins", this.NKid));
            else
                NKLogging.log(String.Format("+E{0} JavaScript Engine could not load NKScripting.js", this.NKid));

            return this;

        }

        protected List<NKScriptSource> _injectedScripts = new List<NKScriptSource>();
        public async Task NKinjectScript(NKScriptSource source)
        {
            if (_injectedScripts.Contains(source))
                throw new InvalidOperationException("Script has already been injected to a context;  create separate NKSCriptSource for each instance");
       
            _injectedScripts.Add(source);
            source.registerInject(this);
            await InjectScript(source);

            NKLogging.log(string.Format("+E{0} Injected {1}", this.NKid, source.filename));
        }

        public NKScriptValue NKgetScriptValue(string key)
        {
            return new NKScriptValue(key, this, null);
        }

        public Task<Task<object>> ensureOnEngineThread(Func<Task<object>> t)
        {
            if (_thread_id != Environment.CurrentManagedThreadId)
                return Task.Factory.StartNew(t,
                         System.Threading.CancellationToken.None, TaskCreationOptions.DenyChildAttach, _async_queue);
            else
                return Task.FromResult<Task<object>>(t.Invoke());
        }

        public Task<Task> ensureOnEngineThread(Func<Task> t)
        {
            if (_thread_id != Environment.CurrentManagedThreadId)
                return Task.Factory.StartNew(t,
                         System.Threading.CancellationToken.None, TaskCreationOptions.DenyChildAttach, _async_queue);
            else
                return Task.FromResult<Task>(t.Invoke());
        }

        public Task<object> ensureOnEngineThread(Func<object> t)
        {
            if (_thread_id != Environment.CurrentManagedThreadId)
                return Task.Factory.StartNew(t,
                         System.Threading.CancellationToken.None, TaskCreationOptions.DenyChildAttach, _async_queue);
            else
                return Task.FromResult<object>(t.Invoke());
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

    public abstract class NKScriptContextAsyncBase : NKScriptContextBase
    {
        protected NKScriptContextAsyncBase(int id) : base(id) {}

        // Abstract Must Inherit Items, All Called Thread Safe on Main JS Thread (whatever thread with which the context instance is created )
        protected abstract Task<object> RunScript(string javaScriptString, string filename);
      
        public override Task<object> NKevaluateJavaScript(string javaScriptString, string filename)
        {
            return ensureOnEngineThread(() =>
            {
                if (filename == null)
                    filename = "";

                return RunScript(javaScriptString, filename);
            }).Unwrap();
        }
    }

    public abstract class NKScriptContextSyncBase : NKScriptContextBase
    {
        protected NKScriptContextSyncBase(int id) : base(id) {}

        // Abstract Must Inherit Items, All Called Thread Safe on Main JS Thread (whatever thread with which the context instance is created )
        protected abstract object RunScript(string javaScriptString, string filename);
 
        public override Task<object> NKevaluateJavaScript(string javaScriptString, string filename)
        {
            return ensureOnEngineThread(() =>
            {
                if (filename == null)
                    filename = "";

                return RunScript(javaScriptString, filename);
            });
        }
    }
}
