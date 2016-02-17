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
using System.Threading.Tasks;

#if WINDOWS_UWP
namespace io.nodekit.NKScripting.Engines.Chakra
#elif WINDOWS_WIN32
namespace io.nodekit.NKScripting.Engines.ChakraCore
#endif
{
    internal class NKSChakraScriptDelegate : NKScriptContentController
    {
        private NKSChakraContext context;

        internal NKSChakraScriptDelegate(NKSChakraContext context)
        {
            this.context = context;
         }

        private JavaScriptValue didReceiveScriptMessageFunction;
        private JavaScriptValue didReceiveScriptMessageSyncFunction;
        private JavaScriptValue logFunction;
        private JavaScriptNativeFunction didReceiveScriptMessageNativeFunction;
        private JavaScriptNativeFunction didReceiveScriptMessageSyncNativeFunction;
        private JavaScriptNativeFunction logNativeFunction;


        public Task createBridge()
        {
            return context.ensureOnEngineThread(() =>
            {
                context.switchContextifNeeded();
                var global = JavaScriptValue.GlobalObject;
                var NKScriptingBridge = JavaScriptValue.CreateObject();
                global.SetProperty(JavaScriptPropertyId.FromString("NKScriptingBridge"), NKScriptingBridge, true);

                IntPtr dataCallBack = IntPtr.Zero;

                didReceiveScriptMessageNativeFunction = didReceiveScriptMessage;
                didReceiveScriptMessageFunction = JavaScriptValue.CreateFunction(didReceiveScriptMessageNativeFunction, dataCallBack);
                NKScriptingBridge.SetProperty(JavaScriptPropertyId.FromString("didReceiveScriptMessage"), didReceiveScriptMessageFunction, true);

                didReceiveScriptMessageSyncNativeFunction = didReceiveScriptMessageSync;
                didReceiveScriptMessageSyncFunction = JavaScriptValue.CreateFunction(didReceiveScriptMessageSyncNativeFunction, dataCallBack);
                NKScriptingBridge.SetProperty(JavaScriptPropertyId.FromString("didReceiveScriptMessageSync"), didReceiveScriptMessageSyncFunction, true);

                logNativeFunction = log;
                logFunction = JavaScriptValue.CreateFunction(logNativeFunction, dataCallBack);
                NKScriptingBridge.SetProperty(JavaScriptPropertyId.FromString("log"), logFunction, true);
            });
        }

        Dictionary<string, NKScriptMessageHandler> msgHandlers = new Dictionary<string, NKScriptMessageHandler>();
        public void NKaddScriptMessageHandler(NKScriptMessageHandler scriptMessageHandler, string name)
        {
            msgHandlers.Add(name, scriptMessageHandler);
            var script = "NKScripting.messageHandlers['" + name + "'] = NKScripting.getMessageHandlers('" + name + "');";
            context.NKevaluateJavaScript(script, "");
        }

        public void NKremoveScriptMessageHandlerForName(string name)
        {
            var cleanup = "delete NKScripting.messageHandlers." + name;
            context.NKevaluateJavaScript(cleanup, "");
        }

        private JavaScriptValue log(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] arguments, ushort argumentCount, IntPtr callbackData)
        {
            var arg = arguments[1].ToString();
            NKLogging.log(arg);
            return JavaScriptValue.Null;
        }

        private JavaScriptValue didReceiveScriptMessage(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] arguments, ushort argumentCount, IntPtr callbackData)
        {
            var channel = arguments[1].ToString();
            var message = arguments[2].ToString();

            if (msgHandlers.ContainsKey(channel))
            {
                var scriptHandler = msgHandlers[channel];
                var body = context.NKdeserialize(message);
                scriptHandler.didReceiveScriptMessage(new NKScriptMessage(channel, body));
            }

            return JavaScriptValue.Null;
        }

        private JavaScriptValue didReceiveScriptMessageSync(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] arguments, ushort argumentCount, IntPtr callbackData)
        {
            var channel = arguments[1].ToString();
            var message = arguments[2].ToString();
            if(msgHandlers.ContainsKey(channel))
            {
                var scriptHandler = msgHandlers[channel];
                var body = context.NKdeserialize(message);
                var result = scriptHandler.didReceiveScriptMessageSync(new NKScriptMessage(channel, body));
                var retValueSerialized = context.NKserialize(result);
                return JavaScriptValue.FromString(retValueSerialized);
            }
            else return JavaScriptValue.Null;
         }
    }
}