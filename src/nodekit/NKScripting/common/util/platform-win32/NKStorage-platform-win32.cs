#if WINDOWS_WIN32
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
#endif