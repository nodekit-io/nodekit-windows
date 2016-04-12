#if WINDOWS_UWP
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
using Windows.Storage;

namespace io.nodekit
{
    public class NKStorage
    {

        public static string getResource(Type t, string name, string folder)
        {
            return getResource(t, null, name, folder);
        }

        public static Task<string> getResourceAsync(Type t, string name, string folder)
        {
            return getResourceAsync(t, null, name, folder);
        }

        public static async Task<string> getResourceAsync(Type t1, Type t2, string name, string folder)
        {
            string source;
             // First try embedded resources
            var stream = t1.GetTypeInfo().Assembly.GetManifestResourceStream(t1.Namespace + "." + folder + "." + name);

            if (stream == null && t2 != null)
                stream = t2.GetTypeInfo().Assembly.GetManifestResourceStream(t2.Namespace + "." + folder + "." + name);

            if (stream != null)
            {
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    source = await streamReader.ReadToEndAsync();
                }
            } else
            {
                // Else get from content folder in installed location
                StorageFolder root = Windows.ApplicationModel.Package.Current.InstalledLocation;
                StorageFolder lib = await root.GetFolderAsync(folder);
                var file = await lib.GetFileAsync(name);
                source = await FileIO.ReadTextAsync(file);
            }

            return source;
        }

        public static string getResource(Type t1, Type t2, string name, string folder)
        {
            var t = getResourceAsync(t1, t2, name, folder);
            t.Wait();
            return t.Result;
        }
    }
}
#endif