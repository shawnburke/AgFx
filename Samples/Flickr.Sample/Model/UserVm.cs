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
using System.Collections.ObjectModel;
using System.Xml.Linq;
using System.Diagnostics;
using System.IO;

namespace Flickr.Sample.Model
{
    [CachePolicy(CachePolicy.CacheThenRefresh, 3600)]  // cache for 3600s (1hr)  
    public class UserVm : ModelItemBase
    {

        public UserVm()
        {   
        }

        public UserVm(string id) : this ()
        {
            this.LoadContext = new LoadContext(id);
        }


        public string UserId
        {
            get
            {
                return (string)LoadContext.Identity;
            }
        }        


        #region Property UserName
        private string _UserName;
        public string UserName
        {
            get
            {
                return _UserName;
            }
            set
            {
                if (_UserName != value)
                {
                    _UserName = value;
                    RaisePropertyChanged("UserName");
                }
            }
        }
        #endregion

        
        
        #region Property FullName
        private string _FullName;
        [DependentOnProperty("UserName")]
        public string FullName
        {
            get
            {
                if (_FullName == null)
                {
                    return _UserName;
                }

                return _FullName;
            }
            set
            {
                if (_FullName != value)
                {
                    _FullName = value;
                    RaisePropertyChanged("FullName");
                }
            }
        }
        #endregion

        private Uri _profileIconUrl;
        public Uri ProfileIconUrl
        {
            get
            {
                return _profileIconUrl;
            }
            set
            {
                if (_profileIconUrl != value)
                {
                    _profileIconUrl = value;
                    RaisePropertyChanged("ProfileIconUrl");
                }
            }
        }


        public ContactListVm Contacts
        {
            get
            {
                return DataManager.Current.Load<ContactListVm>(LoadContext.Identity);
            }
        }


        public PhotostreamVm Photostream
        {
            get
            {
                var psvm = DataManager.Current.Load<PhotostreamVm>(new PhotoCollectionLoadContext((string)LoadContext.Identity) {
                    Page = 0,
                    PerPage = 25
                });
                return psvm;
            }
        }

        #region Property Photosets

        PhotosetListVm _psv;
        
        public ObservableCollection<PhotosetVm> Photosets
        {
            get
            {
                _psv = DataManager.Current.Load<PhotosetListVm>(LoadContext.Identity);
                return _psv.Photosets;
            }           
        }
        #endregion


        private const string IconUrlFormat = "http://farm{0}.static.flickr.com/{1}/buddyicons/{2}.jpg";


        internal static Uri MakeIconUri(string nsid, string iconFarm, string iconServer)                
        {
            return new Uri(String.Format(IconUrlFormat, iconFarm, iconServer, nsid));
        }        

      public class UserVmLoader : FlickrDataLoaderBase, IDataOptimizer {


        public override LoadRequest GetLoadRequest(LoadContext context, Type objectType)
        {
            return BuildRequest(
                context,
                "flickr.people.getInfo",
                new FlickrArgument[]{
                        new FlickrArgument("user_id", context.Identity.ToString())
                    });
        }

        protected override object DeserializeCore(LoadContext context, System.Xml.Linq.XElement xml, Type objectType, System.IO.Stream stream)
        {
            //<person nsid="12037949754@N01" ispro="0" iconserver="122" iconfarm="1">
            //    <username>bees</username>
            //    <realname>Cal Henderson</realname>
            //        <mbox_sha1sum>eea6cd28e3d0003ab51b0058a684d94980b727ac</mbox_sha1sum>
            //    <location>Vancouver, Canada</location>
            //    <photosurl>http://www.flickr.com/photos/bees/</photosurl> 
            //    <profileurl>http://www.flickr.com/people/bees/</profileurl> 
            //    <photos>
            //        <firstdate>1071510391</firstdate>
            //        <firstdatetaken>1900-09-02 09:11:24</firstdatetaken>
            //        <count>449</count>
            //    </photos>
            //</person>

            XElement personElement = xml;

            var user = new UserVm(personElement.Attribute("nsid").Value);
                        
            user.ProfileIconUrl = UserVm.MakeIconUri(user.UserId, personElement.Attribute("iconfarm").Value, personElement.Attribute("iconserver").Value);
            bool success;

            user.UserName = personElement.Element("username").Value;
            user.FullName = TryGetValue(personElement, "realname", "", out success);
            return user;
        }

        public object DeserializeOptimizedData(LoadContext context, Type objectType, System.IO.Stream stream) {
            StreamReader sr = new StreamReader(stream);

            string id = sr.ReadLine();

            UserVm vm = new UserVm(id);

            vm.FullName = sr.ReadLine();
            vm.UserName = sr.ReadLine();
            vm.ProfileIconUrl = new Uri(sr.ReadLine());
            return vm;

        }

        public bool SerializeOptimizedData(object value, System.IO.Stream outputStream) {
            StreamWriter sw = new StreamWriter(outputStream);
            UserVm vm = (UserVm)value;
            sw.WriteLine(vm.LoadContext.Identity);
            sw.WriteLine(vm.FullName);
            sw.WriteLine(vm.UserName);
            sw.WriteLine(vm.ProfileIconUrl);
            sw.Flush();
            return true;
        }
    }
        
   
    }
}
