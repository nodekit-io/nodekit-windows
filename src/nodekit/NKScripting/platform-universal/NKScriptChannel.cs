﻿/*
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
using System.Reflection;
using System.Threading.Tasks;
using Windows.Storage;

namespace io.nodekit.NKScripting
{
    public class NKScriptChannel : NKScriptMessageHandler
    {
        private string id;
        private bool isFactory = false;
        private int sequenceNumber = 0;
        internal int nativeFirstSequence = Int32.MaxValue;

        private WeakReference<NKScriptContentController> _userContentController = null;
        internal NKScriptContentController userContentController {
            get { NKScriptContentController o; _userContentController.TryGetTarget(out o); return o; }
            set { _userContentController = new WeakReference<NKScriptContentController>(value);  }
        }
      
        private WeakReference<NKScriptContext> _context = null;
        internal NKScriptContext context { get { NKScriptContext o; _context.TryGetTarget(out o); return o; } }

        private NKScriptValueNative _principal;
        internal NKScriptValueNative principal { get { return _principal; } }

        internal NKScriptTypeInfo typeInfo;
        internal TaskScheduler queue;
        internal Dictionary<int, NKScriptValueNative> instances = new Dictionary<int, NKScriptValueNative>();

        [ThreadStatic]
        private static NKScriptContext _currentContext;
   
        public NKScriptChannel(NKScriptContext context) : this(context, TaskScheduler.Default) { }

        public NKScriptChannel(NKScriptContext context, TaskScheduler taskScheduler)
        {
            _context = new WeakReference<NKScriptContext>(context);
             this.queue = taskScheduler;
         }

        public static NKScriptContext currentContext()
        {
            return _currentContext;
        }

   
        private bool preparationComplete = false;
        async private Task prepareForPlugin()
        {
            if (preparationComplete) return;
            try
            {
                context.setNKScriptChannel(this);
                var source = await getResource("nkscripting.js", "lib");
                if (source == null)
                    throw new FileNotFoundException("Could not find file nkscripting.js");

                var script = new NKScriptSource(source, "io.nodekit.scripting/NKScripting/nkscripting.js", "NKScripting", null);
                await script.inject(context);
                          
                preparationComplete = true;
            }
            catch 
            {
                 preparationComplete = false;
            }

            var source2 = await getResource("promise.js", "lib");
            var script2 = new NKScriptSource(source2, "io.nodekit.scripting/NKScripting/promise.js", "Promise", null);
            await script2.inject(context);

            if (preparationComplete)
              NKLogging.log(String.Format("+E{0} JavaScript Engine is ready for loading plugins", context.NKid));
            else
                NKLogging.log(String.Format("+E{0} JavaScript Engine could not load NKScripting.js", context.NKid));

        }

        public async Task<NKScriptValue> bindPlugin(object obj, string ns)
        {
            await this.prepareForPlugin();
            if ((this.id != null) || (context == null) ) return null;
            if (this.userContentController == null) return null;

            this.id = (this.sequenceNumber++).ToString();
         
            this.userContentController.NKaddScriptMessageHandler(this, id);

            if (obj.GetType() == typeof(Type))
            {
                // Class, not instance, passed to bindPlugin -- to be used in Factory constructor/instance pattern in js
                isFactory = true;
                typeInfo = new NKScriptTypeInfo((Type)obj);
                // Need to store the channel on the class itself so it can be found when native construct requests come in from other plugins
                obj.setNKScriptChannel(this);
            }
            else {
                // Instance of Princpal passed to bindPlugin -- to be used in singleton/static pattern in js
                isFactory = false;
                typeInfo = new NKScriptTypeInfo(obj.GetType());
            }

            _principal = new NKScriptValueNative(ns, this, obj);
            this.instances[0] = _principal;

            var script = new NKScriptSource(_generateStubs(typeInfo.Name), ns + "/plugin/" + typeInfo.Name + ".js");
            NKLogging.log(script.source);
            await script.inject(context);

            return _principal;
        }

        internal void unbind()
        {
            if (id == null) return;
            id = null;
            instances.Clear();
            if (isFactory)
             _principal.nativeObject.setNKScriptChannel(null);
            _principal = null;
            userContentController.NKremoveScriptMessageHandlerForName(id);
            _userContentController.SetTarget(null);
            _context.SetTarget(null);
            typeInfo = null;
            queue = null;
            instances = null;
        }
   
     public void didReceiveScriptMessage(NKScriptMessage message)
        {
            // A workaround for when postMessage(undefined)
            if (message.body == null) return;

            // thread static
            NKScriptChannel._currentContext = this.context;

            var body = message.body as Dictionary<string, object>;
            if (body != null && body.ContainsKey("$opcode"))
            {
                string opcode = body["$opcode"] as String;
                int target = Convert.ToInt32(body["$target"] as String);
                if (instances.ContainsKey(target))
                {
                    var obj = instances[target];
                    if (opcode == "-")
                    {
                        if (target == 0)
                        {
                            // Dispose plugin
                            this.unbind();
                        }
                        else if (instances.ContainsKey(target))
                        {
                            obj.setNKScriptValue(null);
                        }
                        else
                        {
                            NKLogging.log(String.Format("!Invalid instance id: {0}", target));
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
                    var args = body["$operand"] as Array;
                    var ns = String.Format("{0}[{1}]", principal.ns, target);
                    instances[target] = new NKScriptValueNative(ns, this, args);
                }
                else
                {
                    // else Unknown opcode
                    var obj = principal.plugin as NKScriptMessageHandler;
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
            NKScriptChannel._currentContext = null;
        }


        public object didReceiveScriptMessageSync(NKScriptMessage message)
        {
            // A workaround for when postMessage(undefined)
            if (message.body == null) return false;

            // thread static
            NKScriptChannel._currentContext = this.context;
            object result;

            var body = message.body as Dictionary<string, object>;
            if (body != null && body.ContainsKey("$opcode"))
            {
                string opcode = body["$opcode"] as String;
                int target = Convert.ToInt32(body["$target"] as String);
                if (instances.ContainsKey(target))
                {
                    var obj = instances[target];
                    if (opcode == "-")
                    {
                        if (target == 0)
                        {
                            // Dispose plugin
                            this.unbind();
                            result = true;
                        }
                        else if (instances.ContainsKey(target))
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
                    var args = body["$operand"] as Array;
                    var ns = String.Format("{0}[{1}]", principal.ns, target);
                    instances[target] = new NKScriptValueNative(ns, this, args);
                    result = true;
                }
                else
                {
                    // else Unknown opcode
                    var obj = principal.plugin as NKScriptMessageHandler;
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
            NKScriptChannel._currentContext = null;
            return result;
        }

        private string _generateMethod(string key, string item, bool prebind)
        {
            string stub = String.Format("NKScripting.invokeNative.bind({0}, '{1}')", item, key);
            return prebind ? String.Format("{0};", stub) : String.Format("function(){return {0}.apply(null, arguments);", stub);
        }

        private string _rewriteStub(string stub, string forKey)
        {
            NKScriptExport export = principal.plugin as NKScriptExport;
            if (export != null)
                return export.rewriteGeneratedStub(stub, forKey);
            else
                return stub;
        }
        private string _generateStubs(string name)
        {
            bool prebind = !typeInfo.ContainsMethod("");
            var stubs = "";
            foreach (var member in typeInfo)
            {
                string stub;
                if ((member.isMethod()) && (member.name != ""))
                {
                    var methodStr = _generateMethod(String.Format("{0}{1}", member.name, member.NKScriptingjsType), prebind ? "exports" : "this", prebind);
                    stub = string.Format("exports.{0} = {1}", member.name, methodStr);
                }
                else if (member.isProperty())
                {
                    if (isFactory)
                    {
                        stub = string.Format("NKScripting.defineProperty(exports, '{0}', null, {1});", member.name, (bool)(member.setter != null));
                    }
                    else
                    {
                        var value = context.NKserialize(principal.valueForPropertyNative(member.name));
                        stub = string.Format("NKScripting.defineProperty(exports, '{0}', {1}, {1});", member.name, value, (bool)(member.setter != null));
                    }
                }
                else
                    continue;

                stubs += _rewriteStub(stub, member.name) + "\n";
            }

            string basestub;
            if (typeInfo.ContainsMethod(""))
            {
                var constructor = typeInfo.Item("");
                if (!constructor.isConstructor())
                    throw new NotSupportedException(string.Format("Default initializer for plugin {0} is not a constructor;  valid on Swift not C#", typeInfo.Name));
                // basestub = generateMethod("\(member.type)", this: "arguments.callee", prebind: false)
                basestub = _rewriteStub(string.Format("'{0}'", constructor.NKScriptingjsType), ".base");
            } else
            {
                basestub = _rewriteStub("null", ".base");
            }

            var localstub = _rewriteStub(stubs, ".local");
            var globalstubber = "(function(exports) {\n" + localstub + "})(NKScripting.createPlugin('" + id + "', '" + principal.ns + "', " + basestub + "));\n";

            return _rewriteStub(globalstubber, ".global");
        }
    }

    internal static class objectNKScriptChannelExtension
    {
        private static Dictionary<object, NKScriptChannel> dictionary = new Dictionary<object, NKScriptChannel>();

        public static NKScriptChannel getNKScriptChannel(this object obj)
        {
            return dictionary[obj];
        }

        internal static void setNKScriptChannel(this object obj, NKScriptChannel value)
        {
            if (value != null)
                dictionary[obj] = value;
            else
                dictionary.Remove(obj);
        }
    }
}


