#if WINDOWS_UWP
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
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using io.nodekit.NKScripting.Engines.MSWebView.Callbacks;
using Windows.Foundation;

namespace io.nodekit.NKScripting.Engines.MSWebView
{
    public class NKSMSWebViewContext : NKScriptContextAsyncBase, NKScriptContentController
    {
        private WebView _webView;
        private NKSMSWebViewCallback _webViewCallbackBridge;
        private NKSMSWebViewScriptDelegate _webViewScriptDelegate;

        private bool _isLoaded;
        private bool _isFirstLoaded;
        private TaskCompletionSource<NKScriptContext> tcs;

        public static Task<NKScriptContext> getScriptContext(int id, WebView webView, Dictionary<string, object> options)
        {
            var context = new NKSMSWebViewContext(id, webView, options);
            return context.tcs.Task;
        }

        private NKSMSWebViewContext(int id, WebView webView, Dictionary<string, object> options) : base(id)
        {
            _async_queue = TaskScheduler.FromCurrentSynchronizationContext();
            this._isLoaded = false;
            this._isFirstLoaded = false;
            this._webView = webView;
            this._id = id;
            this.tcs = new TaskCompletionSource<NKScriptContext>();
            _webViewScriptDelegate = new NKSMSWebViewScriptDelegate(this);
            _webViewCallbackBridge = new NKSMSWebViewCallback(_webViewScriptDelegate);
            _webView.NavigationStarting += _webView_NavigationStarting;
            _webView.DOMContentLoaded += _webView_DOMContentLoaded;
            NKLogging.log("+NodeKit Edge JavaScript Engine E" + id);
        }

        private void _webView_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
            var ignore = this.completeInitialization();
        }

        protected override Task InjectScript(NKScriptSource script)
        {
            if (_isLoaded)
              return this.NKevaluateJavaScript(script.source, script.filename);

            // otherwise will be injected in completeInitialization;
            return Task.FromResult<object>(null);
        }

        private void _webView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            this._isLoaded = false;
            _webView.AddWebAllowedObject("NKScriptingBridge", _webViewCallbackBridge);
        }

        async override protected Task PrepareEnvironment()
        {
            if (!_isFirstLoaded)
            {
                // var source = await NKStorage.getResourceAsync(typeof(NKScriptContext), "promise.js", "lib");
                // var script = new NKScriptSource(source, "io.nodekit.scripting/NKScripting/promise.js", "Promise", null);
                // this.NKinjectScript(script);

                var source2 = await NKStorage.getResourceAsync(typeof(NKScriptContext), "init_edge.js", "lib");
                var script2 = new NKScriptSource(source2, "io.nodekit.scripting/NKScripting/init_edge.js");
                await this.NKinjectScript(script2);
                 
            }

            _isLoaded = true;

            foreach (var item in _injectedScripts)
            {
                await this.NKevaluateJavaScript(item.source, item.filename);
            }

            if (!_isFirstLoaded)
            {
                _isFirstLoaded = true;
                this.tcs.SetResult(this);
            }
        }

        protected override async Task<object> RunScript(string javaScriptString, string filename)
        {
            var result = await _webView.InvokeScriptAsync("eval", new[] { javaScriptString });
            return result;
        }

        protected List<string> _projectedNamespaces = new List<string>();

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
                        throw new NotSupportedException("plugin object must be provided for renderer webview projection");
                    }
                    else if (typeof(T) != typeof(Type))
                        throw new ArgumentException("Windows Universal Components can only be provided as a type");
                    else
                    {
                        Type t;
                        if (typeof(T) != typeof(Type))
                            t = (plugin as Type);
                        else
                            t = plugin.GetType();
                        var projectionNamespace = t.Namespace;
                        var projectionName = t.Name;
                        var projectionFullName = (projectionNamespace + "." + projectionName).Replace('.', '_');
                        var targetNamespace = ns;
                    
                        if (!_projectedNamespaces.Contains(projectionFullName))
                        {
                            _webView.AddWebAllowedObject(projectionFullName, plugin);
                            _projectedNamespaces.Add(projectionFullName);
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
#endif
