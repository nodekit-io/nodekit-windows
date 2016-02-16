using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace io.nodekit.NKScripting.Engines.MSWebView.Callbacks
{
    public interface NKSMSWebViewCallbackProtocol
    {
        IAsyncOperation<string> didReceiveScriptMessageAsync(string channel, string message);
        string didReceiveScriptMessageSync(string channel, string message);
        void didReceiveScriptMessage(string channel, string message);
        void log(string message);
    }
}
