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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace io.nodekit.NKElectro
{
    public partial class NKE_WebContents : NKScriptExport, IDisposable
    {
        protected NKE_BrowserWindow _browserWindow;
        protected int _id;
        protected string _type;
        protected NKEventEmitter globalEvents = NKEventEmitter.global;

        internal static Task attachToContext(NKScriptContext context, Dictionary<string, object> options)
        {
            return context.NKloadPlugin(typeof(NKE_WebContents), null, options);
        }


        public NKE_WebContents(NKE_BrowserWindow browserWindow)
        {
            this._browserWindow = browserWindow;
            this._id = browserWindow.id;

            // Event:  'did-fail-load'
            // Event:  'did-finish-load'

            browserWindow.events.on<int>("NKE.DidFinishLoad", (e, id) =>
            {
                this.getNKScriptValue().invokeMethod("emit", new[] { "did-finish-load" });
            });

            browserWindow.events.on<Tuple<int, string>>("NKE.DidFailLoading", (e, item) =>
            {
                this.getNKScriptValue().invokeMethod("emit", new[] { "did-fail-loading", item.Item2 });
            });
        }

        protected void init_IPC()
        {
            _browserWindow.events.on<NKEvent>("NKE.IPCReplytoMain", (e, item) =>
            {
                this.getNKScriptValue().invokeMethod("emit", new[] { "NKE.IPCReplytoMain", item.sender, item.channel, item.replyId, item.arg[0] });

            });
        }

        // Messages to renderer are sent to the window events queue for that renderer which will be in same process as ipcRenderer
        public void ipcSend(string channel, string replyId, object[] arg)
        {
            var payload = new NKEvent(0, channel, replyId, arg);
            _browserWindow.events.emit("NKE.IPCtoRenderer", payload);
        }

        // Replies to renderer to the window events queue for that renderer
        public void ipcReply(int dest, string channel, string replyId, object result)
        {
            var payload = new NKEvent(0, channel, replyId, new[] { result });
            _browserWindow.events.emit("NKE.IPCReplytoRenderer", payload);
        }

        #region NKScriptExport
        private static string defaultNamespace { get { return "io.nodekit.electro.WebContents"; } }

        private static string rewriteGeneratedStub(string stub, string forKey)
        {
            switch (forKey)
            {
                case ".global":
                    var appjs = NKStorage.getResource(typeof(NKE_BrowserWindow), "webcontents.js", "lib_electro");
                    return "function loadplugin(){\n" + appjs + "\n}\n" + stub + "\n" + "loadplugin();" + "\n";
                default:
                    return stub;
            }
        }

        private static string rewritescriptNameForKey(string key, string name)
        {
            return key == ".ctor:browserWindow" ? "" : name;
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                var item = this.getNKScriptValue();
                if (item != null)
                    item.Dispose();

                disposedValue = true;
            }
        }

         public void Dispose()
        {
            Dispose(true);
         }
        #endregion
    }
}

