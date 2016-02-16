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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace io.nodekit.NKScripting
{
    public enum NKEngineType
    {
        JavaScriptCore,  // default standalone javascript engine on iOS and OSX, but no JIT and in process  ** RECOMMENDED FOR DARWIN DEVELOPMENT ** 
        Nitro,  // high performance javascript engine that comes bundled with WKWebView, runs JIT in separate process  ** RECOMMENDED FOR DARWIN PRODUCTION **
        UIWebView,   // same javascriptcore engine as above, but bundled with UIWebView;  access to full window/DOM features
        Chakra,   // default built into Windows 10 post October 2015 and beyond ** RECOMMENDED FOR WINDOWS DEVELOPMENT AND WINDOWS STORE APPLICATIONS ** 
        Trident,   // classic chakra engine available with IE9 and later, but frozen 
        ChakraCore,   // open source version of Chakra that can be bundled as far back as Windows 7, and contains the latest features; ~5Mb download ** RECOMMENDED FOR WINDOWS PRODUCTION **
        V8    //  activescript v8 engine on windows and/or CEFGlue
    }

    public class NKScriptContextFactory
    {
        internal static Dictionary<int, object> _contexts = new Dictionary<int, object>();

        public static int sequenceNumber = 1;


        public Task<NKScriptContext> createContext(Dictionary<string, object> options)
        {
            if (options == null)
            {
                options = new Dictionary<string, object>();
            }

            NKEngineType engine;

            if (options.ContainsKey("Engine"))
                engine = (NKEngineType)options["Engine"];
            else
                engine = NKEngineType.Chakra;

            switch (engine)
            {
                case NKEngineType.JavaScriptCore:
                    throw new NotSupportedException(String.Format("{0} not supported on Windows Platform; use OSX or iOS ", engine.ToString()));
                case NKEngineType.Nitro:
                    throw new NotSupportedException(String.Format("{0} not supported on Windows Platform; use OSX or iOS ", engine.ToString()));
                case NKEngineType.UIWebView:
                    throw new NotSupportedException(String.Format("{0} not supported on Windows Platform; use OSX or iOS ", engine.ToString()));
                case NKEngineType.Chakra:
                    return Engines.Chakra.NKSChakraContextFactory.createContext(options);
                case NKEngineType.Trident:
                    throw new NotImplementedException(String.Format("{0} not implemented on Universal Windows Platform ", engine.ToString()));
                case NKEngineType.ChakraCore:
                    throw new NotImplementedException(String.Format("{0} not required on Universal Windows Platform; use Chakra ", engine.ToString()));
                default:
                    throw new ArgumentException(String.Format("{0} unknown engine type", engine.ToString()));
            }
        }
    }
}


