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

namespace Flickr.Sample.Model
{
    [CachePolicy(CachePolicy.ValidCacheOnly, 3600 * 24 * 365)] // valid for a year
    public class UserNameVm : ModelItemBase
    {
        public UserNameVm()
        {

        }

        public UserNameVm(string userName)
            : base(userName)
        {

        }


        public string UserName
        {
            get
            {
                return (string)LoadContext.Identity;
            }
        }

        #region Property UserId
        private string _UserId;
        public string UserId
        {
            get
            {
                return _UserId;
            }
            set
            {
                if (_UserId != value)
                {
                    _UserId = value;
                    RaisePropertyChanged("UserId");
                }
            }
        }
        #endregion

        public class UserNameVmDataLoader : FlickrDataLoaderBase {

            public override LoadRequest GetLoadRequest(LoadContext context, Type objectType)
            {
                return BuildRequest(
                    context, 
                    "flickr.people.findByUsername", 
                    new FlickrArgument("username", (string)context.Identity)
                    );
            }

            protected override object DeserializeCore(LoadContext context, System.Xml.Linq.XElement xml, Type objectType, System.IO.Stream stream)
            {
                //<user nsid="12037949632@N01">
                //    <username>Stewart</username> 
                //</user>
                bool s;
                string nsid = TryGetValue(xml, "nsid", null, out s);

                if (s)
                {
                    var dl = new UserNameVm((string)context.Identity);
                    dl.UserId = nsid;
                    return dl;
                }
                return null;
            }
        }
    
    
        
    }
}
