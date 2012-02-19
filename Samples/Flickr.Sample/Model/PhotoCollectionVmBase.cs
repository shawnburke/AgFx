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
using System.Collections.Generic;
using System.ComponentModel;

namespace Flickr.Sample.Model
{
       
    public class PhotoCollectionVmBase : ModelItemBase<PhotoCollectionLoadContext>
    {

        public string Id {
            get {
                return LoadContext.Id;
            }
        }
    
        public PhotoCollectionVmBase()
        {

        }

        public PhotoCollectionVmBase(PhotoCollectionLoadContext context)
            : base(context)
        {

        }



        [DependentOnProperty("TotalPhotos")]
        [DependentOnProperty("Page")]
        [DependentOnProperty("PageSize")]
        public bool CanPageForward {

            get {
                return TotalPhotos > 0 && (Page * PageSize) < TotalPhotos;
            }        
        }

        [DependentOnProperty("TotalPhotos")]
        [DependentOnProperty("Page")]
        [DependentOnProperty("PageSize")]
        public bool CanPageBackward {

            get {
                return TotalPhotos > 0 && Page > 1;
            }
        }

        

        public void LoadPage(int page, Action complete)
        {
            if (page < 1 || page > Pages)
            {
                throw new ArgumentOutOfRangeException();
            }
            else if (page == Page)
            {
                return;
            }

            LoadContext.Page = page;

            if (complete != null)
            {
                PropertyChangedEventHandler handler = null;

                handler = (s, e) =>
                {

                    if (e.PropertyName == "Photos")
                    {
                        base.PropertyChanged -= handler;
                        PriorityQueue.AddUiWorkItem(() =>
                        {
                            complete();
                        }, false);
                    }
                };
                base.PropertyChanged += handler;
            }
            Refresh();


        }


        public void PreviousPage(Action complete)
        {
            var currentPage = Page;
            LoadPage(Math.Max(1, --currentPage), complete); 
        }

        public void NextPage(Action complete)
        {
            var currentPage = Page;
            LoadPage(Math.Min(Pages, ++currentPage), complete); 
        }


        #region Property Page
        private int _Page;
        public int Page
        {
            get
            {
                return _Page;
            }
            set
            {
                if (_Page != value)
                {
                    LoadContext.Page = value;
                    _Page = value;
                    RaisePropertyChanged("Page");
                }
            }
        }
        #endregion



        #region Property PageSize
        private int _PageSize;
        public int PageSize
        {
            get
            {
                return _PageSize;
            }
            set
            {
                if (_PageSize != value)
                {
                    LoadContext.PerPage = value;
                    _PageSize = value;
                    RaisePropertyChanged("PageSize");
                }
            }
        }
        #endregion



        #region Property Pages
        private int _Pages;
        public int Pages
        {
            get
            {
                return _Pages;
            }
            set
            {
                if (_Pages != value)
                {
                    _Pages = value;
                    RaisePropertyChanged("Pages");
                }
            }
        }
        #endregion

        #region Property TotalPhotos
        private int _TotalPhotos;
        public int TotalPhotos
        {
            get
            {
                return _TotalPhotos;
            }
            set
            {
                if (_TotalPhotos != value)
                {
                    _TotalPhotos = value;
                    RaisePropertyChanged("TotalPhotos");
                }
            }
        }
        #endregion


        private BatchObservableCollection<PhotoVm> _photos = new BatchObservableCollection<PhotoVm>(4);
        public ObservableCollection<PhotoVm> Photos
        {
            get
            {
                return _photos;
            }
            set
            {
                if (_photos != value)
                {
                    foreach (var photo in value)
                    {
                        DataManager.Current.RegisterProxy<PhotoVm>(photo);
                    }

                    _photos.Merge(value, (x, y) => { return DateTime.Compare(y.LastUpdated, x.LastUpdated); }, EquivelentItemMergeBehavior.UpdateEqualItems);
                    RaisePropertyChanged("Photos");
                }
            }
        }


        public abstract class PhotoCollectionDataLoaderBase<T> : FlickrDataLoaderBase where T : PhotoCollectionVmBase, new()                                                                            
        {

            protected abstract string ApiName
            {
                get;
            }

            internal virtual void PopulateArgs(PhotoCollectionLoadContext context, IList<FlickrArgument> args) {

            }

            public LoadRequest GetLoadRequest(PhotoCollectionLoadContext context, Type objectType) {
                
                List<FlickrArgument> args = new List<FlickrArgument>();
                args.Add(new FlickrArgument("per_page", context.PerPage.ToString()));
                args.Add(new FlickrArgument("page", context.Page.ToString()));

                PopulateArgs(context, args);

                return BuildRequest(
                    context,
                    ApiName,
                    args.ToArray());
            }

            public override LoadRequest GetLoadRequest(LoadContext identifier, Type objectType) {
                return GetLoadRequest((PhotoCollectionLoadContext)identifier, objectType);
            }

            
            protected override object DeserializeCore(LoadContext context, XElement xml, Type objectType, Stream stream)
            {
                // <photos page="2" pages="89" perpage="10" total="881">
                //    <photo id="2636" owner="47058503995@N01" 
                //        secret="a123456" server="2" title="test_04"
                //        ispublic="1" isfriend="0" isfamily="0" />
                //    <photo id="2635" owner="47058503995@N01"
                //        secret="b123456" server="2" title="test_03"
                //        ispublic="0" isfriend="1" isfamily="1" />
                //    <photo id="2633" owner="47058503995@N01"
                //        secret="c123456" server="2" title="test_01"
                //        ispublic="1" isfriend="0" isfamily="0" />
                //    <photo id="2610" owner="12037949754@N01"
                //        secret="d123456" server="2" title="00_tall"
                //        ispublic="1" isfriend="0" isfamily="0" />
                //</photos>
                var vm = new T();
                vm.LoadContext = (PhotoCollectionLoadContext)context;
                bool success;
                vm.PageSize = Int32.Parse(TryGetValue(xml, "perpage", "10", out success));
                vm.TotalPhotos = Int32.Parse(TryGetValue(xml, "total", "0", out success));
                vm.Pages = Int32.Parse(TryGetValue(xml, "pages", "0", out success));
                vm.Page = Int32.Parse(TryGetValue(xml, "page", "0", out success));

                int index = 0;


                List<PhotoVm> photos = new List<PhotoVm>();

                foreach (var ps in xml.Elements("photo"))
                {

                    string id = TryGetValue(ps, "id", "", out success);
                    PhotoVm psvm = new PhotoVm(id);
                    psvm.Title = TryGetValue(ps, "title", null, out success);
                    psvm.OwnerId = TryGetValue(ps, "owner", null, out success);

                    psvm.CollectionIdentifier = String.Format("{0}\t{1}", vm.LoadContext.Identity.ToString(), index++);

                    photos.Add(psvm);
                }
                photos.Sort((x, y) => { return DateTime.Compare(y.LastUpdated, x.LastUpdated); });
                photos.ForEach(p => vm.Photos.Add(p));
                return vm;
            }
        }
    }
}
