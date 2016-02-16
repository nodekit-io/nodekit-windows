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
using System.Collections.Generic;
using io.nodekit.NKScripting;
using System.Threading.Tasks;

namespace io.nodekit.NKElectro
{
    public abstract class NKE_WebContentsBase : NKScriptExport
    {
        protected NKE_BrowserWindow _browserWindow;
        protected int _id;
        protected string _type;
        protected NKEventEmitter globalEvents = NKEventEmitter.global;
     
        protected void init_IPC()
        {
            _browserWindow.events.on<NKE_IPC_Event>("nk.IPCReplytoMain", (item) =>
            {
                this.getNKScriptValue().invokeMethod("emit", new[] { "nk.IPCReplytoMain", item.sender, item.channel, item.replyId, item.arg[0] });

            });
        }

        #region NKScriptExport
        private static string defaultNamespace { get { return "io.nodekit.electro.BrowserWindow"; } }

        private static string rewriteGeneratedStub(string stub, string forKey)
        {
            switch (forKey)
            {
                case ".global":
                    var appjs = NKStorage.getResource(typeof(NKE_BrowserWindow), "webContents.js", "lib_electro");
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
    }
}

