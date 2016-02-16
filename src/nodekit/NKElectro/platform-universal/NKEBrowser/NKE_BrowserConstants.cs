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
    public enum NKEBrowserType
    {
        WKWebView,
        UIWebView,
        Edge
    }

    public struct NKEBrowserDefaults
    {
        public const NKEBrowserType nkBrowserType = NKEBrowserType.Edge;
        public const string kTitle = "NodeKit App";
        public const int kWidth = 800;
        public const int kHeight = 600;
        public const string kPreloadUR = "https://google.com";
    }

    public struct NKEBrowserOptions
    {
        public const string nkBrowserType = "nk.browserType";
        public const string kTitle = "title";
        public const string kIcon = "icon";
        public const string kFrame = "frame";
        public const string kShow = "show";
        public const string kCenter = "center";
        public const string kX = "x";
        public const string kY = "y";
        public const string kWidth = "width";
        public const string kHeight = "height";
        public const string kMinWidth = "minWidth";
        public const string kMinHeight = "minHeight";
        public const string kMaxWidth = "maxWidth";
        public const string kMaxHeight = "maxHeight";
        public const string kResizable = "resizable";
        public const string kFullscreen = "fullscreen";
        // Whether the window should show in taskbar.
        public const string kSkipTaskbar = "skipTaskbar";
        // Start with the kiosk mode, see Opera's page for description:
        // http://www.opera.com/support/mastering/kiosk/
        public const string kKiosk = "kiosk";
        // Make windows stays on the top of all other windows.
        public const string kAlwaysOnTop = "alwaysOnTop";
        // Enable the NSView to accept first mouse event.
        public const string kAcceptFirstMouse = "acceptFirstMouse";
        // Whether window size should include window frame.
        public const string kUseContentSize = "useContentSize";
        // The requested title bar style for the window
        public const string kTitleBarStyle = "titleBarStyle";
        // The menu bar is hidden unless "Alt" is pressed.
        public const string kAutoHideMenuBar = "autoHideMenuBar";
        // Enable window to be resized larger than screen.
        public const string kEnableLargerThanScreen = "enableLargerThanScreen";
        // Forces to use dark theme on Linux.
        public const string kDarkTheme = "darkTheme";
        // Whether the window should be transparent.
        public const string kTransparent = "transparent";
        // Window type hint.
        public const string kType = "type";
        // Disable auto-hiding cursor.
        public const string kDisableAutoHideCursor = "disableAutoHideCursor";
        // Use the OS X's standard window instead of the textured window.
        public const string kStandardWindow = "standardWindow";
        // Default browser window background color.
        public const string kBackgroundColor = "backgroundColor";
        // The WebPreferences.
        public const string kWebPreferences = "webPreferences";
        // The factor of which page should be zoomed.
        public const string kZoomFactor = "zoomFactor";
        // Script that will be loaded by guest WebContents before other scripts.
        public const string kPreloadScript = "preload";
        // Like --preload, but the passed argument is an URL.
        public const string kPreloadURL = "preloadURL";
        // Enable the node integration.
        public const string kNodeIntegration = "nodeIntegration";
        // Instancd ID of guest WebContents.
        public const string kGuestInstanceID = "guestInstanceId";
        // Enable DirectWrite on Windows.
        public const string kDirectWrite = "directWrite";
        // Web runtime features.
        public const string kExperimentalFeatures = "experimentalFeatures";
        public const string kExperimentalCanvasFeatures = "experimentalCanvasFeatures";
        // Opener window's ID.
        public const string kOpenerID = "openerId";
        // Enable blink features.
        public const string kBlinkFeatures = "blinkFeatures";
    }

    public struct NKEBrowserSwitches
    {

        // Enable plugins.
        public const string kEnablePlugins = "enable-plugins";
        // Ppapi Flash path.
        public const string kPpapiFlashPath = "ppapi-flash-path";
        // Ppapi Flash version.
        public const string kPpapiFlashVersion = "ppapi-flash-version";
        // Path to client certificate.
        public const string kClientCertificate = "client-certificate";
        // Disable HTTP cache.
        public const string kDisableHttpCache = "disable-http-cache";
        // Register schemes to standard.
        public const string kRegisterStandardSchemes = "register-standard-schemes";
        // Register schemes to handle service worker.
        public const string kRegisterServiceWorkerSchemes = "register-service-worker-schemes";
        // The minimum SSL/TLS version ("tls1", "tls1.1", or "tls1.2") that
        // TLS fallback will accept.
        public const string kSSLVersionFallbackMin = "ssl-version-fallback-min";
        // Comma-separated list of SSL cipher suites to disable.
        public const string kCipherSuiteBlacklist = "cipher-suite-blacklist";
        // The browser process app model ID
        public const string kAppUserModelId = "app-user-model-id";
        // The command line switch versions of the options.
        public const string kZoomFactor = "zoom-factor";
        public const string kPreloadScript = "preload";
        public const string kPreloadURL = "preload-url";
        public const string kNodeIntegration = "node-integration";
        public const string kGuestInstanceID = "guest-instance-id";
        public const string kOpenerID = "opener-id";
        // Widevine options
        // Path to Widevine CDM binaries.
        public const string kWidevineCdmPath = "widevine-cdm-path";
        // Widevine CDM version.
        public const string kWidevineCdmVersion = "widevine-cdm-version";
    }

}