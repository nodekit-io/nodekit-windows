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
#if WINDOWS_WIN32
using io.nodekit.NKScripting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace io.nodekit.NKCore
{
    public sealed class NKC_Timer
    {
      //  private NKEventEmitter events = NKEventEmitter.global;

        #region NKScriptExport

        internal static Task attachToContext(NKScriptContext context, Dictionary<string, object> options)
        {
            return context.NKloadPlugin(typeof(NKC_Timer), null, options);
        }

        private static string defaultNamespace { get { return "io.nodekit.platform.timer"; } }

        private static string rewriteGeneratedStub(string stub, string forKey)
        {
            switch (forKey)
            {
                case ".global":
                    var appjs = NKStorage.getResource(typeof(NKC_Timer), "timer.js", "lib/platform");
                    return "function loadplugin(){\n" + appjs + "\n}\n" + stub + "\n" + "loadplugin();" + "\n";
                default:
                    return stub;
            }
        }

        private static string rewritescriptNameForKey(string key, string name)
        {
            return key == ".ctor" ? "" : name;
        }
        #endregion


        /* NKTimer
        * Creates _timer JSValue
        *
        * _timer.onTimout returns handler
        * _timer.setOnTimeout(handler)
        * _timer.close
        * _timer.start(delay, repeat)
        * _timer.stop
         */

        public NKC_Timer()
        {
            _repeatPeriod = 0;

        }

        private NKScriptValue _handler;
        private Timer _nsTimer;
        private Int32 _repeatPeriod;

        public NKScriptValue onTimeoutSync()
        {
            return _handler;
        }

        public void setOnTimeOut(NKScriptValue handler)
        {
            _handler = handler;
        }

        public void stop()
        {
            _nsTimer.Stop();
            _nsTimer.Dispose();
            _nsTimer = null;
            _repeatPeriod = 0;
        }

        public void close()
        {
            if (_nsTimer != null)
            {
                _nsTimer.Stop();
                _nsTimer.Dispose();
                _nsTimer = null;
            }
            _repeatPeriod = 0;
            _handler = null;
            this.getNKScriptValue().invokeMethod("dispose", new object[] {  });
        }

        public void start(Int32 delay, Int32 repeat)
        {
            if (_nsTimer != null)
                this.stop();

            _repeatPeriod = repeat;

            var delaySpan = TimeSpan.FromMilliseconds(delay);
            _scheduleTimeout(delaySpan);
        }

        private void _scheduleTimeout(TimeSpan timeout)
        {
            _nsTimer = new Timer(timeout.TotalMilliseconds);
            _nsTimer.Elapsed += _nsTimer_Elapsed;
            _nsTimer.AutoReset = false;
            _nsTimer.Enabled = true;
        }

        private void _nsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
             _handler.callWithArguments(new object[] { });
            if (_repeatPeriod > 0)
            {
                var delaySpan = TimeSpan.FromMilliseconds(_repeatPeriod);
                _scheduleTimeout(delaySpan);
            } else
            {
                _nsTimer = null;
                close();
            }
        
       }

    }
}
#endif