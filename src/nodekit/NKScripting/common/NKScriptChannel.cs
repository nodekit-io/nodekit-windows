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
using System.IO;
using System.Threading.Tasks;

namespace io.nodekit.NKScripting
{
    public interface NKScriptChannelProtocol : NKScriptMessageHandler
    {
        Task<NKScriptValue> bindPlugin<T>(T obj, string ns) where T : class;
        void addInstance(int id, NKScriptValueNative instance);
        void removeInstance(int id);
        int getNativeSeq();
        bool singleInstance { get; set;  }
        NKScriptContext context { get; }
        string ns { get; }
        INKScriptTypeInfo typeInfo { get; }
        TaskScheduler queue { get; }

        // NKScriptChannelProtocol(NKScriptContext context);
        // NKScriptChannelProtocol(NKScriptContext context, TaskScheduler taskScheduler);
        // void didReceiveScriptMessage(NKScriptMessage message)
        // object didReceiveScriptMessageSync(NKScriptMessage message)
    }

     public class NKScriptChannel : NKScriptChannelProtocol
    {
        protected string id;
        protected bool isFactory = false;
        protected bool isRemote = false;
        protected NKScriptContext _context = null;
        protected NKScriptValueNative _principal;
        protected Dictionary<int, NKScriptValueNative> _instances = new Dictionary<int, NKScriptValueNative>();
        protected static Dictionary<string, NKScriptChannel> _channels = new Dictionary<string, NKScriptChannel>();
        protected static Dictionary<int, NKScriptChannel> _instanceChannels = new Dictionary<int, NKScriptChannel>();

        // Internal variables and helpers
        internal static int nativeFirstSequence = Int32.MaxValue;
        internal static int sequenceNumber = 0;
  
        // Public properties
        public NKScriptContext context { get { return _context; } }
        public string ns { get { return _principal.ns; } }
        public INKScriptTypeInfo typeInfo { get { return _typeInfo;  } }
        public TaskScheduler queue { get { return _queue; } }
        public bool singleInstance { get { return _singleInstance; } set { _singleInstance = value; } }
        protected bool _singleInstance = false;

        protected INKScriptTypeInfo _typeInfo;
        protected TaskScheduler _queue;

        // Public constructors
        public NKScriptChannel(NKScriptContext context) : this(context, TaskScheduler.Default) { }

        public NKScriptChannel(NKScriptContext context, TaskScheduler taskScheduler)
        {
            _context = context;
            _queue = taskScheduler;
        }

       // Public methods
       public virtual async Task<NKScriptValue> bindPlugin<T>(T obj, string ns) where T : class
        {
            var context = this.context;
            context.setNKScriptChannel(this);
            if ((this.id != null) || (context == null) ) return null;
      
            this.id = (NKScriptChannel.sequenceNumber++).ToString();
         
           context.NKaddScriptMessageHandler(this, id);

            string name;
            if (typeof(T) == typeof(Type))
           {
                // Class, not instance, passed to bindPlugin -- to be used in Factory constructor/instance pattern in js
                isFactory = true;
                _typeInfo = new NKScriptTypeInfo<T>(obj);
                name = (obj as Type).Name;
                // Need to store the channel on the class itself so it can be found when native construct requests come in from other plugins
                obj.setNKScriptChannel(this);
            }
            else {

                name = (typeof(T)).Name;
                // Instance of Princpal passed to bindPlugin -- to be used in singleton/static pattern in js
                isFactory = false;
                _typeInfo = new NKScriptTypeInfo<T>(obj);
            }

            _principal = new NKScriptValueNative(ns, this, 0, obj);
            this._instances[0] = _principal;
            _channels[ns] = this;
            obj.setNKScriptValue(_principal);

            var export = new NKScriptExportProxy<T>(obj);

            var script = new NKScriptSource(_generateStubs(export, name), ns + "/plugin/" + name + ".js");
            await context.NKinjectScript(script);
            await export.initializeForContext(context);
            return _principal;
        }

        public static NKScriptChannel getChannel(string ns)
        {
            return _channels[ns];
        }

        public int getNativeSeq()
        {
            int id = NKScriptChannel.nativeFirstSequence--;
            _instanceChannels[id] = this;
            return id;
        }

        public static NKScriptChannel getNative(int id)
        {
            if (_instanceChannels.ContainsKey(id))
                return _instanceChannels[id];
            else
                return null;
        }

        public virtual void addInstance(int id, NKScriptValueNative instance) { _instances[id] = instance; }
        public virtual void removeInstance(int id) {
            if (_instances.ContainsKey(id))
                _instances.Remove(id);

            if (_instanceChannels.ContainsKey(id))
               _instanceChannels.Remove(id);

            if (_singleInstance)
                NKEventEmitter.global.emit<string>("NKS.SingleInstanceComplete", ns);

        }

        protected virtual void unbind()
        {
            var context = this.context;
            if (_channels.ContainsKey(ns))
               _channels.Remove(ns);
            if (id == null) return;
            id = null;
            _instances.Clear();
            if (isFactory)
             _principal.nativeObject.setNKScriptChannel(null);
            _principal = null;
            context.NKremoveScriptMessageHandlerForName(id);
            _context = null;
            _typeInfo = null;
            _queue = null;
            _instances = null;
            context = null;
        }
   
     public virtual void didReceiveScriptMessage(NKScriptMessage message)
        {
            // A workaround for when postMessage(undefined)
            if (message.body == null) return;

            // thread static
            NKScriptValue._currentContext = this.context;

            var body = message.body as Dictionary<string, object>;
            if (body != null && body.ContainsKey("$opcode"))
            {
                string opcode = body["$opcode"] as String;
                int target = Int32.Parse(body["$target"].ToString());
                if (_instances.ContainsKey(target))
                {
                    var obj = _instances[target];
                    if (opcode == "-")
                    {
                        if (target == 0)
                        {
                            // Dispose plugin
                            this.unbind();
                        }
                        else 
                        {
                            obj.setNKScriptValue(null);
                            _instances.Remove(target);
                        }
                    }
                    else if (typeInfo.ContainsProperty(opcode))
                    {
                        // Update property
                        obj.updateNativeProperty(opcode, body["$operand"] as object);
                    }
                    else if (typeInfo.ContainsMethod(opcode))
                    {
                        // Invoke method
                        obj.invokeNativeMethod(opcode, body["$operand"] as object[]);
                    }
                    else {
                        NKLogging.log(String.Format("!Invalid member name: {0}", opcode));
                    }
                }
                else if (opcode == "+")
                {
                    // Create instance
                    var args = body["$operand"] as object[];
                     var nsInstance = String.Format("{0}[{1}]", ns, target);
                    _instances[target] = new NKScriptValueNative(nsInstance, this, target, args, true);
                }
                else
                {
                    // else Unknown opcode
                    var obj = _principal.plugin as NKScriptMessageHandler;
                    if (obj != null)
                    {
                        obj.didReceiveScriptMessage(message);
                    }
                    else
                    {
                        // discard unknown message
                        NKLogging.log(String.Format("!Unknown message: {0}", message.body.ToString()));
                    }
                }
            }
            else
            {
                // null body, ignore
            }

            //thread static
            NKScriptValue._currentContext = null;
        }

        public virtual object didReceiveScriptMessageSync(NKScriptMessage message)
        {
            // A workaround for when postMessage(undefined)
            if (message.body == null) return false;

            // thread static
            NKScriptValue._currentContext = this.context;
            object result;

            var body = message.body as Dictionary<string, object>;
            if (body != null && body.ContainsKey("$opcode"))
            {
                string opcode = body["$opcode"] as String;
                int target = Int32.Parse(body["$target"].ToString());
                if (_instances.ContainsKey(target))
                {
                    var obj = _instances[target];
                    if (opcode == "-")
                    {
                        if (target == 0)
                        {
                            // Dispose plugin
                            this.unbind();
                            result = true;
                        }
                        else if (_instances.ContainsKey(target))
                        {
                            obj.setNKScriptValue(null);
                            result = true;
                        }
                        else
                        {
                            NKLogging.log(String.Format("!Invalid instance id: {0}", target));
                            result = false;
                        }
                    }
                    else if (typeInfo.ContainsProperty(opcode))
                    {
                        // Update property
                        obj.updateNativeProperty(opcode, body["$operand"] as object);
                        result = true;
                    }
                    else if (typeInfo.ContainsMethod(opcode))
                    {
                        // Invoke method
                        result = obj.invokeNativeMethodSync(opcode, body["$operand"] as object[]);
                    }
                    else {
                        NKLogging.log(String.Format("!Invalid member name: {0}", opcode));
                        result = false;
                    }
                }
                else if (opcode == "+")
                {
                    // Create instance
                    var args = body["$operand"] as object[];
                    var nsInstance = String.Format("{0}[{1}]", this.ns, target);
                    _instances[target] = new NKScriptValueNative(nsInstance, this, target, args, true);
                    result = true;
                }
                else
                {
                    // else Unknown opcode
                    var obj = _principal.plugin as NKScriptMessageHandler;
                    if (obj != null)
                    {
                        result = obj.didReceiveScriptMessageSync(message);
                    }
                    else
                    {
                        // discard unknown message
                        NKLogging.log(String.Format("!Unknown message: {0}", message.body.ToString()));
                        result = false;
                    }
                }
            }
            else
            {
                // null body
                result = false;
            }

            //thread static
            NKScriptValue._currentContext = null;
            return result;
        }


        private string _generateMethod(string key, string item, bool prebind)
        {
            string stub = String.Format("NKScripting.invokeNative.bind({0}, '{1}')", item, key);
            return prebind ? String.Format("{0};", stub) : "function(){return " + stub + ".apply(null, arguments);}";
        }

        private string _generateStubs<T>(NKScriptExportProxy<T> export, string name) where T : class
        {
            bool prebind = !typeInfo.ContainsConstructor("");
            var stubs = "";
            foreach (var member in typeInfo)
            {
                string stub;
                if ((member.isMethod()) && (member.name != ""))
                {
                    var methodStr = _generateMethod(String.Format("{0}{1}", member.key, member.NKScriptingjsType), prebind ? "exports" : "this", prebind);
                    stub = string.Format("exports.{0} = {1}", member.name, methodStr);
                }
                else if (member.isProperty())
                {
                    if (isFactory)
                    {
                        stub = string.Format("NKScripting.defineProperty(exports, '{0}', null, {1});", member.name, (member.setter != null).ToString().ToLower());
                    }
                    else
                    {
                        var value = context.NKserialize(_principal.valueForPropertyNative(member.name));
                        stub = string.Format("NKScripting.defineProperty(exports, '{0}', {1}, {2});", member.name, value, (member.setter != null).ToString().ToLower());
                    }
                }
                else
                    continue;

                stubs += export.rewriteGeneratedStub(stub, member.name) + "\n";
            }

            string basestub;
            if (typeInfo.ContainsConstructor(""))
            {
                var constructor = typeInfo.DefaultConstructor();
                // basestub = generateMethod("\(member.type)", this: "arguments.callee", prebind: false)
                basestub = export.rewriteGeneratedStub(string.Format("'{0}'", constructor.NKScriptingjsType), ".base");
            } else
            {
                basestub = export.rewriteGeneratedStub("null", ".base");
            }

            var localstub = export.rewriteGeneratedStub(stubs, ".local");
            var globalstubber = "(function(exports) {\n" + localstub + "})(NKScripting.createPlugin('" + id + "', '" + ns + "', " + basestub + "));\n";

            return export.rewriteGeneratedStub( globalstubber, ".global");
        }
     
    }

    // Object extensions for NKScriptChannel
    internal static class objectNKScriptChannelExtension
    {
        private static Dictionary<object, NKScriptChannelProtocol> dictionary = new Dictionary<object, NKScriptChannelProtocol>();

        public static NKScriptChannelProtocol getNKScriptChannel(this object obj)
        {
            return dictionary.ContainsKey(obj) ? dictionary[obj] : null;
        }

        internal static void setNKScriptChannel(this object obj, NKScriptChannelProtocol value)
        {
            if (value != null)
                dictionary[obj] = value;
            else
                dictionary.Remove(obj);
        }
    }
}


