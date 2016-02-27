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
using io.nodekit.NKScripting;

namespace io.nodekit.NKElectro
{
    public sealed class NKE_App 
    {
        private NKEventEmitter events = NKEventEmitter.global;

        #region NKScriptExport
        private static string defaultNamespace { get { return "io.nodekit.electro.app";  } }

        private static string rewriteGeneratedStub(string stub, string forKey)
        {
            switch (forKey)
            {
                case ".global":
                    var appjs = NKStorage.getResource(typeof(NKE_App), "app.js", "lib_electro");
                    return "function loadplugin(){\n" + appjs + "\n}\n" + stub + "\n" + "loadplugin();" + "\n";
                default:
                    return stub;
            }
        }

        private static Task initializeForContext(NKScriptContext context)
        {
            var jsValue = typeof(NKE_App).getNKScriptValue();
            var events = NKEventEmitter.global;

            // Event: 'ready'
            events.once<string>("NK.AppReady", (e, data) =>
            {
                jsValue.invokeMethod("emit", new[] { "ready" });
            });

            events.once<string>("NK.AppDidFinishLaunching", (e, data) =>
            {
                jsValue.invokeMethod("emit", new[] { "will-finish-launching" });
            });

            events.once<string>("NK.AppWillTerminate", (e, data) =>
            {
                jsValue.invokeMethod("emit", new[] { "will-quit" });
                jsValue.invokeMethod("emit", new[] { "quit" });
            });

            return Task.FromResult<object>(null);
          }
        #endregion

        public static void quit()
        {
#if WINDOWS_UWP
            Windows.ApplicationModel.Core.CoreApplication.Exit();
#endif
#if WINDOWS_WIN32_WPF
            System.Windows.Application.Current.Shutdown();
#endif
#if WINDOWS_WIN32_WF
            System.Windows.Forms.Application.Exit();
#endif
        }

        public static void exit(int exitCode)
        {
#if WINDOWS_UWP
            Windows.ApplicationModel.Core.CoreApplication.Exit();
#endif
#if WINDOWS_WIN32_WPF
            System.Windows.Application.Current.Shutdown(exitCode);
#endif
#if WINDOWS_WIN32_WF
            System.Windows.Forms.Application.Exit();
#endif
        }

        public static string getAppPath()
        {
            return NKE_AppDirectory.getPath("exe");
        }

        public static string getPath(string name)
        {
            return NKE_AppDirectory.getPath(name);
        }

        public static string getVersion()
        {
            return NKE_AppDirectory.getVersion();
        }

        public static string getName()
        {
            return NKE_AppDirectory.getName();
        }


        // NOT IMPLEMENTED
       public static void addRecentDocument(string path)
        {
            throw new NotImplementedException();
        }

        public static void allowNTLMCredentialsForAllDomains(bool allow)
        {
            throw new NotImplementedException();
        }

        public static void appendArgument(string value)
        {
            throw new NotImplementedException();
        }

        public static void appendSwitch(string switchvalue, string value)
        {
            throw new NotImplementedException();
        }

        public static void clearRecentDocuments(string path)
        {
            throw new NotImplementedException();
        }

        public static int dockBounce(string type)
        {
            throw new NotImplementedException();
        }

        public static void dockCancelBounce(int id)
        {
            throw new NotImplementedException();
        }

        public static string dockGetBadge()
        {
            throw new NotImplementedException();
        }

        public static void dockHide()
        {
            throw new NotImplementedException();
        }

        public static void dockSetBadge(string text)
        {
            throw new NotImplementedException();
        }

        public static void dockSetMenu(object menu)
        {
            throw new NotImplementedException();
        }

        public static void dockShow()
        {
            throw new NotImplementedException();
        }

    
        public static string getLocale()
        {
            throw new NotImplementedException();
        }

   
   
        public static void makeSingleInstance()
        {
            throw new NotImplementedException();
        }


        public static void setAppUserModelId(string id)
        {
            throw new NotImplementedException();
        }

        public static string setPath(string name, string path)
        {
            throw new NotImplementedException();
        }

        public static void setUserTasks(IDictionary<string, object> tasks)
        {
            throw new NotImplementedException();
        }



        // Event: 'window-all-closed'
        // Event: 'before-quit'
        // Event: 'will-quit'
        // Event: 'quit'
        // Event: 'open-file' OS X
        // Event: 'open-url' OS X
        // Event: 'activate' OS X
        // Event: 'browser-window-blur'
        // Event: 'browser-window-focus'
        // Event: 'browser-window-created'
        // Event: 'certificate-error'
        // Event: 'select-client-certificate'
        // Event: 'login'
        // Event: 'gpu-process-crashed'   
    }
}
