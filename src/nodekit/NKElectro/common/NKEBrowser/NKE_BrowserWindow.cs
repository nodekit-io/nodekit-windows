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

namespace io.nodekit.NKElectro
{
    public partial class NKE_BrowserWindow : NKScriptExport, IDisposable
    {
        internal NKEventEmitter events = new NKEventEmitter();
        private static Dictionary<int, NKE_BrowserWindow> windowArray = new Dictionary<int, NKE_BrowserWindow>();
    
        internal NKScriptContext context;
        internal object webView;
        internal NKEBrowserType browserType;

        private int _id = 0;
        private string _type = "";
        private NKE_WebContents _webContents;
        protected NKEventEmitter globalEvents = NKEventEmitter.global;

        public NKE_BrowserWindow() { }

        public NKE_BrowserWindow(Dictionary<string, object> options)
        {
            _id = NKScriptContextFactory.sequenceNumber++;

            if (options == null)
                options = new Dictionary<string, object>();

            windowArray[_id] = this;
            try
            {
                _webContents = new NKE_WebContents(this);
                var _ = createBrowserWindow(options);  
            }
            catch (Exception ex)
            {
                NKLogging.log("!Error Creating Window" + ex.Message);
                NKLogging.log(ex.StackTrace);
            }
            events.on<int>("NKE.DidFinishLoad", (e, id) =>
            {
                this.getNKScriptValue().invokeMethod("emit", new[] { "did-finish-load" });
            });
   
        }

        private async Task createBrowserWindow(Dictionary<string, object> options)
        {
            // PARSE & STORE OPTIONS
            if (options.ContainsKey(NKEBrowserOptions.nkBrowserType))
                browserType = (NKEBrowserType)Enum.Parse(typeof(NKEBrowserType), (options[NKEBrowserOptions.nkBrowserType]) as string);
            else
                browserType = NKEBrowserDefaults.nkBrowserType;

            switch (browserType)
            {
                case NKEBrowserType.WKWebView:
                    throw new PlatformNotSupportedException();
                case NKEBrowserType.UIWebView:
                    throw new PlatformNotSupportedException();
                case NKEBrowserType.MSWebView:
                     NKLogging.log("+creating Native (Edge/Trident) Renderer");
                 await _webContents.createWebView(options);
                        break;
                default:
                    break;
            }
        }

        // class/helper functions (for C# use only, equivalent functions exist in .js helper )
        internal static NKE_BrowserWindow fromId(int id) { return windowArray[id]; }

        internal int id { get { return _id; } }
        internal string type { get { return _type; } }
        public NKE_WebContents webContents { get { return _webContents; } }

        #region NKScriptExport
        private static string defaultNamespace { get { return "io.nodekit.electro.BrowserWindow"; } }

        private static string rewriteGeneratedStub(string stub, string forKey)
        {
            switch (forKey)
            {
                case ".global":
                    var appjs = NKStorage.getResource(typeof(NKE_BrowserWindow), "browserWindow.js", "lib_electro");
                    return "function loadplugin(){\n" + appjs + "\n}\n" + stub + "\n" + "loadplugin();" + "\n";
                default:
                    return stub;
            }
        }
        
        private static string rewritescriptNameForKey(string key, string name)
        {
            return key == ".ctor:options" ? "" : name;
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_webContents != null)
                    {
                        _webContents.Dispose();
                        _webContents = null;
                    }
                }

                this.getNKScriptValue().Dispose();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
