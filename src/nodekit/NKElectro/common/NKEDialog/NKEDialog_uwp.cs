#if WINDOWS_UWP
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
using System.Threading.Tasks;
using io.nodekit;
using io.nodekit.NKScripting;

namespace io.nodekit.NKElectro
{
    public sealed class NKE_Dialog
    {
        private NKEventEmitter events = NKEventEmitter.global;

        #region NKScriptExport
        private static string defaultNamespace { get { return "io.nodekit.electro.dialog"; } }
        #endregion

        void showOpenDialog(NKE_BrowserWindow browserWindow, Dictionary<string, object> options, NKScriptValue callback)
        {
            throw new NotImplementedException();
        }

        void showSaveDialog(NKE_BrowserWindow browserWindow, Dictionary<string, object> options, NKScriptValue callback)
        {
            throw new NotImplementedException();
        }

        void showMessageBox(NKE_BrowserWindow browserWindow, Dictionary<string, object> options, NKScriptValue callback)
        {
            throw new NotImplementedException();
        }

        void showErrorBox(string title, string content)
        {
            throw new NotImplementedException();
        }
    }
}
#endif