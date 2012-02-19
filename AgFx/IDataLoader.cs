// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.


using System;
using System.IO;

namespace AgFx
{
    /// <summary>
    /// Implmements the DataLoader contract on an object.
    /// </summary>
    public interface IDataLoader<T> where T : LoadContext
    {
        /// <summary>
        /// Retrieves a LoadRequest object that allows loading of a new object value from some resource.
        /// </summary>
        /// <param name="loadContext">The LoadContext for the request.</param>
        /// <param name="objectType">The object type that is to be loaded.</param>
        /// <returns>An implementation of LoadRequest, or null to cancel the load.</returns>
        LoadRequest GetLoadRequest(T loadContext, Type objectType);

        /// <summary>
        /// Deserialize the data in stream to an object of type objectType.
        /// </summary>
        /// <param name="loadContext">The LoadContext for this request</param>
        /// <param name="objectType">The type of object (exact or derived) to return.</param>
        /// <param name="stream">The stream of data ot deserialize from.</param>
        /// <returns></returns>
        object Deserialize(T loadContext, Type objectType, Stream stream);        
    }
   
}


