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
using System.Threading;
using System.Threading.Tasks;

#if WINDOWS_UWP
namespace io.nodekit.NKScripting.Engines.Chakra
#elif WINDOWS_WIN32
namespace io.nodekit.NKScripting.Engines.ChakraCore
#endif
{
    public class NKSChakraContext : NKScriptContextSyncBase, NKScriptContentController
    {
        private static JavaScriptSourceContext currentSourceContext = JavaScriptSourceContext.FromIntPtr(IntPtr.Zero);

        [ThreadStatic]
        internal static JavaScriptContext currentContext ;

        private JavaScriptContext _context;
        private Queue<JavaScriptValue> _jsTaskQueue = new Queue<JavaScriptValue>();
        private NKSChakraScriptDelegate _webViewScriptDelegate;
       
        public NKSChakraContext(int id, JavaScriptContext context, Dictionary<string, object> options) : base(id)
        {
            _async_queue = TaskScheduler.Current;

            this._context = context;
     
            JavaScriptPromiseContinuationCallback promiseContinuationCallback = delegate (JavaScriptValue jsTask, IntPtr callbackState)
            {
                _jsTaskQueue.Enqueue(jsTask);
            };

            ensureOnEngineThread(() =>
            {
                NKSChakraContext.currentContext = context;

                JavaScriptContext.Current = context;

#if WINDOWS_UWP
            Native.ThrowIfError(Native.JsSetPromiseContinuationCallback(promiseContinuationCallback, IntPtr.Zero));
            Native.ThrowIfError(Native.JsProjectWinRTNamespace("Windows"));
         // if (options.ContainsKey("nk.ScriptingDebug") && ((bool)options["nk.ScriptingDebug"] == true);
                 Native.ThrowIfError(Native.JsStartDebugging());
#endif
            });

        }

        public void switchContextifNeeded()
        {
            if (!_context.Equals(currentContext))
            {
                JavaScriptContext.Current = _context;
                currentContext = _context;
            }
        }

        async override protected Task PrepareEnvironment()
        {
            switchContextifNeeded();
            var global = JavaScriptValue.GlobalObject;
            var NKScripting = global.GetProperty(JavaScriptPropertyId.FromString("NKScripting"));

            // var source = await NKStorage.getResourceAsync(typeof(NKScriptContext), "promise.js", "lib");
            // var script = new NKScriptSource(source, "io.nodekit.scripting/NKScripting/promise.js", "Promise", null);
            // await script.inject(this);

            _webViewScriptDelegate = new NKSChakraScriptDelegate(this);
            await _webViewScriptDelegate.createBridge();

            var source2 = await NKStorage.getResourceAsync(typeof(NKScriptContext), "init_chakra.js", "lib");
            var script2 = new NKScriptSource(source2, "io.nodekit.scripting/NKScripting/init_chakra.js");
            await this.NKinjectScript(script2);
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

        protected override Task InjectScript(NKScriptSource script)
        {
            return this.NKevaluateJavaScript(script.source, script.filename);
        }

        protected List<string> _projectedNamespaces = new List<string>();

#if WINDOWS_UWP
        protected override async Task LoadPlugin<T>(T plugin, string ns, Dictionary<string, object> options) 
        {
            bool mainThread = (bool)options["MainThread"];
            NKScriptExportType bridge = (NKScriptExportType)options["PluginBridge"];

            switch (bridge)
            {
                case NKScriptExportType.JSExport:
                    throw new NotSupportedException("JSExport option is for darwin platforms only");
                case NKScriptExportType.WinRT:

                    if (plugin == null)
                    {
                        switchContextifNeeded();
                        if (!_projectedNamespaces.Contains(ns))
                        {
                            Native.ThrowIfError(Native.JsProjectWinRTNamespace(ns));
                            _projectedNamespaces.Add(ns);
                            NKLogging.log("+Windows Unversal Component namespace loaded at " + ns);
                        }
                    }
                    else if (typeof(T) != typeof(Type))
                        throw new ArgumentException("Windows Universal Components can only be provided as a type");
                    else
                    {
                        var t = (plugin as Type);
                        var projectionNamespace = t.Namespace;
                        var projectionName = t.Name;
                        var projectionFullName = projectionNamespace + "." + projectionName;
                        var targetNamespace = ns;
                    
                        switchContextifNeeded();
                        if (!_projectedNamespaces.Contains(projectionNamespace))
                        {
                            Native.ThrowIfError(Native.JsProjectWinRTNamespace(projectionNamespace));
                            _projectedNamespaces.Add(projectionNamespace);
                        }

                        var cs = new NKScriptExportProxy<T>(plugin);
                        var localstub = cs.rewriteGeneratedStub("", ".local");
                        var globalstubber = "(function(exports) {\n" + localstub + "})(NKScripting.createProjection('" + targetNamespace + "', " + projectionFullName + "));\n";
                        var globalstub = cs.rewriteGeneratedStub(globalstubber, ".global");

                        var script = new NKScriptSource(globalstub, targetNamespace + "/plugin/" + projectionName + ".js");
                        await this.NKinjectScript(script);
                        await cs.initializeForContext(this);
                        plugin.setNKScriptValue(this.NKgetScriptValue(targetNamespace));

                       NKLogging.log("+Windows Unversal Component Plugin with script loaded at " + targetNamespace);
                    }
                    break;
                default:
                    throw new NotImplementedException("Unknown Scripting Plugin Bridge Option");
            }
        }
#endif
#if WINDOWS_WIN32
        protected override Task LoadPlugin<T>(T plugin, string ns, Dictionary<string, object> options)
        {
            NKScriptExportType bridge = (NKScriptExportType)options["PluginBridge"];

            switch (bridge)
            {
                case NKScriptExportType.JSExport:
                    throw new NotSupportedException("JSExport option is for darwin platforms only");
                case NKScriptExportType.WinRT:
                    throw new NotSupportedException("WinRT option is for Windows Store Applications only");
                default:
                    throw new NotImplementedException("Unknown Scripting Plugin Bridge Option");
            }
        }
#endif

        public override void NKaddScriptMessageHandler(NKScriptMessageHandler scriptMessageHandler, string name)
        {
            ((NKScriptContentController)_webViewScriptDelegate).NKaddScriptMessageHandler(scriptMessageHandler, name);
        }

        public override void NKremoveScriptMessageHandlerForName(string name)
        {
            ((NKScriptContentController)_webViewScriptDelegate).NKremoveScriptMessageHandlerForName(name);
        }
    
    }
}

