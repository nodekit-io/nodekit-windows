using System;
using System.Collections.Generic;
using io.nodekit.NKScripting;
using System.Threading.Tasks;

namespace io.nodekit
{
    public class NKC_NodeKit
    {

        public static void start(Dictionary<string, object> options)
        {
            var nodekit = new NKC_NodeKit();
            string[] args = NKOptions.itemOrDefault<string[]>(options, "NKS.Args", new string[0]);
            if (args.Length > 0 && args[0].StartsWith("NKR="))
            {
                var _ = nodekit.startNodeKitRenderer(options);
            }
            else
            {
                var _ = nodekit.startNodeKitMain(options);
            }
        }

        private NKScriptContext context;
        NKScriptContextRemotingProxy proxy;

        private async Task startNodeKitMain(Dictionary<string, object> options)
        {
            NKEventEmitter.isMainProcess = true;

            options.set("NKS.MainThreadScheduler", TaskScheduler.FromCurrentSynchronizationContext());
            options.set("NKS.MainThreadId", Environment.CurrentManagedThreadId);
            var entryClass = options.itemOrDefault("NKS.Entry", typeof(NKC_NodeKit));
      
            context = await NKScriptContextFactory.createContext(options);
         
            // SCRIPT ENGINE LOADED, ADD {NK} NODEKIT
            await NKElectro.Main.addElectro(context, options);

            // {NK} NODEKIT ADDED, START APPLICATION 
            var appjs = await NKStorage.getResourceAsync(entryClass, "index.js", "app");
            var script = "function loadapp(){\n" + appjs + "\n}\n" + "loadapp();" + "\n";
            await context.NKevaluateJavaScript(script, "io.nodekit.electro.main");

            NKEventEmitter.global.emit<string>("NK.AppReady");
        }

        private async Task startNodeKitRenderer(Dictionary<string, object> options)
        {
            string[] args = (string[])options["NKS.Args"];

            NKEventEmitter.isMainProcess = false;

            options.set("NKS.MainThreadScheduler", TaskScheduler.FromCurrentSynchronizationContext());
            options.set("NKS.MainThreadId", Environment.CurrentManagedThreadId);
            options.set("NKS.RemoteProcess", true);

            proxy = NKRemoting.NKRemotingProxy.registerAsClient(args[0]);
            context = await NKScripting.Engines.NKRemoting.NKSNKRemotingContext.createContext(proxy, options);

            // REMOTE SCRIPT ENGINE LOADED, ADD {NK} NODEKIT PROXY
            await NKElectro.Main.addElectroRemoteProxy(context, options);

            proxy.NKready();
        }
    }
}

