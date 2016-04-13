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
            Application.Run(new App(args));
        }
    }

    class App : ApplicationContext
    {
        Form form;

        public App(string[] args) : base()
        {
            var window = new Form();
            var handle = window.Handle;

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
