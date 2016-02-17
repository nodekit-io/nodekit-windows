#if WINDOWS_WIN32_WF
/*
* nodekit.io
*
* Copyright (c) 2016 OffGrid Networks.All Rights Reserved.
* Portions Copyright (c) 2013 GitHub, Inc.under MIT License
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace io.nodekit.NKElectro
{
    public partial class NKE_BrowserWindow
    {
        private static NKEventEmitter globalEvents = NKEventEmitter.global;
        private NKE_Window _window;
        private Dictionary<string, object> options;
      
        internal void createWebView(Dictionary<string, object> options)
        {
            this.options = options;
            ensureOnUIThread(createWebViewUI);
         }

        internal void createWebViewUI()
        {
            WebBrowser webView = new WebBrowser();

            this.webView = webView;

            createWindow(options, webView);
            string url;
            if (options.ContainsKey(NKEBrowserOptions.kPreloadURL))
                url = (string)options[NKEBrowserOptions.kPreloadURL];
            else
                url = NKEBrowserDefaults.kPreloadURL;

            webView.Navigate(new Uri(url));
            //       context = await NKSMSWebBrowserContext.getScriptContext(_id, webView, options);
            //        webView.LoadCompleted += WebView_LoadCompleted;
            //   events.emit("did-finish-load", _id);

        }

        private void WebView_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            events.emit("did-finish-load", _id);
        }

    }
}
#endif