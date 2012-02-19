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
using AgFx;
using Flickr.Sample.Model;

namespace Flickr.Sample {
    public partial class CollectionView : PhoneApplicationPage {
        public CollectionView() {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
            string photosetid;

            if (NavigationContext.QueryString.TryGetValue("photoset", out photosetid)) {
                this.DataContext = DataManager.Current.Load<PhotosetVm>(new PhotoCollectionLoadContext(photosetid) {
                    Page = 0,
                    PerPage = 25
                });
            }

            base.OnNavigatedTo(e);
        }
    }
}