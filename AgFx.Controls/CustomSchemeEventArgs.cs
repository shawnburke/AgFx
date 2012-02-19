// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.

using System;

namespace AgFx.Controls
{
    /// <summary>
    /// Used for HyperlinkbuttonEx CustomSchemeClick handler.
    /// </summary>
    public class CustomSchemeEventArgs : EventArgs
    {
        public string Scheme
        {
            get
            {
                return Uri.Scheme;
            }
        }

        public Uri Uri
        {
            get;
            private set;
        }

        public bool Handled { get; set; }

        public CustomSchemeEventArgs(Uri uri)
        {
            if (uri == null) throw new ArgumentNullException();
            Uri = uri;
        }
    }
}
