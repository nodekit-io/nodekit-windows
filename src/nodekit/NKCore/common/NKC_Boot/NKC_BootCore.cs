/*
* nodekit.io
*
* Copyright (c) 2016 OffGrid Networks. All Rights Reserved.
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
using System;

namespace io.nodekit.NKCore
{
    public class Main
    {
        public async static Task addCorePlatform(NKScriptContext context, Dictionary<string, object> options)
        {
            entryType = (System.Type)options["NKS.Entry"];


            // PROCESS SHOULD BE FIRST CORE PLATFORM PLUGIN
            await NKC_Process.attachToContext(context, options);

            // LOAD REMAINING CORE PLATFORM PLUGINS
            await NKC_FileSystem.attachToContext(context, options);
      //      await NKC_Console.attachToContext(context, options);
            await NKC_Crypto.attachToContext(context, options);
            await NKC_SocketTCP.attachToContext(context, options);
            await NKC_SocketUDP.attachToContext(context, options);
            await NKC_Timer.attachToContext(context, options);
        }

        public async static Task bootCore(NKScriptContext context, Dictionary<string, object> options)
        {
            // FINALLY LOAD BOOTSTRAP CODE AFTER ALL PLATFORM PLUGINS
            var appjs = await NKStorage.getResourceAsync(typeof(Main), "_nodekit_bootstrapper.js", "lib");
            var script = "function loadbootstrap(){\n" + appjs + "\n}\n" + "loadbootstrap();" + "\n";
            var scriptsource = new NKScriptSource(script, "io.nodekit.core/lib-core/_nodekit_bootstrapper.js", "io.nodekit.core.bootstrapper");
            await context.NKinjectScript(scriptsource);

        }

        internal static Type entryType;
    }
}

