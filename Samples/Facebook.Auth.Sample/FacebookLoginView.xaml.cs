using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using AgFx.Controls.Authorization;
using AgFx;
using System.Diagnostics;
using System.IO;

namespace Facebook.Auth.Sample {

    /// <summary>
    /// Our login view "dialog" that will pop up any time the LoginModel reports as not being 
    /// logged in.  This manages the process of walking through the Facebook OAuth URLs.
    /// </summary>
    public partial class FacebookLoginView : UserControl {

        /// <summary>
        /// We need to tell this control when we are trying to log out.  This is because Facebook
        /// login via the browser is cookie based.  So when we are logging out, directing to the browser
        /// will just log us back in again.
        /// </summary>
        public static bool LogoutMode { get; set; }


        public class FacebookLoginEventArgs : EventArgs {
            public bool Success { get; set; }
            public string Error { get; set; }
            public string AccessToken { get; set; }
            public DateTime Expiration { get; set; }
        }

        public string Status {
            get { return (string)GetValue(StatusProperty); }
            set { SetValue(StatusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Status.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register("Status", typeof(string), typeof(FacebookLoginView), new PropertyMetadata(new PropertyChangedCallback(Status_Changed)));


        private static void Status_Changed(DependencyObject d, DependencyPropertyChangedEventArgs de) {
            var owner = (FacebookLoginView)d;
        }


        /// <summary>
        /// Fired when a login has succesfully completed.
        /// </summary>
        public event EventHandler<FacebookLoginEventArgs> Complete;

        public FacebookLoginView() {
            InitializeComponent();
            statusText.DataContext = this;

            // when we get a logout message, redirect the browser to the logout page.
            //
            AgFx.NotificationManager.Current.RegisterForMessage(LoginModel.LogoutMessage,
                (message, value) =>
                {

                    PriorityQueue.AddUiWorkItem(() =>
                    {
                        Status = "Logging out...";
                        browser.Navigate(new Uri(FacebookUrls.LogoutUrl));
                        
                    });
                }
            );

            Loaded += new RoutedEventHandler(FacebookLoginView_Loaded);
            browser.Navigated += new EventHandler<System.Windows.Navigation.NavigationEventArgs>(browser_Navigated);

        }

        void browser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e) {
            
            // any time we are on an actual page, hide the overlay.
            //
            overlay.Visibility = System.Windows.Visibility.Collapsed;
            browser.Visibility = System.Windows.Visibility.Visible;
            Status = "Please wait...";
        }


        void FacebookLoginView_Loaded(object sender, RoutedEventArgs e) {
            if (!LogoutMode) {
                GotoLoginPage();
            }
        }

        private void GotoLoginPage() {
            Status = "Loading Facebook Login...";
            browser.Navigate(new Uri(FacebookUrls.AuthUrl));           
        }



        private void browser_Navigating(object sender, Microsoft.Phone.Controls.NavigatingEventArgs e) {

            // show the overlay
            //
            overlay.Visibility = System.Windows.Visibility.Visible;
            browser.Visibility = System.Windows.Visibility.Collapsed;

            // if we somehow got to the home page, redirect to the login page.
            // this will kick off our login process and result in us getting an access token.
            //
            if (FacebookUrls.IsFacebookHome(e.Uri)) {
                GotoLoginPage();
                e.Cancel = true;
                return;
            }

            // if we see the redirect URL that we passed as part of the login process,
            // we know that we need to start looking for parameters.
            //
            if (FacebookUrls.IsRedirectUrl(e.Uri)) {
                Status = "Processing Login...";
                string query = e.Uri.Query;
                if (!ProcessParams(query)) {
                    e.Cancel = true;
                }
            }
        }

        /// <summary>
        /// This function walks parameters looking for tokens, etc.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private bool ProcessParams(string query) {

            // pick out all of the params.
            Match queryParams = Regex.Match(query, "(?<name>[^?=&]+)(=(?<value>[^&]*)?)");

            string access_token = null;
            string code = null;
            int expires_in_seconds = -1;
            bool? fail = null;
            string error = null;

            // walk through the matches looking for code, access_token, and expiration.
            //
            while (queryParams.Success) {

                string value = queryParams.Groups["value"].Value;

                switch (queryParams.Groups["name"].Value) {

                    // Due to the URL # problem in the WebBrowser control, we need to do this process in two steps,
                    // which is to first get a code that we can then exchange for a token.  This code parameter is 
                    // what we need.
                        //
                    case "code":
                        code = value;
                        string tokenAccessUrl = FacebookUrls.GetTokenUrl(code);

                        // now just use a web request rather than the browser to load up the
                        // actual page that will have the access token..
                        HttpWebRequest hwr = HttpWebRequest.CreateHttp(tokenAccessUrl);

                        hwr.BeginGetResponse(
                            (asyncObject) =>
                            {
                                try {
                                    HttpWebResponse resp = (HttpWebResponse)hwr.EndGetResponse(asyncObject);

                                    var c = resp.StatusCode;
                                    if (c == HttpStatusCode.OK) {
                                        string html = new StreamReader(resp.GetResponseStream()).ReadLine();
                                        Dispatcher.BeginInvoke(
                                            () =>
                                            {
                                                // recurse with the content of the page.
                                                ProcessParams(html);
                                            });

                                    }
                                }
                                catch (WebException ex) {
                                    Debug.WriteLine(ex.ToString());
                                }
                            }
                            , null);

                        return false;
                    case "access_token":
                        access_token = value;
                        fail = false;
                        break;
                    case "state":
                        fail = (value != FacebookUrls.VerificationState);
                        break;
                    case "error":
                        fail = true;
                        break;
                    case "error_description":
                        fail = true;
                        error = value;
                        break;
                    case "expires":
                        expires_in_seconds = int.Parse(value);
                        break;
                }
                queryParams = queryParams.NextMatch();
            }

            // if we don't hae a failure and we do have an access token,
            // fire the completion event.
            //
            if (!fail.GetValueOrDefault() && access_token != null) {

                FacebookLoginEventArgs args = new FacebookLoginEventArgs {
                    AccessToken = access_token,
                    Error = error,
                    Expiration = DateTime.Now.AddSeconds(expires_in_seconds)
                };
                OnComplete(args);
            }
            return true;
        }


        private void OnComplete(FacebookLoginEventArgs args) {

            overlay.Visibility = System.Windows.Visibility.Visible;
            browser.Visibility = System.Windows.Visibility.Collapsed;


            if (Complete != null) {
                Complete(this, args);
            }
        }
    }
}
