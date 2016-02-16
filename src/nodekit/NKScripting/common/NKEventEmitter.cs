using System;
using System.Collections.Generic;
using System.Linq;

namespace io.nodekit
{
    public interface NKEventSubscription
    {
        void remove();
    }

    public class NKEventSubscriptionGeneric<T> : NKEventSubscription
    {
        private static int seq = 1;

        public void remove()
        {
            emitter.subscriptions[eventType].Remove(id);
        }

        private NKEventEmitter emitter;
        private string eventType;
        internal int id;

        internal Action<T> handler;

        internal NKEventSubscriptionGeneric(NKEventEmitter emitter, string eventType, Action<T> handler)
        {
            id = seq++;
            this.eventType = eventType;
            this.emitter = emitter;
            this.handler = handler; 
        }
    }

    public class NKEventEmitter
    {
        public static NKEventEmitter global = new NKSignalEmitter();
        protected NKEventSubscription currentSubscription;

        // TODO: Switch to ConcurrentDictionary
        internal Dictionary<string, Dictionary<int, NKEventSubscription>> subscriptions = new Dictionary<string, Dictionary<int, NKEventSubscription>>();

        public NKEventSubscription on<T>(string eventType, Action<T> handler)
        {
            Dictionary<int, NKEventSubscription> eventSubscriptions;
            if (subscriptions.ContainsKey(eventType))
                eventSubscriptions = subscriptions[eventType];
            else
                eventSubscriptions = new Dictionary<int, NKEventSubscription>();

            var subscription = new NKEventSubscriptionGeneric<T>(this, eventType, handler);
            eventSubscriptions[subscription.id] = subscription;
            subscriptions[eventType] = eventSubscriptions;
            return subscription; 
        }

        public virtual void once<T>(string eventType, Action<T> handler)
        {
            on<T>(eventType, (T data) =>
            {
                this.currentSubscription.remove();
                handler(data);
            });
        }

        public void removeAllListeners(string eventType)
        {
            if (eventType != null)
                subscriptions.Remove(eventType);
            else
                subscriptions.Clear();
        }

        public virtual void emit<T>(string eventType, T data)
        {
            if (subscriptions.ContainsKey(eventType)) {
                var eventSubscriptions = subscriptions[eventType].Values.ToArray<NKEventSubscription>();

                for (int i = eventSubscriptions.Length - 1; i >= 0; i--) //Loop backwards so you can remove elements.
                {
                    var item = eventSubscriptions[i];
                    currentSubscription = item;
                    (item as NKEventSubscriptionGeneric<T>).handler.Invoke(data);
                } 
            }
        }
    }

    public class NKSignalEmitter : NKEventEmitter
    {
        private Dictionary<string, object> earlyTriggers = new Dictionary<string, object>();

        override public void once<T>(string eventType, Action<T> handler) 
        {
            if (earlyTriggers.ContainsKey(eventType))
            {
                var data = earlyTriggers[eventType];
                earlyTriggers.Remove(eventType);
                handler.Invoke((T)data);
                return;
            }

            this.on<T>(eventType, (data) =>
            {
                currentSubscription.remove();
                handler(data);
            });
        }

        override public void emit<T>(string eventType, T data)
        {
            if (subscriptions.ContainsKey(eventType))
            {
                var eventSubscriptions = subscriptions[eventType].Values.ToArray<NKEventSubscription>();

                for (int i = eventSubscriptions.Length - 1; i >= 0; i--) //Loop backwards so you can remove elements.
                {
                    var item = eventSubscriptions[i];
                    currentSubscription = item;
                    (item as NKEventSubscriptionGeneric<T>).handler.Invoke(data);
                }
            } else
            {
                earlyTriggers[eventType] = data;
            }
        }
    }
}