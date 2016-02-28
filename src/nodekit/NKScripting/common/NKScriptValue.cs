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
    public class NKScriptValue : IDisposable
    {
        public string ns;

        [ThreadStatic]
        internal static NKScriptContext _currentContext;

        public static NKScriptContext currentContext()
        {
            return _currentContext;
        }

        protected NKScriptContext _context;
        public NKScriptContext context { get { return _context; } }

        protected NKScriptValue _origin;
        public NKScriptValue origin { get { return _origin; } }

        internal NKScriptValue() {}

        internal NKScriptValue(string ns, NKScriptContext context, NKScriptValue origin)
        {
            this.ns = ns;
            _context = context;
            if (origin != null)
                _origin = origin;
            else
                _origin = this;
        }

        // The object is a stub for a JavaScript object which was retained as an argument.
        private int reference = 0;
        internal NKScriptValue(int reference, NKScriptContext context, NKScriptValue origin)
        {
            this.ns = String.Format("{0}.$references[{1}]", origin.ns, reference);
            this.reference = reference;
            _context = context;
        }

        public Task<object> constructWithArguments(object[] args)
        {
            string exp = "new" + scriptForCallingMethod(null, args);
            return evaluateExpression(exp);
        }

        public Task callWithArguments(object[] args)
        {
            string exp = scriptForCallingMethod(null, args);
            return evaluateExpression(exp, false);
        }

        public virtual Task invokeMethod(string method, object[] args)
        {
            string exp = scriptForCallingMethod(method, args);
            return evaluateExpression(exp, false);
        }

        public Task<object> callWithArgumentsWithResult(object[] args)
        {
            string exp = scriptForCallingMethod(null, args);
            return evaluateExpression(exp, true);
        }

        public virtual Task<object> invokeMethodWithResult(string method, object[] args)
        {
            string exp = scriptForCallingMethod(method, args);
            return evaluateExpression(exp, true);
        }

        public Task defineProperty(string property, object descriptor)
        {
            string exp = String.Format("Object.defineProperty({0}, {1}, {2})", ns, property, _context.NKserialize(descriptor));
            return evaluateExpression(exp, false);
        }

        public Task deleteProperty(string property)
        {
            string exp = String.Format("delete {0}", scriptForFetchingProperty(property));
            return evaluateExpression(exp, false);
        }

        public async Task<bool> hasProperty(string property)
        {
            string exp = String.Format("{0} != undefined", scriptForFetchingProperty(property));
            return (bool)await evaluateExpression(exp, false);
        }

        public virtual Task<object> valueForProperty(string property)
        {
            return evaluateExpression(scriptForFetchingProperty(property));
        }

        public virtual Task setValue(object value, string property)
        {
            return evaluateExpression(scriptForUpdatingProperty(property, _context.NKserialize(value)), false);
        }

        public Task<object> valueAtIndex(int index)
        {
            string exp = String.Format("{0}[{1}]", ns, index);
            return evaluateExpression(exp);
        }

        public Task setValue(object value, int index)
        {
            string exp = String.Format("{0}[{1}] = {2}", ns, index, _context.NKserialize(value));
            return evaluateExpression(exp);
        }

        // Private JavaScript scripts and helpers

        async private Task<object> evaluateExpression(string expression, bool retain = true)
        {
            if (retain)
            {
                var result = await _context.NKevaluateJavaScript(expression);
                if (result != null)
                {
                    var wrapped = wrapScriptObject(result);
                    return wrapped;
                }
                return null;
            } else
            {
                return _context.NKevaluateJavaScript(expression);
            }
        }

        private string scriptForFetchingProperty(string name)
        {
            if (name == null)
            {
                return ns;
            }
            else if (name == "")
            {
                return String.Format("{0}['']", ns);
            }
            else
            {
                int idx;
                bool isNumeric = int.TryParse(name, out idx);

                if (isNumeric)
                    return String.Format("{0}[{1}]", ns, idx);
                else
                    return String.Format("{0}.{1}", ns, name);
            }
        }

        private string scriptForUpdatingProperty(string name, object value)
        {
           return scriptForFetchingProperty(name) + " = " + _context.NKserialize(value);
        }

        private string scriptForCallingMethod(string name, object[] args)
        {
           string[] argsSerialized = args.Select(arg => _context.NKserialize(arg)).ToArray<string>();

            string script = scriptForFetchingProperty(name) + "(" + String.Join(", ", argsSerialized) + ")";
            return "(function line_eval(){ try { return " + script + "} catch(ex) { console.log(ex.toString()); return ex} })()";
        }

        private string scriptForRetaining(string script)
        {
            var origin = this.origin;
            return (origin != null) ? String.Format("{0}.$retainObject({1})", origin.ns, script) : script;
        }

        protected object wrapScriptObject(object obj)
        {
            var dict = obj as Dictionary<string, object>;
            if ((dict != null) && dict.ContainsKey("$sig") && (Convert.ToInt32(dict["$sig"]) == 0x5857574F))
            {
                var num = Convert.ToInt32(dict["$ref"]);
                return new NKScriptValue(num, _context, this);
            }
            if ((dict != null) && dict.ContainsKey("$ns"))
            {
                var ns = dict["$ns"] as String;
                if (ns != null)
                    return new NKScriptValue(ns, _context, this);
            }
            return obj;
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
              
                string script;
                if (reference == 0)
                {
                    script = string.Format("delete {0}", this.ns);
                }
                else
                {
                    var origin = this.origin;
                    if (origin != null)
                        script = string.Format("{0}.$releaseObject(${1})", origin.ns, reference);
                    else return;
                }
                var _ = _context.NKevaluateJavaScript(script);
                _context = null;
                _origin = null;
                _context = null;
                _origin = null;
                reference = 0;
                ns = null;

                disposedValue = true;
            }
        }

        ~NKScriptValue()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
