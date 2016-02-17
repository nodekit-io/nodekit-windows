#if WINDOWS_WIN32
using System.IO;
using System.Reflection;
using static System.Environment;

namespace io.nodekit.NKElectro
{
    internal static class NKE_AppDirectory
    {
        internal static string getPath(string name)
        {
            switch (name)
            {
                case "home":
                    return GetFolderPath(SpecialFolder.UserProfile);
                 case "appData":
                    return GetFolderPath(SpecialFolder.ApplicationData);
                case "userData":
                    return GetFolderPath(SpecialFolder.UserProfile);
                case "temp":
                    return Path.GetTempPath();
                case "exe":
                    return Assembly.GetEntryAssembly().Location;
                case "module":
                    return "";
                case "desktop":
                    return GetFolderPath(SpecialFolder.Desktop);
                case "documents":
                    return GetFolderPath(SpecialFolder.MyDocuments);
                case "downloads":
                    string pathUser = GetFolderPath(SpecialFolder.UserProfile);
                    return Path.Combine(pathUser, "Downloads");
                 case "music":
                    return GetFolderPath(SpecialFolder.MyMusic);
                case "pictures":
                    return GetFolderPath(SpecialFolder.MyPictures);
                case "videos":
                    return GetFolderPath(SpecialFolder.MyVideos);
                default:
                    return "";
            }
        }

        internal static string getName()
        {
            return Assembly.GetEntryAssembly().GetName().Name;
        }

        internal static string getVersion()
        {
            return Assembly.GetEntryAssembly().GetName().Version.ToString();
        }
    }
}
#endif