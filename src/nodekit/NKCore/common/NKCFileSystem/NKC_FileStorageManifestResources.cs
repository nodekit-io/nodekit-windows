using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace io.nodekit.NKCore
{
    public interface NKC_FileStorageAdpater
    {
        bool exists(string path);
        Dictionary<string, object> stat(string path);
        string[] getDirectory(string path);
        Task<string> getContent(string path);
    }

    public class NKC_FileStorageManifestResources : NKC_FileStorageAdpater
    {
        private Assembly assembly;
        private string ns;
        private string[] resources;
        private DateTime compiledOn;

        public NKC_FileStorageManifestResources(Type t1)
        {
            assembly = t1.GetTypeInfo().Assembly;
             ns = t1.Namespace;
            resources = assembly.GetManifestResourceNames();

            var version = new System.Version( assembly.FullName.Split(',')[1].Split('=')[1]  );

            compiledOn = new DateTime(
               version.Build * TimeSpan.TicksPerDay + version.Revision * TimeSpan.TicksPerSecond * 2
            ).AddYears(1999);
        }

        public bool exists(string path)
        {
           return resources.Any(t => t.StartsWith(_getResourcePath(path)));
        }

        private bool fileExists(string path)
        {
            return resources.Any(t => t.Equals(_getResourcePath(path)));
        }

        public async Task<string> getContent(string path)
        {
            string source;

            using (var stream = assembly.GetManifestResourceStream(_getResourcePath(path)))
            {
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    source = await streamReader.ReadToEndAsync();
                }
            }        

            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(source);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public Dictionary<string, object> stat(string path)
        {
            bool isFile = false;

            if (fileExists(path))
                isFile = true;
            else if (!exists(path))
                return null;

            Dictionary<string, object> storageItem = new Dictionary<string, object>();

            storageItem["birthtime"] = compiledOn;
            storageItem["path"] = path;
            storageItem["mtime"] = compiledOn;

            if (isFile)
            {
                using (var stream = assembly.GetManifestResourceStream(_getResourcePath(path)))
                {
                    storageItem["size"] = stream.Length;
                }

                storageItem["filetype"] = "File";
            }
            else
            {
                storageItem["size"] = 0;
                storageItem["filetype"] = "Directory";
            }

            return storageItem;
        }


        public string[] getDirectory(string path)
        {
            var resourcePath = _getResourcePath(path);
            var i = resourcePath.Length;
            IEnumerable<string> query = from t in resources
                                        where t.StartsWith(resourcePath)
                                        select new StringBuilder(t.Substring(i))
                                        .Replace(".", @"\", 0, t.Substring(i).LastIndexOf("."))
                                        .ToString();
            return query.ToArray();
        }

        // private helpers
        private string _getResourcePath(string path)
        {
            return ns + "." + path.Replace(@"\", ".").Replace("/", ".").Replace("-","_");
        }
    }
}
