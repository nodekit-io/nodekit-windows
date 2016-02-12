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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace io.nodekit.NKScripting
{
    public class NKScriptValueNative : NKScriptValue
    {
        private NKScriptInvocation proxy;
        internal object plugin { get { return proxy.target; } }
        private WeakReference<object> _nativeObject;
        public object nativeObject { get { object o; _nativeObject.TryGetTarget(out o); return o; } }

        internal NKScriptValueNative(string ns, NKScriptChannel channel, object obj) : base(ns, channel, null)
        {
            this.proxy = bindObject(obj);
        }

        internal NKScriptValueNative(object value, NKScriptContext inContext) : base()
        {

            Type t = value.GetType();
            NKScriptChannel channel = value.getNKScriptChannel();
            string pluginNS = channel.principal.ns;
            int id = channel.nativeFirstSequence++;
            string ns = string.Format("{0}[{1}]", pluginNS, id);

            // super.init(ns: ns, channel: channel, origin: nil)
            this.ns = ns;
            _channel = new WeakReference<NKScriptChannel>(channel);
            if (origin != null)
                _origin = new WeakReference<NKScriptValue>(origin);
            else
                _origin = new WeakReference<NKScriptValue>(this);
            // end super init

            channel.instances[id] = this;
            proxy = bindObject(value);
            syncCreationWithProperties();
        }

 
        // Create new instance of plugin for given channel
        internal NKScriptValueNative(string ns, NKScriptChannel channel, object[] args) : base(ns, channel, null)
        {
            Type cls = channel.typeInfo.pluginType;
            var constructor = channel.typeInfo.Item("");
            if (constructor == null || !constructor.isConstructor())
            {
                throw new ArgumentException(String.Format("!Plugin class {0} is not a constructor"), cls.Name);
            }

            object[] argsWrapped = args.Select(wrapScriptObject).ToArray<object>();

            NKScriptValue promise = null;
            var arity = constructor.arity;

            if (arity == argsWrapped.Length - 1 || arity < 0)
            {
                promise = argsWrapped.Last() as NKScriptValue;
                argsWrapped.Take(argsWrapped.Length - 1).ToArray();
            }
            //   if (constructor.name == "initByScriptWithArguments:")
            //   {
            //       arguments = [arguments]
            //  }
            // TO DO map nulls to nulls if needed
            object instance = NKScriptInvocation.construct(cls, (ConstructorInfo)constructor.method, argsWrapped);

            if (instance == null)
                throw new ArgumentException(String.Format("!Failed to create instance for plugin class {0}"), cls.Name);

            proxy = bindObject(instance);
            syncProperties();
            if (promise != null)
                promise.invokeMethod("resolve", new[] { this });
        }

        private NKScriptInvocation bindObject(object obj) {
            _nativeObject = new WeakReference<object>(obj);
             var queue = channel.queue;
            var proxy = new NKScriptInvocation(obj, queue);
            obj.setNKScriptValue(this);
            var typeinfo = channel.typeInfo;
            if (typeinfo.hasSettableProperties)
            {
                var observable = obj as System.ComponentModel.INotifyPropertyChanged;
                if (observable == null)
                {
                    throw new ArgumentException(String.Format("!Plugin class {0} has non private settable properties but does not implement INotifyPropertyChanged"), typeinfo.pluginType.Name);
                }
                observable.PropertyChanged += Observable_PropertyChanged;
            }
            return proxy;
        }

    
        private void unbindObject(object obj)
        {
            _nativeObject.SetTarget(null);
            _nativeObject = null;
            obj.setNKScriptValue(null);

            var typeinfo = channel.typeInfo;
            if (typeinfo.hasSettableProperties)
            {
                var observable = obj as System.ComponentModel.INotifyPropertyChanged;
                if (observable == null)
                {
                    throw new ArgumentException(String.Format("!Plugin class {0} has non private settable properties but does not implement INotifyPropertyChanged"), typeinfo.pluginType.Name);
                }
                observable.PropertyChanged -= Observable_PropertyChanged;
            }
            proxy = null;
        }

        private void syncProperties()
        {
            var context = this.channel.context;

            var script = "";
            foreach (var member in channel.typeInfo.Where(m => m.isProperty()))
            {
                object value = proxy.call(member.getter, null);
                script += String.Format("{$0}.$properties[{$1}] = {$2};\n", ns, member.name, context.NKserialize(value));
            }
            context.NKevaluateJavaScript(script);
        }

        private void syncCreationWithProperties()
        {
            var context = this.channel.context;


            var nsComponents = ns.Split('[');
            var nsPlugin = nsComponents[0];
            var id = nsComponents[1].Split(']')[0];

            string script = "";

            script += String.Format("var instance = {$0}.NKcreateForNative({$1});\n", nsPlugin, id);
            foreach (var member in channel.typeInfo.Where(m => m.isProperty()))
            {
                object value = proxy.call(member.getter, null);
                script += String.Format("instance.$properties[{$1}] = {$2};\n", member.name, context.NKserialize(value));
            }
            context.NKevaluateJavaScript(script);
        }


        private void Observable_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var context = this.channel.context;

            var prop = e.PropertyName;
            var ti = channel.typeInfo.Item(prop);
            if (ti != null)
            {
                object value = proxy.call(ti.getter, null);
                string script = String.Format("{$0}.$properties[{$1}] = {$2}", ns, prop, context.NKserialize(value));
                context.NKevaluateJavaScript(script);
            }     
        }

        internal Task<object> invokeNativeMethod(string method, object[] args)
        {
            if (proxy == null) { throw new InvalidOperationException("Already disposed"); };

            var member = channel.typeInfo.Item(method);
            if (member != null)
            {
                var mi = member.method;
                object[] argsWrapped = args.Select(wrapScriptObject).ToArray<object>();
                return proxy.callAsync(mi, argsWrapped);
            }
            return Task.FromResult<object>(null);
        }

        internal object invokeNativeMethodSync(string method, object[] args)
        {
            if (proxy == null) { throw new InvalidOperationException("Already disposed"); };

            var member = channel.typeInfo.Item(method);
            if (member != null)
            {
                var mi = member.method;
                object[] argsWrapped = args.Select(wrapScriptObject).ToArray<object>();
                return proxy.call(mi, argsWrapped);
            }
            return null;
        }

        internal void updateNativeProperty(string key, object value)
        {
            if (proxy == null) { throw new InvalidOperationException("Already disposed"); };

            var member = channel.typeInfo.Item(key);
            if (member != null)
            {
                var mi = member.setter;
                var argsWrapped = new[] { wrapScriptObject(value) };
                proxy.call(mi, argsWrapped);
            }
        }

        internal object valueForPropertyNative(string key)
        {
            if (proxy == null) { throw new InvalidOperationException("Already disposed"); };

            var member = channel.typeInfo.Item(key);
            if (member != null)
            {
                var mi = member.getter;
                return proxy.call(mi, null);
            }
            return null;
        }

        // OVERRIDE METHODS IN NKScriptValue
        public override Task invokeMethod(string method, object[] args)
        {
            if (proxy == null) { throw new InvalidOperationException("Already disposed"); };

            var member = channel.typeInfo.Item(method);
            if (member != null)
            {
                var mi = member.method;
                object[] argsWrapped = args.Select(wrapScriptObject).ToArray<object>();
                return proxy.callAsync(mi, argsWrapped);
            }
            else
               return base.invokeMethod(method, args);
        }

        public override Task<object> invokeMethodWithResult(string method, object[] args)
        {
            if (proxy == null) { throw new InvalidOperationException("Already disposed"); };

            var member = channel.typeInfo.Item(method);
            if (member != null)
            {
                var mi = member.method;
                object[] argsWrapped = args.Select(wrapScriptObject).ToArray<object>();
                return proxy.callAsync(mi, argsWrapped);
            }
            else
                return base.invokeMethodWithResult(method, args);
        }

        public override Task<object> valueForProperty(string key)
        {
            if (proxy == null) { throw new InvalidOperationException("Already disposed"); };

            var member = channel.typeInfo.Item(key);
            if (member != null)
            {
                var mi = member.getter;
                proxy.call(mi, null);
                return Task.FromResult<object>(null);
            }
            else
                return base.valueForProperty(key);
        }

        public override Task setValue(object value, string key)
        {

            if (proxy == null) { throw new InvalidOperationException("Already disposed"); };

            var member = channel.typeInfo.Item(key);
            if (member != null)
            {
                var mi = member.setter;
                var argsWrapped = new[] { wrapScriptObject(value) };
                proxy.call(mi, argsWrapped);
                return Task.FromResult<object>(null);
            }
            else
                return base.setValue(value, key);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {

            if (!disposedValue)
                unbindObject(plugin);

            base.Dispose(disposing);
            disposedValue = true;
        }

        ~NKScriptValueNative()
        {
            Dispose(false);
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    public static class objectNKScriptValueExtension
    {
        private static Dictionary<object, NKScriptValue> dictionary = new Dictionary<object, NKScriptValue>();

        public static NKScriptValue getNKScriptValue(this object obj)
        {
            return dictionary[obj];
        }

        internal static void setNKScriptValue(this object obj, NKScriptValue value)
        {
            if (value != null)
                dictionary[obj] = value;
            else
                dictionary.Remove(obj);
        }
    }
}
