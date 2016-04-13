/*
* nodekit.io
*
* Copyright (c) 2016 OffGrid Networks. All Rights Reserved.
* Portions Copyright (c) 2013 GitHub, Inc. under MIT License
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
using System.Threading.Tasks;
using System.Collections.Generic;

namespace io.nodekit.NKElectro
{
    public sealed class NKE_IpcMain 
    {
        private static NKEventEmitter globalEvents = NKEventEmitter.global;

        private static Task initializeForContext(NKScriptContext context)
        {
            var jsValue = typeof(NKE_IpcMain).getNKScriptValue();
            var events = NKEventEmitter.global;

            globalEvents.on<NKEvent>("NKE.IPCtoMain", (e, item) =>
            {
                jsValue.invokeMethod("emit", new object[] { "NKE.IPCtoMain", item.sender, item.channel, item.replyId, item.arg });
            });

            return Task.FromResult<object>(null);
        }

        // Forward replies to renderer to the events queue for that renderer, using global queue since we may be cross process
        public static void ipcReply(int dest, string channel, string replyId, object result)
        {
            var payload = new NKEvent(0, channel, replyId, new [] { result });
            globalEvents.emit("NKE.IPCReplytoRenderer." + dest.ToString(), payload, true);
        }

        public static void ipcSend(string channel, string replyId, object[] arg)
        {
            throw new InvalidOperationException("Event subscription only API.  Sends are handled in WebContents API");      
        }


        #region NKScriptExport
        internal static Task attachToContext(NKScriptContext context, Dictionary<string, object> options)
        {
            return context.NKloadPlugin(typeof(NKE_IpcMain), null, options);
        }

        private static string defaultNamespace { get { return "io.nodekit.electro.ipcMain"; } }

        private static string rewriteGeneratedStub(string stub, string forKey)
        {
            switch (forKey)
            {
                case ".global":
                    var appjs = NKStorage.getResource(typeof(NKE_IpcMain), "ipcmain.js", "lib_electro");
                    return "function loadplugin(){\n" + appjs + "\n}\n" + stub + "\n" + "loadplugin();" + "\n";
                default:
                    return stub;
            }
        }
        #endregion
    }
}