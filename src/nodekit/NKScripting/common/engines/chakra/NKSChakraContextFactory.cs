using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace io.nodekit.NKScripting.Engines.Chakra
{
    public class NKSChakraContextFactory
    {
        private static JavaScriptRuntime runtime;
        private static bool runtimeCreated = false;

        public static Task<NKScriptContext> createContext(Dictionary<string, object> options = null)
        {
            if (options == null)
                options = new Dictionary<string, object>();

            if (!runtimeCreated)
            {
                runtime = JavaScriptRuntime.Create(JavaScriptRuntimeAttributes.None, JavaScriptRuntimeVersion.VersionEdge, null);
                runtimeCreated = true;
            }

            var chakra = runtime.CreateContext();
            JavaScriptContext.Current = chakra;
            NKSChakraContext.currentContext = chakra;
            var context = new NKSChakraContext(chakra, options);

            var id = context.NKid;
            var item = new Dictionary<String, object>();
            NKScriptContextFactory._contexts[id] = item;
            item["JSVirtualMachine"] = runtime;  // if future non-shared runtimes required;
            item["context"] = context;

            return Task<NKScriptContext>.FromResult<NKScriptContext>(context);
        }
    }
}
