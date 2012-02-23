
// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;


#if WIN8
using Windows.UI.Xaml;
#else
using System.Windows;
using System.Windows.Threading;
#endif

namespace AgFx
{
    /// <summary>
    /// Special type of ObservableCollection that does two things:
    /// 1. Adds items to the underlying OC in batches to give the UI thread some time to process
    /// 2. Adds a merging concept for updating data that is likely to be much the same.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BatchObservableCollection<T> : ObservableCollection<T>
    {
        private const int DefaultMillisecondsTimeout = 500;

        int _batchSize; // how many items to add at a time
        bool _dequeuing; // if a dequeue operation is in progress.
        Queue<T> _batchItems = new Queue<T>(); // the queue for new items
        DispatcherTimer _timer;

        private bool DelayMode
        {
            get
            {
                return _batchItems.Count > 0;
            }
        }
        
        // The amount of time to wait between batched adds.
        //
        private TimeSpan TimeBetweenBatches
        {
            get;
            set;
        }

        // helper to determine if the current thread is the UI thread.
        private bool IsUiThread
        {
            get
            {
                return Current.Dispatcher.CheckAccess();
            }
        }

        /// <summary>
        /// Create a default collection, with batch size 3
        /// </summary>
        public BatchObservableCollection() : this(3, TimeSpan.FromMilliseconds(DefaultMillisecondsTimeout))
        {

        }

        /// <summary>
        /// Create a collection with the specified batch size.
        /// </summary>
        /// <param name="batchSize"></param>
        public BatchObservableCollection(int batchSize) : this(batchSize, TimeSpan.FromMilliseconds(DefaultMillisecondsTimeout))
        {
        }

        /// <summary>
        /// Create a collection with the specified batch size and time between updates.
        /// </summary>
        /// <param name="batchSize"></param>
        /// <param name="timeBetweenBatches"></param>
        public BatchObservableCollection(int batchSize, TimeSpan timeBetweenBatches)
        {
            _batchSize = batchSize;
            TimeBetweenBatches = timeBetweenBatches;

            if (_batchSize > 0 && IsUiThread)
            {
                PriorityQueue.AddUiWorkItem(() =>
                {
                    _timer = new DispatcherTimer();
                    _timer.Tick += new EventHandler(Timer_Tick);
                    _timer.Interval = TimeBetweenBatches;

                    if (_batchItems.Count > 0)
                    {
                        _timer.Start();
                    }
                });
            }
        }

        /// <summary>
        /// This timer serves as the mechanism for batching.  We'll a batch
        /// of items at each tick.
        /// </summary>
        void Timer_Tick(object sender, EventArgs e)
        
        {            
            _dequeuing = true;
        
            // grab a batch and add it to the underlying ObservableCollection
            //
            int count = _batchSize;
            while (_batchItems.Count > 0 && count > 0)
            {
                var item = _batchItems.Dequeue();

                base.Add(item);
                count--;
            }
            _dequeuing = false;

            // cancel the timer when no items remain.
            //
            if (_batchItems.Count == 0)
            {
                CancelBatch();
            }
        }

        /// <summary>
        /// Add a set of items.  If this is done on the UI thread,
        /// batching will occur.  Otherwise, items are added directly.
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<T> items)
        {
            bool delay = IsUiThread && _batchSize > 0;
            if (delay && items.Count() > _batchSize)
            {
                foreach (var item in items)
                {
                    _batchItems.Enqueue(item);
                }

                if (_timer != null)
                {
                    _timer.Start();
                }
            }
            else
            {
                foreach (var item in items)
                {
                    base.Add(item);
                }
            }
        }

        /// <summary>
        /// Cancel the batch and stop the timer.
        /// </summary>
        private void CancelBatch()
        {
            if (_timer != null)
            {
                _timer.Stop();
            }
            _batchItems.Clear();
            _dequeuing = false;
            base.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        }

       /// <summary>
       /// Clear the items out of the collection.
       /// </summary>
        protected override void ClearItems()
        {
            CancelBatch();
            base.ClearItems();
        }
        
       /// <summary>
       /// insert an item into the collection
       /// </summary>
       /// <param name="index"></param>
       /// <param name="item"></param>
        protected override void InsertItem(int index, T item)
        {
            if (DelayMode && !_dequeuing)
            {
                throw new InvalidOperationException("Can't do direct inserts when a pending delayed add exists.");
            }            
            base.InsertItem(index, item);
        }

        /// <summary>
        /// Merge a list of items with the contents of this ObsColl.
        /// 
        /// IMPORTANT: This method assumes that the contents of the this list are sorted
        /// in the same sort order as specified by the compare action.  
        /// 
        /// If they are not, then the resulting list is likely to be wrong.
        /// </summary>
        /// <param name="newItems">The new list of ites</param>
        /// <param name="compare">The comparison function between two items</param>
        /// <param name="itemMergeBehavior">The way toi handle equivlent items.</param>
        public void Merge(IList<T> newItems, Comparison<T> compare, EquivelentItemMergeBehavior itemMergeBehavior)
        {
            // Shortcuts for 0 items
            //
            if (newItems == null || newItems.Count == 0)
            {
                ClearItems();
                return;
            }

            if (Count == 0)
            {
                AddRange(newItems);
                return;
            }

            CancelBatch();

            
            var sortedExisting = this; // we have to assume the list is currently sorted by the specified comparer.

            // sort the newArray
            var sortedNew = newItems.ToArray();
            Array.Sort<T>(sortedNew, compare);

            int currentPos = 0;

            // Now walk each item in the new array and compare it against the current
            // items in the collection.
            //
            foreach(var newItem in sortedNew) {
            

                T existingItem = default(T);
                
                // if we're past the end of the old list,
                // just start adding.
                //
                if (currentPos < Count) {
                    existingItem = this[currentPos];
                }
                else{
                    Add(newItem);
                    continue;
                }

                int compareResult = compare(newItem, existingItem);

                if (compareResult == 0) {

                    // we found the match, so just replace the item
                    // or do nothing.
                    //
                    bool isSameObject = Object.Equals(existingItem, newItem);
                    if (isSameObject)
                    {
                        switch (itemMergeBehavior)
                        {
                            case EquivelentItemMergeBehavior.ReplaceEqualItems:
                                this[currentPos] = newItem;
                                break;
                            case EquivelentItemMergeBehavior.UpdateEqualItems:
                                ReflectionSerializer.UpdateObject(newItem, existingItem, true, null);
                                break;
                            default:
                                break;
                        }                                                
                    }
                    else
                    {
                        // TODO: WRITE TEST FOR THIS CASE
                        // something compared as equal, but it's a different object
                        // so insert the new one before the existing one.
                        //
                        this.Insert(currentPos, newItem);                        
                    }
                    currentPos++;
                    
                }
                else if (compareResult < 0) {
                    // the new item comes before the existing item, so add it.
                    //
                    this.Insert(currentPos, newItem);
                    currentPos++;
                }
                else if (compareResult > 0) {
                    // the new item should come after this item, 
                    // so just replace the current item with our new item.
                    //
                    this[currentPos] = newItem;
                    currentPos++;
                }                
            }

            // remove any that are left in the old list.
            //
            for (int i = this.Count - 1; i >= currentPos; i-- ) {
                this.RemoveAt(i);
            }
        }

        /// <summary>
        /// Property changed handler.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Count" && DelayMode)
            {
                // don't fire change notifications in DelayMode.
                return;
            }
            base.OnPropertyChanged(e);
        }
    }

    /// <summary>
    /// Describes the behavior for BatchObservbleCollection.Merge, based on the specifice Compare lambda.
    /// </summary>
    public enum EquivelentItemMergeBehavior
    {
        /// <summary>
        /// If items are equal, do nothing
        /// </summary>
        SkipEqualItems,

        /// <summary>
        /// If items are equal, replace the old item with the new one
        /// </summary>
        ReplaceEqualItems,

        /// <summary>
        /// If items are equal, copy property values from new to old.
        /// </summary>
        UpdateEqualItems
    }
}
