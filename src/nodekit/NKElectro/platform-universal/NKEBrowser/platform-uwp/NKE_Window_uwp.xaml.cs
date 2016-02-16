using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace io.nodekit.NKElectro
{

     public sealed partial class NKE_Window : Page
    {
        public WebView webView;

        public UIElementCollection controls { get { return this.Contents.Children;  } }

        public NKE_Window()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            int id = (int)e.Parameter;
            NKEventEmitter.global.emit("nk.window." + id, this);
         }


    }
}
