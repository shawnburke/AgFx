using AgFx;
using System.Collections.ObjectModel;

namespace Flickr.Sample.Model
{
    [CachePolicy(CachePolicy.CacheThenRefresh, CacheTimeInSeconds = 3600)]    
    public class PhotosetVm : PhotoCollectionVmBase
    {

        public PhotosetVm()
        {

        }

        public PhotosetVm(string id)
            : base(new PhotoCollectionLoadContext(id))
        {

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


        #region Property Description
        private string _Description;
        public string Description
        {
            get
            {
                return _Description;
            }
            set
            {
                if (_Description != value)
                {
                    _Description = value;
                    RaisePropertyChanged("Description");
                }
            }
        }
        #endregion




        #region Property PrimaryPhotoId
        private string _PrimaryPhotoId;
        public string PrimaryPhotoId
        {
            get
            {
                return _PrimaryPhotoId;
            }
            set
            {
                if (_PrimaryPhotoId != value)
                {
                    _PrimaryPhotoId = value;
                    RaisePropertyChanged("PrimaryPhotoId");
                }
            }
        }
        #endregion

        

        public PhotoVm PrimaryPhoto
        {
            get
            {
                return DataManager.Current.Load<PhotoVm>(PrimaryPhotoId);
            }
            
        }
        
        public class PhotosetDataLoader : PhotoCollectionDataLoaderBase<PhotosetVm>
        {
            protected override string ApiName
            {
                get { return "flickr.photosets.getPhotos"; }
            }


            internal override void PopulateArgs(PhotoCollectionLoadContext context, System.Collections.Generic.IList<FlickrArgument> args)
            {
                base.PopulateArgs(context, args);
                args.Add(new FlickrArgument("photoset_id", context.Identity.ToString()));
            }

            protected override object DeserializeCore(LoadContext identifer, System.Xml.Linq.XElement xml, System.Type objectType, System.IO.Stream stream)
            {
                PhotosetVm vm = (PhotosetVm)base.DeserializeCore(identifer, xml, objectType, stream);

                bool s;
                string primaryPhotoId = TryGetValue(xml, "primary", null, out s);
                vm.PrimaryPhotoId = primaryPhotoId;
                return vm;
            }
        }
    }
}
