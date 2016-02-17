using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace io.nodekit.Samples.nodekit_sample
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
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
            var ignoreTask = startNodeKit();
        }

        private async Task startNodeKit(Dictionary<string, object> options = null)
        {
            var context = await NKScripting.NKScriptContextFactory.createContext(options);
            await NKElectro.Main.addElectro(context);

            var appjs = await NKStorage.getResourceAsync(typeof(App), "index.js", "app");
            var script = "function loadapp(){\n" + appjs + "\n}\n" + "loadapp();" + "\n";
            await context.NKevaluateJavaScript(script, "io.nodekit.electro.main");

            NKEventEmitter.global.emit<string>("nk.jsApplicationReady");
        }

    }


}
