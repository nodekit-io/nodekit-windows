using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if WINDOWS_UWP
namespace io.nodekit.NKScripting.Engines.Chakra
#elif WINDOWS_WIN32
namespace io.nodekit.NKScripting.Engines.ChakraCore
#endif
{
    public class NKSChakraContextFactory
    {
        private static JavaScriptRuntime runtime;
        private static bool runtimeCreated = false;
        private static SingleThreadSynchronizationContext syncContext;

        public static Task<NKScriptContext> createContext(Dictionary<string, object> options = null)
        {

            syncContext = new SingleThreadSynchronizationContext();

            Task.Factory.StartNew(() => {syncContext.RunOnCurrentThread(); }, TaskCreationOptions.LongRunning );
            var tcs = new TaskCompletionSource<NKScriptContext>();

            var oldSyncContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(syncContext);
            var taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(oldSyncContext);

            syncContext.Post((state) =>
            {
               
            }, null);
           
            return Task.Factory.StartNew(() =>
             {
                 if (options == null)
                     options = new Dictionary<string, object>();

                 if (!runtimeCreated)
                 {
                     runtime = JavaScriptRuntime.Create(JavaScriptRuntimeAttributes.None, JavaScriptRuntimeVersion.VersionEdge, null);
                     runtimeCreated = true;
                 }

                 var chakra = runtime.CreateContext();
                 int id = NKScriptContextFactory.sequenceNumber++;
                 var context = new NKSChakraContext(id, chakra, options);

                 var item = new Dictionary<String, object>();
                 NKScriptContextFactory._contexts[id] = item;
                 item["JSVirtualMachine"] = runtime;  // if future non-shared runtimes required;
                 item["context"] = context;

                return context.completeInitialization();
             }, Task.Factory.CancellationToken, TaskCreationOptions.LongRunning, taskScheduler).Unwrap();
        }
    }


    /// <summary>Provides a SynchronizationContext that's single-threaded.</summary>
    internal sealed class SingleThreadSynchronizationContext : SynchronizationContext
    {
        /// <summary>The queue of work items.</summary>
        private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> m_queue =
            new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();
      
        /// <summary>Dispatches an asynchronous message to the synchronization context.</summary>
        /// <param name="d">The System.Threading.SendOrPostCallback delegate to call.</param>
        /// <param name="state">The object passed to the delegate.</param>
        public override void Post(SendOrPostCallback d, object state)
        {
            if (d == null) throw new ArgumentNullException("d");
            m_queue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
        }

        /// <summary>Not supported.</summary>
        public override void Send(SendOrPostCallback d, object state)
        {
            throw new NotSupportedException("Synchronously sending is not supported.");
        }

        /// <summary>Runs an loop to process all queued work items.</summary>
        public void RunOnCurrentThread()
        {
            foreach (var workItem in m_queue.GetConsumingEnumerable())
                workItem.Key(workItem.Value);
        }

        /// <summary>Notifies the context that no more work will arrive.</summary>
        public void Complete() { m_queue.CompleteAdding(); }
    }
}

