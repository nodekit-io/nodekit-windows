using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NKElectro
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
