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
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using io.nodekit.NKScripting.Engines.MSWebView;
using io.nodekit.NKScripting;

namespace io.nodekit.NKElectro
{
    public partial class NKE_BrowserWindow
    {

        private static NKEventEmitter globalEvents = NKEventEmitter.global;
        private NKE_Window _window;

        internal async Task createWebView(Dictionary<string, object> options)
        {
          
            _window = await createWindow(options);

            string url;
            if (options.ContainsKey(NKEBrowserOptions.kPreloadURL))
                url = (string)options[NKEBrowserOptions.kPreloadURL];
            else
                url = "https://google.com";

            WebView webView = new WebView(WebViewExecutionMode.SeparateThread);
            this._webView = webView;

            _window.controls.Add(webView);
            this.context = new NKSMSWebViewContext(_id, webView, options);
           
            webView.Navigate(new Uri(url));
        }

    }
}
