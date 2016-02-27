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

using System.Threading.Tasks;
using io.nodekit.NKScripting;
using System.Collections.Generic;

namespace io.nodekit.NKElectro
{
    public class Main
    {
        public async static Task addElectro(NKScriptContext context, bool multiProcess = false)
        {
            var appjs = await NKStorage.getResourceAsync(typeof(Main), "_nke_main.js", "lib_electro");
            var script = "function loadbootstrap(){\n" + appjs + "\n}\n" + "loadbootstrap();" + "\n";
            var scriptsource = new NKScriptSource(script, "io.nodekit.electro/lib-electro/_nke_main.js", "io.nodekit.electro.main");
            await context.NKinjectScript(scriptsource);

            var options = new Dictionary<string, object>
            {
                ["NKS.PluginBridge"] = NKScriptExportType.NKScriptExport
            };

            var optionsMulti = new Dictionary<string, object>
            {
                ["NKS.PluginBridge"] = NKScriptExportType.NKScriptExport,
                ["NKS.RemoteProcess"] = true
            };

            await context.NKloadPlugin(typeof(NKE_App), null, options);

            if (!multiProcess)
                await context.NKloadPlugin(typeof(NKE_BrowserWindow), null, options);
            else
                await context.NKloadPlugin(typeof(NKE_BrowserWindow), null, optionsMulti);

            if (!multiProcess)
                await context.NKloadPlugin(typeof(NKE_WebContents), null, options);
            else
                await context.NKloadPlugin(typeof(NKE_WebContents), null, optionsMulti);


            // await context.NKloadPlugin(typeof(NKEDialog), "io.nodekit.electro.dialog", options);

            // NKE_BrowserWindow.attachTo(context);
            // NKE_WebContentsBase.attachTo(context);
            // NKE_Dialog.attachTo(context);
            // NKE_IpcMain.attachTo(context);
            // NKE_Menu.attachTo(context);
            // NKE_Protocol.attachTo(context);
        }

        public async static Task addElectroRemoteProxy(NKScriptContext context)
        {
            var options = new Dictionary<string, object>
            {
                ["NKS.PluginBridge"] = NKScriptExportType.NKScriptExport
            };

            await context.NKloadPlugin(typeof(NKE_BrowserWindow), null, options);
            await context.NKloadPlugin(typeof(NKE_WebContents), null, options);

            // await context.NKloadPlugin(typeof(NKEDialog), "io.nodekit.electro.dialog", options);

            // NKE_BrowserWindow.attachTo(context);
            // NKE_WebContentsBase.attachTo(context);
            // NKE_Dialog.attachTo(context);
            // NKE_IpcMain.attachTo(context);
            // NKE_Menu.attachTo(context);
            // NKE_Protocol.attachTo(context);
        }
    }
}

