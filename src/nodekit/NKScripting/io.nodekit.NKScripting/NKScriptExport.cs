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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace io.nodekit.NKScripting
{
    public enum NKScriptExportType 
    {
         NKScriptExport,  // Default for most users!
         JSExport,
         WinRT  // Universal Windows Component Projection directly to Chakra or ChakraCore scripting engines
    }

    public abstract class NKScriptExport: INotifyPropertyChanged
    {
        public abstract event PropertyChangedEventHandler PropertyChanged;
        public virtual string rewriteGeneratedStub(string stub, string forKey) { return stub; }
        public virtual string rewritescriptNameForKey(string key) { return key; }
        public virtual bool isExcludedFromScript(string key) { return false; }
    }

    internal class NKScriptExportProxy
    {
        private object instance;
        private Type t;
        private MethodInfo _rewriteGeneratedStub;
        private MethodInfo _rewritescriptNameForKey;
        private MethodInfo _isExcludedFromScript;

        internal NKScriptExportProxy(object plugin)
        {
            t = plugin.GetType();
            if (t == typeof(Type))
            {
                instance = null;  // static methods only for constructor based plugins
                t = (Type)plugin;
            }
            else
                instance = plugin;

            _rewriteGeneratedStub = t.GetMethod("rewriteGeneratedStub");
            _rewritescriptNameForKey = t.GetMethod("rewritescriptNameForKey");
            _isExcludedFromScript = t.GetMethod("isExcludedFromScript");
        }

        internal string rewriteGeneratedStub(string stub, string forKey)
        {
            if (_rewriteGeneratedStub != null)
                return (string)_rewriteGeneratedStub.Invoke(null, new[] { stub, forKey });
            else
                return stub; 
        }

        internal string rewritescriptNameForKey(string key)
        {
            if (_rewritescriptNameForKey != null)
                return (string)_rewritescriptNameForKey.Invoke(null, new[] { key });
            else
                return key;
        }

        internal bool isExcludedFromScript(string key)
        {
            if (_rewritescriptNameForKey != null)
                return (bool)_isExcludedFromScript.Invoke(null, new[] { key });
            else
                return false;
        }

    }
}
