using io.nodekit.NKScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using io.nodekit.NKScripting.Engines.ChakraCore;

namespace io.nodekit.Samples.nodekit_sample
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new App());
        }
    }

    class App : ApplicationContext
    {

        Form form; 

         public App() : base()
        {

            Form form = new Form();
            form.Visible = false;
            form.Load += Form_Load;
          
            var ignoreTask = startNodeKit();

            NotifyIcon notifyIcon = new NotifyIcon();
            notifyIcon.Visible = true;
            NKElectro.NKE_BrowserWindow.setupSync();
        }

        private void Form_Load(object sender, EventArgs e)
        {
            form.Close();

            var ignore = startNodeKit();
         }

        private NKScriptContext context;

        private async Task startNodeKit(Dictionary<string, object> options = null)
        {
            NKElectro.NKE_BrowserWindow.setupSync();
            context = await NKScriptContextFactory.createContext(options);
            await NKElectro.Main.addElectro(context);

            var appjs = await NKStorage.getResourceAsync(typeof(App), "index.js", "app");
            var script = "function loadapp(){\n" + appjs + "\n}\n" + "loadapp();" + "\n";
            await context.NKevaluateJavaScript(script, "io.nodekit.electro.main");

            NKEventEmitter.global.emit<string>("nk.jsApplicationReady");
        }
    }
}
