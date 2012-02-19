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
using System.Threading;

namespace AgFx.Controls.Authorization {


    /// <summary>
    /// Base clas for Login models.
    /// 
    /// Derive from this and add a static Current property as follows:
    /// 
    /// public static MyLoginModel Current {
    ///     get {
    ///         return GetCurrentLoginModel &lt;MyLoginModel, MyLoginModelLoadContext&gt;()
    ///    }
    /// }
    /// </summary>
    [CachePolicy(CachePolicy.ValidCacheOnly, 3600 * 24 * 7)]
    public class LoginModel : ModelItemBase<LoginLoadContext>  {

        public const string LoginMessage = "login";
        public const string LogoutMessage = "logout";
        private bool _hasLoggedIn;

        private static LoginModel _current;

        /// <summary>
        /// Call this from the derived class's static Current property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="L"></typeparam>
        /// <returns></returns>
        protected static T GetCurrentLoginModel<T, L>() 
                                            where T : LoginModel, new() 
                                            where L : LoginLoadContext, new() {
            if (_current == null) {     

                L defaultContext = new L();

                _current = DataManager.Current.LoadFromCache<T>(defaultContext);

                if (_current.IsLoggedIn) {
                    _current.RaiseLogin();
                }
            }
            return (T)_current;
        }



        #region Property ExpirationTimeUtc
        private DateTime? _ExpirationTimeUtc;
        public DateTime? ExpirationTimeUtc
        {
            get
            {
                return _ExpirationTimeUtc;
            }
            set
            {
                if (_ExpirationTimeUtc != value)
                {
                    _ExpirationTimeUtc = value;
                    RaisePropertyChanged("ExpirationTimeUtc");
                }
            }
        }
        #endregion

        
       

        [DependentOnProperty("Token")]
        public virtual bool IsLoggedIn
        {
            get
            {
                bool hasToken = Token != null;

                if (hasToken)
                {
                    bool valid = (ExpirationTimeUtc == null || ExpirationTimeUtc > DateTime.UtcNow);
                    if (valid)
                    {
                        return true;
                    }
                    else if (_hasLoggedIn)
                    {                        
                        Logout(this);
                    }
                }
                return false;
            }            
        }               

        #region Property Token
        private string _Token;
        public string Token
        {
            get
            {
                return _Token;
            }
            set
            {
                if (_Token != value)
                {
                    _Token = value;
                    RaisePropertyChanged("Token");
                }
            }
        }
        #endregion
  
                
        protected virtual void OnLoggingIn() {
        }

        protected virtual void OnLoginFail(Exception ex) {
        }

        protected virtual void OnLoggedIn() {
        }

        protected virtual bool OnLoggingOut() {
            return true;
        }

        protected virtual bool OnLoggedOut() {
            RaisePropertyChanged("IsLoggedIn");                
            return true;
        }

        protected static void Login<T>(T model) where T: LoginModel, new()
        {
            if (model.IsLoggedIn)
            {                
                if (model != _current && _current != null) {
                    _current.UpdateFrom(model);
                }
                _current.RaiseLogin();
                DataManager.Current.Save(model, model.LoadContext);
            }
        }

        public static void Login<T>(T current, string username, string password, Action<Exception> error) where T: LoginModel, new() {
            
            if (current.IsLoggedIn) {                
                return;
            }

            current.LoadContext.Login = username;
            current.LoadContext.Password = password;

            current.OnLoggingIn();

            DataManager.Current.Refresh<T>(current.LoadContext,
                (lm) =>
                {
                    if (lm.IsLoggedIn) {
                        current.UpdateFrom(lm);                        
                        lm.RaiseLogin();
                    }
                }
                ,
                (ex) =>
                {
                    current.OnLoginFail(ex);
                    if (error != null) {
                        error(ex);
                    }
                }
            );
        }

        public static void Logout<T>(T current) where T: LoginModel, new(){
            if (current != null) {
                if (current.OnLoggingOut()) {
                    current.Token = null;                    
                    NotificationManager.Current.RaiseMessage(LogoutMessage, current);
                    DataManager.Current.Clear<T>(current.LoadContext);
                    DataManager.Current.RegisterProxy<T>(current);
                    current._hasLoggedIn = false;                    
                    current.OnLoggedOut();
                    
                }
            }
        }

        protected void RaiseLogin() {
            _hasLoggedIn = true;
            OnLoggedIn();
            RaisePropertyChanged("IsLoggedIn");                
            NotificationManager.Current.RaiseMessage(LoginMessage, this);                        
        }
       
    }

}
