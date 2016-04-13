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

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace io.nodekit
{
    public class NKStorage
    {
        public async static Task<string> getResourceAsync(Type t, string name, string folder)
        {
            string source;
            // First try embedded resources
            var stream = t.GetTypeInfo().Assembly.GetManifestResourceStream(t.Namespace + "." + folder + "." + name);

            if (stream != null)
            {
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    source = await streamReader.ReadToEndAsync();
                }
            }
            else
            {

                // Else get from content folder in installed location
                var root = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                var lib = System.IO.Path.Combine(root, folder);
                var file = System.IO.Path.Combine(root, name);

                using (StreamReader streamReader = new StreamReader(file))
                {
                    source = await streamReader.ReadToEndAsync();
                }
            }

            return source;
        }


        public static string getResource(Type t, string name, string folder)
        {
            string source;
            // First try embedded resources
            var stream = t.GetTypeInfo().Assembly.GetManifestResourceStream(t.Namespace + "." + folder + "." + name);
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
                var root = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                var lib = System.IO.Path.Combine(root, folder);
                var file = System.IO.Path.Combine(root, name);

                using (StreamReader streamReader = new StreamReader(file))
                {
                    source = streamReader.ReadToEnd();
                }
            }

            return source;
        }
    }

}
#endif