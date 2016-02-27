#if WINDOWS_WIN32
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
using System.Threading.Tasks;
#if WINDOWS_WIN32_WPF
using System.Windows.Controls;
#elif WINDOWS_WIN32_WF
using System.Windows.Forms;
#endif

namespace io.nodekit.NKScripting.Engines.MSWebBrowser
{
    public class NKSMSWebBrowserContext : NKScriptContextAsyncBase, NKScriptContentController
    {
        private WebBrowser _webView;
        private NKSMSWebBrowserCallback _webViewCallbackBridge;
        private NKSMSWebViewScriptDelegate _webViewScriptDelegate;

        private bool _isLoaded;
        private bool _isFirstLoaded;
        private TaskCompletionSource<NKScriptContext> tcs;

        public static Task<NKScriptContext> getScriptContext(int id, WebBrowser webView, Dictionary<string, object> options)
        {
            var context = new NKSMSWebBrowserContext(id, webView, options);
            return context.tcs.Task;
        }

        private NKSMSWebBrowserContext(int id, WebBrowser webView, Dictionary<string, object> options) : base(id)
        {
            _async_queue = TaskScheduler.FromCurrentSynchronizationContext();
            this._isLoaded = false;
            this._isFirstLoaded = false;
            this._webView = webView;
            this._id = id;
            this.tcs = new TaskCompletionSource<NKScriptContext>();
            _webViewScriptDelegate = new NKSMSWebViewScriptDelegate(this);
            _webViewCallbackBridge = new NKSMSWebBrowserCallback(_webViewScriptDelegate);
            _webView.Navigating += _webView_Navigating;

#if WINDOWS_WIN32_WPF
            webView.LoadCompleted += _webView_LoadCompleted;
#elif WINDOWS_WIN32_WF
            webView.DocumentCompleted += _webView_DocumentCompleted;
#endif
            _webView.ObjectForScripting = _webViewCallbackBridge;
            NKLogging.log("+NodeKit Trident JavaScript Engine E" + id);
        }

#if WINDOWS_WIN32_WPF
        private void _webView_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            var _ = this.completeInitialization();
        }

        private void _webView_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            this._isLoaded = false;
        }
#elif WINDOWS_WIN32_WF
        private void _webView_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            var _ = this.completeInitialization();
        }

        private void _webView_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            this._isLoaded = false;
        }
#endif

        protected override Task InjectScript(NKScriptSource script)
        {
            if (_isLoaded)
              return this.NKevaluateJavaScript(script.source, script.filename);

            // otherwise will be injected in completeInitialization;
            return Task.FromResult<object>(null);
        }

        async override protected Task PrepareEnvironment()
        {

            if (!_isFirstLoaded)
            {
                var source = await NKStorage.getResourceAsync(typeof(NKScriptContext), "promise.js", "lib");
                var script = new NKScriptSource(source, "io.nodekit.scripting/NKScripting/promise.js", "Promise", null);
                await this.NKinjectScript(script);

                var source2 = await NKStorage.getResourceAsync(typeof(NKScriptContext), "init_trident.js", "lib");
                var script2 = new NKScriptSource(source2, "io.nodekit.scripting/NKScripting/init_trident.js");
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

        protected override Task<object> RunScript(string javaScriptString, string filename)
        {
#if WINDOWS_WIN32_WPF
           var result = _webView.InvokeScript("eval", new[] { javaScriptString });
#elif WINDOWS_WIN32_WF
            var result = _webView.Document.InvokeScript("eval", new[] { javaScriptString });
#endif
            return Task.FromResult<object>(result);
        }

        protected List<string> _projectedNamespaces = new List<string>();

        protected override Task LoadPlugin<T>(T plugin, string ns, Dictionary<string, object> options) 
        {
            bool mainThread = (bool)options["NKS.MainThread"];
            NKScriptExportType bridge = (NKScriptExportType)options["NKS.PluginBridge"];

            switch (bridge)
            {
                case NKScriptExportType.JSExport:
                    throw new NotSupportedException("JSExport option is for darwin platforms only");
                case NKScriptExportType.WinRT:
                    throw new NotSupportedException("WinRT option is for Windows Store Apps only");
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
