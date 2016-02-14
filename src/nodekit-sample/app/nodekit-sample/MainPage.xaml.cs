using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using io.nodekit.NKScripting.Engines.Chakra;
using io.nodekit.NKScripting;
using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace io.nodekit.Samples.nodekit_sample
{
    public sealed partial class MainPage : Page
    {
        NKScriptContext host;
   
        public MainPage()
        {
            this.InitializeComponent();
            NKSChakraContextFactory.createContext(new Dictionary<string, object>()).ContinueWith(async task => { host = task.Result; await onJavaScriptEngineReady(); });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            host.NKevaluateJavaScript(JsInput.Text).ContinueWith(task =>
            {
                JsOutput.Text = JsOutput.Text + "\n> " + JsInput.Text + "\n" + task.Result;
                JsOutput.UpdateLayout();
                JsOutputScroll.ChangeView(null, double.MaxValue, null);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async Task onJavaScriptEngineReady()
        {
            await host.NKloadPlugin(new io.nodekit.Samples.Plugin.MyPlugin(), "io.nodekit.Samples.Plugin.MyPlugin");
            await io.nodekit.NKElectro.Main.addElectro(host);
            io.nodekit.NKLogging.log("JS Engine Ready");
        }
    }

    
}

