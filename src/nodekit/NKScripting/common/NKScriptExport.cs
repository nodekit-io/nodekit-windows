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
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;

namespace io.nodekit.NKScripting
{
    public enum NKScriptExportType 
    {
         NKScriptExport,  // Default for most users!
         JSExport,
         WinRT  // Universal Windows Component Projection directly to Chakra or ChakraCore scripting engines
    }

    public interface NKScriptExport
    {
     //   public abstract event PropertyChangedEventHandler PropertyChanged;
     //   public virtual string rewriteGeneratedStub(string stub, string forKey) { return stub; }
     //   public virtual string rewritescriptNameForKey(string key) { return key; }
     //   public virtual bool isExcludedFromScript(string key) { return false; }
     //   public virtual Task initializeForContext(NKScriptContext context) { return Task.FromResult<object>(null); }
    }

    internal class NKScriptExportProxy<T> where T : class
    {
        private T instance;
        private MethodInfo _rewriteGeneratedStub;
        private MethodInfo _rewritescriptNameForKey;
        private MethodInfo _isExcludedFromScript;
        private MethodInfo _initializeForContext;
        private MethodInfo _defaultNamespace;
        private Type t;

        internal NKScriptExportProxy(T plugin)
        {
            t = typeof(T);
            if (t == typeof(Type))
            {
                t = (plugin as Type);
                instance = null;
            }
            else
                instance = plugin;

            var ti = t.GetTypeInfo();
            _rewriteGeneratedStub = ti.GetDeclaredMethod("rewriteGeneratedStub");
            _rewritescriptNameForKey = ti.GetDeclaredMethod("rewritescriptNameForKey");
            _isExcludedFromScript = ti.GetDeclaredMethod("isExcludedFromScript");
            _initializeForContext = ti.GetDeclaredMethod("initializeForContext");
            var prop = ti.GetDeclaredProperty("defaultNamespace");
            if (prop != null)
                _defaultNamespace = prop.GetMethod;
            else
                _defaultNamespace = null;          
        }

        internal string rewriteGeneratedStub(string stub, string forKey)
        {
            if (_rewriteGeneratedStub != null)
                return (string)_rewriteGeneratedStub.Invoke(instance, new[] { stub, forKey });
            else
                return stub; 
        }

        internal string rewritescriptNameForKey(string key)
        {
            if (_rewritescriptNameForKey != null)
                return (string)_rewritescriptNameForKey.Invoke(instance, new[] { key });
            else
                return key;
        }

        internal bool isExcludedFromScript(string key)
        {
            if (_rewritescriptNameForKey != null)
                return (bool)_isExcludedFromScript.Invoke(instance, new[] { key });
            else
                return false;
        }

        internal Task initializeForContext(NKScriptContext context)
        {
            if (_initializeForContext != null)
                return (Task)_initializeForContext.Invoke(instance, new[] { context });
            else
                return Task.FromResult<object>(null);
        }

        internal string defaultNamespace
        {
            get
            {
                if (_defaultNamespace != null)
                    return (string)_defaultNamespace.Invoke(instance, null);
                else
                    return t.Namespace + "." + t.Name;
            }
        }

    }
}
