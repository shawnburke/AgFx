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
using System.Collections.ObjectModel;
using AgFx;
using System.Xml.Linq;
using System.IO;

namespace Flickr.Sample.Model
{
    [CachePolicy(CachePolicy.CacheThenRefresh, 300)]    
    public class PhotosetListVm : AgFx.ModelItemBase
    {

        public PhotosetListVm()
        {

        }

        public PhotosetListVm(object id)
            : base(id)
        {

        }

        
        private BatchObservableCollection<PhotosetVm> _Photosets = new BatchObservableCollection<PhotosetVm>(4);
        public ObservableCollection<PhotosetVm> Photosets
        {
            get
            {
                return _Photosets;
            }
            set
            {
                if (_Photosets != value)
                {
                    foreach (var photoSet in value) {
                        DataManager.Current.RegisterProxy(photoSet);
                    }

                    _Photosets.Merge(value, (x, y) => { return DateTime.Compare(y.LastUpdated, x.LastUpdated); }, EquivelentItemMergeBehavior.UpdateEqualItems);
                    RaisePropertyChanged("Photosets");
                }
            }
        }

        public class PhotosetListVmDataLoader : FlickrDataLoaderBase
        {
            public override LoadRequest GetLoadRequest(LoadContext context, Type objectType)
            {
                return BuildRequest(
                    context,
                    "flickr.photosets.getList",
                    new FlickrArgument[]{
                        new FlickrArgument("user_id", context.Identity.ToString())
                    });
            }

            protected override object DeserializeCore(LoadContext identifier, XElement xml, Type objectType, Stream stream)
            {
                //<photosets cancreate="1">
                //    <photoset id="5" primary="2483" secret="abcdef"
                //        server="8" photos="4" farm="1">
                //        <title>Test</title>
                //        <description>foo</description>
                //    </photoset>
                //    <photoset id="4" primary="1234" secret="832659"
                //        server="3" photos="12" farm="1">
                //        <title>My Set</title>
                //        <description>bar</description>
                //    </photoset>
                //</photosets>

                PhotosetListVm vm = new PhotosetListVm(identifier.Identity);

                vm.Photosets = new System.Collections.ObjectModel.ObservableCollection<PhotosetVm>();

                foreach (var ps in xml.Elements("photoset"))
                {
                    bool success;

                    string id = TryGetValue(ps, "id", "", out success);

                    PhotosetVm psvm = new PhotosetVm(id);

                    psvm.Title = TryGetValue(ps, "title", null, out success);
                    psvm.Description = TryGetValue(ps, "description", null, out success);
                    psvm.PrimaryPhotoId = TryGetValue(ps, "primary", null, out success);

                    vm.Photosets.Add(psvm);
                }

                return vm;
            }
        }
    }
}
