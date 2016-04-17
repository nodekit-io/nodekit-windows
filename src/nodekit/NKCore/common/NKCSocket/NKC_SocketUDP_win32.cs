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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using io.nodekit.NKScripting;
using System.Net.Sockets;
using System.Net;

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
         private Socket _socket;
        private SocketFlags _socketFlags = SocketFlags.None;

        public NKC_SocketUDP()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        // public methods
        public Task<Int32> bind(string address, Int32 port, Int32 flags)
        {
            var endPoint = new IPEndPoint(IPAddress.Any, port);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket.Bind(endPoint);

            return Task.FromResult<Int32>(0);
        }

        public void recvStart()
        {
            _receiveData();
        }

        public void recvStop()
        {
            throw new NotImplementedException();
        }

        private byte[] buffer = new byte[8096];

        // incoming data receive loop 
        private void _receiveData()
        {
            var remoteEP = new IPEndPoint(IPAddress.Any, 0);
            var task = Task<int>.Factory.FromAsync(
                (c, s) =>
                {
                    EndPoint endPoint = (EndPoint)s;
                    return _socket.BeginReceiveFrom(buffer, 0, buffer.Length, _socketFlags, ref endPoint, c, s);
                },
                (ia) =>
                {
                    EndPoint endPoint = (EndPoint)ia.AsyncState;
                    return _socket.EndReceiveFrom(ia, ref endPoint);
                },
                remoteEP);

            task.ContinueWith(t =>
            {

                if (task.Result == 0)
                {
                     _socket = null;
                
                }
                else
                {
                    _emitRecv(buffer, 0, task.Result, remoteEP.Address.ToString(), remoteEP.Port);
                    _receiveData(); // Receive more data
                }

            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        public Dictionary<string, object> localAddressSync()
        {
            IPEndPoint local = (IPEndPoint)_socket.LocalEndPoint;
            var address = local.Address.ToString();
            var port = local.Port;
            return new Dictionary<string, object> { { "address", address }, { "port", port } };
        }

        public void send(string contents, string address, Int32 port)
        {
             var base64EncodedBytes = System.Convert.FromBase64String(contents);
             var _ = Task.Factory.FromAsync<Int32>(_socket.BeginSend(base64EncodedBytes, 0, base64EncodedBytes.Length, SocketFlags.None, null, _socket), _socket.EndSend);
        }
        
        public void addMembership(string mcastAddr, string ifaceAddr)
        {

            MulticastOption mcastOpt = new MulticastOption(IPAddress.Parse(mcastAddr));

            // TO DO ICommsInterface 
            if (ifaceAddr != string.Empty && ifaceAddr != null)
                throw new NotImplementedException();

            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, mcastOpt);
        }

        public void dropMembership(string mcastAddr, string ifaceAddr)
        {
            MulticastOption mcastOpt = new MulticastOption(IPAddress.Parse(mcastAddr));

            // TO DO ICommsInterface 
            if (ifaceAddr != string.Empty && ifaceAddr != null)
                throw new NotImplementedException();

            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, mcastOpt);
        }

        public void setMulticastTTL(Int32 ttl)
        {
            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, ttl);
        }

        public void setMulticastLoopback(bool flag)
        {
            _socket.MulticastLoopback = flag;
        }

        public void setTTL(Int32 ttl)
        {
            _socket.Ttl = (short)ttl;
        }

        public void setBroadcast(bool flag)
        {
            _socket.EnableBroadcast = flag;
        }

        public void disconnect()
        {
            if (_socket != null)
                _socket.Dispose();

            _socket = null;
        }

         
        // private methods
        private void _emitRecv(byte[] data, int offset, int length, string host, Int32 port)
        {
            string str = Convert.ToBase64String(data, offset, length);
            this.getNKScriptValue().invokeMethod("emit", new object[] { "recv", str, host, port });
        }

    }
}

#endif