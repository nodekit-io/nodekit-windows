#if WINDOWS_WIN32_WF
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
using System.Windows.Forms;
using System.Threading.Tasks;

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
            string caption = NKOptions.itemOrDefault(options, "title", "");
            string message = NKOptions.itemOrDefault(options, "message", "");
            string [] buttonArray = NKOptions.itemOrDefault(options, "buttons", new string[] { "OK" });
            string detail = NKOptions.itemOrDefault(options, "detail", "");

            MessageBoxIcon icon;
            switch (detail)
            {
                case "info":
                    icon = MessageBoxIcon.Information;
                    break;
                case "warning":
                    icon = MessageBoxIcon.Warning;
                    break;
                case "error":
                    icon = MessageBoxIcon.Error;
                    break;
                default:
                    icon = MessageBoxIcon.None;
                    break;
            }
            
            MessageBoxButtons buttons = buttons = MessageBoxButtons.OK;

            if ((Array.IndexOf(buttonArray, "OK") > -1) && (Array.IndexOf(buttonArray, "Cancel") > -1))
                buttons = MessageBoxButtons.OKCancel;
            else if (Array.IndexOf(buttonArray, "OK") > -1)
                   buttons = MessageBoxButtons.OK;
            else if ((Array.IndexOf(buttonArray, "Abort") > -1) && (Array.IndexOf(buttonArray, "Retry") > -1) && (Array.IndexOf(buttonArray, "Ignore") > -1))
                buttons = MessageBoxButtons.AbortRetryIgnore;
            else if ((Array.IndexOf(buttonArray, "Yes") > -1) && (Array.IndexOf(buttonArray, "No") > -1) && (Array.IndexOf(buttonArray, "Cancel") > -1))
                buttons = MessageBoxButtons.YesNoCancel;
            else if ((Array.IndexOf(buttonArray, "Yes") > -1) && (Array.IndexOf(buttonArray, "No") > -1))
                buttons = MessageBoxButtons.YesNo;
            else if ((Array.IndexOf(buttonArray, "Retry") > -1) && (Array.IndexOf(buttonArray, "Cancel") > -1))
                buttons = MessageBoxButtons.RetryCancel;

            DialogResult result = MessageBox.Show(message, caption, buttons, icon);
            if (callback != null)
                 callback.callWithArguments(new object[] { result.ToString() });
        }

       public void showErrorBox(string title, string content)
        {
            showMessageBox(null, new Dictionary<string, object> { ["title"] = title, ["message"] = content, ["detail"] = "error" }, null);
        }
    }
}



#endif