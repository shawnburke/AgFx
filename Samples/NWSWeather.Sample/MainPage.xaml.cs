// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.

using System;
using System.Windows;
using Microsoft.Phone.Controls;
using AgFx;
using NWSWeather.Sample.ViewModels;
using System.IO.IsolatedStorage;

namespace NWSWeather.Sample
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {            
            InitializeComponent();            
        }

        private void btnAddZipCode_Click(object sender, RoutedEventArgs e)
        {
            // Load up a new ViewModel based on the zip.
            // This will either fetch new data from the Internet, or load the cached data off disk
            // as appropriate.
            //
            this.DataContext = DataManager.Current.Load<WeatherForecastVm>(txtZipCode.Text,
                    (vm) =>
                    {
                        // upon a succesful load, show the info panel.
                        // this is a bit of a hack, but we can't databind against
                        // a non-existant data context...
                        info.Visibility = Visibility.Visible;
                    },
                    (ex) =>
                    {
                        MessageBox.Show("Failed to get data for " + txtZipCode.Text);
                    }
             );
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            // when we navigage away, save the zip.
            IsolatedStorageSettings.ApplicationSettings["zip"] = txtZipCode.Text;            
            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            string zip;
            try {
                // if we have a saved zip, automatically load that up.
                if (IsolatedStorageSettings.ApplicationSettings.TryGetValue("zip", out zip) && !String.IsNullOrEmpty(zip)) {
                    txtZipCode.Text = zip;
                    btnAddZipCode_Click(null, null);
                    
                    // remove it in case of failure, we'll re-add it later.
                    IsolatedStorageSettings.ApplicationSettings.Remove("zip");
                }
            }
            catch {
            }
            base.OnNavigatedTo(e);
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            IUpdatable vm = DataContext as IUpdatable;
            if (vm != null)
            {
                vm.Refresh();
            }
        }
    }
}