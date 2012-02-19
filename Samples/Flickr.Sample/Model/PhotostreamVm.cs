using AgFx;
using System.Collections.Generic;

namespace Flickr.Sample.Model
{
    [CachePolicy(CachePolicy.CacheThenRefresh, 300)]
    public class PhotostreamVm : PhotoCollectionVmBase
    {
        public PhotostreamVm()
        {

        }

        public PhotostreamVm(string id)
            : base(new PhotoCollectionLoadContext(id))
        {

        }

        public class PhotostreamVmDataLoader : PhotoCollectionDataLoaderBase<PhotostreamVm>
        {

            protected override string ApiName
            {
                get { return "flickr.people.getPublicPhotos"; }
            }

            internal override void PopulateArgs(PhotoCollectionLoadContext context, IList<FlickrArgument> args)
            {
                base.PopulateArgs(context, args);
                args.Add(new FlickrArgument("user_id", context.Identity.ToString()));
            }
        }

        #region DataLoader


#if false

        public override bool SerializeOptimizedData(object value, Stream outputStream)
        {
            StreamWriter sw = new StreamWriter(outputStream);
            PhotostreamVm vm = (PhotostreamVm)value;
            sw.WriteLine(vm._photos.Count);
            foreach (var photo in _photos)
            {
                sw.WriteLine(photo.Identity);
                sw.WriteLine(photo.Title);
            }
            sw.Flush();
            return true;
        }

        public override object DeserializeOptimizedData(object identifier, Type objectType, Stream stream)
        {
            StreamReader sr = new StreamReader(stream);

            PhotostreamVm vm = new PhotostreamVm(sr.ReadLine());

            int count = Int32.Parse(sr.ReadLine());

            List<PhotoVm> _photos = new List<PhotoVm>();

            for (int i = 0; i < count; i++)
            {
                PhotoVm photo = new PhotoVm(sr.ReadLine());
                photo.Title = sr.ReadLine();
            }

            return vm;
        }
#endif
        #endregion
    }
}
