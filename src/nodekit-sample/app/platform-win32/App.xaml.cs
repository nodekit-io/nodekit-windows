using io.nodekit.NKScripting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace io.nodekit.Samples.nodekit_sample
{
    public partial class App : Application
    {
        [STAThread]
        public static void Main()
        {
            var application = new App();
      
            application.InitializeComponent();
            application.Run();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var args = e.Args;

            if (args.Length > 0 && args[0].StartsWith("NKR="))
            {
                var _ = startNodeKitRenderer(args);
            }
            else
            {
                var _ = startNodeKitMain(args);
           }
        }

        private NKScriptContext context;
        NKScriptContextRemotingProxy proxy;

        private async Task startNodeKitMain(string[] args)
        {
            NKEventEmitter.isMainProcess = true;

            var options = new Dictionary<string, object>
            {
                ["NKS.MainThreadScheduler"] = TaskScheduler.FromCurrentSynchronizationContext(),
                ["NKS.MainThreadId"] = Environment.CurrentManagedThreadId,
                ["NKS.RemoteProcess"] = true,
                ["NKS.Engine"] = NKEngineType.Chakra
            };

            context = await NKScriptContextFactory.createContext(options);
            await NKElectro.Main.addElectro(context, options);

            var appjs = await NKStorage.getResourceAsync(typeof(App), "index.js", "app");
            var script = "function loadapp(){\n" + appjs + "\n}\n" + "loadapp();" + "\n";
            await context.NKevaluateJavaScript(script, "io.nodekit.electro.main");

            NKEventEmitter.global.emit<string>("NK.AppReady");
        }

        // FOR FUTURE USE IF NEEDED
        private async Task startNodeKitRenderer(string[] args)
        {
            NKEventEmitter.isMainProcess = false;

            var options = new Dictionary<string, object>
            {
                ["NKS.MainThreadScheduler"] = TaskScheduler.FromCurrentSynchronizationContext(),
                ["NKS.MainThreadId"] = Environment.CurrentManagedThreadId,
                ["NKS.RemoteProcess"] = true,
            };
            proxy = NKRemoting.NKRemotingProxy.registerAsClient(args[0]);
            context = await NKScripting.Engines.NKRemoting.NKSNKRemotingContext.createContext(proxy, options);
            await NKElectro.Main.addElectroRemoteProxy(context, options);
            proxy.NKready();
        }
    }
}