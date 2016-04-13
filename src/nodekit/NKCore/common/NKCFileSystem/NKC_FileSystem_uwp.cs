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
#if WINDOWS_UWP

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using io.nodekit.NKScripting;
using Windows.Storage;
using System.Linq;
using System.Reflection;

namespace io.nodekit.NKCore
{
    public sealed class NKC_FileSystem
    {
      internal static NKC_FileSystem current = new NKC_FileSystem();
 
#region NKScriptExport
        internal static Task attachToContext(NKScriptContext context, Dictionary<string, object> options)
        {
            return context.NKloadPlugin(current, null, options);
        }

        private static string defaultNamespace { get { return "io.nodekit.platform.filesystem"; } }

        private static string rewriteGeneratedStub(string stub, string forKey)
        {
            switch (forKey)
            {
                case ".global":
                    var appjs = NKStorage.getResource(typeof(NKC_FileSystem), "fs.js", "lib_core/platform");
                    return "function loadplugin(){\n" + appjs + "\n}\n" + stub + "\n" + "loadplugin();" + "\n";
                default:
                    return stub;
            }
        }
#endregion

        NKC_FileStorageAdpater appResources;
        NKC_FileStorageAdpater localResources;

        public NKC_FileSystem()
        {
            appResources = new NKC_FileStorageManifestResources(Main.entryType);
            localResources = new NKC_FileStorageManifestResources(typeof(NKC_FileSystem));
        }

        public async Task<Dictionary<string, object>> stat(string path)
        {

            if (appResources.exists(path))
                return appResources.stat(path);

            if (localResources.exists(path))
                return localResources.stat(path);

            Dictionary<string, object> storageItem = new Dictionary<string, object>();

            var foldername = System.IO.Path.GetDirectoryName(path);
            var filename = System.IO.Path.GetFileName(path);

            var folder = await StorageFolder.GetFolderFromPathAsync(foldername);

            var item = await folder.TryGetItemAsync(filename);
            if (item != null)
            {
                storageItem["birthtime"] = item.DateCreated;
                storageItem["path"] = item.Path;

                var properties = await item.GetBasicPropertiesAsync();
                storageItem["mtime"] = properties.DateModified;

                if (item.IsOfType(StorageItemTypes.Folder))
                {
                    storageItem["size"] = 0;
                    storageItem["filetype"] = "Directory";
                }
                else
                {
                    storageItem["size"] = properties.Size;
                    storageItem["filetype"] = "File";
                }
            }

            return storageItem;
        }

        public async Task<bool> exists(string path)
        {
            if (appResources.exists(path))
                return true;

            if (localResources.exists(path))
                return true;

            var foldername = System.IO.Path.GetDirectoryName(path);
            var filename = System.IO.Path.GetFileName(path);

            var folder = await StorageFolder.GetFolderFromPathAsync(foldername);

            var item = await folder.TryGetItemAsync(filename);
            return (item != null);
        }

        public async Task<string[]> getDirectory(string path)
        {
            if (appResources.exists(path))
                return appResources.getDirectory(path);

            if (localResources.exists(path))
                return localResources.getDirectory(path);

            var folder = await StorageFolder.GetFolderFromPathAsync(path);
            var items = await folder.GetFilesAsync();
            return items.Select(t => t.Name).ToArray();
        }

        public string getTempDirectorySync()
        {
            return Windows.Storage.ApplicationData.Current.TemporaryFolder.Path.ToString();
        }

        public async Task<string> getContent(Dictionary<string, object> storageItem)
        {
            var path = storageItem["path"] as string;

            if (appResources.exists(path))
                return await appResources.getContent(path);

            if (localResources.exists(path))
                return await localResources.getContent(path);

            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
            var source = await FileIO.ReadTextAsync(file);

            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(source);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public async Task<bool> writeContent(Dictionary<string, object> storageItem, string contents)
        {
            try
            {
                var base64EncodedBytes = System.Convert.FromBase64String(contents);
                var plainText = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);

                var path = storageItem["path"] as string;
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
                await FileIO.WriteTextAsync(file, plainText);
                return true;
            } catch 
            {
                return false;
            }
        }

        public async Task<string> getSource(string path)
        {
            var foldername = System.IO.Path.GetDirectoryName(path);
            var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            var fileExtension = System.IO.Path.GetExtension(path);
            if (fileExtension == String.Empty)
                fileExtension = ".js";

            var pathAdjusted = System.IO.Path.Combine(foldername, fileName + fileExtension);

            if (appResources.exists(pathAdjusted))
                return await appResources.getContent(pathAdjusted);

            if (localResources.exists(pathAdjusted))
                return await localResources.getContent(pathAdjusted);

            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(pathAdjusted);
            var source = await FileIO.ReadTextAsync(file);

            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(source);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public async Task<bool> mkdir(string path)
        {
            try
            {
                var folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(System.IO.Path.GetDirectoryName(path));
                await folder.CreateFolderAsync(System.IO.Path.GetFileName(path));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> rmdir(string path)
        {
            try
            {
                var folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(path);
                await folder.DeleteAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> move(string path, string path2)
        {
            try
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
                await file.RenameAsync(path2);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> unlink(string path)
        {
            try
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
                await file.DeleteAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
#endif
