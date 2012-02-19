// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using AgFx;

namespace AgFx.Controls
{

    /// <summary>
    /// Extended HyperlinkButton that allows you to databind a parameter and a uri format.
    /// Also adds handling for special schemes to be processed with special handling.
    /// 
    /// Example:
    /// 
    ///     NagivationUrlFormat="/SomePage.xaml?param={0}" NavigationUrlParam="{Binding ID}"
    ///     
    /// Results in NavigationUrl being "/SomePage.xaml?param=1", if ID = 1
    /// 
    /// Also provides a special call back for custom schemes.
    /// </summary>
    public class HyperlinkButtonEx : HyperlinkButton
    {

        public event EventHandler<CustomSchemeEventArgs> CustomSchemeClick;

        public string NavigateUrlFormat
        {
            get { return (string)GetValue(NavigateUrlFormatProperty); }
            set { SetValue(NavigateUrlFormatProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NavigateUrlFormat.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NavigateUrlFormatProperty =
            DependencyProperty.Register("NavigateUrlFormat", typeof(string), typeof(HyperlinkButtonEx), new PropertyMetadata(new PropertyChangedCallback(NavigateUrlFormat_Changed)));


        private static void NavigateUrlFormat_Changed(DependencyObject d, DependencyPropertyChangedEventArgs de)
        {
            var owner = (HyperlinkButtonEx)d;
            owner.UpdateNavigateUrl();
        }

        public string NavigateUrlParam
        {
            get { return (string)GetValue(NavigateUrlParamProperty); }
            set { SetValue(NavigateUrlParamProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NavigateUrlParam.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NavigateUrlParamProperty =
            DependencyProperty.Register("NavigateUrlParam", typeof(string), typeof(HyperlinkButtonEx), new PropertyMetadata(new PropertyChangedCallback(NavigateUrlParam_Changed)));


        private static void NavigateUrlParam_Changed(DependencyObject d, DependencyPropertyChangedEventArgs de)
        {
            var owner = (HyperlinkButtonEx)d;
            owner.UpdateNavigateUrl();
        }



        public string CustomSchemes
        {
            get { return (string)GetValue(CustomSchemesProperty); }
            set { SetValue(CustomSchemesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CustomSchemes.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CustomSchemesProperty =
            DependencyProperty.Register("CustomSchemes", typeof(string), typeof(HyperlinkButtonEx), new PropertyMetadata(new PropertyChangedCallback(CustomSchemes_Changed)));


        private static void CustomSchemes_Changed(DependencyObject d, DependencyPropertyChangedEventArgs de)
        {
            var owner = (HyperlinkButtonEx)d;
        }

        


        private void UpdateNavigateUrl()
        {
            if (!System.ComponentModel.DesignerProperties.IsInDesignTool)
            {
                if (!String.IsNullOrEmpty(NavigateUrlFormat) && !String.IsNullOrEmpty(NavigateUrlParam))
                {
                    NavigateUri = new Uri(String.Format(NavigateUrlFormat, Uri.EscapeDataString(NavigateUrlParam)), UriKind.RelativeOrAbsolute);
                }
            }
        }

        protected override void OnClick()
        {
            if (NavigateUri != null && NavigateUri.IsAbsoluteUri)
            {
                string scheme = NavigateUri.Scheme.ToLower();

                if (CustomSchemes != null && CustomSchemes.ToLower().Contains(scheme))
                {
                    var e = new CustomSchemeEventArgs(NavigateUri);
                    OnCustomSchemeClick(e);
                    if (e.Handled)
                    {
                        return;
                    }
                }
            }
            base.OnClick();
        }

        protected virtual void OnCustomSchemeClick(CustomSchemeEventArgs e)
        {
            if (CustomSchemeClick != null)
            {
                CustomSchemeClick(this, new CustomSchemeEventArgs(NavigateUri));
            }
        }
    }

   
}
