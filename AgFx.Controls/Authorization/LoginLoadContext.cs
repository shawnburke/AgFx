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

namespace AgFx.Controls.Authorization {
    public class LoginLoadContext : LoadContext {

        
        private const string DefaultIdentity = "_Current_User_";

        public LoginLoadContext()
            : base(DefaultIdentity) {
            
        }

        public string Login { get; set; }
        public string Password { get; set; }

        /// <summary>
        /// Return true if a login attempt shoudl be made.
        /// </summary>
        public virtual bool CanAttemptLogin {
            get {
                return !String.IsNullOrEmpty(Login) && !String.IsNullOrEmpty(Password);
            }
        }

    }
}
