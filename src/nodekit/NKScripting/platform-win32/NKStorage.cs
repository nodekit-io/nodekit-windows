using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace io.nodekit
{
    public class NKStorage
    {
        public static string getResource(System.Type t, string name, string folder)
        {
            string source = null;
            var resourceNamespace = t.Namespace;

            var assembly = t.GetTypeInfo().Assembly;
            var resources = assembly.GetManifestResourceNames();

            // try embedded resources
            var stream = assembly.GetManifestResourceStream(resourceNamespace + "." + folder + "." + name);

            if (stream != null)
            {
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    source = streamReader.ReadToEnd();
                }
            }
      
            return source;
        }

        public static Task<string> getResourceAsync(System.Type t, string name, string folder)
        {
            return Task.FromResult(getResource(t, name, folder));
        }
    }
}
