// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.

using System;
using System.IO;

namespace AgFx
{
    /// <summary>
    /// Base class for AgFx to perform loads of fresh data.  See the Execute method for
    /// where AgFx will call in.
    /// </summary>
    public abstract class LoadRequest
    {
        /// <summary>
        /// The LoadContext of the object that is being requested.
        /// </summary>
        public LoadContext LoadContext{
            get;
            private set;
        }

        /// <summary>
        /// protected ctor.
        /// </summary>
        /// <param name="loadContext"></param>
        protected LoadRequest(LoadContext loadContext)
        {
            this.LoadContext = loadContext;
        }


        /// <summary>
        /// Execute will be called by AgFx to perform the actual load.  This call must result in an
        /// invocation of the result action in order for a request to complete, synchrounously or asynchronously, and this result must pass 
        /// a value for LoadRequestResult.
        /// </summary>
        /// <param name="result"></param>
        public abstract void Execute(Action<LoadRequestResult> result);

       
    }
    
}
