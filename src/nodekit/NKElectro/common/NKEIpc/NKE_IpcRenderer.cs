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
    public sealed class NKE_IpcRenderer 
    {
        private static NKEventEmitter globalEvents = NKEventEmitter.global;
        internal NKE_BrowserWindow _window;
        internal int _id;
     
        public NKE_IpcRenderer(int id)
        {
            _id = id;
            _window = NKE_BrowserWindow.fromId(id);


            _window.events.on<NKEvent>("NKE.IPCtoRenderer", (e, item) =>
            {
                this.getNKScriptValue().invokeMethod("emit", new object[] { "NKE.IPCtoRenderer", item.sender, item.channel, item.replyId, item.arg });
            });

            _window.events.on<NKEvent>("NKE.IPCReplytoRenderer", (e, item) =>
            {
                this.getNKScriptValue().invokeMethod("emit", new object[] { "NKE.IPCReplytoRenderer", item.sender, item.channel, item.replyId, item.arg });
            });

            globalEvents.on<NKEvent>("NKE.IPCtoMain", (e, item) =>
            {
                this.getNKScriptValue().invokeMethod("emit", new object[] { "NKE.IPCtoMain", item.sender, item.channel, item.replyId, item.arg });
            });

        }

        // Messages to main are sent to the global events queue
        public void ipcSend(string channel, string replyId, object[] arg)
        {
            var payload = new NKEvent(0, channel, replyId, arg);
            globalEvents.emit("NKE.IPCtoMain", payload);
        }

        // Replies to main are sent directly to the webContents window that sent the original message
        public void ipcReply(int dest, string channel, string replyId, object result)
        {
            var payload = new NKEvent(0, channel, replyId, new[] { result });
            _window.events.emit("NKE.IPCReplytoMain", payload);
        }

        #region NKScriptExport
        private static string defaultNamespace { get { return "io.nodekit.electro.ipcRenderer"; } }

        private static string rewriteGeneratedStub(string stub, string forKey)
        {
            switch (forKey)
            {
                case ".global":
                    var appjs = NKStorage.getResource(typeof(NKE_IpcRenderer), "ipc-renderer.js", "lib_electro");
                    return "function loadplugin(){\n" + appjs + "\n}\n" + stub + "\n" + "loadplugin();" + "\n";
                default:
                    return stub;
            }
        }

        private static string rewritescriptNameForKey(string key, string name) {
            return key == ".ctor:id" ? "" : name;
        }
  
       #endregion
    }
}
