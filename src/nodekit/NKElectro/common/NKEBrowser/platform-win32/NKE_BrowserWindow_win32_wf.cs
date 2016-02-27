#if WINDOWS_WIN32_WF
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

using System;
using System.Collections.Generic;
using io.nodekit.NKScripting;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

namespace io.nodekit.NKElectro
{
    public partial class NKE_BrowserWindow
    {
        private NKE_Window _window;
   
        private static TaskFactory syncContext;

        public static void setupSync()
        {
            syncContext = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        }

        internal Task ensureOnUIThread(Action action)
        {
             return syncContext.StartNew(action);
        }

        internal Task ensureOnUIThread(Func<Task> action)
        {
            return syncContext.StartNew(action).Unwrap();
        }

        internal void createWindow(Dictionary<string, object> options, Control customWebView)
        {
            var window = new NKE_Window(customWebView);
            window.FormClosed += Window_FormClosed;
            window.Show();
            window.Activate();
            _window = window;
        }

        private void Window_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.events.emit<string>("NKE.WindowClosed");
            
            _window = null;
            windowArray.Remove(_id);
            context = null;
            webView = null;
            this.Dispose();
        }

        public void blurWebView()
        {
            throw new NotImplementedException();
        }

        public void capturePage(Dictionary<string, object> rect, NKScriptValue callback)
        {
            throw new NotImplementedException();
        }

        public void center()
        {
            throw new NotImplementedException();
        }

        public void close()
        {
            _window.Close();
        }

        public void destroy()
        {
            throw new NotImplementedException();
        }

        public void flashFrame(bool flag)
        {
            throw new NotImplementedException();
        }

        public void focus()
        {
            throw new NotImplementedException();
        }

        public void focusOnWebView()
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, object> getBounds()
        {
            throw new NotImplementedException();
        }

        public int[] getContentSize()
        {
            throw new NotImplementedException();
        }

        public int[] getMaximumSize()
        {
            throw new NotImplementedException();
        }

        public int[] getMinimumSize()
        {
            throw new NotImplementedException();
        }

        public int[] getPosition()
        {
            throw new NotImplementedException();
        }

        public string getRepresentedFilename()
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, object> getSize()
        {
            throw new NotImplementedException();
        }

        public void getTitle()
        {
            throw new NotImplementedException();
        }

        public void hide()
        {
            throw new NotImplementedException();
        }

        public void hookWindowMessage(int message, NKScriptValue callback)
        {
            throw new NotImplementedException();
        }

        public bool isAlwaysOnTop()
        {
            throw new NotImplementedException();
        }

        public bool isDocumentEdited()
        {
            throw new NotImplementedException();
        }

        public bool isFocused()
        {
            throw new NotImplementedException();
        }

        public bool isFullScreen()
        {
            throw new NotImplementedException();
        }

        public bool isKiosk()
        {
            throw new NotImplementedException();
        }

        public bool isMaximized()
        {
            throw new NotImplementedException();
        }

        public bool isMenuBarAutoHide()
        {
            throw new NotImplementedException();
        }

        public bool isMenuBarVisible()
        {
            throw new NotImplementedException();
        }

        public bool isMinimized()
        {
            throw new NotImplementedException();
        }

        public bool isResizable()
        {
            throw new NotImplementedException();
        }

        public bool isVisible()
        {
            throw new NotImplementedException();
        }

        public bool isVisibleOnAllWorkspaces()
        {
            throw new NotImplementedException();
        }

        public void isWindowMessageHooked(int message)
        {
            throw new NotImplementedException();
        }

        public void loadURL(string url, Dictionary<string, object> options)
        {
            throw new NotImplementedException();
        }

        public void maximize()
        {
            throw new NotImplementedException();
        }

        public void minimize()
        {
            throw new NotImplementedException();
        }

        public void print(Dictionary<string, object> options)
        {
            throw new NotImplementedException();
        }

        public void printToPDF(Dictionary<string, object> options, NKScriptValue callback)
        {
            throw new NotImplementedException();
        }

        public void reload()
        {
            throw new NotImplementedException();
        }

        public void setAlwaysOnTop(bool flag)
        {
            throw new NotImplementedException();
        }

        public void setAspectRatio(double aspectRatio, int[] extraSize)
        {
            throw new NotImplementedException();
        }

        public void setAutoHideMenuBar(bool hide)
        {
            throw new NotImplementedException();
        }

        public void setBounds(Dictionary<string, object> options)
        {
            throw new NotImplementedException();
        }

        public void setContentSize(int width, int height)
        {
            throw new NotImplementedException();
        }

        public void setDocumentEdited(bool edited)
        {
            throw new NotImplementedException();
        }

        public void setFullScreen(bool flag)
        {
            throw new NotImplementedException();
        }

        public void setIgnoreMouseEvents(bool ignore)
        {
            throw new NotImplementedException();
        }

        public void setKiosk(bool flag)
        {
            throw new NotImplementedException();
        }

        public void setMaximumSize(int width, int height)
        {
            throw new NotImplementedException();
        }

        public void setMenuBarVisibility(bool visible)
        {
            throw new NotImplementedException();
        }

        public void setMinimumSize(int width, int height)
        {
            throw new NotImplementedException();
        }

        public void setPosition(int x, int u)
        {
            throw new NotImplementedException();
        }

        public void setProgressBar(double progress)
        {
            throw new NotImplementedException();
        }

        public void setRepresentedFilename(string filename)
        {
            throw new NotImplementedException();
        }

        public void setResizable(bool resizable)
        {
            throw new NotImplementedException();
        }

        public void setSize(int width, int height)
        {
            throw new NotImplementedException();
        }

        public void setSkipTaskbar(bool skip)
        {
            throw new NotImplementedException();
        }

        public void setTitle(string title)
        {
            throw new NotImplementedException();
        }

        public void setVisibleOnAllWorkspaces(bool visible)
        {
            throw new NotImplementedException();
        }

        public void show()
        {
            throw new NotImplementedException();
        }

        public void showDefinitionForSelection()
        {
            throw new NotImplementedException();
        }

        public void showInactive()
        {
            throw new NotImplementedException();
        }

        public void unhookAllWindowMessages()
        {
            throw new NotImplementedException();
        }

        public void unhookWindowMessage(int message)
        {
            throw new NotImplementedException();
        }

        public void unmaximize()
        {
            throw new NotImplementedException();
        }

        // void setMenu(menu); //LINUX WINDOWS
        // void setOverlayIcon(overlay, description); //WINDOWS 7+
        // void setThumbarButtons(buttons); //WINDOWS 7+
    }
}
#endif