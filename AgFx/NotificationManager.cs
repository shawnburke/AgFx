// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace AgFx
{
    /// <summary>
    /// Notification allows a simple way for objects to register for, initiate, and receive notifications
    /// </summary>
    public class NotificationManager
    {
        private static NotificationManager _current = new NotificationManager();

        /// <summary>
        /// Singleton instance to be used.
        /// </summary>
        public static NotificationManager Current
        {
            get
            {
                return _current;
            }
        }

       
        private Dictionary<object, List<WeakReference>> _messages = new Dictionary<object, List<WeakReference>>();

        /// <summary>
        /// Register the given handler for the message described in the key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="handler"></param>
        public void RegisterForMessage(object key, Action<object, object> handler)
        {
            List<WeakReference> messagelist;

            if (!_messages.TryGetValue(key, out messagelist))
            {
                messagelist = new List<WeakReference>();
                _messages[key] = messagelist;
            }

            lock (messagelist)
            {
                messagelist.Add(new WeakReference(handler));
            }
        }

        /// <summary>
        /// Raise the message for the given key with the given data.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        public void RaiseMessage(object key, object data)
        {

            List<WeakReference> messagelist;

            if (!_messages.TryGetValue(key, out messagelist))
            {
                return;
            }

            var messages =  messagelist.Where(r => r.IsAlive).ToArray();
            
            // do some cleanup.
            //
            var deadHandlers = messagelist.Where(r => !r.IsAlive);

            lock (messagelist)
            {
                foreach (var h in deadHandlers)
                {
                    messagelist.Remove(h);
                }
            }

            PriorityQueue.AddUiWorkItem(() =>
            {
                // push the list out ot an array in case one of the handlers removes itself
                //

                foreach (var wr in messages)
                {
                    try
                    {
                        Action<object, object> action = wr.Target as Action<object, object>;
                        if (action != null)
                        {
                            action(key, data);
                        }
                    }
                    catch
                    {

                    }
                }
            }
            );
        }

        /// <summary>
        /// Unregister the handler from the given message
        /// </summary>
        /// <param name="key"></param>
        /// <param name="handler"></param>
        public void UnregisterForMessage(object key, Action<object, object> handler)
        {
            List<WeakReference> messagelist;

            if (!_messages.TryGetValue(key, out messagelist))
            {
                return;
            }

            // find the weak ref
            //
            var refs = from wr in messagelist
                       where (Action<object,object>)wr.Target == handler
                       select wr;

            lock (messagelist)
            {
                foreach (var wr in refs)
                {
                    messagelist.Remove(wr);
                }
            }
        }
    }
}
