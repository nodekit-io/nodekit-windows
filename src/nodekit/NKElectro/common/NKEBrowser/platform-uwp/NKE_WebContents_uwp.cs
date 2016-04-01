#if WINDOWS_UWP
/*
* nodekit.io
*
* Copyright (c) 2016 OffGrid Networks. All Rights Reserved.
* Portions Copyright (c) 2013 GitHub, Inc. under MIT License
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
using io.nodekit.NKScripting;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using io.nodekit.NKScripting.Engines.MSWebView;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace io.nodekit.NKElectro
{
    public partial class NKE_WebContents
    {
        internal WebView webView;
        private bool _isLoading;

        internal async Task createWebView(Dictionary<string, object> options)
        {
            _browserWindow._window = await _browserWindow.createWindow(options);

            string url;
            if (options.ContainsKey(NKEBrowserOptions.kPreloadURL))
                url = (string)options[NKEBrowserOptions.kPreloadURL];
            else
                url = NKEBrowserDefaults.kPreloadURL;

            WebView webView = new WebView(WebViewExecutionMode.SeparateThread);
            this.webView = webView;
            _browserWindow.webView = webView;

            _browserWindow._window.controls.Add(webView);
            webView.Navigate(new Uri(url));
            _browserWindow.context = await NKSMSWebViewContext.getScriptContext(_id, webView, options);

            webView.NavigationStarting += WebView_NavigationStarting;
            webView.NavigationCompleted += this.WebView_NavigationCompleted;

            this.init_IPC();

            this._type = NKEBrowserType.MSWebView.ToString();
            if (options.itemOrDefault<bool>("NKE.InstallElectro", true))
                await Renderer.addElectro(_browserWindow.context, options);
            NKLogging.log(string.Format("+E{0} Renderer Ready", _id));

            _browserWindow.events.emit("NKE.DidFinishLoad", _id);

        }

        private void WebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            _isLoading = false;

            if (args.IsSuccess)
                _browserWindow.events.emit("NKE.DidFinishLoad", _id);
            else
                _browserWindow.events.emit("NKE.DidFailLoading", new Tuple<int, string>(_id, args.WebErrorStatus.ToString()));
        }

        private void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            _isLoading = true;
        }

        public void loadURL(string url, Dictionary<string, object> options)
        {
            var httpReferrer = options.itemOrDefault<string>("httpReferrer");
            var userAgent = options.itemOrDefault<string>("userAgent");
            var extraHeaders = options.itemOrDefault<Dictionary<string, object>>("extraHeaders");
            var uri = new Uri(url);

            var request = new Windows.Web.Http.HttpRequestMessage(Windows.Web.Http.HttpMethod.Get, uri);
            if ((userAgent) != null)
                  request.Headers["User-Agent"] = userAgent;

            if ((httpReferrer) != null)
                request.Headers["Referrer"] = httpReferrer;

            if ((extraHeaders) != null)
            {
                foreach (var item in extraHeaders)
                {
                    request.Headers[item.Key] = item.Value as string;
                }
            }

            webView.NavigateWithHttpRequestMessage(request);
        }
        /*
        
    func isLoading()  -> Bool { return self.webView?.loading ?? false }
*/

        public string getURL()
        {
            return webView.Source.AbsoluteUri;
        }

        public string getTitle()
        {
      //      return "HELLO TITLE";
           return webView.DocumentTitle;
        }

        public bool isLoading()
        {
            return _isLoading;
        }

        public bool canGoBack()
        {
            return webView.CanGoBack;
        }

        public bool canGoForward()
        {
            return webView.CanGoForward;
        }

        public void executeJavaScript(string code, string userGesture)
        {
            _browserWindow.context.NKevaluateJavaScript(code);
        }
   
        public string getUserAgent()
        {
            return webView.InvokeScript("eval", new[] { "navigator.userAgent" });
        }

        public void goBack()
        {
            webView.GoBack();
        }

        public void goForward()
        {
            webView.GoForward();
        }

         public void reload()
        {
            webView.Refresh();
        }

        public void reloadIgnoringCache()
        {
            webView.Navigate(webView.Source);
        }

        public void setUserAgent(string userAgent)
        {
            throw new NotImplementedException();
        }

        public void stop()
        {
            webView.Stop();
        }

        /* ****************************************************************** *
         *               REMAINDER OF ELECTRO API NOT IMPLEMENTED             *
         * ****************************************************************** */
        public NKScriptValue getSession()
        {
            throw new NotImplementedException();
        }

        public void addWorkSpace(string path)
        {
            throw new NotImplementedException();
        }

        public void beginFrameSubscription(NKScriptValue callback)
        {
            throw new NotImplementedException();
        }

        public void canGoToOffset(int offset)
        {
            throw new NotImplementedException();
        }

        public void clearHistory()
        {
            throw new NotImplementedException();
        }

        public void closeDevTools()
        {
            throw new NotImplementedException();
        }

        public void copyclipboard()
        {
            throw new NotImplementedException();
        }

        public void cut()
        {
            throw new NotImplementedException();
        }

        public void delete()
        {
            throw new NotImplementedException();
        }

        public void disableDeviceEmulation()
        {
            throw new NotImplementedException();
        }

        public void downloadURL(string url)
        {
            throw new NotImplementedException();
        }

        public void enableDeviceEmulation(Dictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }

        public void endFrameSubscription()
        {
            throw new NotImplementedException();
        }

        public void goToIndex(int index)
        {
            throw new NotImplementedException();
        }

        public void goToOffset(int offset)
        {
            throw new NotImplementedException();
        }

        public void hasServiceWorker(NKScriptValue callback)
        {
            throw new NotImplementedException();
        }

        public void insertCSS(string css)
        {
            throw new NotImplementedException();
        }

        public void inspectElement(int x, int y)
        {
            throw new NotImplementedException();
        }

        public void inspectServiceWorker()
        {
            throw new NotImplementedException();
        }

        public bool isAudioMuted()
        {
            throw new NotImplementedException();
        }

        public void isCrashed()
        {
            throw new NotImplementedException();
        }

        public void isDevToolsFocused()
        {
            throw new NotImplementedException();
        }

        public void isDevToolsOpened()
        {
            throw new NotImplementedException();
        }

        public bool isWaitingForResponse()
        {
            throw new NotImplementedException();
        }

        public void openDevTools(Dictionary<string, object> options)
        {
            throw new NotImplementedException();
        }

        public void paste()
        {
            throw new NotImplementedException();
        }

        public void pasteAndMatchStyle()
        {
            throw new NotImplementedException();
        }

        public void print(Dictionary<string, object> options)
        {
            throw new NotImplementedException();
        }

        public void printToPDF(Dictionary<string, object> options, NKScriptValue callback)
        {
            throw new NotImplementedException();
        }

        public void redo()
        {
            throw new NotImplementedException();
        }

        public void removeWorkSpace(string path)
        {
            throw new NotImplementedException();
        }

        public void replace(string text)
        {
            throw new NotImplementedException();
        }

        public void replaceMisspelling(string text)
        {
            throw new NotImplementedException();
        }

        public void savePage(string fullstring, string saveType, NKScriptValue callback)
        {
            throw new NotImplementedException();
        }

        public void selectAll()
        {
            throw new NotImplementedException();
        }

        public void sendInputEvent(Dictionary<string, object> e)
        {
            throw new NotImplementedException();
        }

        public void setAudioMuted(bool muted)
        {
            throw new NotImplementedException();
        }

        public void toggleDevTools()
        {
            throw new NotImplementedException();
        }

        public void undo()
        {
            throw new NotImplementedException();
        }

        public void unregisterServiceWorker(NKScriptValue callback)
        {
            throw new NotImplementedException();
        }

        public void unselect()
        {
            throw new NotImplementedException();
        } 

        // Event:  'certificate-error'
        // Event:  'crashed'
        // Event:  'destroyed'
        // Event:  'devtools-closed'
        // Event:  'devtools-focused'
        // Event:  'devtools-opened'
        // Event:  'did-frame-finish-load'
        // Event:  'did-get-redirect-request'
        // Event:  'did-get-response-details'
        // Event:  'did-start-loading'
        // Event:  'did-stop-loading'
        // Event:  'dom-ready'
        // Event:  'login'
        // Event:  'new-window'
        // Event:  'page-favicon-updated'
        // Event:  'plugin-crashed'
        // Event:  'select-client-certificate'
        // Event:  'will-navigate'
        
    }
}
#endif