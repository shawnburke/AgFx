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
using Flickr.Sample.Model;
using AgFx;
using System.Collections.Specialized;

namespace Flickr.Sample
{
    public partial class PhotoView : PhoneApplicationPage
    {

        PhotoCollectionVmBase _collectionVm;

        PhotoVm CurrentImage
        {
            get
            {
                return (PhotoVm)DataContext;
            }
        }

        public PhotoView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            string id;

            if (NavigationContext.QueryString.TryGetValue("photoid", out id))
            {
                this.DataContext = DataManager.Current.Load<PhotoVm>(id);
            }

            string type;

            if (NavigationContext.QueryString.TryGetValue("collection", out id) && NavigationContext.QueryString.TryGetValue("type", out type))
            {
                Type collectionType = Type.GetType(type);

                if (collectionType != null)
                {
                    var identifier = PhotoCollectionLoadContext.FromString(id);

                    _collectionVm = (PhotoCollectionVmBase)DataManager.Current.Load(collectionType, identifier);
                }
            }
            base.OnNavigatedTo(e);
        }


        private bool IsTap(Point p)
        {
            return Math.Abs(p.X) < 5 && Math.Abs(p.Y) < 5;
        }

        private void PreviousImage_Tap(object sender, ManipulationCompletedEventArgs e)
        {
            if (!IsTap(e.TotalManipulation.Translation))
            {
                return;
            }

            int index = GetCurrentImageIndex();

            if (index > 0)
            {
                index--;
                this.DataContext = _collectionVm.Photos[index];
            }
            else
            {
                _collectionVm.PreviousPage(
                      () =>
                      {
                          this.DataContext = _collectionVm.Photos[_collectionVm.Photos.Count-1];
                      }
                  );
            }

        }

        private int GetCurrentImageIndex()
        {
            if (CurrentImage != null && _collectionVm != null)
            {
                int index = _collectionVm.Photos.IndexOf(CurrentImage);

                return index;
            }
            return -1;
        }

        private void NextImage_Tap(object sender, ManipulationCompletedEventArgs e)
        {
            if (!IsTap(e.TotalManipulation.Translation))
            {
                return;
            }
            int index = GetCurrentImageIndex();

            if (index != -1 && index < _collectionVm.TotalPhotos)
            {
                index++;

                if (index >= _collectionVm.Photos.Count)
                {
                    _collectionVm.NextPage(
                        () =>
                        {
                            this.DataContext = _collectionVm.Photos[0];
                        }
                    );
                }
                else
                {
                    this.DataContext = _collectionVm.Photos[index];
                }

            }
        }

        private void Image_Tap(object sender, ManipulationCompletedEventArgs e)
        {
            if (IsTap(e.TotalManipulation.Translation) && e.OriginalSource == img)
            {

                detailsPivot.Visibility = detailsPivot.Visibility == System.Windows.Visibility.Collapsed ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }

        }
    }
}