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
    public sealed class NKC_SocketTCP : NKScriptExport
    {
   
#region NKScriptExport

        internal static Task attachToContext(NKScriptContext context, Dictionary<string, object> options)
        {
            return context.NKloadPlugin(typeof(NKC_SocketTCP), null, options);
        }

        private static string defaultNamespace { get { return "io.nodekit.platform.TCP"; } }

        private static string rewriteGeneratedStub(string stub, string forKey)
        {
            switch (forKey)
            {
                case ".global":
                    var appjs = NKStorage.getResource(typeof(NKC_SocketTCP), "tcp.js", "lib/platform");
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

        /*
       * Creates _tcp javascript value that inherits from EventEmitter
       * _tcp.on("connection", function(_tcp))
       * _tcp.on("afterConnect", function())
       * _tcp.on('data', function(chunk))
       * _tcp.on('end')
       * _tcp.writeBytes(data)
       * _tcp.fd returns {fd}
       * _tcp.remoteAddress  returns {String addr, int port}
       * _tcp.localAddress returns {String addr, int port}
       * _tcp.bind(String addr, int port)
       * _tcp.listen(int backlog)
       * _tcp.connect(String addr, int port)
       * _tcp.close()
       *
       */

        // local variables and init
        private List<NKC_SocketTCP> connections = new List<NKC_SocketTCP>();
         private Socket _socket;
        private NKC_SocketTCP _server;

        public NKC_SocketTCP()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public NKC_SocketTCP(Socket socket, NKC_SocketTCP server)
        {
            _socket = socket;
            _server = server;
        }

        // public methods
        public Task<Int32> bind(string address, Int32 port)
        {
            var endPoint = new IPEndPoint(IPAddress.Any, port);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket.Bind(endPoint);

            return Task.FromResult<Int32>(0);
        }

        public Task connect(string address, Int32 port)
        {

            var endpoint = new IPEndPoint(IPAddress.Parse(address), port);

            var task = Task.Factory.FromAsync(_socket.BeginConnect(endpoint, null, null), _socket.EndConnect);
            return task.ContinueWith(t =>
            {
                IPEndPoint remote = (IPEndPoint)_socket.RemoteEndPoint;
                var remoteaddress = remote.Address.ToString();
                var remoteport = remote.Port;
                _emitAfterConnect(remoteaddress, remoteport);
            }
                   , TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        public void listen(Int32 backlog)
        {
            _socket.Listen(backlog);

            _socket_BeginAccepting();
        }

        private void _socket_BeginAccepting()
        {
            var task = Task.Factory.FromAsync<Socket>(_socket.BeginAccept, _socket.EndAccept, null);
            task.ContinueWith(t =>
            {
                _socket_ConnectionReceived(t.Result);
                _socket_BeginAccepting();  
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private void _socket_ConnectionReceived(Socket newSocket)
        {
            var socketConnection = new NKC_SocketTCP(newSocket, this);
            connections.Add(socketConnection);
            _emitConnection(socketConnection);
            socketConnection._receiveData();
        }

        public Int32 fdSync()
        {
            return 0;
        }

        public Dictionary<string, object> remoteAddressSync()
        {
            IPEndPoint remote = (IPEndPoint)_socket.RemoteEndPoint;
            var address = remote.Address.ToString();
            var port = remote.Port;
            return new Dictionary<string, object> { { "address", address }, { "port", port } };
        }
        public Dictionary<string, object> localAddressSync()
        {
            IPEndPoint local = (IPEndPoint)_socket.LocalEndPoint;
            var address = local.Address.ToString();
            var port = local.Port;
            return new Dictionary<string, object> { { "address", address }, { "port", port } };
        }

        public Task writeString(string contents)
        {
            if (_socket.Connected)
            {
                var base64EncodedBytes = System.Convert.FromBase64String(contents);
                return Task.Factory.FromAsync<Int32>(_socket.BeginSend(base64EncodedBytes, 0, base64EncodedBytes.Length, SocketFlags.None, null, _socket), _socket.EndSend);
            }
            else
                return Task.FromResult<object>(null);
        }

        public void close()
        {
            if (_socket != null)
                _socket.Dispose();

            if (_server != null)
                _server.close();

            _socket = null;
            _server = null;
        }

        private byte[] buffer = new byte[8096];

        // incoming data receive loop 
        private void _receiveData()
        {
           var task = Task.Factory.FromAsync<int>(_socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, null, null), _socket.EndReceive);
            task.ContinueWith(t =>
            {

                if (task.Result == 0)
                {
                    _emitEnd();
                    _socket = null;
                    if (_server != null)
                    {
                        _server._connectionDidClose(this);
                    }

                } else
                {
                    _emitData(buffer, 0, task.Result);
                    _receiveData(); // Receive more data
                }
              
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

         
        // private methods

        private void _connectionDidClose(NKC_SocketTCP socketConnection)
        {
            this.connections.Remove(socketConnection);
        }

        private void _emitConnection(NKC_SocketTCP tcp)
        {
            this.getNKScriptValue().invokeMethod("emit", new object[] { "connection", tcp });
        }

        private void _emitAfterConnect(string host, Int32 port)
        {
            this.getNKScriptValue().invokeMethod("emit", new object[] { "afterConnect", host, port });
        }

        private void _emitData(byte[] data, int offset, int length)
        {
            string str = Convert.ToBase64String(data, offset, length);
            this.getNKScriptValue().invokeMethod("emit", new object[] { "data", str });
        }

        private void _emitEnd()
        {
            this.getNKScriptValue().invokeMethod("emit", new object[] { "end", "" });
        }
    }
}

#endif