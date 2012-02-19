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
using System.IO.IsolatedStorage;
using System.Diagnostics;

namespace Flickr.Sample
{
    public partial class Homepage : PhoneApplicationPage
    {

        private string DefaultUserId = null;

        string _userid;

        public Homepage()
        {
            var defaultUser = App.Current.Resources["defaultUserId"] as string;

            if (String.IsNullOrEmpty(defaultUser)) {
                MessageBox.Show("Please enter your Flickr user nsid in app.xaml");
                return;
            }

            DefaultUserId = defaultUser.ToString();
            InitializeComponent();
        }
         

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (State.ContainsKey("userid"))
            {
                _userid = (string)State["userid"];
            }
            else if (!NavigationContext.QueryString.TryGetValue("userid", out _userid))
            {
                _userid = DefaultUserId;
            }

            this.DataContext = DataManager.Current.Load<UserVm>(_userid);                        
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            State["userid"] = _userid;
            base.OnNavigatedFrom(e);
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DataManager.Current.DeleteCache();

            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                foreach (var file in isoStore.GetFileNames())
                {
                    Debug.WriteLine(file);
                }
            }
        }

        private void TextBlock_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            IUpdatable upd = ((UserVm)this.DataContext).Photostream as IUpdatable;

            if (upd != null)
            {
                upd.Refresh();
            }
        }
               
    }

   
}