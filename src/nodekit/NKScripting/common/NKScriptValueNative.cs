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
using System.Threading.Tasks;

namespace io.nodekit.NKScripting
{
    public interface NKScriptValueNativeProtocol
    {
        Task<object> invokeNativeMethod(string method, object[] args);
        object invokeNativeMethodSync(string method, object[] args);
        void updateNativeProperty(string key, object value);
        object valueForPropertyNative(string key);
    }

    public class NKScriptValueNative : NKScriptValue
    {
        private NKScriptInvocation proxy;
        internal object plugin { get { return proxy.target; } }
        protected object _nativeObject;
  
        public object nativeObject { get { return _nativeObject; } }

        protected NKScriptChannelProtocol _channel;
        protected int _instanceid;

        public NKScriptChannelProtocol channel { get { return _channel; }  }

        internal NKScriptValueNative(string ns, NKScriptChannelProtocol channel, int instanceid, object obj) : base(ns, channel.context, null)
        {
            this._channel = channel;
             this._instanceid = instanceid;
             this.proxy = bindObject(obj);
        }

        protected NKScriptValueNative(string ns, NKScriptChannelProtocol channel) : base(ns, channel.context, null)
        { 
        }

        internal NKScriptValueNative(object value, NKScriptContext context) : base()
        {

            Type t = value.GetType();
            NKScriptChannelProtocol channel = t.getNKScriptChannel();
            if (channel == null)
            {
                channel = t.GetTypeInfo().BaseType.getNKScriptChannel();
                if (channel == null)
                    throw new MissingFieldException("Cannot find channel for NKScriptExport member " + t.Name);
            }

            string pluginNS = channel.ns;
            int id = channel.getNativeSeq();
            _instanceid = id;
            string ns = string.Format("{0}[{1}]", pluginNS, id);
            this._channel = channel;
          
            // super.init(ns: ns, context: channel, origin: nil)
            _context = context;

            this.ns = ns;
            if (origin != null)
                _origin = origin;
            else
                _origin = this;
            // end super init

            channel.addInstance(id, this);
            proxy = bindObject(value);
            syncCreationWithProperties();
        }

        // Create new instance of plugin for given channel
        internal NKScriptValueNative(string ns, NKScriptChannelProtocol channel, int instanceid, object[] args, bool create) : base(ns, channel.context, null)
        {
            if (create != true)
                throw new ArgumentException();

            this._channel = channel;
            this._instanceid = instanceid;

            Type cls = channel.typeInfo.pluginType;
            var constructor = channel.typeInfo.DefaultConstructor();
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
            _nativeObject = obj;
             var queue = _channel.queue;
            var proxy = new NKScriptInvocation(obj, queue);
            obj.setNKScriptValue(this);
            var typeinfo = _channel.typeInfo;
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
            _channel.removeInstance(_instanceid);
             _nativeObject = null;
            obj.setNKScriptValue(null);

            var typeinfo = _channel.typeInfo;
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
        
            var script = "";
            foreach (var member in _channel.typeInfo.Where(m => m.isProperty()))
            {
                object value = proxy.call(member.getter, null);
                script += String.Format("{0}.$properties['{1}'] = {2};\n", ns, member.name, _context.NKserialize(value));
            }
            //         if (script != "")
            _context.NKevaluateJavaScript(script);
        }

        private void syncCreationWithProperties()
        {
            var nsComponents = ns.Split('[');
            var nsPlugin = nsComponents[0];
            var id = nsComponents[1].Split(']')[0];

            string script = "";

            script += String.Format("var instance = {0}.NKcreateForNative({1});\n", nsPlugin, id);
            foreach (var member in _channel.typeInfo.Where(m => m.isProperty()))
            {
                object value = proxy.call(member.getter, null);
                script += String.Format("instance.$properties['{1}'] = {2};\n", member.name, _context.NKserialize(value));
            }
            _context.NKevaluateJavaScript(script);
        }


        private void Observable_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var prop = e.PropertyName;
            var ti = _channel.typeInfo.Item(prop);
            if (ti != null)
            {
                object value = proxy.call(ti.getter, null);
                string script = String.Format("{0}.$properties['{1}'] = {2}", ns, prop, _context.NKserialize(value));
                _context.NKevaluateJavaScript(script);
            }     
        }

        internal virtual Task<object> invokeNativeMethod(string method, object[] args)
        {
            if (proxy == null) { throw new InvalidOperationException("Already disposed"); };
            var member = _channel.typeInfo.Item(method);
            if (member != null)
            {
                var mi = member.method;
                object[] argsWrapped = args.Select(wrapScriptObject).ToArray<object>();
                return proxy.callAsync(mi, argsWrapped);
            }
            return Task.FromResult<object>(null);
        }

        internal virtual object invokeNativeMethodSync(string method, object[] args)
        {
            if (proxy == null) { throw new InvalidOperationException("Already disposed"); };
             var member = _channel.typeInfo.Item(method);
            if (member != null)
            {
                var mi = member.method;
                object[] argsWrapped = args.Select(wrapScriptObject).ToArray<object>();
                return proxy.call(mi, argsWrapped);
            }
            return null;
        }

        internal virtual void updateNativeProperty(string key, object value)
        {
            if (proxy == null) { throw new InvalidOperationException("Already disposed"); };
            var member = _channel.typeInfo.Item(key);
            if (member != null)
            {
                var mi = member.setter;
                var argsWrapped = new[] { wrapScriptObject(value) };
                proxy.call(mi, argsWrapped);
            }
        }

        internal virtual object valueForPropertyNative(string key)
        {
            if (proxy == null) { throw new InvalidOperationException("Already disposed"); };
             var member = _channel.typeInfo.Item(key);
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
            var member = _channel.typeInfo.Item(method);
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
            var member = _channel.typeInfo.Item(method);
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
            var member = _channel.typeInfo.Item(key);
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
            var member = _channel.typeInfo.Item(key);
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
                unbindObject(_nativeObject);

             base.Dispose(disposing);
            _channel = null;

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
             return dictionary.ContainsKey(obj) ? dictionary[obj] : null;
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
