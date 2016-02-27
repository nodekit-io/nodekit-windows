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
    public sealed class Renderer
    {
        public async static Task addElectro(NKScriptContext context, Dictionary<string, object> options)
        {
            var appjs = await NKStorage.getResourceAsync(typeof(Renderer), "_nke_renderer.js", "lib_electro");
            var script = "function loadbootstrap(){\n" + appjs + "\n}\n" + "loadbootstrap();" + "\n";
            var scriptsource = new NKScriptSource(script, "io.nodekit.electro/lib-electro/_nke_renderer.js", "io.nodekit.electro.renderer");
            await context.NKinjectScript(scriptsource);

            // NKE_IpcRenderer.attachTo(context);
        }
    }
}

