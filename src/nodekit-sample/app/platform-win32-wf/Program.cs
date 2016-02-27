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
            NKElectro.NKE_BrowserWindow.setupSync();
            var _ = startNodeKit(args);
       }

        private NKScriptContext context;

        private async Task startNodeKit(string[] args)
        {
            var options = new Dictionary<string, object>();
            options["NKS.Engine"] = NKEngineType.Chakra;
            context = await NKScriptContextFactory.createContext(options);
            await NKElectro.Main.addElectro(context, true);

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
            NKElectro.NKE_BrowserWindow.setupSync();
            var _ = startNodeKit(args);
        }

        private NKScriptContext context;
        NKScriptContextRemotingProxy proxy;
        private async Task startNodeKit(string[] args)
        {
             var options = new Dictionary<string, object>();
            proxy = NKRemoting.NKRemotingProxy.registerAsClient(args[0]);
            context = await NKScripting.Engines.NKRemoting.NKSNKRemotingContext.createContext(proxy, options);
            await NKElectro.Main.addElectroRemoteProxy(context);
            proxy.NKready();
        }
    }
}
