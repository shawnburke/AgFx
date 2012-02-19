using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using AgFx.Controls.Authorization;
using System.IO.IsolatedStorage;
using AgFx;

namespace Facebook.Auth.Sample {

    /// <summary>
    /// Our load context.  Since OAuth doesn't allow you to directly request
    /// a token with a server call, we need to first get the token, which happens in The FacebookLoginView control.
    /// 
    /// With that in hand, we populate this LoadContext to continue the process in an AgFx-friendly way.
    /// </summary>
    public class FacebookLoginLoadContext : LoginLoadContext {

        /// <summary>
        /// Our Facebook auth token.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// The Expiration date
        /// </summary>
        public DateTime Expiration { get; set; }

        /// <summary>
        /// If we don't have a token, we won't do any caching or logging in.
        /// </summary>
        public override bool CanAttemptLogin {
            get {
                return AccessToken != null;
            }
        }
    }
}
