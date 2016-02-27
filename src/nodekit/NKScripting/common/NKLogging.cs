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

namespace io.nodekit
{

    public static class NKLogging
    {
        public static void log(object value)
        {
            if (NKEventEmitter.isMainProcess)
                System.Diagnostics.Debug.WriteLine(value);
            else
                NKEventEmitter.global.emit<NKEvent>("NK.Logging", new NKEvent(0, null, null, new object[] { value }), true);

#if WINDOWS_WIN32
     //    Console.WriteLine(value);
#endif
        }

        static NKLogging()
        {
            if (NKEventEmitter.isMainProcess)
                NKEventEmitter.global.on<NKEvent>("NK.Logging", (e, data) =>
                {
                    log(data.arg[0]);
                });
        }
    }
}
