using System;

namespace AgFx {

    /// <summary>
    /// Exception type reported by the DataManager upon a failed LoadRequest.Execute call.  This will be passed to error handlers on 
    /// DataManager.Load or Refresh, as well as to the DataManager.UnhandledError event, of not handled earlier.
    /// </summary>
    public class LoadRequestFailedException : Exception {

        /// <summary>
        /// The type of the object that was being loaded.
        /// </summary>
        public Type ObjectType {
            get;
            private set;
        }

        /// <summary>
        /// The load context from the load call.
        /// </summary>
        public LoadContext LoadContext {
            get;
            private set;
        }

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="objectType">The type of the object being loaded</param>
        /// <param name="loadContext">The LoadContext for the load.</param>
        /// <param name="innerException">The original exception which caused the failure.</param>
        public LoadRequestFailedException(Type objectType, LoadContext loadContext, Exception innerException) : base("An error occurred loading an object of type " + objectType.Name + ", see InnerException for details.", innerException) {
            if (objectType == null) throw new ArgumentNullException();
            if (loadContext == null) throw new ArgumentNullException();

            ObjectType = objectType;
            LoadContext = loadContext;
        }
        
    }
}
