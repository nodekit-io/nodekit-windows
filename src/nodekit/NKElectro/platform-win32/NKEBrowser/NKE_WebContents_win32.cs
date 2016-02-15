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

namespace io.nodekit.NKElectro
{
    public class NKE_WebContents : NKE_WebContentsBase
    {

        public NKE_WebContents(NKE_BrowserWindow parent) { }


        // Event:  'did-fail-load'
        // Event:  'did-finish-load'

        public bool canGoBack()
        {
            throw new NotImplementedException();
        }

        public bool canGoForward()
        {
            throw new NotImplementedException();
        }

        public void executeJavaScript(string code, string userGesture)
        {
            throw new NotImplementedException();
        }

        public string getTitle()
        {
            throw new NotImplementedException();
        }

        public string getURL()
        {
            throw new NotImplementedException();
        }

        public string getUserAgent()
        {
            throw new NotImplementedException();
        }

        public void goBack()
        {
            throw new NotImplementedException();
        }

        public void goForward()
        {
            throw new NotImplementedException();
        }

        public void ipcReply(int dest, string channel, string replyId, object result)
        {
            throw new NotImplementedException();
        }

        public void ipcSend(string channel, string replyId, object[] arg)
        {
            throw new NotImplementedException();
        }

        public bool isLoading()
        {
            throw new NotImplementedException();
        }

        public void loadURL(string url, Dictionary<string, object> options)
        {
            throw new NotImplementedException();
        }

        public void reload()
        {
            throw new NotImplementedException();
        }

        public void reloadIgnoringCache()
        {
            throw new NotImplementedException();
        }

        public void setUserAgent(string userAgent)
        {
            throw new NotImplementedException();
        }

        public void stop()
        {
            throw new NotImplementedException();
        }





        /* ****************************************************************** *
         *               REMAINDER OF ELECTRO API NOT IMPLEMENTED             *
         * ****************************************************************** */

        public NKScriptValue session
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void addWorkSpace(string path)
        {
            throw new NotImplementedException();
        }

        public void beginFrameSubscription(NKScriptValue callback)
        {
            throw new NotImplementedException();
        }

        public void canGoToOffset(int offset)
        {
            throw new NotImplementedException();
        }

        public void clearHistory()
        {
            throw new NotImplementedException();
        }

        public void closeDevTools()
        {
            throw new NotImplementedException();
        }

        public void copyclipboard()
        {
            throw new NotImplementedException();
        }

        public void cut()
        {
            throw new NotImplementedException();
        }

        public void delete()
        {
            throw new NotImplementedException();
        }

        public void disableDeviceEmulation()
        {
            throw new NotImplementedException();
        }

        public void downloadURL(string url)
        {
            throw new NotImplementedException();
        }

        public void enableDeviceEmulation(Dictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }

        public void endFrameSubscription()
        {
            throw new NotImplementedException();
        }

        public void goToIndex(int index)
        {
            throw new NotImplementedException();
        }

        public void goToOffset(int offset)
        {
            throw new NotImplementedException();
        }

        public void hasServiceWorker(NKScriptValue callback)
        {
            throw new NotImplementedException();
        }

        public void insertCSS(string css)
        {
            throw new NotImplementedException();
        }

        public void inspectElement(int x, int y)
        {
            throw new NotImplementedException();
        }

        public void inspectServiceWorker()
        {
            throw new NotImplementedException();
        }

        public bool isAudioMuted()
        {
            throw new NotImplementedException();
        }

        public void isCrashed()
        {
            throw new NotImplementedException();
        }

        public void isDevToolsFocused()
        {
            throw new NotImplementedException();
        }

        public void isDevToolsOpened()
        {
            throw new NotImplementedException();
        }

        public bool isWaitingForResponse()
        {
            throw new NotImplementedException();
        }

        public void openDevTools(Dictionary<string, object> options)
        {
            throw new NotImplementedException();
        }

        public void paste()
        {
            throw new NotImplementedException();
        }

        public void pasteAndMatchStyle()
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

        public void redo()
        {
            throw new NotImplementedException();
        }

        public void removeWorkSpace(string path)
        {
            throw new NotImplementedException();
        }

        public void replace(string text)
        {
            throw new NotImplementedException();
        }

        public void replaceMisspelling(string text)
        {
            throw new NotImplementedException();
        }

        public void savePage(string fullstring, string saveType, NKScriptValue callback)
        {
            throw new NotImplementedException();
        }

        public void selectAll()
        {
            throw new NotImplementedException();
        }

        public void sendInputEvent(Dictionary<string, object> e)
        {
            throw new NotImplementedException();
        }

        public void setAudioMuted(bool muted)
        {
            throw new NotImplementedException();
        }

        public void toggleDevTools()
        {
            throw new NotImplementedException();
        }

        public void undo()
        {
            throw new NotImplementedException();
        }

        public void unregisterServiceWorker(NKScriptValue callback)
        {
            throw new NotImplementedException();
        }

        public void unselect()
        {
            throw new NotImplementedException();
        } 

        // Event:  'certificate-error'
        // Event:  'crashed'
        // Event:  'destroyed'
        // Event:  'devtools-closed'
        // Event:  'devtools-focused'
        // Event:  'devtools-opened'
        // Event:  'did-frame-finish-load'
        // Event:  'did-get-redirect-request'
        // Event:  'did-get-response-details'
        // Event:  'did-start-loading'
        // Event:  'did-stop-loading'
        // Event:  'dom-ready'
        // Event:  'login'
        // Event:  'new-window'
        // Event:  'page-favicon-updated'
        // Event:  'plugin-crashed'
        // Event:  'select-client-certificate'
        // Event:  'will-navigate'
        
    }
}