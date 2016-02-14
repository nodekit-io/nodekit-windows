using System;
using System.Collections.Generic;
using io.nodekit;
using Windows.ApplicationModel.Core;
using io.nodekit.NKScripting;
using System.Threading.Tasks;

namespace NKElectro
{
    public sealed class NKEApp 
    {
        private NKEventEmitter events = NKEventEmitter.global;

        #region NKScriptExport
        private static string rewriteGeneratedStub(string stub, string forKey)
        {
            switch (forKey)
            {
                case ".global":
                    var appjs = NKStorage.getResource(typeof(NKEApp), "app.js", "lib_electro");
                    return "function loadplugin(){\n" + appjs + "\n}\n" + stub + "\n" + "loadplugin();" + "\n";
                default:
                    return stub;
            }
        }

        private static async Task initializeForContext(NKScriptContext context)
        {
            var jsEvents = await context.NKgetJavaScriptValue("io.nodekit.events");
            var events = NKEventEmitter.global;

            // Event: 'ready'
            events.once<string>("nk.jsApplicationReady", (data) =>
            {
                jsEvents.invokeMethod("emit", new[] { "ready" });
            });

            events.once<string>("nk.ApplicationDidFinishLaunching", (data) =>
            {
                jsEvents.invokeMethod("emit", new[] { "will-finish-launching" });
            });


            events.once<string>("nk.ApplicationWillTerminate", (data) =>
            {
                jsEvents.invokeMethod("emit", new[] { "will-quit" });
                jsEvents.invokeMethod("emit", new[] { "quit" });
            });
          }
        #endregion

        public static void quit()
        {
            CoreApplication.Exit();
        }

        public static void exit(int exitCode)
        {
            CoreApplication.Exit();
        }

        public static string getAppPath()
        {
            return NKEAppDirectory.getPath("exe");
        }

        public static string getPath(string name)
        {
            return NKEAppDirectory.getPath(name);
        }

        public static string getVersion()
        {
            return NKEAppDirectory.getVersion();
        }

        public static string getName()
        {
            return NKEAppDirectory.getName();
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
