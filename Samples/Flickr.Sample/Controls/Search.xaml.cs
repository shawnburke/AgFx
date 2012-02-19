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

namespace Flickr.Sample.Controls
{
    public partial class Search : UserControl
    {
        public Search()
        {
            InitializeComponent();
            view.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string tag = null;
            string text = null;
            string user = null;

            string search = txtSearch.Text;

            if (String.IsNullOrEmpty(search))
            {
                return;
            }

            if (rbTag.IsChecked.GetValueOrDefault())
            {
                tag = search;
            }

            if (rbText.IsChecked.GetValueOrDefault())
            {
                text = search;
            }

            if (rbUser.IsChecked.GetValueOrDefault())
            {
                user = search;
            }

            SearchVm.Search(text, user, tag,
                (vm) =>
                {
                    view.DataContext = vm;
                    view.Visibility = System.Windows.Visibility.Visible;
                }
            );




        }
    }
}
