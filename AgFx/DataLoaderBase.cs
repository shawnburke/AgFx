// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.



using System;
using System.IO;

namespace AgFx {
    
    /// <summary>
    /// Base generic class for DataLoaders
    /// </summary>
    /// <typeparam name="T">The LoadContext type, which derives from LoadContext</typeparam>
    public abstract class DataLoaderBase<T> : IDataLoader<T> where T : LoadContext {

        /// <summary>
        /// Retrieve the LoadRequest for this object type.
        /// </summary>
        /// <param name="loadContext">The current LoadContext for the object being loaded.</param>
        /// <param name="objectType">The type of the object to load.</param>
        /// <returns>An instance of a LoadRequest, or null to cancel the load.</returns>
        public abstract LoadRequest GetLoadRequest(T loadContext, Type objectType);

        /// <summary>
        /// Deserialize the given stream into an object of type objectType.
        /// </summary>
        /// <param name="loadContext">The LoadContext that was used to generate the request</param>
        /// <param name="objectType">The type of object to return. </param>
        /// <param name="stream">The raw data returned from the fetch</param>
        /// <returns>An object which is of the type, or a derived type) of objectType.</returns>
        public abstract object Deserialize(T loadContext, Type objectType, Stream stream);

    }
}
