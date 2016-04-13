using io.nodekit.NKScripting;
using System;
using System.Collections.Generic;
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

            var options = new Dictionary<string, object>
            {
                ["NKS.Args"] = args,
                ["NKS.Entry"] = typeof(App),
                ["NKS.RemoteProcess"] = true,
                ["NKS.Engine"] = NKEngineType.Chakra
            };

            NKC_NodeKit.start(options);
        }
    }
}