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

namespace io.nodekit.NKCore
{
    public sealed class NKC_SocketTCP : NKScriptExport
    {
        //  private NKEventEmitter events = NKEventEmitter.global;

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
        private StreamSocketListener _socketListener;
        private String _socketListenerLocalAddress;
        private StreamSocket _socket;
        private NKC_SocketTCP _server;

        public NKC_SocketTCP()
        {
            _socket = new StreamSocket();
        }

        public NKC_SocketTCP(StreamSocket socket, NKC_SocketTCP server)
        {
            _socket = socket;
            _server = server;
        }

        // public methods
        public async Task<Int32> bind(string address, Int32 port)
        {
            _socketListener = new StreamSocketListener();
            _socketListenerLocalAddress = address;
            _socketListener.ConnectionReceived += _socket_ConnectionReceived;
            await _socketListener.BindEndpointAsync(new HostName(address), port.ToString());
            return 0;
        }

        public async Task connect(string address, Int32 port)
        {
            await _socket.ConnectAsync(new HostName(address), port.ToString());
            _emitAfterConnect(_socket.Information.RemoteAddress.DisplayName, Int32.Parse(_socket.Information.RemotePort));
            var _ = _receiveData();
        }

        public void listen(Int32 backlog)
        {
        }

        public Int32 fdSync()
        {
            return 0;
        }

        public Dictionary<string, object> remoteAddressSync()
        {
            var address = _socket.Information.RemoteAddress.DisplayName;
            var port = _socket.Information.RemotePort;
            return new Dictionary<string, object> { { "address", address }, { "port", port } };
        }
        public Dictionary<string, object> localAddressSync()
        {
            if (_socketListener != null)
            {
                var address = _socketListenerLocalAddress;
                var port = _socketListener.Information.LocalPort;
                return new Dictionary<string, object> { { "address", address }, { "port", port } };
            }
            else
            {
                var address = _socket.Information.LocalAddress.DisplayName;
                var port = _socket.Information.LocalPort;
                return new Dictionary<string, object> { { "address", address }, { "port", port } };
            }
        }

        public async Task writeString(string contents)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(contents);
            var plainText = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            IBuffer buffUTF8 = CryptographicBuffer.ConvertStringToBinary(plainText, BinaryStringEncoding.Utf8);

            await _socket.OutputStream.WriteAsync(buffUTF8);
        }

        public void close()
        {
            if (_socketListener != null)
                _socketListener.Dispose();

            if (_socket != null)
                _socket.Dispose();

            if (_server != null)
                _server.close();

            _socketListener = null;
            _socket = null;
            _server = null;
        }

        private void _socket_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            var socketConnection = new NKC_SocketTCP(args.Socket, this);
            connections.Add(socketConnection);
            // args.Socket.setDelegate(socketConnection, delegateQueue: NKScriptChannel.defaultQueue

            _emitConnection(socketConnection);
            var _ = socketConnection._receiveData();
        }

        // incoming data receive loop 
        private async Task _receiveData()
        {
            try
            {
                var stream = _socket.InputStream;
                IBuffer buffer = new Windows.Storage.Streams.Buffer(10240);
                while (true)
                {
                    IBuffer data = await stream.ReadAsync(buffer, buffer.Capacity, InputStreamOptions.Partial);
                    if (data.Length == 0) break;
                    _emitData(data);
                }
                // Client disconnected.
                _emitEnd();
                _socket = null;
                if (_server != null)
                {
                    _server._connectionDidClose(this);
                }


            }
            catch (System.IO.IOException ex)
            {
                NKLogging.log(ex.ToString());
            }
            catch (Exception ex)
            {
                NKLogging.log(ex.ToString());
            }
        }

        // private methods

        private void _connectionDidClose(NKC_SocketTCP socketConnection)
        {
            this.connections.Remove(socketConnection);
        }

        private void _emitConnection(NKC_SocketTCP tcp)
        {
            var js = this.getNKScriptValue();

           js.invokeMethod("emit", new object[] { "connection", tcp });
        }

        private void _emitAfterConnect(string host, Int32 port)
        {
            this.getNKScriptValue().invokeMethod("emit", new object[] { "afterConnect", host, port });
        }

        private void _emitData(IBuffer data)
        {
            string str = Windows.Security.Cryptography.CryptographicBuffer.EncodeToBase64String(data);
            var js = this.getNKScriptValue();
            if (js != null)
                js.invokeMethod("emit", new object[] { "data2", str });
            else
                NKLogging.log("!TCP: Cannot find NKScriptValue");
        }

        private void _emitEnd()
        {
            this.getNKScriptValue().invokeMethod("emit", new object[] { "end", "" });
        }
    }
}

#endif