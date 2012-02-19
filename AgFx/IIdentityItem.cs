// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.


namespace AgFx
{
    /// <summary>
    /// Interface for objects that have a load context.
    /// </summary>
    public interface ILoadContextItem
    {
        /// <summary>
        /// The LoadContext for this item.
        /// </summary>
        LoadContext LoadContext { get; set; }
    }
}
