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

namespace io.nodekit.NKElectro
{
    public partial class NKE_BrowserWindow : NKScriptExport
    {
        internal NKEventEmitter events = new NKEventEmitter();
        private static Dictionary<int, NKE_BrowserWindow> windowArray = new Dictionary<int, NKE_BrowserWindow>();
        internal object window;

        internal NKScriptContext context;
        internal object webView;
        internal NKEBrowserType browserType;


        private int _id = 0;
        private string _type = "";
        private Dictionary<string, object> _options = new Dictionary<string, object>();
        private object _nke_renderer;
        private NKE_WebContentsBase _webContents;

        public NKE_BrowserWindow() { }

        public NKE_BrowserWindow(Dictionary<string, object> options)
        {
            // PARSE & STORE OPTIONS
            if (options.ContainsKey("nk.InstallElectro"))
                _options["nk.InstallElectro"] = options["nk.InstallElectro"];
            else
                _options["nk.InstallElectro"] = true;

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
                case NKEBrowserType.Edge:
                    NKLogging.log("+creating Edge Renderer");
                    _id = this.createWebView(options);
                    _type = NKEBrowserType.Edge.ToString();
                    NKE_WebContents webContents = new NKE_WebContents(this);
                    _webContents = webContents;
                    break;
                default:
                    break;
            }

            windowArray[_id] = this;
        }

        // class/helper functions (for C# use only, equivalent functions exist in .js helper )
        public static NKE_BrowserWindow fromId(int id) { return windowArray[id]; }

        public int id { get { return _id; } }
        public string type { get { return _type; } }
        public NKE_WebContentsBase webContents { get { return _webContents; } }

        #region NKScriptExport
        private static string defaultNamespace { get { return "io.nodekit.electro.BrowserWindow"; } }

        private static string rewriteGeneratedStub(string stub, string forKey)
        {
            switch (forKey)
            {
                case ".global":
                    var appjs = NKStorage.getResource(typeof(NKE_BrowserWindow), "browser-window.js", "lib_electro");
                    return "function loadplugin(){\n" + appjs + "\n}\n" + stub + "\n" + "loadplugin();" + "\n";
                default:
                    return stub;
            }
        }
        
/*     private static bool isExcludedFromScript(string key)
       {
               return key.StartsWith("webView") ||
                  key.StartsWith("NKScriptEngineLoaded") ||
                   key.StartsWith("NKApplicationReady"); 
       }                                              */

        private static string rewritescriptNameForKey(string key)
        {
            return key == ".ctor" ? "" : key;
        }
        #endregion

    }
}

       

         /*
        internal func NKScriptEngineDidLoad(context: NKScriptContext) -> Void {
        log("+E\(context.NKid) Renderer Loaded")

        if (!(self._options["nk.InstallElectro"] as! Bool)) { return;}
        self._context = context

        // INSTALL JAVASCRIPT ENVIRONMENT ON RENDERER CONTEXT
        NKE_BootElectroRenderer.bootTo(context)
    }

    internal func NKScriptEngineReady(context: NKScriptContext) -> Void {
        switch self._browserType {
        case .WKWebView:
            WKScriptEnvironmentReady()
         case .UIWebView:
            UIScriptEnvironmentReady()
        }
        log("+E\(id) Renderer Ready")
        
    } */
