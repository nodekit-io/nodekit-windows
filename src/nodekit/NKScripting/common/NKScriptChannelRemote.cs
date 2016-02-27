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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace io.nodekit.NKScripting
{
    internal sealed class NKScriptChannelRemote : NKScriptChannel
    {
        private const int NKRANGEPERPROCESS = 10;

        // private variables
        private Dictionary<int, CancellationTokenSource> _cancelTokens = new Dictionary<int, CancellationTokenSource>();
        private Dictionary<int, NKScriptMessageHandler> _proxies = new Dictionary<int, NKScriptMessageHandler>();
        private static Dictionary<int, NKScriptMessageHandler> _proxiesNatives = new Dictionary<int, NKScriptMessageHandler>();

        // Public constructors
        public NKScriptChannelRemote(NKScriptContext context) : this(context, TaskScheduler.Default) { }
        public NKScriptChannelRemote(NKScriptContext context, TaskScheduler taskScheduler) : base(context, taskScheduler) { isRemote = true; }

        // Public methods from base not overridden
        // public async Task<NKScriptValue> bindPlugin<T>(T obj, string ns)
        // public void addInstance(int id, NKScriptValueNative instance) { _instances[id] = instance; }

        // Public methods overriden
        protected override void unbind()
        {
            // Dispose proxy channel by signalling cancel tokens
            foreach (var item in _cancelTokens)
            {
                item.Value.Cancel();
            }

            base.unbind();
        }

        public override void didReceiveScriptMessage(NKScriptMessage message)
        {
            // A workaround for when postMessage(undefined)
            if (message.body == null) return;

            var body = message.body as Dictionary<string, object>;
            if (body != null && body.ContainsKey("$opcode"))
            {
                string opcode = body["$opcode"] as String;
                int target = Int32.Parse(body["$target"].ToString());
                NKScriptMessageHandler proxy = null;
                if (_proxies.ContainsKey(target))
                {
                    proxy = _proxies[target];
                }
                else if (target > 1500)
                {
                    var targetFloor = target - (target % NKRANGEPERPROCESS);
                    if (_proxiesNatives.ContainsKey(targetFloor))
                        proxy = _proxiesNatives[targetFloor];
                }

                if (proxy != null)
                {
                    if (opcode == "-")
                    {
                        if (target == 0)
                        {
                            // TRANSFER TO REMOTE, WITH SYNC
                            var _ = proxy.didReceiveScriptMessageSync(message);
                            this.unbind();
                        }
                        else 
                        {
                            // TRANSFER TO REMOTE, WITH SYNC
                            var _ = proxy.didReceiveScriptMessageSync(message);

                            // UNBIND PROXY
                            _cancelTokens[target].Cancel();
                        }
                    }
                    else if (typeInfo.ContainsProperty(opcode))
                    {
                        // ALSO TRANSFER TO REMOTE
                       proxy.didReceiveScriptMessage(message);
                    }
                    else if (typeInfo.ContainsMethod(opcode))
                    {
                        // Invoke method

                        // TRANSFER TO REMOTE ONLY
                        proxy.didReceiveScriptMessage(message);
                    }
                    else {
                        NKLogging.log(String.Format("!Invalid member name: {0}", opcode));
                    }
                }
                else if (opcode == "+")
                {
                    throw new NotImplementedException("+ opcode must be called using synchronous messages");
                }
                else
                {
                    // else Unknown opcode
                    var obj = _principal.plugin as NKScriptMessageHandler;
                    if (obj != null)
                    {
                        obj.didReceiveScriptMessage(message);
                    }
                    else
                    {
                        // discard unknown message
                        NKLogging.log(String.Format("!Unknown message: {0}", message.body.ToString()));
                    }
                }
            }
            else
            {
                // null body, ignore
            }

        }

        public override object didReceiveScriptMessageSync(NKScriptMessage message)
        {
            // A workaround for when postMessage(undefined)
            if (message.body == null) return false;

            // thread static
            NKScriptValue._currentContext = this.context;
            object result;

            var body = message.body as Dictionary<string, object>;
            if (body != null && body.ContainsKey("$opcode"))
            {
                string opcode = body["$opcode"] as String;
                int target = Int32.Parse(body["$target"].ToString());
                NKScriptMessageHandler proxy = null;
                if (_proxies.ContainsKey(target))
                {
                    proxy = _proxies[target];
                } else if (target > 1500)
                {
                    var targetFloor = target - (target % 10);
                    if (_proxiesNatives.ContainsKey(targetFloor))
                        proxy = _proxiesNatives[targetFloor];
                }

                if (proxy != null)
                {
                    if (opcode == "-")
                    {
                        if (target == 0)
                        {
                            // TRANSFER TO REMOTE
                            var _ = proxy.didReceiveScriptMessageSync(message);

                            this.unbind();
                            result = true;
                        }
                        else
                        {
                            // TRANSFER TO REMOTE
                            var _ = proxy.didReceiveScriptMessageSync(message);

                            // UNBIND PROXY
                            _cancelTokens[target].Cancel();

                            result = true;
                        }
                          }
                    else if (typeInfo.ContainsProperty(opcode))
                    {
                        // TRANSFER TO REMOTE
                        proxy.didReceiveScriptMessageSync(message);
                       result = true;
                    }
                    else if (typeInfo.ContainsMethod(opcode))
                    {
                        // Invoke method via REMOTE proxy
                         result = proxy.didReceiveScriptMessageSync(message);
                    }
                    else {
                        NKLogging.log(String.Format("!Invalid member name: {0}", opcode));
                        result = false;
                    }
                }
                else if (opcode == "+")
                {
                   _instances[target] = null;

                    int maxNativeFirst = NKScriptChannel.nativeFirstSequence - (NKScriptChannel.nativeFirstSequence % NKRANGEPERPROCESS) - 1;
                    int minNativeFirst = (maxNativeFirst - NKRANGEPERPROCESS) + 1;
                    NKScriptChannel.nativeFirstSequence = minNativeFirst - 1;
              
                    var cancelTokenSource = new CancellationTokenSource();
                    _cancelTokens[target] = cancelTokenSource;
                     proxy = NKRemoting.NKRemotingProxy.createClient(ns, id, maxNativeFirst, message, context, cancelTokenSource.Token);
                    _proxies[target] = proxy;
                    _proxiesNatives[minNativeFirst] = proxy;

                    result = true;
                }
                else
                {
                    // else Unknown opcode
                    var obj = _principal.plugin as NKScriptMessageHandler;
                    if (obj != null)
                    {
                        result = obj.didReceiveScriptMessageSync(message);
                    }
                    else
                    {
                        // discard unknown message
                        NKLogging.log(String.Format("!Unknown message: {0}", message.body.ToString()));
                        result = false;
                    }
                }
            }
            else
            {
                // null body
                result = false;
            }

            //thread static
            NKScriptValue._currentContext = null;
            return result;
        }
    }
}