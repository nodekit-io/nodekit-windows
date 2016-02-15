/*
* nodekit.io
*
* Copyright (c) 2016 OffGrid Networks. All Rights Reserved.
* Portions Copyright (c) 2013 GitHub, Inc. under MIT License
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

namespace io.nodekit.NKElectro
{
    internal static class NKEAppDirectory
    {
        internal static string getPath(string name)
        {
            switch (name)
            {
                case "home":
                    return Windows.Storage.KnownFolders.HomeGroup.Path.ToString();
                case "appData":
                    return Windows.Storage.ApplicationData.Current.LocalFolder.Path.ToString();
                case "userData":
                    return Windows.Storage.KnownFolders.HomeGroup.Path.ToString();
                case "temp":
                    return Windows.Storage.ApplicationData.Current.TemporaryFolder.Path.ToString();
                case "exe":
                    return Windows.ApplicationModel.Package.Current.InstalledLocation.Path.ToString();
                case "module":
                    return "";
                case "desktop":
                    return "";
                case "documents":
                    return Windows.Storage.KnownFolders.DocumentsLibrary.Path.ToString();
                case "downloads":
                    return "$Downloads"; // Windows.Storage.DownloadsFolder.
                case "music":
                    return Windows.Storage.KnownFolders.MusicLibrary.Path.ToString();
                case "pictures":
                    return Windows.Storage.KnownFolders.PicturesLibrary.Path.ToString();
                case "videos":
                    return Windows.Storage.KnownFolders.VideosLibrary.Path.ToString();
                default:
                    return "";
            }
        }

        internal static string getName()
        {
            return Windows.ApplicationModel.Package.Current.DisplayName;
        }

        internal static string getVersion()
        {
            return Windows.ApplicationModel.Package.Current.Id.Version.ToString();
        }
    }
}
