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
using AgFx;
using System.IO;
using System.Text.RegularExpressions;

namespace Facebook.Auth.Sample {
    
    /// <summary>
    /// A simple user model class, all it knows how to do is get the users name.
    /// </summary>
    public class FacebookUserModel : ModelItemBase<LoadContext> {

        /// <summary>
        /// The username itself is the identity for this object.
        /// </summary>
        public string UserName {
            get {
                return (string)LoadContext.Identity;
            }            
        }


        private string _name;

        /// <summary>
        /// The users full name.
        /// </summary>
        public string Name {
            get {
                return _name;
            }
            set {
                if (_name != value) {
                    _name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }

        public FacebookUserModel() {

        }

        public FacebookUserModel(string username)
            : base(new LoadContext(username)) {

        }

        /// <summary>
        /// The loader for the user object.  This simple version only picks out the display name.
        /// </summary>
        public class FacebookUserModelLoader : IDataLoader<LoadContext> {

            public LoadRequest GetLoadRequest(LoadContext loadContext, Type objectType) {

                // abort if we don't have a token.
                //
                if (!FacebookLoginModel.Current.IsLoggedIn) {
                    return null;
                }

                // build the "me" url including the auth token.
                //
                string meUri = String.Format("https://graph.facebook.com/{0}?access_token={1}", loadContext.Identity, FacebookLoginModel.Current.Token);

                return new WebLoadRequest(loadContext, new Uri(meUri));
            }

            /// <summary>
            /// Very simple deserializer
            /// </summary>
            /// <param name="loadContext"></param>
            /// <param name="objectType"></param>
            /// <param name="stream"></param>
            /// <returns></returns>
            public object Deserialize(LoadContext loadContext, Type objectType, System.IO.Stream stream) {

                StreamReader sr = new StreamReader(stream);

                string json = sr.ReadToEnd();

                // for a real application, you'd do the full json parsing here, but I'm cheating for simplicity.
                //
                Match m = Regex.Match(json, @"""name"":""(?<name>[^""]+)""");

                // create the model.
                //
                var model = new FacebookUserModel();
                model.LoadContext = loadContext;

                // populate properties.
                if (m.Success) {
                    model.Name = m.Groups["name"].Value;
                }
                else {
                    throw new FormatException("Couldn't find name in user info.");
                }
                return model;
            }
        }
    }
}
