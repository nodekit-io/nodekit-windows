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

using System.Windows;
using System.Windows.Controls;

namespace io.nodekit.NKElectro
{
    public class NKE_Window_old : Window
    {

        Border border;
        Grid hostGrid;
       public WebBrowser webView;

        public NKE_Window_old() : base() {

            border = new Border();
            hostGrid = new Grid();

            hostGrid.Children.Add(border);
            webView = new WebBrowser();
            border.Height = webView.Height;
            border.Width = webView.Width;

            //Set the user control's dimensions to double.NaN so that it auto sizes   
            //to fill the window.   
            webView.Height = double.NaN;
            webView.Width = double.NaN;

            hostGrid.Children.Add(webView);
            this.Content = hostGrid;
            this.SizeToContent = SizeToContent.WidthAndHeight;
        }

        internal void addWebView(WebBrowser webView)
        {
            border.Height = webView.Height;
            border.Width = webView.Width;

            //Set the user control's dimensions to double.NaN so that it auto sizes   
            //to fill the window.   
            webView.Height = double.NaN;
            webView.Width = double.NaN;

           hostGrid.Children.Add(webView);
        }

        
    }
}
