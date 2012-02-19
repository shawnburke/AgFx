using System;
using System.Text;
using System.Collections.Generic;
using Flickr.Sample.Model;
using AgFx;

namespace Flickr.Sample.Model
{
    public class FlickrClient : System.ComponentModel.INotifyPropertyChanged
    {

        private static FlickrClient _current;

        public static FlickrClient Current
        {
            get
            {
                if (_current == null)
                {
                    throw new InvalidOperationException("Must call Initialize first.");
                }
                return _current;
            }
        }

        public UserVm GetUser(string name)
        {
            return DataManager.Current.Load<UserVm>(name);
        }        

        public static void Initialize(string apiKey, string sharedSecret)
        {
            if (_current != null)
            {
                if (apiKey != _current.ApiKey || sharedSecret != _current.SharedSecret)
                {
                    throw new ArgumentException("Already initialized with different ApiKey or SharedSecret");
                }
                return;
            }
            if (_current == null)
            {
                _current = new FlickrClient(apiKey, sharedSecret);
            }
        }

        public string ApiKey
        {
            get;
            private set;
        }

        public string SharedSecret
        {
            get;
            private set;
        }

        private FlickrClient(string apiKey, string sharedSecret)
        {
            ApiKey = apiKey;
            SharedSecret = sharedSecret;        

        }

   
        private const string RestUri = "http://api.flickr.com/services/rest/?";

        public string BuildApiCall(string methodName, bool needsApiKey, bool needsToken, bool needsSig, params FlickrArgument[] args)
        {
            List<FlickrArgument> argList = new List<FlickrArgument>();

            argList.Add(new FlickrArgument("method", methodName));

            if (needsApiKey)
            {
                argList.Add(new FlickrArgument("api_key", ApiKey));
            }

            if (needsToken)
            {
                throw new NotImplementedException();
            }

            if (args != null)
            {
                argList.AddRange(args);
            }

            if (needsSig)
            {
                throw new NotImplementedException();                
            }


            StringBuilder apiString = new StringBuilder();



            foreach (var arg in argList)
            {
                string value = UrlEncode(arg.Value);
                apiString.AppendFormat("{0}={1}&", arg.Name, value);
            }

            return RestUri + apiString.ToString();

        }

        protected virtual string UrlEncode(string value)
        {
            return value;
        }

        static DateTime _startTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);

        internal static DateTime FromUnixTime(string timeStamp)
        {
            int s = Int32.Parse(timeStamp);

            return _startTime.AddSeconds(s).ToLocalTime();
        }
     


        #region INotifyPropertyChanged Members

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(prop));
            }
        }

        #endregion


    }


}
