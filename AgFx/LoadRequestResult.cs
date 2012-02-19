// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.

using System;
using System.IO;

namespace AgFx
{
    /// <summary>
    /// Result object to be returned from a call to LoadRequest.Execute
    /// </summary>
    public class LoadRequestResult
    {
        /// <summary>
        /// The stream result from a LoadRequest.Execute call.  A value here implies a successful fetch.
        /// </summary>
        public Stream Stream
        {
            get;
            private set;
        }

        /// <summary>
        /// An exception encountered by a LoadRequest.Execute call. A value here implies a failed fetch.
        /// </summary>
        public Exception Error
        {
            get;
            private set;
        }

        /// <summary>
        /// Construct a LoadRequestResult with a data stream as a result of a
        /// LoadRequest.Execute invocation.
        /// </summary>
        /// <param name="stream"></param>
        public LoadRequestResult(Stream stream)
        {
            Stream = stream;
        }

        /// <summary>
        /// Construct a result with a failure exception from a LoadRequest.Execute invocation.
        /// </summary>
        /// <param name="error"></param>
        public LoadRequestResult(Exception error)
        {
            Error = error;
        }
    }
}
