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

namespace Facebook.Auth.Sample {

    /// <summary>
    /// Helper class for holding and formating FacebookUrls.
    /// </summary>
    public static class FacebookUrls {


        private const string AppId = "[FACEBOOK APP ID]";

        /// <summary>
        /// Normall we wouldn't want this in the client but it's required to work around an issue with the
        /// WebBrowser control and OAuth.
        /// </summary>
        private const string AppSecret = "[FACEBOOK APP SECRET]";
        
        private const string RedirectUrl = "http://www.facebook.com/connect/login_success.html";
        private const string FacebookOAuthUrlFormat = "http://www.facebook.com/dialog/oauth?client_id={0}&redirect_uri={1}&display=wap&state={2}&response_type=code";
        private const string FacebookTokenAccessUrlFormat = "https://graph.facebook.com/oauth/access_token?client_id={0}&client_secret={1}&code={2}&redirect_uri=" + RedirectUrl;
        private const string LogoutUrlFormat = "http://www.facebook.com/logout.php";
        private const string HomeUrlFormat = "http://m.facebook.com/index.php";
       

        private static string _verify;

        /// <summary>
        /// We create basically a random number here to make sure
        /// we get the right response back from our OAuth process. We compare the result we 
        /// get back in one of the browser redirects to this value to ensure it's ours.
        /// </summary>
        public static string VerificationState {
            get {
                if (_verify == null) {
                    _verify = Environment.TickCount.ToString();
                }
                return _verify;
            }
        }       

        /// <summary>
        /// The URL we call to initialize the auth process.
        /// </summary>
        public static string AuthUrl {
            get {
                if (AppId.StartsWith("[")) {
                    MessageBox.Show("A Facebook AppId is required to run this sample. See FacebookUrls.AppId.");
                    throw new ArgumentException();
                }
                return String.Format(FacebookOAuthUrlFormat, AppId, RedirectUrl, VerificationState);
            }
        }

        public static string HomeUrl {
            get {
                return HomeUrlFormat;
            }
        }


        public static string LogoutUrl {
            get {
                return LogoutUrlFormat;
            }
        }
        
        /// <summary>
        /// Gets the URL to convert the code to a real access_token.
        /// </summary>
        /// <param name="code">The code passed back from the AuthUrl response.</param>
        /// <returns></returns>
        public static string GetTokenUrl(string code) {
            string tokenAccessUrl = String.Format(FacebookTokenAccessUrlFormat, AppId, AppSecret, code);
            return tokenAccessUrl;                        
        }

        public static bool IsRedirectUrl(Uri uri) {
            return uri.AbsoluteUri.StartsWith(RedirectUrl);
        }

        public static bool IsFacebookHome(Uri uri) {
            return uri.AbsolutePath.Contains("index.php");
        }     
    }
}
