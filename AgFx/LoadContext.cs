// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.


using System;
using System.Runtime.Serialization;

namespace AgFx {

    /// <summary>
    /// Base class for describing the load parameters for a model object.
    /// </summary>

    [DataContract]
    public class LoadContext {

        private object _id;
        

        /// <summary>
        /// The identity for the object, that is a unique identifer for the object.
        /// </summary>
        [IgnoreDataMember]
        public object Identity {
            get {
                return _id;
            }
        }

        /// <summary>
        /// A unique key describing the LoadRequest's identity plus it's state.  Varying
        /// this value with other parameters will result in results being cached seperately.
        /// </summary>
        public string UniqueKey {
            get {
                return GenerateKey();
            }
        }

        /// <summary>
        /// Default ctor that takes an identifier.
        /// </summary>
        /// <param name="identifier"></param>
        public LoadContext(object identifier) {
            _id = identifier;
        }

        /// <summary>
        /// Creates a unique key for this load request.
        /// Default implementation returns ToString for primative Identifier types,
        /// otherwise uses GetHashCode.  
        /// 
        /// Override this to provide a UniqueKey that varies with LoadContext parameters
        /// such as paging or data set size information.
        /// </summary>
        /// <returns></returns>
        protected virtual string GenerateKey() {

            string uniqueKey;

            if (_id is string || _id.GetType().IsPrimitive) {
                uniqueKey = _id.ToString();
            }
            else {
                uniqueKey = _id.GetHashCode().ToString();
            }
            return uniqueKey;
        }

        /// <summary>
        /// Equals.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {

            LoadContext other = obj as LoadContext;

            if (other == null) return false;

            return Object.Equals(UniqueKey, other.UniqueKey);
        }


        /// <summary>
        /// GetHashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            return UniqueKey.GetHashCode();
        }
        
        internal string ETag { get; set; }
    }
}
