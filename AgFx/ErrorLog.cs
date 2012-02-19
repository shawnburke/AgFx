// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.


using System;
using System.IO.IsolatedStorage;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace AgFx {
    /// <summary>
    /// Helper for persisting unhandled exceptions to disk and being able to retrive them later.   
    /// </summary>
    public static class ErrorLog {
        private const string LogFile = "error.log";
        private const string Delimiter = "_\t_";

        /// <summary>
        /// Deletes the error log.
        /// </summary>
        public static void Clear() {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication()) {
                if (store.FileExists(LogFile)) {
                    store.DeleteFile(LogFile);
                }
            }
        }

        /// <summary>
        /// Write an exception to disk.
        /// </summary>
        /// <param name="description"></param>
        /// <param name="ex"></param>
        public static void WriteError(string description, Exception ex) {
            try
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    var lf = store.OpenFile(LogFile, System.IO.FileMode.Append);

                    StreamWriter sw = new StreamWriter(lf);
                    sw.WriteLine(DateTime.UtcNow);
                    sw.WriteLine(description);
                    sw.WriteLine(Delimiter);
                    sw.WriteLine(ex);
                    sw.WriteLine(Delimiter);
                    sw.WriteLine(Delimiter);
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (NotSupportedException)
            {
                // sometimes this gets thrown from new StreamWriter...
                // the innards of StreamWriter.ctor checkPosition, then SeekNotSuported gets raised.
                // Wierd.

            }
            catch (IsolatedStorageException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (ArgumentException)
            {
                // "Value does not fall within the expected range."
            }
            catch (IOException)
            {
            }

        }

        /// <summary>
        /// Get the list of exceptions
        /// </summary>
        /// <param name="clear">clear the list after retreival</param>
        /// <returns></returns>
        public static IEnumerable<ErrorEntry> GetErrors(bool clear) {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication()) {
                if (store.FileExists(LogFile)) {
                    var lf = store.OpenFile(LogFile, System.IO.FileMode.Open);

                    StreamReader sr = new StreamReader(lf);


                    List<ErrorEntry> entries = new List<ErrorEntry>();

                    for (
                        string ln = sr.ReadLine();
                        ln != null;
                        ln = sr.ReadLine()) {
                        DateTime ts;
                        if (DateTime.TryParse(ln, out ts)) {
                            try {
                                var currentErrorEntry = new ErrorEntry();
                                currentErrorEntry.Timestamp = ts;

                                currentErrorEntry.Description = ReadItem(sr, Delimiter);
                                currentErrorEntry.Exception = ReadItem(sr, Delimiter);

                                ln = sr.ReadLine();
                                Debug.Assert(ln == Delimiter, "Expected delimiter");
                                entries.Add(currentErrorEntry);
                            }
                            catch {

                            }
                        }
                    }
                    sr.Close();

                    if (clear) {
                        store.DeleteFile(LogFile);
                    }
                    return entries;
                }
            }
            return new ErrorEntry[0];

        }

        private static string ReadItem(StreamReader sr, string delimiter) {
            StringBuilder sb = new StringBuilder();
            for (
                string ln = sr.ReadLine();
                ln != null;
                ln = sr.ReadLine()) {
                sb.Append(ln);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>
        /// Class describing an entry in the error log.
        /// </summary>
        public class ErrorEntry {
            /// <summary>
            /// The time of the error.
            /// </summary>
            public DateTime Timestamp {
                get;
                internal set;
            }

            /// <summary>
            /// A description
            /// </summary>
            public string Description {
                get;
                internal set;
            }

            /// <summary>
            /// The Exception that initiated the error
            /// </summary>
            public string Exception {
                get;
                internal set;
            }

            /// <summary>
            /// override
            /// </summary>
            /// <returns></returns>
            public override string ToString() {
                return String.Format("{0}: {1}\r\nStack trace:\r\n{2}\r\n\r\n", Timestamp, Description, Exception);
            }
        }
    }
}
