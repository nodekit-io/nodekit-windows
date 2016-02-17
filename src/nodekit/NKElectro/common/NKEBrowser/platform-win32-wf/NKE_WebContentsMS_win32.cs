#if WINDOWS_WIN32_WF
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
using System.Windows.Forms;

namespace io.nodekit.NKElectro
{
    public class NKE_WebContentsMS : NKE_WebContentsBase
    {
        internal WebBrowser webView;
        private bool _isLoading;
    
        public NKE_WebContentsMS(NKE_BrowserWindow browserWindow)
        {
            this._browserWindow = browserWindow;
            this._id = browserWindow.id;

            // Event:  'did-fail-load'
            // Event:  'did-finish-load'

            browserWindow.events.on<int>("did-finish-load", (id) =>
            {
                this.getNKScriptValue().invokeMethod("emit", new[] { "did-finish-load" });
            });
   
            browserWindow.events.on<Tuple<int, string>>("did-fail-loading", (item) =>
            {
                this.getNKScriptValue().invokeMethod("emit", new[] { "did-fail-loading", item.Item2 });
            });

            webView = (WebBrowser)_browserWindow.webView;
            webView.Navigating += WebView_Navigating;
            webView.DocumentCompleted += WebView_DocumentCompleted;
     
            this.init_IPC();
        }

        private void WebView_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            _isLoading = false;
        }

        private void WebView_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            _isLoading = true;
        }

        // Messages to renderer are sent to the window events queue for that renderer
        public void ipcSend(string channel, string replyId, object[] arg)
        {
            var payload = new NKE_IPC_Event(0, channel, replyId, arg);
            _browserWindow.events.emit("nk.IPCtoRenderer", payload);

            throw new NotImplementedException();
        }

        // Replies to renderer to the window events queue for that renderer
        public void ipcReply(int dest, string channel, string replyId, object result)
        {
            var payload = new NKE_IPC_Event(0, channel, replyId, new[] { result });
            _browserWindow.events.emit("nk.IPCReplytoRenderer", payload);
        }

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
      
        public string getURL()
        {
            return webView.Document.Url.AbsoluteUri;
        }

        public string getTitle()
        {
            return (string)webView.Document.InvokeScript("eval", new object[] { "document.title" });
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
            return (string)webView.Document.InvokeScript("eval", new[] { "navigator.userAgent" });
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
            webView.Navigate(webView.Document.Url);
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

        public NKScriptValue session
        {
            get
            {
                throw new NotImplementedException();
            }
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