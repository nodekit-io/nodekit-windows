#if WINDOWS_WIN32
/*
* nodekit.io
*
* Copyright (c) 2016 OffGrid Networks. All Rights Reserved.
* Portions Copyright Copyright (c) 2013 Andrew C. Dvorak <andy@andydvorak.net> under MIT license
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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using io.nodekit.NKScripting;
using System.Threading;
using System.IO.Pipes;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;

namespace io.nodekit.NKRemoting
{
    [Serializable]
    internal sealed class NKRemotingMessage
    {
        internal enum Command
        {
            NKRemotingHandshake,
            NKRemotingReady,
            NKRemotingClose,
            NKevaluateJavaScript,
            NKScriptMessageSync,
            NKScriptMessageSyncReply,
            NKScriptMessage,
            NKEvent
        }
    
        public Command command;
        public string[] args;
    }

    public sealed class NKRemotingProxy: NKScriptMessageHandler, NKScriptContextRemotingProxy
    {
        // HOST-MAIN PROCESS
        public static NKScriptMessageHandler createClient(string ns, string id, int nativeSeqMax, NKScriptMessage message, NKScriptContext context, CancellationToken cancelToken)
        {
            var proxy = new NKRemotingProxy(ns, id, nativeSeqMax, message, context, cancelToken);
            var _ = ((NKScriptMessageHandler)proxy).didReceiveScriptMessageSync(message);
            return proxy;
        }

        // RE-ENTRANT RENDERER PROCESS
        public static NKRemotingProxy registerAsClient(string arg)
        {
            if (!arg.StartsWith("NKR="))
                throw new ArgumentException();

            string initMessage = Unprotect(arg.Substring(4).TrimEnd());
    
            if (initMessage == null)
                Environment.Exit(999);

            var msgArray = initMessage.Split(';');
            if (msgArray[0] != "NKREMOTING")
                Environment.Exit(999);

          return new NKRemotingProxy(msgArray);
        }

        private string id;
        private string ns;
        internal NKScriptContext context;
      
        Process process;

        PipeStream syncPipeOut;
        PipeStream syncPipeIn;
        PipeStream asyncPipe;

        private readonly BinaryFormatter _binaryFormatter = new BinaryFormatter();
        private CancellationTokenSource cancelTokenSource;
        private CancellationToken cancelToken;

        private Dictionary<string, NKScriptMessageHandler> _localHandlers;

        int NKScriptContextRemotingProxy.NKid
        {
            get
            {
                return Int32.Parse(id);
            }
        }

        NKScriptContext NKScriptContextRemotingProxy.context
        {
            get
            {
                return context;
            }

            set
            {
                context = value;
            }
        }

        //MAIN
        private NKRemotingProxy(string ns, string id, int nativeSeqMax, NKScriptMessage message, NKScriptContext context, CancellationToken cancelToken)
        {
            this.context = context;
            this._localHandlers = null;
            this.ns = ns;

            this.cancelToken = cancelToken;
            
            var exe = System.Reflection.Assembly.GetEntryAssembly().Location;
            var path = System.IO.Path.GetDirectoryName(exe);
            ProcessStartInfo startInfo = Process.GetCurrentProcess().StartInfo;
            startInfo.FileName = exe;
            startInfo.WorkingDirectory = path;
            startInfo.UseShellExecute = false;
         
            var syncPipeOut = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            var syncPipeIn = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            var pipeOutHandle = syncPipeOut.GetClientHandleAsString();
            var pipeInHandle = syncPipeIn.GetClientHandleAsString();

            this.syncPipeOut = syncPipeOut;
            this.syncPipeIn = syncPipeIn;
            
            startInfo.Arguments = "NKR=" + buildInitMessage(pipeOutHandle, pipeInHandle);

            this.id = id;
            process = Process.Start(startInfo);
            NKEventEmitter.global.emit<string>("NKS.ProcessAdded", id, false);
            process.EnableRaisingEvents = true;
            process.Exited += Process_Exited;
       
            var pipeName = Convert.ToBase64String(getUniqueKey());
            var asyncPipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
            this.asyncPipe = asyncPipe;
            NKScriptChannel.nativeFirstSequence -= 5;

            string nativeFirstSeq = NKScriptChannel.nativeFirstSequence.ToString();
   
            var handshake = new NKRemotingMessage();
            handshake.command = NKRemotingMessage.Command.NKRemotingHandshake;
            handshake.args = new string[] { pipeName, ns, id, nativeSeqMax.ToString() };

            syncPipeOut.WriteByte(100);
            syncPipeOut.Flush();
            syncPipeOut.WaitForPipeDrain();
            syncPipeOut.DisposeLocalCopyOfClientHandle();
            syncPipeIn.DisposeLocalCopyOfClientHandle();

            writeObject(syncPipeOut, handshake);
            syncPipeOut.WaitForPipeDrain();

            var handshakeReply = readObject(syncPipeIn);
            if (handshakeReply == null || handshakeReply.command != NKRemotingMessage.Command.NKRemotingHandshake)
                Environment.Exit(911);

            asyncPipe.WaitForConnection();
            cancelToken.Register(requestClientTeardown);
            var nkready = readObject(asyncPipe);
            if (nkready == null || nkready.command != NKRemotingMessage.Command.NKRemotingReady)
               Environment.Exit(910);
     
            Task.Factory.StartNew((s)=> _processServerMessages(asyncPipe), null, cancelToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            NKEventEmitter.global.forward<NKEvent>(eventForwarderFromMain);

        }

        private void requestClientTeardown()
        {
            var handshake = new NKRemotingMessage();
            handshake.command = NKRemotingMessage.Command.NKRemotingClose;
            handshake.args = new string[] { };
            writeObject(syncPipeOut, handshake);
            try
            {
                asyncPipe.Close();
                syncPipeIn.Close();
                syncPipeOut.Close();
            }
            catch { }
        }

        //RENDERER
        private NKRemotingProxy(string[] args)
        {
            NKLogging.log("+Started Renderer in new Process");
            this.context = null;
            this._localHandlers = new Dictionary<string, NKScriptMessageHandler>();
            this.cancelTokenSource = new CancellationTokenSource();
            this.cancelToken = cancelTokenSource.Token;

            var outHandle = args[2];
            var inHandle = args[1];

            var syncPipeOut = new AnonymousPipeClientStream(PipeDirection.Out, outHandle);
            var syncPipeIn = new AnonymousPipeClientStream(PipeDirection.In, inHandle);
            this.syncPipeOut = syncPipeOut;
            this.syncPipeIn = syncPipeIn;

            syncPipeIn.ReadByte();

            var handshake = readObject(syncPipeIn);
            if (handshake.command != NKRemotingMessage.Command.NKRemotingHandshake)
            {
                Environment.Exit(911);
            }

            var pipeName = handshake.args[0];
            ns = handshake.args[1];
            id = handshake.args[2];
             NKScriptChannel.nativeFirstSequence = Int32.Parse(handshake.args[3]);

            handshake.args = new string[] { };
            writeObject(syncPipeOut, handshake);
       
            var asyncPipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
            asyncPipe.Connect();
            cancelToken.Register(requestSelfTeardown);

            this.asyncPipe = asyncPipe;
       
            Task.Factory.StartNew((s) => _processClientMessages(), null, cancelToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
  
        }

        private void requestSelfTeardown()
        {
            try
            {
                asyncPipe.Close();
                syncPipeIn.Close();
                syncPipeOut.Close();
                Environment.Exit(0);
            }
            catch { }
        }

        private void eventForwarderFromMain(string eventType, NKEvent nke)
        {
            var handshake = new NKRemotingMessage();
            handshake.command = NKRemotingMessage.Command.NKEvent;
            var eventSerialized = NKData.jsonSerialize(nke);
            handshake.args = new string[] {eventType, eventSerialized };
            writeObject(syncPipeOut, handshake);
        }

        private void eventForwarderFromRenderer(string eventType, NKEvent nke)
        {
            var handshake = new NKRemotingMessage();
            handshake.command = NKRemotingMessage.Command.NKEvent;
            var eventSerialized = NKData.jsonSerialize(nke);
            handshake.args = new string[] { eventType, eventSerialized };
            writeObject(asyncPipe, handshake);
        }

        private void Process_Exited(object sender, EventArgs e)
        {   
            NKLogging.log("+[MAIN] The Renderer Process has exited");
            try
            {
                asyncPipe.Close();
                syncPipeIn.Close();
                syncPipeOut.Close();
            }
            catch { }
            NKEventEmitter.global.emit<string>("NKS.ProcessRemoved", id, false);
        }
        // MAIN PROCESS PIPE EVENT LOOP
        private async Task _processServerMessages(Stream stream)
        {
            var message = await readObjectAsync(stream);
            if (message == null)
                return;

            if (message.command == NKRemotingMessage.Command.NKevaluateJavaScript)
            {
                var _ = context.NKevaluateJavaScript(message.args[0], message.args[1]);
            }
            else if (message.command == NKRemotingMessage.Command.NKEvent)
            {
                var eventType = message.args[0];
                NKEvent eventObject = new NKEvent((IDictionary<string, object>)(NKData.jsonDeserialize(message.args[1])));
                NKEventEmitter.global.emit<NKEvent>(eventType, eventObject, false);
            } 

            if (!cancelToken.IsCancellationRequested)
                await _processServerMessages(stream);
            else
                return ;
        }
   
        // RENDERER PROCESS PIPE EVENT LOOP
        private async Task _processClientMessages()
        {
             var message = await readObjectAsync(syncPipeIn);
            if (message == null)
            {
                NKLogging.log("!Renderer Received Empty Message");
                cancelTokenSource.Cancel();
                return;
            }

            if (message.command == NKRemotingMessage.Command.NKScriptMessageSync)
            {
          
                var name = message.args[0];
                Dictionary<string, object> body = null;
                try
                {
                    body = context.NKdeserialize(message.args[1]) as Dictionary<string, object>;
                }
                catch (Exception ex)
                {
                    NKLogging.log("!Renderer Message Deserialization Error: " + ex.Message);
                }
                var nks = new NKScriptMessage(name, body);

                NKScriptMessageHandler handler = null;
                if (_localHandlers.ContainsKey(name))
                    handler = _localHandlers[name];
                else
                {
                    int target = Int32.Parse(body["$target"].ToString());
                    handler = NKScriptChannel.getNative(target);
                }
                if (handler != null)
                {
                    var nkr = new NKRemotingMessage();
                    nkr.command = NKRemotingMessage.Command.NKScriptMessageSyncReply;

                    try
                    {
                        var result = handler.didReceiveScriptMessageSync(nks);
                        nkr.args = new string[] { context.NKserialize(result) };
                    } catch (Exception ex)
                    {
                        NKLogging.log("!Renderer Message Processing Error: " + ex.Message);
                        NKLogging.log(ex.StackTrace);
                        nkr.args = new string[] { };
                    }
                    writeObject(syncPipeOut, nkr);
                }
                else
                {
                    NKLogging.log("+Renderer Received Unknown Script Message Sync");             
                }
            }
            else if (message.command == NKRemotingMessage.Command.NKScriptMessage)
            {

                var name = message.args[0];
                Dictionary<string, object> body = null;
                try
                {
                    body = context.NKdeserialize(message.args[1]) as Dictionary<string, object>;
                }
                catch (Exception ex)
                {
                    NKLogging.log("!Renderer Message Deserialization Error: " + ex.Message);

                }
                var nks = new NKScriptMessage(name, body);

                NKScriptMessageHandler handler = null;
                if (_localHandlers.ContainsKey(name))
                    handler = _localHandlers[name];
                else
                {
                    int target = Int32.Parse(body["$target"].ToString());
                    handler = NKScriptChannel.getNative(target);
                }
                if (handler != null)
                {
                      handler.didReceiveScriptMessage(nks);
              } else
                {
                    NKLogging.log("+Renderer Received Unknown Script Message " + message.args[1]);

                }
            }
            else if (message.command == NKRemotingMessage.Command.NKRemotingClose)
            {
                try
                {
                    syncPipeIn.Close();
                    syncPipeOut.Close();
                    asyncPipe.Close();

                }
                catch { }

                Environment.Exit(0);
            }
            else if (message.command == NKRemotingMessage.Command.NKEvent)
            {
                var eventType = message.args[0];
                NKEvent eventObject = new NKEvent((IDictionary<string, object>)(NKData.jsonDeserialize(message.args[1])));
                NKEventEmitter.global.emit<NKEvent>(eventType, eventObject, false);
            }

            if (!cancelToken.IsCancellationRequested)
                await _processClientMessages();
            else
                return;
        }

        // MAIN PROCESS NKScriptMessageHandler 
        void NKScriptMessageHandler.didReceiveScriptMessage(NKScriptMessage message)
        {
            var msg = new NKRemotingMessage();
            msg.command = NKRemotingMessage.Command.NKScriptMessage;
            msg.args = new[] { message.name, context.NKserialize(message.body) };
            writeObject(syncPipeOut, msg);
        }

        object NKScriptMessageHandler.didReceiveScriptMessageSync(NKScriptMessage message)
        {
            var loger = context.NKserialize(message.body);
            var task = Task.Factory.StartNew(() =>
            {
                var msg = new NKRemotingMessage();
                msg.command = NKRemotingMessage.Command.NKScriptMessageSync;
                msg.args = new[] { message.name, context.NKserialize(message.body) };
                writeObject(syncPipeOut, msg);
                return readObject(syncPipeIn);
            }
          , cancelToken, TaskCreationOptions.None, TaskScheduler.Default);

            if (!task.Wait(10000, cancelToken) && !cancelToken.IsCancellationRequested)
            {
                NKLogging.log("!Renderer is not responsive " + loger );
                if (!process.HasExited)
                    process.Kill();
                return null;
            }

            NKRemotingMessage nkr = task.Result;
            if (nkr == null)
                return null;

            object result = null;
            if (nkr.args.Length>0)
               result = context.NKdeserialize(nkr.args[0]);
     
            return result;
        }

        // RENDERER NKScriptContextRemotingProxy Methods
        void NKScriptContextRemotingProxy.NKready()
        {
            var _channel = NKScriptChannel.getChannel(ns);
            context = _channel.context;
            _localHandlers[id] = _channel;
            _channel.singleInstance = true;
            NKEventEmitter.global.once("NKS.SingleInstanceComplete", (string e, string s) =>
            {
                Task.Delay(500).ContinueWith((t) => { NKLogging.log("+[RENDERER] Window Closed"); this.cancelTokenSource.Cancel(); });
            });
            
            var msg = new NKRemotingMessage();
            msg.command = NKRemotingMessage.Command.NKRemotingReady;
            msg.args = new string[] { };
            writeObject(asyncPipe, msg);
            NKEventEmitter.global.forward<NKEvent>(eventForwarderFromRenderer);

        }

        void NKScriptContextRemotingProxy.NKevaluateJavaScript(string javaScriptString, string filename)
        {
            if (!asyncPipe.IsConnected)
                return;
            var msg = new NKRemotingMessage();
            msg.command = NKRemotingMessage.Command.NKevaluateJavaScript;
            msg.args = new[] { javaScriptString, filename };
            writeObject(asyncPipe, msg);
        }

        void NKScriptContentController.NKaddScriptMessageHandler(NKScriptMessageHandler scriptMessageHandler, string name)
        {
            _localHandlers[name] = scriptMessageHandler;
        }

        void NKScriptContentController.NKremoveScriptMessageHandlerForName(string name)
        {
            _localHandlers[name] = null;
            _localHandlers.Remove(name);
        }

        // PRIVATE STREAM HELPERS
        private async Task<int> _readLengthAsync(Stream stream)
        {
            const int lensize = sizeof(int);
            var lenbuf = new byte[lensize];
            var bytesRead = await stream.ReadAsync(lenbuf, 0, lensize, cancelToken);
            if (bytesRead == 0)
                  return 0;
  
            if (bytesRead != lensize)
                throw new IOException(string.Format("Expected {0} bytes but read {1}", lensize, bytesRead));
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lenbuf, 0));
        }

        private async Task<NKRemotingMessage> _readObjectAsync(Stream stream, int len)
        {
            byte[] buffer = new byte[len];

            int totalBytesRead = 0;
            while (totalBytesRead < len)
            {
                int bytesRead = await stream.ReadAsync(buffer, totalBytesRead, len - totalBytesRead, cancelToken);
                if (bytesRead == 0)
                    break;
                totalBytesRead += bytesRead;
            }

            if (totalBytesRead < len)
                 return null;
     
            using (var memoryStream = new MemoryStream(buffer))
            {
                return (NKRemotingMessage)_binaryFormatter.Deserialize(memoryStream);
            }
        }

        private async Task<NKRemotingMessage> readObjectAsync(Stream stream)
        {
            var len = await _readLengthAsync(stream);
            return len == 0 ? null : await _readObjectAsync(stream, len);
        }

        private int _readLength(Stream stream)
        {
            const int lensize = sizeof(int);
            var lenbuf = new byte[lensize];
            var bytesRead = stream.Read(lenbuf, 0, lensize);
            if (bytesRead == 0)
                  return 0;
           
            if (bytesRead != lensize)
                throw new IOException(string.Format("Expected {0} bytes but read {1}", lensize, bytesRead));
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lenbuf, 0));
        }

        private NKRemotingMessage _readObject(Stream stream, int len)
        {
 
            byte[] buffer = new byte[len];

            int totalBytesRead = 0;
            while (totalBytesRead < len)
            {
                int bytesRead = stream.Read(buffer, totalBytesRead, len - totalBytesRead);
                if (bytesRead == 0)
                    break;
                totalBytesRead += bytesRead;
            }

            if (totalBytesRead < len)
                 return null;
 
            using (var memoryStream = new MemoryStream(buffer))
            {
                return (NKRemotingMessage)_binaryFormatter.Deserialize(memoryStream);
            }
        }

        private NKRemotingMessage readObject(Stream stream)
        {
            NKRemotingMessage obj = null;
            try
            {

                var len = _readLength(stream);
                if (len == 0)
                    return null;
                obj = _readObject(stream, len);
            }
            catch (Exception ex) { NKLogging.log("!Read Error" + ex.Message); }
            return obj;
        }

        private void _writeLength(Stream stream, int len)
        {
            var lenbuf = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(len));
            stream.Write(lenbuf, 0, lenbuf.Length);
        }

        private void _writeObject(Stream stream, byte[] data)
        {
                stream.Write(data, 0, data.Length);
               stream.Flush();
            }

        private void writeObject(Stream stream, NKRemotingMessage obj)
        {
            lock (stream) {
                byte[] data;
                using (var memoryStream = new MemoryStream())
                {
                    _binaryFormatter.Serialize(memoryStream, obj);
                    data = memoryStream.ToArray();
                }
                try
                {
                    _writeLength(stream, data.Length);
                    _writeObject(stream, data);
                }
                catch (Exception ex) { Console.WriteLine("!Write Error" + ex.Message); }
            }
        }

        // PRIVATE PROCESS/CRYPTO HELPERS
        private static string buildInitMessage(string outHandle, string inHandle)
        {
            string msg = string.Format("NKREMOTING;{0};{1}", outHandle, inHandle);
            return Protect(msg);
        }

        private static byte[] getAppGuid()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            var attribute = (GuidAttribute)assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0];
            var id = attribute.Value;
            return Encoding.UTF8.GetBytes(id);
        }

        private static byte[] getUniqueKey()
        {
            var key = new byte[32];
            using (var generator = RandomNumberGenerator.Create())
                generator.GetBytes(key);
            return key;
        }

        private static string Protect(string secret)
        {
            try
            {
               return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(secret), getAppGuid(), DataProtectionScope.CurrentUser));
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        private static string Unprotect(string msg)
        {
            try
            {
               return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(msg), getAppGuid(), DataProtectionScope.CurrentUser));
            }
            catch (CryptographicException)
            {
                  return null;
            }
        }
    }
}
#endif