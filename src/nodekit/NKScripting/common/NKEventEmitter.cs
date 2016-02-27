using System;
using System.Collections.Generic;
using System.Linq;

namespace io.nodekit
{

    public class NKEvent : Dictionary<string, object>
    {
        public int sender { get { return this.itemOrDefault<int>("sender");  } }
        public string channel { get { return this.itemOrDefault<string>("channel"); } }
        public string replyId { get { return this.itemOrDefault<string>("replyId"); } }
        public object[] arg { get { return this.itemOrDefault<object[]>("arg"); } }

        public NKEvent(int sender, string channel, string replyId, object[] arg)
        {
            this.Add("sender", sender);
            this.Add("channel", channel);
            this.Add("replyId", replyId);
            this.Add("arg", arg);
        }

        public NKEvent(IDictionary<string, object> dict)
        {
            this.Add("sender", dict["sender"]);
            this.Add("channel", dict["channel"]);
            this.Add("replyId", dict["replyId"]);
            this.Add("arg", dict["arg"]);
        }
    }

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

        internal Action<string, T> handler;

        internal NKEventSubscriptionGeneric(NKEventEmitter emitter, string eventType, Action<string, T> handler)
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

        public NKEventSubscription on<T>(string eventType, Action<string, T> handler)
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

        public virtual void once<T>(string eventType, Action<string, T> handler)
        {
            on<T>(eventType, (string e, T data) =>
            {
                this.currentSubscription.remove();
                handler(e, data);
            });
        }

        public virtual void forward<T>(Action<string, T> handler)
        {
            on<T>("*" + typeof(T).Name, handler);
        }

        public void removeAllListeners(string eventType)
        {
            if (eventType != null)
                subscriptions.Remove(eventType);
            else
                subscriptions.Clear();
        }

        public virtual void emit<T>(string eventType, T data = default(T), bool forward = true)
        {
            if (forward && subscriptions.ContainsKey("*" + typeof(T).Name))
            {
                var eventSubscriptions = subscriptions["*" + typeof(T).Name].Values.ToArray<NKEventSubscription>();

                for (int i = eventSubscriptions.Length - 1; i >= 0; i--) //Loop backwards so you can remove elements.
                {
                    var item = eventSubscriptions[i];
                    currentSubscription = item;
                    (item as NKEventSubscriptionGeneric<T>).handler.Invoke(eventType, data);
                }
            }
            if (subscriptions.ContainsKey(eventType)) {
                var eventSubscriptions = subscriptions[eventType].Values.ToArray<NKEventSubscription>();

                for (int i = eventSubscriptions.Length - 1; i >= 0; i--) //Loop backwards so you can remove elements.
                {
                    var item = eventSubscriptions[i];
                    currentSubscription = item;
                    (item as NKEventSubscriptionGeneric<T>).handler.Invoke(eventType, data);
                } 
            }
        }
    }

    public class NKSignalEmitter : NKEventEmitter
    {
        private Dictionary<string, object> earlyTriggers = new Dictionary<string, object>();

        override public void once<T>(string eventType, Action<string, T> handler) 
        {
            if (earlyTriggers.ContainsKey(eventType))
            {
                var data = earlyTriggers[eventType];
                earlyTriggers.Remove(eventType);
                handler.Invoke(eventType, (T)data);
                return;
            }

            this.on<T>(eventType, (e, data) =>
            {
                currentSubscription.remove();
                handler(e, data);
            });
        }

        override public void emit<T>(string eventType, T data, bool forward = true)
        {
            if (forward && subscriptions.ContainsKey("*" + typeof(T).Name))
            {
                var eventSubscriptions = subscriptions["*" + typeof(T).Name].Values.ToArray<NKEventSubscription>();

                for (int i = eventSubscriptions.Length - 1; i >= 0; i--) //Loop backwards so you can remove elements.
                {
                    var item = eventSubscriptions[i];
                    currentSubscription = item;
                    (item as NKEventSubscriptionGeneric<T>).handler.Invoke(eventType, data);
                }
            } 

            if (subscriptions.ContainsKey(eventType))
            {
                var eventSubscriptions = subscriptions[eventType].Values.ToArray<NKEventSubscription>();

                for (int i = eventSubscriptions.Length - 1; i >= 0; i--) //Loop backwards so you can remove elements.
                {
                    var item = eventSubscriptions[i];
                    currentSubscription = item;
                    (item as NKEventSubscriptionGeneric<T>).handler.Invoke(eventType, data);
                }
            } else
            {
                earlyTriggers[eventType] = data;
            }
        }
    }
}