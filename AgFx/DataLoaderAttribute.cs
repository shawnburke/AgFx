
// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.



using System;

namespace AgFx
{
    /// <summary>
    /// Marks the data loader to be used with a particlar object type.
    /// 
    /// Typically, objects use the default pattern of having a public nested class that implements IDataLoader.
    /// 
    /// This allows other scenarios where the loader is specified seperately.
    /// </summary>
    [
        AttributeUsage(
            AttributeTargets.Class, 
            AllowMultiple=false, 
            Inherited = true)
    ]
    public class DataLoaderAttribute : Attribute
    {
        /// <summary>
        /// The type of data loader object, which implements IDataLoader
        /// </summary>
        public Type DataLoaderType { get; set; }

        /// <summary>
        /// Default ctor.
        /// </summary>
        public DataLoaderAttribute()
        {

        }

        /// <summary>
        /// Ctor specifying a type.
        /// </summary>
        /// <param name="dataLoaderType">The data loader type</param>
        public DataLoaderAttribute(Type dataLoaderType)
        {
            DataLoaderType = dataLoaderType;
        }                
    }
}
