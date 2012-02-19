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
using System.Collections.Generic;

namespace Flickr.Sample.Model
{
    [CachePolicy(AgFx.CachePolicy.CacheThenRefresh, 3600)]
    public class ContactListVm : ModelItemBase<IdLoadContext>
    {
        public ContactListVm()
        {

        }

        public ContactListVm(string nsid)
            : base(new IdLoadContext(nsid))
        {

        }

        BatchObservableCollection<UserVm> _contacts = new BatchObservableCollection<UserVm>(10);

        public ObservableCollection<UserVm> Contacts
        {
            get
            {
                return _contacts;
            }
            set
            {
                _contacts.Merge(value, (u1, u2) => { return String.Compare((string)u1.UserId, (string)u2.UserId); }, EquivelentItemMergeBehavior.UpdateEqualItems);
            }
        }

        public class ContactListDataLoader : FlickrDataLoaderBase
        {

            public override LoadRequest GetLoadRequest(LoadContext context, Type objectType)
            {
                return BuildRequest(context, "flickr.contacts.getPublicList", new FlickrArgument("user_id", (string)context.Identity));               
            }

            protected override object DeserializeCore(LoadContext context, System.Xml.Linq.XElement xml, Type objectType, System.IO.Stream stream)
            {
                //<contacts page="1" pages="1" perpage="1000" total="3">
                //    <contact nsid="12037949629@N01" username="Eric" iconserver="1"
                //        realname="Eric Costello"
                //        friend="1" family="0" ignored="1" /> 
                //    <contact nsid="12037949631@N01" username="neb" iconserver="1"
                //        realname="Ben Cerveny"
                //        friend="0" family="0" ignored="0" /> 
                //    <contact nsid="41578656547@N01" username="cal_abc" iconserver="1"
                //        realname="Cal Henderson"
                //        friend="1" family="1" ignored="0" />
                //</contacts>

                ContactListVm contacts = new ContactListVm();
                contacts.LoadContext = (IdLoadContext)context;

                foreach (var c in xml.Elements("contact"))
                {
                    string nsid = c.Attribute("nsid").Value;

                    UserVm userVm = new UserVm(nsid);
                    userVm.UserName = c.Attribute("username").Value;

                    var fullNameAttr = c.Attribute("realname");

                    if (fullNameAttr != null)
                    {
                        userVm.FullName = fullNameAttr.Value;
                    }
                    userVm.ProfileIconUrl = UserVm.MakeIconUri(nsid, c.Attribute("iconfarm").Value, c.Attribute("iconserver").Value);
                    contacts.Contacts.Add(userVm);
                }
                return contacts;
            }
        }
    }
}
