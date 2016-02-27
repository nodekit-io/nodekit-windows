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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace io.nodekit.NKScripting
{
    public class NKScriptSource
    {
        public string source;
        public string cleanup;
        public string filename;
        public string ns;
        private bool injected;

        private NKScriptContext _context;
    
        public NKScriptSource(string source, string asFilename, string ns = null, string cleanup = null)
        {
            injected = false;

            this.filename = asFilename;

            if (cleanup != null)
            {
                this.cleanup = cleanup;
                this.ns = ns;
            }
            else if (ns != null)
            {
                this.cleanup = string.Format("delete {0}", ns);
            }
            else
            {
                this.ns = null;
                this.cleanup = null;
            }

            if (this.filename == "")
            {
                this.source = source;
            }
            else
            {
                this.source = source + "\n//# sourceURL=" + this.filename;
            }

        }

        internal void registerInject(NKScriptContext context)
        {
            this.injected = true;
            this._context = context;
        }

        public void eject()
        {
            if (!injected)
               return;

             if (_context == null) return;

            _context.NKevaluateJavaScript(cleanup);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                eject();
                source = null;
                cleanup = null;
                filename = null;
                _context = null;
                _context = null;

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            //      GC.SuppressFinalize(this);
        }
        #endregion
    }
}

