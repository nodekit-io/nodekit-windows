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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace io.nodekit.NKScripting.Engines.NKRemoting
{

    public class NKSNKRemotingContext : NKScriptContextSyncBase, NKScriptContentController
    {

        public static Task<NKScriptContext> createContext(NKScriptContextRemotingProxy proxy, Dictionary<string, object> options)
        {

            if (options == null)
                options = new Dictionary<string, object>();

            NKScriptContext context = new NKSNKRemotingContext(proxy, options);

            return Task.FromResult(context);
        }

        private NKScriptContextRemotingProxy _proxy;

        private NKSNKRemotingContext(NKScriptContextRemotingProxy proxy, Dictionary<string, object> options) : base(proxy.NKid)
        {
            _async_queue = null;
            this._proxy = proxy;
            proxy.context = this;
            NKLogging.log("+NodeKit Renderer Remoting JavaScript Proxy E" + _id);
        }

        protected override object RunScript(string javaScriptString, string filename)
        {
            _proxy.NKevaluateJavaScript(javaScriptString, filename);
            return null;
        }

        // NKScriptContextBase METHODS NOT APPLICABLE FOR REMOTE CONTEXTS
        public override void NKaddScriptMessageHandler(NKScriptMessageHandler scriptMessageHandler, string name)
        {
            // ignore as setup in remoteproxy NKReady
        }

        public override void NKremoveScriptMessageHandlerForName(string name)
        {
            // ignore as setup in remoteproxy NKReady
        }

        // NKScriptContextBase METHODS NOT APPLICABLE FOR REMOTE CONTEXTS
        override protected Task PrepareEnvironment()
        {
            // should never occur as factory for remote objects does not call context.prepareEnvironment();  instead use proxy.NKready()
            throw new NotImplementedException();
        }

        protected override Task InjectScript(NKScriptSource script)
        {
            // silently ignore:  script injections also handled in main process.
            return Task.FromResult<object>(null);
        }

        protected override Task LoadPlugin<T>(T plugin, string ns, Dictionary<string, object> options)
        { 
             throw new NotSupportedException("Remote (main process) plugins are loaded by the default handler");
        }

        public override Task<Task<object>> ensureOnEngineThread(Func<Task<object>> t)
        {
            
                return Task.FromResult<Task<object>>(t.Invoke());
        }

        public override Task<Task> ensureOnEngineThread(Func<Task> t)
        {
         
                return Task.FromResult<Task>(t.Invoke());
        }

        public override Task<object> ensureOnEngineThread(Func<object> t)
        {
       
                return Task.FromResult<object>(t.Invoke());
        }

        public override Task ensureOnEngineThread(Action t)
        {
                t.Invoke();
                return Task.FromResult<object>(null);
        }
    }
}
