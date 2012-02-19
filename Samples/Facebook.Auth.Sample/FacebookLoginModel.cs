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
using AgFx;
using System.IO.IsolatedStorage;
using System.IO;

namespace Facebook.Auth.Sample {

    /// <summary>
    /// Our LoginModel.  This is an OAuth model so it's loader doesn't talk directly to a service.
    /// 
    /// Instead, it relies on a separate process to populate the LoadContext with a token for us to go ahead and cache.
    /// 
    /// Once that is specified, we can proceed as normal.
    /// </summary>
    [CachePolicy(CachePolicy.CacheThenRefresh)]
    public class FacebookLoginModel : LoginModel, ICachedItem {


        /// <summary>
        /// All access from the app will go through this Current.
        /// </summary>
        public static FacebookLoginModel Current {
            get {
                return LoginModel.GetCurrentLoginModel<FacebookLoginModel, FacebookLoginLoadContext>();
            }
        }

        /// <summary>
        /// The expiration time of this token.
        /// </summary>
        public DateTime? ExpirationTime {
            get {
                if (!base.ExpirationTimeUtc.HasValue) {
                    return null;
                }
                return ExpirationTimeUtc.Value.ToLocalTime();
            }
        }


        /// <summary>
        /// Actually do the login, given a token and expiration time.
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="expirationTimeUtc"></param>
        public static void Login(string accessToken, DateTime expirationTimeUtc) {
            FacebookLoginLoadContext defaultContext = (FacebookLoginLoadContext)FacebookLoginModel.Current.LoadContext;
            defaultContext.AccessToken = accessToken;
            defaultContext.Expiration = expirationTimeUtc;

            DataManager.Current.Refresh<FacebookLoginModel>(defaultContext,
                (lm) =>
                {
                    if (lm.IsLoggedIn) {
                        lm.RaiseLogin();
                    }
                },
                null);
        }

        /// <summary>
        /// Log out.
        /// </summary>
        public static void Logout() {

            if (!Current.IsLoggedIn) { return; }           
            LoginModel.Logout(Current);            
        }


        /// <summary>
        /// The loader impl.
        /// </summary>
        public class FacebookLoginModelDataLoader : IDataLoader<FacebookLoginLoadContext> {
            

            public LoadRequest GetLoadRequest(FacebookLoginLoadContext loadContext, Type objectType) {
                if (loadContext.CanAttemptLogin) { 
                    return new FacebookLoadRequest(loadContext);
                }
                return null;
            }

            public object Deserialize(FacebookLoginLoadContext loadContext, Type objectType, System.IO.Stream stream) {

                // just pick the values back out of the stream.
                //                
                StreamReader sr = new StreamReader(stream);

                string access_token = sr.ReadLine();
                DateTime expiration_time = DateTime.Parse(sr.ReadLine());
              
                // build our loginmodel.
                //
                FacebookLoginModel flm = new FacebookLoginModel();
                flm.LoadContext = loadContext;
                flm.Token = access_token;
                flm.ExpirationTimeUtc = expiration_time.ToUniversalTime();
             
                return flm;
            }

            /// <summary>
            /// A custom LoadRequest that just takes values off of the LoadContext and writes
            /// then to a simple stream reader.  This is a bit of an extra step, but it sets up the stream
            /// that AgFx can then cache and deserialize later.
            /// </summary>
            private class FacebookLoadRequest : LoadRequest {
                public FacebookLoadRequest(LoadContext context)
                    : base(context) {

                }

                /// <summary>
                /// AgFx will call this method when it wants to load the new value -
                /// even though it's just the values we got off of the OAuth process.
                /// </summary>
                /// <param name="result"></param>
                public override void Execute(Action<LoadRequestResult> result) {

                    // Grab the values off of the LoadContext.
                    //
                    FacebookLoginLoadContext context = (FacebookLoginLoadContext)LoadContext;

                    string access_token = context.AccessToken;
                    DateTime expiration_time = context.Expiration;
                
                    // Make sure we have a token.
                    if (access_token != null) {

                        // write the values into a stream and return that.
                        MemoryStream ms = new MemoryStream();
                        StreamWriter sw = new StreamWriter(ms);

                        sw.WriteLine(access_token);
                        sw.WriteLine(expiration_time);
                        sw.Flush();                        

                        ms.Seek(0, SeekOrigin.Begin);

                        LoadRequestResult r = new LoadRequestResult(ms);
                        result(r);
                    }
                    else {
                        result(new LoadRequestResult(new UnauthorizedAccessException()));
                    }
                }
            }

        }        
    }

}
