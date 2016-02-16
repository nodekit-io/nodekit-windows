using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace io.nodekit.NKScripting.Engines.MSWebView.Callbacks
{
    [AllowForWeb]
    [MarshalingBehavior(MarshalingType.Agile)] 
    public sealed class NKSMSWebViewCallback
    {

        private NKSMSWebViewCallbackProtocol callback;

        public NKSMSWebViewCallback(NKSMSWebViewCallbackProtocol callback)
        {
            this.callback = callback;
        }

        public IAsyncOperation<string> didReceiveScriptMessageAsync(string channel, string message)
        {
            return callback.didReceiveScriptMessageAsync(channel, message);
        }

        public string didReceiveScriptMessageSync(string channel, string message)
        {
            return callback.didReceiveScriptMessageSync(channel, message);
        }

        public void didReceiveScriptMessage(string channel, string message)
        {
            callback.didReceiveScriptMessage(channel, message);
        }

        public void log(string message)
        {
            callback.log(message);
        }


    }
}
