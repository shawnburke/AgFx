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
using Microsoft.Phone.Controls;
using AgFx;
using AgFx.Controls.Authorization;
using System.IO;

namespace Facebook.Auth.Sample {
    public partial class MainPage : PhoneApplicationPage {
        // Constructor
        public MainPage() {
            InitializeComponent();

           
            // register for the login message so we can populate user info.
            //
            AgFx.NotificationManager.Current.RegisterForMessage(LoginModel.LoginMessage,
                (msg, obj) =>
                {
                    GetUserInfo();
                }
            );

            // if already logged in, populate now.
            if (FacebookLoginModel.Current.IsLoggedIn) {
                GetUserInfo();

            }
        }

        private void GetUserInfo() {

            this.DataContext = DataManager.Current.Load<FacebookUserModel>("me", null, 
                (ex) => {
                    // logout if we get an error.
                    Logout_Click(null, null);
                    return;        
                }
            );
        }

        /// <summary>
        /// invoke a logout.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Logout_Click(object sender, RoutedEventArgs e) {

            if (FacebookLoginModel.Current.IsLoggedIn) {
                DataManager.Current.Clear<FacebookUserModel>("me");
                FacebookLoginView.LogoutMode = true;
                FacebookLoginModel.Logout();
            }
        }
    }
}