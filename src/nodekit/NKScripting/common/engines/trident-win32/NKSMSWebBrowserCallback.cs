#if WINDOWS_WIN32
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

using System.Security.Permissions;

namespace io.nodekit.NKScripting.Engines.MSWebBrowser
{
    public interface NKSMSWebBrowserCallbackProtocol
    {
        string didReceiveScriptMessageSync(string channel, string message);
        void didReceiveScriptMessage(string channel, string message);
        void log(string message);
    }

    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed class NKSMSWebBrowserCallback
    {

        private NKSMSWebBrowserCallbackProtocol callback;

        public NKSMSWebBrowserCallback(NKSMSWebBrowserCallbackProtocol callback)
        {
            this.callback = callback;
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
#endif