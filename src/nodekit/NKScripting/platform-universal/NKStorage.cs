using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Storage;

namespace io.nodekit
{
    public class NKStorage
    {
        public static string getResource(Type t, string name, string folder)
        {
            string source = null;
            var resourceNamespace = t.Namespace;

            var assembly = t.GetTypeInfo().Assembly;
            var resources = assembly.GetManifestResourceNames();

            // First try embedded resources
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

        public static async Task<string> getResourceAsync(Type t, string name, string folder)
        {
            string source;
            var resourceNamespace = t.Namespace;

             var assembly = t.GetTypeInfo().Assembly;
            var resources = assembly.GetManifestResourceNames();

            // First try embedded resources
            var stream = assembly.GetManifestResourceStream(resourceNamespace + "." + folder + "." + name);

            if (stream != null)
            {
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    source = streamReader.ReadToEnd();
                }
            }
            else
            {
                // Else get from content folder in installed location
                StorageFolder root = Windows.ApplicationModel.Package.Current.InstalledLocation;
                StorageFolder assets = await root.GetFolderAsync(resourceNamespace);
                StorageFolder lib = await assets.GetFolderAsync(folder);
                var file = await lib.GetFileAsync(name);
                source = await FileIO.ReadTextAsync(file);
            }

            return source;
        }

    }
}
