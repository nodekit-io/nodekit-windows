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

using System.Collections.Generic;
using System.Threading.Tasks;

namespace io.nodekit.NKScripting
{
    public interface NKScriptContext: NKScriptContentController
    {
        int NKid { get; }
        Task NKloadPlugin<T>(T plugin, string ns = null, Dictionary<string, object> options = null) where T : class;
        Task<object> NKevaluateJavaScript(string javaScriptString, string filename = null);
        Task NKinjectScript(NKScriptSource source);
        string NKserialize(object obj);
        object NKdeserialize(string json);
        NKScriptValue NKgetScriptValue(string key);
    }

    public interface NKScriptContextRemotingProxy : NKScriptContentController
    {
        int NKid { get; }
        void NKevaluateJavaScript(string javaScriptString, string filename = null);
        void NKready();
        NKScriptContext context { get; set; }
    }

    public interface NKScriptContentController
    {
        void NKaddScriptMessageHandler(NKScriptMessageHandler scriptMessageHandler, string name);
        void NKremoveScriptMessageHandlerForName(string name);
    }
}