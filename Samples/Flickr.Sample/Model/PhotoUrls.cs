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
using System.Xml.Linq;

namespace Flickr.Sample.Model
{
    // Cache for a day, and never show stale cache data while loading 
    // new data
    [CachePolicy(CachePolicy.CacheThenRefresh, 3600 * 24)] // 1 day
    public class PhotoUrls : ModelItemBase<IdLoadContext>
    {
        public PhotoUrls()
        {           
        }

        public PhotoUrls(string id)
            : base(new IdLoadContext(id))
        {           
        }

        [DependentOnProperty("MediumUrl")]
        [DependentOnProperty("LargeUrl")]
        [DependentOnProperty("ThumbnailUrl")]
        public string DisplayUrl
        {
            get
            {
                if (!String.IsNullOrEmpty(_MediumUrl))
                {
                    return MediumUrl;
                }
                else if (!String.IsNullOrEmpty(_LargeUrl))
                {
                    return LargeUrl;
                }
                return ThumbnailUrl;
            }
        }


        #region Property SmallUrl
        private string _SmallUrl;
        public string SmallUrl
        {
            get
            {
                return _SmallUrl;
            }
            set
            {
                if (_SmallUrl != value)
                {
                    _SmallUrl = value;
                    RaisePropertyChanged("SmallUrl");
                }
            }
        }
        #endregion

        

        #region Property MediumUrl
        private string _MediumUrl;
        public string MediumUrl
        {
            get
            {
                return _MediumUrl;
            }
            set
            {
                if (_MediumUrl != value)
                {
                    _MediumUrl = value;
                    RaisePropertyChanged("MediumUrl");
                }
            }
        }
        #endregion



        #region Property LargeUrl
        private string _LargeUrl;
        public string LargeUrl
        {
            get
            {
                return _LargeUrl;
            }
            set
            {
                if (_LargeUrl != value)
                {
                    _LargeUrl = value;
                    RaisePropertyChanged("LargeUrl");
                }
            }
        }
        #endregion

        
        

        #region Property ThumbnailUrl
        private string _ThumbnailUrl;
        public string ThumbnailUrl
        {
            get
            {
                return _ThumbnailUrl;
            }
            set
            {
                if (_ThumbnailUrl != value)
                {
                    _ThumbnailUrl = value;
                    RaisePropertyChanged("ThumbnailUrl");
                }
            }
        }
        #endregion

        public class PhotoUrlsLoader : FlickrDataLoaderBase
        {

            public override LoadRequest GetLoadRequest(LoadContext identifier, Type objectType)
            {
                return BuildRequest(
                    identifier,
                    "flickr.photos.getSizes",
                    new FlickrArgument[]{
                        new FlickrArgument("photo_id", identifier.Identity.ToString())
                    });
            }

            protected override object DeserializeCore(LoadContext identifier, XElement xml, Type objectType, Stream stream)
            {
                //  <sizes>
                //<size label="Square" width="75" height="75"
                //      source="http://farm2.static.flickr.com/1103/567229075_2cf8456f01_s.jpg"
                //      url="http://www.flickr.com/photos/stewart/567229075/sizes/sq/"/>
                //<size label="Thumbnail" width="100" height="75"
                //      source="http://farm2.static.flickr.com/1103/567229075_2cf8456f01_t.jpg"
                //      url="http://www.flickr.com/photos/stewart/567229075/sizes/t/"/>
                //<size label="Small" width="240" height="180"
                //      source="http://farm2.static.flickr.com/1103/567229075_2cf8456f01_m.jpg"
                //      url="http://www.flickr.com/photos/stewart/567229075/sizes/s/"/>
                //<size label="Medium" width="500" height="375"
                //      source="http://farm2.static.flickr.com/1103/567229075_2cf8456f01.jpg"
                //      url="http://www.flickr.com/photos/stewart/567229075/sizes/m/"/>
                //<size label="Original" width="640" height="480"
                //      source="http://farm2.static.flickr.com/1103/567229075_6dc09dc6da_o.jpg"
                //      url="http://www.flickr.com/photos/stewart/567229075/sizes/o/"/>
                //</sizes>

                var vm = new PhotoUrls((string)identifier.Identity);

                foreach (var s in xml.Elements("size"))
                {
                    bool success;
                    string label = TryGetValue(s, "label", null, out success);
                    string src = TryGetValue(s, "source", null, out success);
                    switch (label)
                    {
                        case "Thumbnail":
                            vm.ThumbnailUrl = src;
                            break;
                        case "Medium":
                        case "Medium 500":
                        case "Medium 640":
                            vm.MediumUrl = src;
                            break;
                        case "Large":
                            vm.LargeUrl = src;
                            break;
                        case "Small":
                            vm.SmallUrl = src;
                            break;

                    }
                }
                return vm;
            }


            public bool SerializeOptimizedData(object value, Stream outputStream) {
                StreamWriter sw = new StreamWriter(outputStream);
                PhotoUrls vm = (PhotoUrls)value;
                sw.WriteLine(vm.LoadContext.Identity);
                sw.WriteLine(vm.ThumbnailUrl);
                sw.Flush();
                return true;
            }

            public object DeserializeOptimizedData(LoadContext identifier, Type objectType, Stream stream) {
                StreamReader sr = new StreamReader(stream);

                PhotoUrls vm = new PhotoUrls(sr.ReadLine());
                vm.ThumbnailUrl = sr.ReadLine();
                return vm;
            }
        }
    }
}
