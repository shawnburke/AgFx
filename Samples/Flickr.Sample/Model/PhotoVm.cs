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
using System.Xml.Linq;
using System.IO;
using System.Collections.ObjectModel;

namespace Flickr.Sample.Model
{

    [CachePolicy(CachePolicy.CacheThenRefresh, 300)]
    public class PhotoVm : ModelItemBase
    {
        public PhotoVm()
        {            
        }

        public PhotoVm(object id)
            : base(id)
        {         
        }

        private void PopulateValues()
        {
            // register this object as a proxy and kick off a load.
            // this will cause the values to be fully populated back to this
            // instance.
            //
            DataManager.Current.RegisterProxy<PhotoVm>(this, true, null, true);
        }

        public string CollectionIdentifier
        {
            get;
            set;
        }

        #region Property Description
        private string _description;
        public string Description
        {
            get
            {
                if (_description == null)
                {
                    PopulateValues();
                }
                return _description;
            }
            set
            {
                if (_description != value)
                {
                    _description = value;
                    RaisePropertyChanged("Description");
                }
            }
        }
        #endregion

        public string PhotoId
        {
            get
            {
                return (string)LoadContext.Identity;
            }
        }


        #region Property Posted
        private DateTime _posted;
        public DateTime Posted
        {
            get
            {
                if (_posted == default(DateTime))
                {
                    PopulateValues();
                }
                return _posted;
            }
            set
            {
                if (_posted != value)
                {
                    _posted = value;
                    RaisePropertyChanged("Posted");
                }
            }
        }
        #endregion



        #region Property Taken
        private string _taken;
        public string Taken
        {
            get
            {
                if (_taken == default(string))
                {
                    PopulateValues();
                }
                return _taken;
            }
            set
            {
                if (_taken != value)
                {
                    _taken = value;
                    RaisePropertyChanged("Taken");
                }
            }
        }
        #endregion



        #region Property Updated
        private DateTime _updated;
        public DateTime Updated
        {
            get
            {
                return _updated;
            }
            set
            {
                if (_updated != value)
                {
                    _updated = value;
                    RaisePropertyChanged("Updated");
                }
            }
        }
        #endregion


        #region Property OwnerId
        private string _ownerId;
        public string OwnerId
        {
            get
            {
                return _ownerId;
            }
            set
            {
                if (_ownerId != value)
                {
                    _ownerId = value;
                    RaisePropertyChanged("OwnerId");
                }
            }
        }
        #endregion

        [DependentOnProperty("Owner")]
        public UserVm Owner
        {
            get
            {
                if (_ownerId == null)
                {
                    PopulateValues();
                    return null;
                }
                return DataManager.Current.Load<UserVm>(_ownerId);
            }
        }



        #region Property Title
        private string _Title;
        public string Title
        {
            get
            {
                return _Title;
            }
            set
            {
                if (_Title != value)
                {
                    _Title = value;
                    RaisePropertyChanged("Title");
                }
            }
        }
        #endregion


        public PhotoUrls PhotoUrls
        {
            get
            {
                return DataManager.Current.Load<PhotoUrls>(LoadContext.Identity);
            }
        }

        #region Property Tags
        private ObservableCollection<PhotoTag> _tags;
        public ObservableCollection<PhotoTag> Tags
        {
            get
            {
                return _tags;
            }
            set
            {
                if (_tags != value)
                {
                    _tags = value;
                    RaisePropertyChanged("Tags");
                }
            }
        }
        #endregion

        public class PhotoVmDataLoader : FlickrDataLoaderBase
        {

            public override LoadRequest GetLoadRequest(LoadContext context, Type objectType)
            {
                return BuildRequest(
                    context,
                    "flickr.photos.getInfo",
                    new FlickrArgument[]{
                        new FlickrArgument("photo_id", context.Identity.ToString())
                    });
            }

            static DateTime _startTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);

            internal static DateTime FromUnixTime(string timeStamp)
            {
                int s = Int32.Parse(timeStamp);

                return _startTime.AddSeconds(s).ToLocalTime();
            }

            protected override object DeserializeCore(LoadContext context, XElement xml, Type objectType, Stream stream)
            {
                //           <photo id="2733" secret="123456" server="12"
                //    isfavorite="0" license="3" rotation="90" 
                //    originalsecret="1bc09ce34a" originalformat="png">
                //    <owner nsid="12037949754@N01" username="Bees"
                //        realname="Cal Henderson" location="Bedford, UK" />
                //    <title>orford_castle_taster</title>
                //    <description>hello!</description>
                //    <visibility ispublic="1" isfriend="0" isfamily="0" />
                //    <dates posted="1100897479" taken="2004-11-19 12:51:19"
                //        takengranularity="0" lastupdate="1093022469" />
                //    <permissions permcomment="3" permaddmeta="2" />
                //    <editability cancomment="1" canaddmeta="1" />
                //    <comments>1</comments>
                //    <notes>
                //        <note id="313" author="12037949754@N01"
                //            authorname="Bees" x="10" y="10"
                //            w="50" h="50">foo</note>
                //    </notes>
                //    <tags>
                //        <tag id="1234" author="12037949754@N01" raw="woo yay">wooyay</tag>
                //        <tag id="1235" author="12037949754@N01" raw="hoopla">hoopla</tag>
                //    </tags>
                //    <urls>
                //        <url type="photopage">http://www.flickr.com/photos/bees/2733/</url> 
                //    </urls>
                //</photo>

                var vm = new PhotoVm(context.Identity);

                bool s;
                vm.Title = TryGetValue(xml, "title", null, out s);
                vm.Description = TryGetValue(xml, "description", null, out s);

                var owner = xml.Element("owner");

                vm.OwnerId = TryGetValue(owner, "nsid", null, out s);

                var dates = xml.Element("dates");

                vm.Posted = FromUnixTime(TryGetValue(dates, "posted", null, out s));

                var taken = TryGetValue(dates, "taken", null, out s);

                if (taken != null)
                {
                    vm.Taken = taken;
                }
                vm.Updated = FromUnixTime(TryGetValue(dates, "lastupdate", null, out s));

                ObservableCollection<PhotoTag> tags = new ObservableCollection<PhotoTag>();
                foreach (var tag in xml.Element("tags").Elements("tag"))
                {
                    bool success;
                    PhotoTag pt = new PhotoTag
                    {
                        Tag = TryGetValue(tag, "raw", "", out success),
                        RawTag = tag.Value,
                        ID = TryGetValue(tag, "id", null, out success)
                    };
                    tags.Add(pt);
                }
                vm.Tags = tags;
                return vm;
            }


            //public bool SerializeOptimizedData(object value, Stream outputStream) {
            //    StreamWriter sw = new StreamWriter(outputStream);
            //    PhotoVm vm = (PhotoVm)value;
            //    sw.WriteLine(vm.LoadContext.Identifier);
            //    sw.WriteLine(vm.Title);
            //    sw.Flush();
            //    return true;
            //}

            //public object DeserializeOptimizedData(LoadContext context, Type objectType, Stream stream) {
            //    StreamReader sr = new StreamReader(stream);

            //    PhotoVm vm = new PhotoVm(sr.ReadLine());
            //    vm.Title = sr.ReadLine();
            //    return vm;
            //}

        }

    }

    public class PhotoTag
    {
        public string ID { get; set; }
        public string Tag { get; set; }
        public string RawTag { get; set; }
    }
}
