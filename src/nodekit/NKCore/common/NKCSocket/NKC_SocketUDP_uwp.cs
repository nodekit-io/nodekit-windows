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
#if WINDOWS_UWP
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Networking;

using io.nodekit.NKScripting;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using System.IO;

namespace io.nodekit.NKCore
{
    public sealed class NKC_SocketUDP
    {
   
        #region NKScriptExport

        internal static Task attachToContext(NKScriptContext context, Dictionary<string, object> options)
        {
            return context.NKloadPlugin(typeof(NKC_SocketUDP), null, options);
        }

        private static string defaultNamespace { get { return "io.nodekit.platform.UDP"; } }

        private static string rewriteGeneratedStub(string stub, string forKey)
        {
            switch (forKey)
            {
                case ".global":
                    var appjs = NKStorage.getResource(typeof(NKC_SocketUDP), "udp.js", "lib/platform");
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

        /* NKC_SocketUDP
        * Creates _udp JSValue that inherits from EventEmitter
        *
        * _udp.bind(ip, port)
        * _udp.recvStart()
        * _udp.send(string, port, address)
        * _udp.recvStop()
        * _udp.localAddress returns {String addr, int port}
        * _udp.remoteAddress  returns {String addr, int port}
        * _udp.addMembership(mcastAddr, ifaceAddr)
        * _udp.setMulticastTTL(ttl)
        * _udp.setMulticastLoopback(flag);
        * _udp.setBroadcast(flag);
        * _udp.setTTL(ttl);
        *
        * emits 'recv'  (base64 chunk)
        *
        */

        // local variables and init
        private DatagramSocket _socket;
     
        public NKC_SocketUDP()
        {
            _socket = new DatagramSocket();
        }

        private async void _socket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            var stream = args.GetDataStream();
        
            IBuffer buffer = new Windows.Storage.Streams.Buffer(10240);
            var bytesRead = buffer.Capacity;
            while (bytesRead == buffer.Capacity)
            {
                IBuffer data = await stream.ReadAsync(buffer, buffer.Capacity, InputStreamOptions.Partial);
                bytesRead = data.Length;
                if (data.Length == 0) break;
                _emitRecv(data, args.RemoteAddress.DisplayName, Int32.Parse(args.RemotePort));
            }
        }

        // public methods
        public async Task<Int32> bind(string address, Int32 port, Int32 flags)
        {
            await _socket.BindEndpointAsync(new HostName(address), port.ToString());
            return 0;
        }

        public void recvStart()
        {
            _socket.MessageReceived += _socket_MessageReceived;
        }

        public void recvStop()
        {
            _socket.MessageReceived -= _socket_MessageReceived;
        }

        public Dictionary<string, object> localAddressSync()
        {
            var address = _socket.Information.LocalAddress.DisplayName;
            var port = _socket.Information.LocalPort;
            return new Dictionary<string, object> { { "address", address }, { "port", port } };
        }

        public void send(string contents, string address, Int32 port)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(contents);
            var plainText = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            IBuffer buffUTF8 = CryptographicBuffer.ConvertStringToBinary(plainText, BinaryStringEncoding.Utf8);

            var _ = _socket.OutputStream.WriteAsync(buffUTF8);
        }
       
        public void addMembership(string mcastAddr, string ifaceAddr)
        {
            _socket.JoinMulticastGroup(new HostName(mcastAddr));

            if (ifaceAddr != string.Empty && ifaceAddr != null)
                throw new NotImplementedException();

            // TO DO ICommsInterface 
        }

        public void dropMembership(string mcastAddr, string ifaceAddr)
        {
            throw new NotSupportedException("Multicast Leave Group not supported on Universal Windows Platform");
        }

        public void setMulticastTTL(Int32 ttl)
        {
            throw new NotSupportedException("Multicast TTL not supported on Universal Windows Platform");
        }

        public void setMulticastLoopback(bool flag)
        {
            throw new NotSupportedException("Multicast loopback not supported on Universal Windows Platform");
        }

        public void setTTL(Int32 ttl)
        {
            _socket.Control.OutboundUnicastHopLimit = (byte) ttl;
        }

        public void setBroadcast(bool flag)
        {
            throw new NotSupportedException("UDP Broadcast not supported on Universal Windows Platform");
        }

        public void disconnect()
        {
            if (_socket != null)
                _socket.Dispose();

            _socket = null;
        }

        // private methods
        private void _emitRecv(IBuffer data, string host, Int32 port)
        {
            string str = Windows.Security.Cryptography.CryptographicBuffer.EncodeToBase64String(data);
            this.getNKScriptValue().invokeMethod("emit", new object[] { "recv", str, host, port });
        }

    }
}

#endif