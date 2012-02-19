// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.


using System;

namespace AgFx
{
    /// <summary>
    /// Interface for describing an updatable object.
    /// 
    /// This interface allows DataManager to notify clients when an object 
    /// is updating and when it was last updated.
    /// </summary>
    public interface IUpdatable : ILoadContextItem
    {
        /// <summary>
        /// Set to true when DataManager is in the process of updating a value
        /// </summary>
        bool IsUpdating { get; set; }

        /// <summary>
        /// Set to the last time the data for this object was succesfully fetched.
        /// </summary>
        DateTime LastUpdated { get; set; }

        /// <summary>
        /// Updates this object's values from a source object of the same type.
        /// </summary>
        /// <param name="source"></param>
        void UpdateFrom(object source);

        /// <summary>
        /// Initiates a refresh of this object's value.
        /// For an object of type T, this is the equivelent of calling
        /// 
        /// DataManager.Current.Refresh(this.LoadContext)
        /// </summary>
        void Refresh();
    }
}
