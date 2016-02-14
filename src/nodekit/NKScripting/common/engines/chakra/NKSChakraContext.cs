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
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace io.nodekit.NKScripting.Engines.Chakra
{
    public class NKSChakraContext : NKScriptContextSyncBase
    {
       
        private static JavaScriptSourceContext currentSourceContext = JavaScriptSourceContext.FromIntPtr(IntPtr.Zero);
 
        [ThreadStatic]
        internal static JavaScriptContext currentContext;

        private JavaScriptContext _context;
        private Queue<JavaScriptValue> _jsTaskQueue = new Queue<JavaScriptValue>();

        internal NKSChakraContext(JavaScriptContext context, Dictionary<string, object> options) : base()
        {
            this._context = context;

            JavaScriptPromiseContinuationCallback promiseContinuationCallback = delegate (JavaScriptValue jsTask, IntPtr callbackState)
            {
                _jsTaskQueue.Enqueue(jsTask);
            };

            Native.ThrowIfError(Native.JsSetPromiseContinuationCallback(promiseContinuationCallback, IntPtr.Zero));

            Native.ThrowIfError(Native.JsProjectWinRTNamespace("Windows"));
     //     Native.ThrowIfError(Native.JsProjectWinRTNamespace("io.nodekit"));

    //        if (options.ContainsKey("nk.ScriptingDebug") && ((bool)options["nk.ScriptingDebug"] == true))
                Native.ThrowIfError(Native.JsStartDebugging());
        }

        public void switchContextifNeeded()
        {
            if (!_context.Equals(currentContext))
            {
                JavaScriptContext.Current = _context;
                currentContext = _context;
            }
        }

        protected override NKScriptValueProtocol getJavaScriptValue(string key)
        {
            switchContextifNeeded();
            var global = JavaScriptValue.GlobalObject;
            var NKScripting = global.GetProperty(JavaScriptPropertyId.FromString(key));
            var item = key.Split('.').Aggregate(JavaScriptValue.GlobalObject, (prod, next) => prod.GetProperty(JavaScriptPropertyId.FromString(next)));
            return new NKChakraContextValue(this, item);
        }

        protected override object RunScript(string javaScriptString, string filename)
        {
            IntPtr returnValue;

            try
            {
                JavaScriptValue result;

                switchContextifNeeded();
                result = JavaScriptContext.RunScript(javaScriptString, currentSourceContext, filename);
                currentSourceContext = JavaScriptSourceContext.Increment(currentSourceContext);
                // Execute promise tasks stored in taskQueue 
                while (_jsTaskQueue.Count != 0)
                {
                    JavaScriptValue jsTask = (JavaScriptValue)_jsTaskQueue.Dequeue();
                    JavaScriptValue promiseResult;
                    JavaScriptValue global;
                    Native.JsGetGlobalObject(out global);
                    JavaScriptValue[] args = new JavaScriptValue[1] { global };
                    Native.JsCallFunction(jsTask, args, 1, out promiseResult);
                }

                // Convert the return value.
                JavaScriptValue stringResult;
                UIntPtr stringLength;
                Native.ThrowIfError(Native.JsConvertValueToString(result, out stringResult));
                Native.ThrowIfError(Native.JsStringToPointer(stringResult, out returnValue, out stringLength));
            }
            catch (Exception e)
            {
                throw e;
            }
            return Marshal.PtrToStringUni(returnValue);
        }

         protected override void LoadPlugin<T>(T plugin, string ns, Dictionary<string, object> options)
        {
            bool mainThread = (bool)options["MainThread"];
            NKScriptExportType bridge = (NKScriptExportType)options["PluginBridge"];

            switch (bridge)
            {
                case NKScriptExportType.JSExport:
                    throw new NotSupportedException("JSExport option is for darwin platforms only");
                case NKScriptExportType.WinRT:


                     switchContextifNeeded();
                    Native.ThrowIfError(Native.JsProjectWinRTNamespace(ns));

                    if (plugin == null)
                    {
                        NKLogging.log("+Windows Unversal Component Plugin loaded at " + ns);
                    }
                    else if (typeof(T) != typeof(Type))
                        throw new ArgumentException("Windows Universal Components can only be provided as a type");
                    else
                    {
                        var name = (plugin as Type).Name;

                        var cs = new NKScriptExportProxy<T>(plugin);
                        var appjs = cs.rewriteGeneratedStub("", ".global");
                        if (appjs != "")
                        {
                            var script = new NKScriptSource(appjs, ns + "/plugin/" + name + ".js");
                            NKLogging.log(script.source);
                            script.inject(this).ContinueWith((t) =>
                            {
                                cs.initializeForContext(this);

                                NKLogging.log("+Windows Unversal Component Plugin with script loaded at " + ns + "." + name);
                            });
                        }
                        else
                        {
                            cs.initializeForContext(this);
                            NKLogging.log("+Windows Unversal Component Plugin loaded at " + ns + "." + name);
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException("Unknown Scripting Plugin Bridge Option");
            }
        }

        private Dictionary<IntPtr, string> callBacktoScriptMessageHandlerName = new Dictionary<IntPtr, string>();
        private Dictionary<IntPtr, NKScriptMessageHandler> callBacktoScriptMessageHandler = new Dictionary<IntPtr, NKScriptMessageHandler>();
     
        public override void NKaddScriptMessageHandler(NKScriptMessageHandler scriptMessageHandler, string name)
        {
  
            ensureOnEngineThread(() =>
            {        
               switchContextifNeeded();
                var global = JavaScriptValue.GlobalObject;
                var NKScripting = global.GetProperty(JavaScriptPropertyId.FromString("NKScripting"));
                var messageHandlers = NKScripting.GetProperty(JavaScriptPropertyId.FromString("messageHandlers"));
                NKScripting.SetProperty(JavaScriptPropertyId.FromString("serialize"), JavaScriptValue.FromBoolean(true), true);

                var nameProperty = JavaScriptPropertyId.FromString(name);
                JavaScriptValue namedHandler;
                if (messageHandlers.HasProperty(nameProperty))
                    namedHandler = messageHandlers.GetProperty(nameProperty);
                else
                {
                    namedHandler = JavaScriptValue.CreateObject();
                    messageHandlers.SetProperty(nameProperty, namedHandler, true);
                }

                GCHandle dataCallBackHandle = GCHandle.Alloc(scriptMessageHandler);
                IntPtr dataCallBack = (IntPtr)dataCallBackHandle;
                callBacktoScriptMessageHandlerName[dataCallBack] = name;
                callBacktoScriptMessageHandler[dataCallBack] = scriptMessageHandler;

                var postMessageFunction = JavaScriptValue.CreateFunction(postMessage, dataCallBack);
                namedHandler.SetProperty(JavaScriptPropertyId.FromString("postMessage"), postMessageFunction, true);

                var postMessageSyncFunction = JavaScriptValue.CreateFunction(postMessageSync, dataCallBack);
                namedHandler.SetProperty(JavaScriptPropertyId.FromString("postMessageSync"), postMessageSyncFunction, true);
            });
        }   

        public override void NKremoveScriptMessageHandlerForName(string name)
        {
            ensureOnEngineThread(() =>
            {
                var cleanup = "delete NKScripting.messageHandlers." + name;
                NKevaluateJavaScript(cleanup, "");
            });
        }

        private JavaScriptValue postMessage(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] arguments, ushort argumentCount, IntPtr callbackData)
        {
            var arg = arguments[1].ToString();
            var body = this.NKDeserialize(arg);
            var name = callBacktoScriptMessageHandlerName[callbackData];
            var scriptHandler = callBacktoScriptMessageHandler[callbackData];
            scriptHandler.didReceiveScriptMessage(new NKScriptMessage(name, body));
            return JavaScriptValue.Null;
        }

        private JavaScriptValue postMessageSync(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] arguments, ushort argumentCount, IntPtr callbackData)
        {
            var arg = arguments[1].ToString();
            var body = this.NKDeserialize(arg);
            var name = callBacktoScriptMessageHandlerName[callbackData];
            var scriptHandler = callBacktoScriptMessageHandler[callbackData];
            var result = scriptHandler.didReceiveScriptMessageSync(new NKScriptMessage(name, body));
            var retValueSerialized = NKserialize(result);
            return JavaScriptValue.FromString(retValueSerialized);
        }

    }
}

