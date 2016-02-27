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

        // Replies to renderer to the window events queue for that renderer
        public static void ipcReply(int dest, string channel, string replyId, object result)
        {
            var payload = new NKEvent(0, channel, replyId, new [] { result });
            var window = NKE_BrowserWindow.fromId(dest) as NKE_BrowserWindow;
            if (window == null) return;
            window.events.emit("NKE.IPCReplytoRenderer", payload);
        }

        public static void ipcSend(string channel, string replyId, object[] arg)
        {
            throw new InvalidOperationException("Event subscription only API.  Sends are handled in WebContents API");      
        }


        #region NKScriptExport
        private static string defaultNamespace { get { return "io.nodekit.electro.ipcMain"; } }

        private static string rewriteGeneratedStub(string stub, string forKey)
        {
            switch (forKey)
            {
                case ".global":
                    var appjs = NKStorage.getResource(typeof(NKE_IpcMain), "ipc-main.js", "lib_electro");
                    return "function loadplugin(){\n" + appjs + "\n}\n" + stub + "\n" + "loadplugin();" + "\n";
                default:
                    return stub;
            }
        }
        #endregion
    }
}