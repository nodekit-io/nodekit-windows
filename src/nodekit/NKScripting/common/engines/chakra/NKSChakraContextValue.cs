using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace io.nodekit.NKScripting.Engines.Chakra
{
    internal class NKChakraContextValue : NKScriptValueProtocol
    {
        private JavaScriptValue value;
        private NKSChakraContext context;
        private string ns;

        internal NKChakraContextValue(NKSChakraContext context, string ns, JavaScriptValue value)
        {
            this.value = value;
            this.ns = ns;
            this.context = context;
        }

        string NKScriptValueProtocol.ns { get { return this.ns; } }

        public Task invokeMethod(string method, object[] args)
        {
            return context.ensureOnEngineThread(() =>
            {
                context.switchContextifNeeded();
                var methodFn = value.GetProperty(JavaScriptPropertyId.FromString(method));
                string[] argsSerialized = args.Select(arg => context.NKserialize(arg)).ToArray<string>();
                var argsConverted = argsSerialized.Select(arg => JavaScriptValue.FromString(arg)).ToArray<JavaScriptValue>();
                methodFn.CallFunction(argsConverted);
            });
        }
    }
}
