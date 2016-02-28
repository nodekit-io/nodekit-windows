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
using io.nodekit.NKScripting;
using Windows.UI.Popups;

namespace io.nodekit.NKElectro
{
    public sealed class NKE_Dialog
    {
        private NKEventEmitter events = NKEventEmitter.global;

        #region NKScriptExport

        internal static Task attachToContext(NKScriptContext context, Dictionary<string, object> options)
        {
            return context.NKloadPlugin(new NKE_Dialog(), null, options);
        }

        private static string defaultNamespace { get { return "io.nodekit.electro.dialog"; } }
        #endregion

        public void showOpenDialog(NKE_BrowserWindow browserWindow, Dictionary<string, object> options, NKScriptValue callback)
        {
            throw new NotImplementedException();
        }

        public void showSaveDialog(NKE_BrowserWindow browserWindow, Dictionary<string, object> options, NKScriptValue callback)
        {
            throw new NotImplementedException();
        }

        public void showMessageBox(NKE_BrowserWindow browserWindow, Dictionary<string, object> options, NKScriptValue callback)
        {
            string title = NKOptions.itemOrDefault(options, "title", "");
            string message = NKOptions.itemOrDefault(options, "message", "");
            string[] buttonArray = NKOptions.itemOrDefault(options, "buttons", new string[] { "OK" });
            string detail = NKOptions.itemOrDefault(options, "detail", "");

            MessageDialog msgbox = new MessageDialog(message, title);

            msgbox.Commands.Clear();
            int count = 0;
            foreach (var item in buttonArray)
            {
                msgbox.Commands.Add(new UICommand { Label = item, Id = count++ });
            }

            var t = msgbox.ShowAsync();
            if (callback != null)
                t.AsTask().ContinueWith((u) => callback.callWithArguments(new object[] { u.Result.Label }));
        }

        public void showErrorBox(string title, string content)
        {
            showMessageBox(null, new Dictionary<string, object> { ["title"] = title, ["message"] = content, ["detail"] = "error" }, null);
        }
    }
}
#endif