using io.nodekit.NKScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using io.nodekit.NKScripting.Engines.ChakraCore;
using System.Threading;

namespace io.nodekit.Samples.nodekit_sample
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        
            if (args.Length > 0 && args[0].StartsWith("NKR="))
            {
                try {
                    Application.Run(new Renderer(args));
                } catch (Exception ex) { NKLogging.log(ex);  }
            } else
            {
                 Application.Run(new Main(args));
            }
        }
    }
      
    class Main : ApplicationContext
    {

        Form form; 

         public Main(string[] args) : base()
        {
            var window = new Form();
            var handle = window.Handle;
            var _ = startNodeKit(args);
       }

        private NKScriptContext context;

        private async Task startNodeKit(string[] args)
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

            var appjs = await NKStorage.getResourceAsync(typeof(Main), "index.js", "app");
            var script = "function loadapp(){\n" + appjs + "\n}\n" + "loadapp();" + "\n";
            await context.NKevaluateJavaScript(script, "io.nodekit.electro.main");

            NKEventEmitter.global.emit<string>("NK.AppReady");
        }
    }

    class Renderer : ApplicationContext
    {

        Form form;

        public Renderer(string[] args) : base()
        {
            var window = new Form();
            var handle = window.Handle;
             var _ = startNodeKit(args);
        }

        private NKScriptContext context;
        NKScriptContextRemotingProxy proxy;
        private async Task startNodeKit(string[] args)
        {
            NKEventEmitter.isMainProcess = false;

            var options = new Dictionary<string, object>
            {
                ["NKS.MainThreadScheduler"] = TaskScheduler.FromCurrentSynchronizationContext(),
                ["NKS.MainThreadId"] = Environment.CurrentManagedThreadId,
                ["NKS.Engine"] = NKEngineType.Chakra
            };

            proxy = NKRemoting.NKRemotingProxy.registerAsClient(args[0]);
            context = await NKScripting.Engines.NKRemoting.NKSNKRemotingContext.createContext(proxy, options);
            await NKElectro.Main.addElectroRemoteProxy(context, options);
            proxy.NKready();
        }
    }
}
