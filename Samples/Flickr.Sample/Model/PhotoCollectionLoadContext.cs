using System;
using AgFx;

namespace Flickr.Sample.Model {
    public class PhotoCollectionLoadContext : IdLoadContext {

        public int Page
        {
            get;
            set;
        }

        public int PerPage
        {
            get;
            set;
        }

        public PhotoCollectionLoadContext(string nsid)
            : base(nsid) {

        }

        //protected override string GenerateKey() {
        //    return string.Format("{0}_{1}_{2}", Identity, Page, PerPage);
        //}

        public override string ToString() {
            return string.Format("{0}\t{1}\t{2}", Identity, Page, PerPage);
        }

        public static PhotoCollectionLoadContext FromString(string str) {
            string[] parts = str.Split('\t');
            var lc = new PhotoCollectionLoadContext(parts[0]);

            if (parts.Length == 3) {
                lc.Page = Int32.Parse(parts[1]);
                lc.PerPage = Int32.Parse(parts[2]);
            }
            return lc;
        }

    }


}
