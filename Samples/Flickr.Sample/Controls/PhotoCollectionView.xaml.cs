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
using Flickr.Sample.Model;
using AgFx;
using AgFx.Controls;

namespace Flickr.Sample.Controls
{
    public partial class PhotoCollectionView : UserControl
    {
        PhotoCollectionVmBase VM
        {
            get
            {
                return (PhotoCollectionVmBase)DataContext;
            }
            set {
                DataContext = value;
            }
        }

        public PhotoCollectionView()
        {
            InitializeComponent();
        }

        private void UpdatedTextBlock_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {

        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            VM.PreviousPage(null);

            sv.ScrollToVerticalOffset(0);
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            VM.NextPage(null);
            sv.ScrollToVerticalOffset(0);
        }

   

        private void Photo_Click(object sender, RoutedEventArgs e)
        {
            HyperlinkButtonEx btn = (HyperlinkButtonEx)sender;

            string urlFormat = btn.NavigateUrlFormat;

            PhotoVm photo = (PhotoVm)btn.DataContext;

            string url = String.Format(urlFormat, String.Format("collection={0}&photoid={1}&type={2}", System.Net.HttpUtility.UrlEncode(VM.LoadContext.Identity.ToString()), photo.PhotoId, VM.GetType().FullName));

            btn.NavigateUri = new Uri(url, UriKind.Relative);

        }

      
    }
}
