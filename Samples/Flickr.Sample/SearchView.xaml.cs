using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Flickr.Sample.Controls;
using Flickr.Sample.Model;

namespace Flickr.Sample
{
    public partial class SearchView : PhoneApplicationPage
    {
        public SearchView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            string tag;
            string username = null;

            if (NavigationContext.QueryString.TryGetValue("tag", out tag))
            {
                search.txtSearch.Text = tag;
                search.rbTag.IsChecked = true;               
            }
            else if (NavigationContext.QueryString.TryGetValue("username", out username))
            {
                search.txtSearch.Text = username;
                search.rbUser.IsChecked = true;               
            }


            if (tag != null || username != null) {
             SearchVm.Search(null, username, tag, (vm) =>
                {
                    this.DataContext = vm;
                });
            }
            base.OnNavigatedTo(e);
        }
    }
}