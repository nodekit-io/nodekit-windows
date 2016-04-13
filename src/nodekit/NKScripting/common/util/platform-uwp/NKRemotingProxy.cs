#if WINDOWS_UWP
/*
* nodekit.io
*
* Copyright (c) 2016 OffGrid Networks. All Rights Reserved.
* Portions Copyright Copyright (c) 2013 Andrew C. Dvorak <andy@andydvorak.net> under MIT license
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
using System;
using io.nodekit.NKScripting;
using System.Threading;

namespace io.nodekit.NKRemoting
{
    internal sealed class NKRemotingMessage
    {
        internal enum Command
        {
            NKRemotingHandshake,
            NKRemotingReady,
            NKRemotingClose,
            NKevaluateJavaScript,
            NKScriptMessageSync,
            NKScriptMessageSyncReply,
            NKScriptMessage
        }

        public Command command = default(Command);
        public string[] args = null;
    }

    public sealed class NKRemotingProxy : NKScriptMessageHandler, NKScriptContextRemotingProxy
    {
        // HOST-MAIN PROCESS
        public static NKScriptMessageHandler createClient(string ns, string id, int nativeSeqMax, NKScriptMessage message, NKScriptContext context, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }

        // RE-ENTRANT RENDERER PROCESS
        public static NKRemotingProxy registerAsClient(string arg)
        {
            throw new NotImplementedException();
        }

        int NKScriptContextRemotingProxy.NKid
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        NKScriptContext NKScriptContextRemotingProxy.context
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        void NKScriptMessageHandler.didReceiveScriptMessage(NKScriptMessage message)
        {
            throw new NotImplementedException();
        }

        object NKScriptMessageHandler.didReceiveScriptMessageSync(NKScriptMessage message)
        {
            throw new NotImplementedException();
        }

        void NKScriptContextRemotingProxy.NKready()
        {
            throw new NotImplementedException();
        }

        void NKScriptContextRemotingProxy.NKevaluateJavaScript(string javaScriptString, string filename)
        {
            throw new NotImplementedException();
        }

        void NKScriptContentController.NKaddScriptMessageHandler(NKScriptMessageHandler scriptMessageHandler, string name)
        {
            throw new NotImplementedException();
        }

        void NKScriptContentController.NKremoveScriptMessageHandlerForName(string name)
        {
            throw new NotImplementedException();
        }


    }
}
#endif