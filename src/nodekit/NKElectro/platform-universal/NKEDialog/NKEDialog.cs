using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using io.nodekit;
using io.nodekit.NKScripting;

namespace io.nodekit.NKElectro
{
    public sealed class NKEDialog
    {
        private NKEventEmitter events = NKEventEmitter.global;

        #region NKScriptExport

        #region NKScriptExport
        private static string defaultNamespace { get { return "io.nodekit.electro.dialog"; } }

        private static Task initializeForContext(NKScriptContext context)
        {
            var jsValue = typeof(NKEApp).getNKScriptValue();
            var events = NKEventEmitter.global;

            // Event: 'ready'
            events.once<string>("nk.jsApplicationReady", (data) =>
            {
                jsValue.invokeMethod("emit", new[] { "ready" });
            });

            events.once<string>("nk.ApplicationDidFinishLaunching", (data) =>
            {
                jsValue.invokeMethod("emit", new[] { "will-finish-launching" });
            });

            events.once<string>("nk.ApplicationWillTerminate", (data) =>
            {
                jsValue.invokeMethod("emit", new[] { "will-quit" });
                jsValue.invokeMethod("emit", new[] { "quit" });
            });

            return Task.FromResult<object>(null);
        }
        #endregion

        void showOpenDialog(NKEBrowserWindow browserWindow, Dictionary<string, object>, NKScriptValue callback)
        { }

