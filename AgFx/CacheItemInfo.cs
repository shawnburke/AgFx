// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.



using System;

namespace AgFx
{

    /// <summary>
    /// Represents an entry in the Cache
    /// </summary>
    public class CacheItemInfo
    {
        /// <summary>
        /// A unique name key for this entry
        /// </summary>
        public string UniqueName { get; private set; }

        /// <summary>
        /// The timestamp from the data in this entry
        /// </summary>
        public DateTime UpdatedTime { get; set; }

        /// <summary>
        /// When this entry expires.
        /// </summary>
        public DateTime ExpirationTime { get; set; }

        /// <summary>
        /// Is the Entry optimized data?
        /// </summary>
        public bool IsOptimized { get; set; }


        /// <summary>
        /// Ctor that takes the unique key
        /// </summary>
        /// <param name="uniqueName"></param>
        public CacheItemInfo(string uniqueName)
        {
            UniqueName = uniqueName;
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="uniqueName"></param>
        /// <param name="updated">When the value was last updated</param>
        /// <param name="expires">When the value expires</param>
        public CacheItemInfo(string uniqueName, DateTime updated, DateTime expires)
            : this(uniqueName)
        {
            UpdatedTime = updated;
            ExpirationTime = expires;
        }

        /// <summary>
        /// override
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var other = (CacheItemInfo)obj;

            return other.UniqueName == UniqueName && other.UpdatedTime == UpdatedTime && other.ExpirationTime == ExpirationTime && other.IsOptimized == IsOptimized;
        }

        /// <summary>
        /// override
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return UniqueName.GetHashCode() ^ UpdatedTime.GetHashCode() ^ ExpirationTime.GetHashCode() ^ IsOptimized.GetHashCode();
        }
    }
}
