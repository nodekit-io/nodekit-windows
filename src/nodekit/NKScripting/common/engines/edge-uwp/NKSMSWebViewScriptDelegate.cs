#if WINDOWS_UWP
/*
* nodekit.io
*
* Copyright (c) 2016 OffGrid Networks. All Rights Reserved.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
using io.nodekit.NKScripting.Engines.MSWebView.Callbacks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace io.nodekit.NKScripting.Engines.MSWebView
{
   internal class NKSMSWebViewScriptDelegate : NKSMSWebViewCallbackProtocol, NKScriptContentController
    {
        private NKScriptContext context;

        internal NKSMSWebViewScriptDelegate(NKScriptContext context)
        {
            this.context = context;
        }

        Dictionary<string, NKScriptMessageHandler> msgHandlers = new Dictionary<string, NKScriptMessageHandler>();
        public void NKaddScriptMessageHandler(NKScriptMessageHandler scriptMessageHandler, string name)
        {
            msgHandlers.Add(name, scriptMessageHandler);
            var script = "NKScripting.messageHandlers." + name + " = NKScripting.getMessageHandlers('" + name + "');";
            context.NKevaluateJavaScript(script, "");
        }

        public void NKremoveScriptMessageHandlerForName(string name)
        {
            var cleanup = "delete NKScripting.messageHandlers." + name;
            context.NKevaluateJavaScript(cleanup, "");
        }

        string NKSMSWebViewCallbackProtocol.didReceiveScriptMessageSync(string channel, string message)
        {
            if (msgHandlers.ContainsKey(channel))
            {
                var scriptHandler = msgHandlers[channel];
                var body = context.NKdeserialize(message);
                var result = scriptHandler.didReceiveScriptMessageSync(new NKScriptMessage(channel, body));
                var retValueSerialized = context.NKserialize(result);
                return retValueSerialized;
            }
            else return "";
        }

        void NKSMSWebViewCallbackProtocol.didReceiveScriptMessage(string channel, string message)
        {
            if (msgHandlers.ContainsKey(channel))
            {
                var scriptHandler = msgHandlers[channel];
                var body = context.NKdeserialize(message);
                scriptHandler.didReceiveScriptMessage(new NKScriptMessage(channel, body));
            }
        }

        IAsyncOperation<string> NKSMSWebViewCallbackProtocol.didReceiveScriptMessageAsync(string channel, string message)
        {
            if (msgHandlers.ContainsKey(channel))
            {
                var scriptHandler = msgHandlers[channel];
                var body = context.NKdeserialize(message);
                var result = scriptHandler.didReceiveScriptMessageSync(new NKScriptMessage(channel, body));
                var retValueSerialized = context.NKserialize(result);
                return Task.FromResult<string>(retValueSerialized).AsAsyncOperation();
            }
            else
                return Task.FromResult<string>("").AsAsyncOperation();
        }

        void NKSMSWebViewCallbackProtocol.log(string message)
        {
            NKLogging.log(message);
        }
    }
}
#endif