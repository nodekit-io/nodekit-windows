#if WINDOWS_WIN32
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
using System.Threading.Tasks;
using io.nodekit.NKScripting.Engines.MSWebBrowser;
#if WINDOWS_WIN32_WPF
using System.Windows.Controls;
#elif WINDOWS_WIN32_WF
using System.Windows.Forms;
#endif

namespace io.nodekit.NKElectro
{
    public partial class NKE_WebContents
    {
        internal WebBrowser webView;
        private bool _isLoading;

        internal async Task createWebView(Dictionary<string, object> options)
        {
            try
            {
            //    throw new NotImplementedException();
#if WINDOWS_WIN32_WPF
                _browserWindow.createWindow(options);
                WebBrowser webView = _browserWindow._window.webBrowser;
#elif WINDOWS_WIN32_WF
                     WebBrowser webView = new WebBrowser();       
                    _browserWindow.createWindow(options, webView);
#endif
                this.webView = webView;
                _browserWindow.webView = webView;

                string url;
                if (options.ContainsKey(NKEBrowserOptions.kPreloadURL))
                    url = (string)options[NKEBrowserOptions.kPreloadURL];
                else
                    url = NKEBrowserDefaults.kPreloadURL;

                webView.Navigate(new Uri(url));

#if WINDOWS_WIN32_WPF
                webView.Navigating += this.WebView_Navigating;
                webView.LoadCompleted += this.WebView_LoadCompleted;
#elif WINDOWS_WIN32_WF
                    webView.Navigating += this.WebView_Navigating;
                    webView.DocumentCompleted += this.WebView_DocumentCompleted;
#endif
                this.init_IPC();

                _browserWindow.context = await NKSMSWebBrowserContext.getScriptContext(_id, webView, options);

                this._type = NKEBrowserType.MSWebView.ToString();
                if (options.itemOrDefault<bool>("NKE.InstallElectro", true))
                    await Renderer.addElectro(_browserWindow.context, options);
                NKLogging.log(string.Format("+E{0} Renderer Ready", _id));

                _browserWindow.events.emit("NKE.DidFinishLoad", _id);
      
            }
            catch (Exception ex)
            {
                NKLogging.log("!Error creating browser webcontent: " + ex.Message);
                NKLogging.log(ex.StackTrace);
            }
            options = null;

        }


#if WINDOWS_WIN32_WPF
        private void WebView_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            _isLoading = false;
        }

        private void WebView_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            _isLoading = true;
        }
#elif WINDOWS_WIN32_WF
        private void WebView_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            _isLoading = false;
        }

        private void WebView_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            _isLoading = true;
        }
#endif

          public void loadURL(string url, Dictionary<string, object> options)
        {
            var httpReferrer = options.itemOrDefault<string>("httpReferrer");
            var userAgent = options.itemOrDefault<string>("userAgent");
            var extraHeaders = "";

            foreach (var item in options.itemOrDefault<Dictionary<string, object>>("extraHeaders", new Dictionary<string, object>()))
            {
                extraHeaders += item.Key + ": " + item.Value + "\n";
            }
            var uri = new Uri(url);

            webView.Navigate(uri, "_self", null, extraHeaders);
        }

        public string getURLSync()
        {

#if WINDOWS_WIN32_WPF
              return webView.Source.AbsoluteUri;
#elif WINDOWS_WIN32_WF
               return webView.Document.Url.AbsoluteUri;
#endif
        }

        public string getTitle()
        {
#if WINDOWS_WIN32_WPF
            return (string)webView.InvokeScript("eval", new object[] { "document.title" });
#elif WINDOWS_WIN32_WF
           return (string)webView.Document.InvokeScript("eval", new object[] { "document.title" });
#endif
        }

        public bool isLoadingSync()
        {
            return _isLoading;
        }

        public bool canGoBackSync()
        {
            return webView.CanGoBack;
        }

        public bool canGoForwardSync()
        {
            return webView.CanGoForward;
        }

        public void executeJavaScript(string code, string userGesture)
        {
            _browserWindow.context.NKevaluateJavaScript(code);
        }

        public string getUserAgent()
        {
#if WINDOWS_WIN32_WPF
            return (string)webView.InvokeScript("eval", new[] { "navigator.userAgent" });
#elif WINDOWS_WIN32_WF
           return (string)webView.Document.InvokeScript("eval", new[] { "navigator.userAgent" });
#endif
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
#if WINDOWS_WIN32_WPF
            webView.Refresh(true);
#elif WINDOWS_WIN32_WF
            webView.Refresh(WebBrowserRefreshOption.Completely);
#endif
        }

        public void setUserAgent(string userAgent)
        {
            throw new NotImplementedException();
        }

        public void stop()
        {
            throw new NotImplementedException();
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