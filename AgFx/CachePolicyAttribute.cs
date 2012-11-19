﻿// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.



using System;

namespace AgFx
{
    /// <summary>
    /// Attribute for marking the CachePolicy on a cached item.
    /// 
    /// If no attriute is found, objects will default to a policy of 
    /// 
    /// CachePolicy.CacheThenRefresh, 5 minutes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CachePolicyAttribute : Attribute
    {
        private const int DefaultSeconds = 300;

        /// <summary>
        /// The default value of CacheThenRefresh for 300 seconds (5 minutes)
        /// </summary>
        public static CachePolicyAttribute Default = new CachePolicyAttribute(CachePolicy.CacheThenRefresh, DefaultSeconds);

        /// <summary>
        /// The amount of time a cache should be considered valid, in seconds.
        /// 
        /// Ignored for NoCache (assumed 0) and Forever (assumed Int32.MaxValue).
        /// </summary>
        public int CacheTimeInSeconds
        {
            get { return CacheTimeOut.TimeOutValue; }
            set { CacheTimeOut = new DefaultCacheTimeOut(value); }
        }

        /// <summary>
        /// The CachePolicy for this item.
        /// </summary>
        public CachePolicy CachePolicy { get; set; }

        /// <summary>
        /// Creates a cache policy with CacheTimeInSeconds set to 300
        /// </summary>
        /// <param name="policy"></param>
        public CachePolicyAttribute(CachePolicy policy)
            : this(policy, DefaultSeconds)
        {
            
        }

        private readonly CacheTimeOut _defaultCacheTimeOut = new DefaultCacheTimeOut(300);
        private CacheTimeOut _cacheTimeOut;

        /// <summary>
        /// 
        /// </summary>
        public CacheTimeOut CacheTimeOut
        {
            private get { return _cacheTimeOut ?? _defaultCacheTimeOut; }
            set { _cacheTimeOut = value; }
        }

        /// <summary>
        /// Creates a CachePolicyAttribute
        /// </summary>
        /// <param name="policy">The CachePolicy value</param>
        /// <param name="cacheTimeInSeconds">The time to cache, in seconds.  Ignored for NoCache (assumed 0) and Forever (assumed Int32.MaxValue).</param>
        public CachePolicyAttribute(CachePolicy policy, int cacheTimeInSeconds)
        {
            switch (policy) {
                case AgFx.CachePolicy.NoCache:
                    CacheTimeInSeconds = 0;
                    break;
                case AgFx.CachePolicy.Forever:
                    CacheTimeInSeconds = Int32.MaxValue;
                    break;
                default:
                    CacheTimeInSeconds = cacheTimeInSeconds;
                    break;
            }
            CachePolicy = policy;
        }

        /// <summary>
        /// Using CacheTimeout object allows us to have a variable cache time
        /// </summary>
        /// <param name="policy">The CachePolicy value</param>
        /// <param name="cacheTimeOut">The calculated time to cache.  Ignored for NoCache (assumed 0) and Forever (assumed Int32.MaxValue).</param>
        public CachePolicyAttribute(CachePolicy policy, Type cacheTimeOut)
        {
            CacheTimeOut timeOut = (CacheTimeOut)cacheTimeOut.GetConstructor(new Type[0]).Invoke(null);
            switch (policy)
            {
                case AgFx.CachePolicy.NoCache:
                    CacheTimeInSeconds = 0;
                    break;
                case AgFx.CachePolicy.Forever:
                    CacheTimeInSeconds = Int32.MaxValue;
                    break;
                default:
                    CacheTimeOut = timeOut;
                    break;
            }
            CachePolicy = policy;
        }
    }
}
