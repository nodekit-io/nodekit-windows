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

    internal struct NKE_IPC_Event
    {
        public int sender;
        public string channel;
        public string replyId;
        public object[] arg;

        public NKE_IPC_Event(int sender, string channel, string replyId, object[] arg)
        {
            this.sender = sender;
            this.channel = channel;
            this.replyId = replyId;
            this.arg = arg;
        }
    }
}

