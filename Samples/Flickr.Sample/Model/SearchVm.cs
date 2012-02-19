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
using System.Collections.Generic;

namespace Flickr.Sample.Model
{
    [CachePolicy(CachePolicy.NoCache)]
    public class SearchVm : PhotoCollectionVmBase
    {

        private static string BuildSearchIdentity(string q, string u, string t)
        {
            return String.Format("{0}\b{1}\b{2}", q, u, t);
        }

        private static void ParseSearchIdentity(string id, out string query, out string userid, out string tags)
        {
            string[] parts = id.Split('\b');

            query = parts[0];
            userid = parts[1];
            tags = parts[2];
        }


        public SearchVm()
        {

        }

        public static void Search(string query, string username, string tag, Action<SearchVm> complete)
        {

            Action<string> handler = (userid) =>
            {
                string s = BuildSearchIdentity(query, userid, tag);

                PhotoCollectionLoadContext id = new PhotoCollectionLoadContext(s)
                {                     
                    Page = 0,
                    PerPage = 25
                };

                DataManager.Current.Load<SearchVm>(id,
                    (svm) =>
                    {
                        complete(svm);
                    },
                    null);
            };

            if (!String.IsNullOrEmpty(username))
            {
                DataManager.Current.Load<UserNameVm>(username,
                    (vm) =>
                    {
                        handler(vm.UserId);
                    },
                    (ex) =>
                    {
                        MessageBox.Show("Username not found.");
                    }
               );
            }
            else
            {
                handler(null);
            }
        }

        public class SearchDataLoader : PhotoCollectionDataLoaderBase<SearchVm>
        {
            protected override string ApiName
            {
                get { return "flickr.photos.search"; }
            }

            internal override void PopulateArgs(PhotoCollectionLoadContext context, IList<FlickrArgument> args)
            {
                base.PopulateArgs(context, args);

                string query;
                string userid;
                string tags;

                ParseSearchIdentity((string)context.Identity, out query, out userid, out tags);


                if (!String.IsNullOrEmpty(query))
                {
                    args.Add(new FlickrArgument("text", query));
                }
                else if (!String.IsNullOrEmpty(tags))
                {
                    args.Add(new FlickrArgument("tags", tags));
                }
                else if (!String.IsNullOrEmpty(userid))
                {
                    args.Add(new FlickrArgument("user_id", userid));
                }
            }
        }

    }
}
