// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.



namespace AgFx
{
    // TODO: Investigate the right scenarios for this and tombstoning.

    /// <summary>
    /// Marks the model item's cache policy.
    /// </summary>
    public enum CachePolicy
    {
        /// <summary>
        /// This item should not take advantage of the cache.
        /// </summary>
        NoCache,

        /// <summary>
        /// The items' cached value should be displayed, followed by a refresh
        /// if expired.
        /// </summary>
        CacheThenRefresh,

        /// <summary>
        /// Only a valid cache item should be displayed. If the cache has
        /// expired, the loader will get new data before returning the
        /// valid item and its data.
        /// </summary>
        ValidCacheOnly,

        /// <summary>
        /// Automatically refresh the item as indicated by the cache timeline.
        /// </summary>
        AutoRefresh,

        /// <summary>
        /// Cache forever.
        /// </summary>
        Forever
    }
}
