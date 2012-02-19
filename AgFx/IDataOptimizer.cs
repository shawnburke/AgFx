// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.


using System;
using System.IO;

namespace AgFx
{
    /// <summary>
    /// Interface for optimizing the data that is cached for an object.
    /// 
    /// This interface should be implemented on an object's IDataLoader implementation.
    /// 
    /// After a succesful fetch, if a data loader implmements IDataOptimizer, SerializeOptimizedData will be called
    /// with the object deserialized after the fetch.  The returned stream will be written to the cache store and will be handed to
    /// DeoptimizeSerialize data in subsequent cache loads.  If this load fails, the cache item is discarded and a fresh fetch is initiated.
    /// </summary>
    public interface IDataOptimizer
    {
        /// <summary>
        /// Called in the cache loading process for data that was serialized via SerializeOptimizedData
        /// </summary>
        object DeserializeOptimizedData(LoadContext context, Type objectType, Stream stream);

        /// <summary>
        /// When implmemented on an IDataLoader object, this will be called after a successful fetch and Deserialize of
        /// an objects value.  If SerializeOptimizedData returns true, the data in the outputStream parameter will be written 
        /// to the store and will be passed to DeserializeOptimizeData on subsequent cache loads.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="outputStream"></param>
        /// <returns>True to commit the value of outputStream to the store, false to allow raw data to be written to the store</returns>
        bool SerializeOptimizedData(object value, Stream outputStream);
    }
}


