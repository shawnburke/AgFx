// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.


using System.Collections.Generic;
using System.Linq;
using System;

namespace AgFx {

    /// <summary>
    /// Base class for the AgFx store provider.
    /// 
    /// Ideally you could redirect AgFx to use a database or non-IsoStore implemenation. 
    /// This is not currently a tested scenario.
    /// </summary>
    public abstract class StoreProviderBase {

        /// <summary>
        /// Returns true if this implmenation is doing buffered writes.
        /// </summary>
        public abstract bool IsBuffered {
            get;
        }

        /// <summary>
        /// Clears the store of all items.
        /// </summary>
        public virtual void Clear() {
            var items = GetItems().ToArray();
            foreach (var item in items) {
                Delete(item);
            }
        }

        /// <summary>
        /// Gets the items with the specified unique name.
        /// </summary>
        /// <param name="uniqueName">a unique key.</param>
        /// <returns>A set of CacheItemInfo objects</returns>
        public virtual IEnumerable<CacheItemInfo> GetItems(string uniqueName) {


            var matchingItems = from i in GetItems()
                                where uniqueName == i.UniqueName
                                select i;

            return matchingItems;

        }

        /// <summary>
        /// Return all the items in the store.  Avoid this, it's usually expensive.
        /// </summary>
        /// <returns>An enumerable of all the items in the store.</returns>
        public abstract IEnumerable<CacheItemInfo> GetItems();

        /// <summary>
        /// Gets the most recent object with the given unique name.
        /// </summary>
        /// <param name="uniqueName"></param>
        /// <returns></returns>
        public virtual CacheItemInfo GetLastestExpiringItem(string uniqueName) {
            try {

                var newestItem = GetItems(uniqueName).
                                OrderByDescending(i => i.ExpirationTime).
                                    FirstOrDefault();

                return newestItem;


            }
            catch {
                return null;
            }
        }

        /// <summary>
        /// Flush the store, for buffered stores.
        /// </summary>
        /// <param name="synchronous"></param>
        public abstract void Flush(bool synchronous);

        /// <summary>
        /// Delete's the given item from the store.
        /// </summary>
        /// <param name="item">The item to delete</param>
        public abstract void Delete(CacheItemInfo item);

        /// <summary>
        /// Delete all of the items with the given unique key.
        /// </summary>
        /// <param name="uniqueName">The unique key being deleted.</param>
        public virtual void DeleteAll(string uniqueName) {
            var items = GetItems(uniqueName).ToArray();
            foreach (var item in items) {
                Delete(item);
            }
        }

        /// <summary>
        /// Read the data for the given item.
        /// </summary>
        /// <param name="item">The info describing the item to read</param>
        /// <returns>The data in the store for the specified item.</returns>
        public abstract byte[] Read(CacheItemInfo item);

        /// <summary>
        /// Write the item's data to the store.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="data"></param>
        public abstract void Write(CacheItemInfo info, byte[] data);

        /// <summary>
        /// Update an items details in the store
        /// </summary>
        /// <param name="oldInfo">The old cache entry</param>
        /// <param name="newInfo">The new cache entry</param>
        public virtual void Update(CacheItemInfo oldInfo, CacheItemInfo newInfo)
        {
            throw new NotImplementedException("");
        }
    }
}
