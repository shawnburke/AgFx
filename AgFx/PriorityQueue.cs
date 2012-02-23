// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.


using System;
using System.Collections.Generic;
using System.Threading;

#if WIN8
#else
using System.Windows;
using System.Windows.Threading;
#endif

using System.Diagnostics;

namespace AgFx
{
    /// <summary>
    /// Queing mechanism for different types of work items. Use this instead of DispatcherBeginInvoke, 
    /// ThreadPool, etc.  
    /// </summary>
    public class PriorityQueue
    {

        private class WorkerThread : IDisposable
        {
            Thread t;
            Queue<Action> q = new Queue<Action>();
            AutoResetEvent e = new AutoResetEvent(false);

            public string _name;


            public WorkerThread(int sleepyTime, string name)
            {
                _name = name;
                SleepyTime = sleepyTime;
                t = new Thread(WorkerThreadProc);
                t.Name = _name;
                t.IsBackground = true;
                t.Start();
            }

            private void WorkerThreadProc(object state)
            {
                while (true)
                {
                    {
                        IEnumerable<Action> workItems = null;

                        lock (q)
                        {
                            workItems = q.ToArray();
                            q.Clear();
                        }
                         
                        foreach (var item in workItems)
                        {
                                                            
                                if (item != null)
                                {
                                    var workItem = item;
                                    ThreadPool.QueueUserWorkItem(
                                        (s) =>
                                        {

                                            workItem();
                                        }
                                    );
                                    Thread.Sleep(SleepyTime);
                                }
                        }
                        
                    }
                    e.WaitOne();
                }
            }

            public int SleepyTime { get; set; }
            public void AddWorkItem(Action a)
            {
                lock (q)
                {
                    q.Enqueue(a);
                }
                e.Set();
            }

            public void Dispose() {
                e.Close();
            }
        }


        static WorkerThread storageWorker = new WorkerThread(10, "Storage Thread");
        //static WorkerThread networkWorker = new WorkerThread(10, "Network thread");
        static WorkerThread workWorker = new WorkerThread(25, "General Worker");

        static PriorityQueue()
        {   
        }

        private static Dispatcher _dispatcher;


#if false
        private static double _uiThreadMilliseconds = 0;
#endif

        /// <summary>
        /// Add a work item to execute on the UI thread.
        /// </summary>
        /// <param name="workitem">The Action to execute</param>
        /// <param name="checkThread">true to first check the thread, and if the thread is already the UI thread, execute the item synchrounously.</param>
        public static void AddUiWorkItem(Action workitem, bool checkThread)
        {

#if false
            Action originalWorkitem = workitem;
            Action wrappedWorkitem = () =>
            {
                var start = Environment.TickCount;
                originalWorkitem();
                var end = Environment.TickCount;
                _uiThreadMilliseconds += (end - start);
                Debug.WriteLine("Cumulative UI time: {0}ms", _uiThreadMilliseconds);
            };
            workitem = wrappedWorkitem;
#endif

            if (_dispatcher == null)
            {
                _dispatcher = Deployment.Current.Dispatcher;
                Debug.Assert(_dispatcher != null);
            }

            if (checkThread && _dispatcher.CheckAccess())
            {
                workitem();
            }
            else
            {
                _dispatcher.BeginInvoke(workitem);
            }
        }

        /// <summary>
        /// Add a work item to be performed on the UI thread asynchrously.
        /// </summary>
        /// <param name="workitem"></param>
        public static void AddUiWorkItem(Action workitem) {
            AddUiWorkItem(workitem, false);
        }

        private static void AddThreadPoolItem(Action item)
        {
            ThreadPool.QueueUserWorkItem(
                (state) => {
                    item();
                },
                null
            );
        }

        /// <summary>
        /// Add a work item that will affect storage
        /// </summary>
        /// <param name="workItem"></param>
        public static void AddStorageWorkItem(Action workItem)
        {
            storageWorker.AddWorkItem(workItem);
        }

        /// <summary>
        /// Add a general work item.
        /// </summary>
        /// <param name="workitem"></param>
        public static void AddWorkItem(Action workitem)
        {
            workWorker.AddWorkItem(workitem);
            //AddThreadPoolItem(workitem);
        }

        /// <summary>
        /// Add a work item that will result in a network requeset
        /// </summary>
        /// <param name="workitem"></param>
        public static void AddNetworkWorkItem(Action workitem)
        {
            //networkWorker.AddWorkItem(workitem);
            AddThreadPoolItem(workitem);
        }
    }
}
