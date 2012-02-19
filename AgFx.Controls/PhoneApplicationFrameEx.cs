using System;
using System.Windows;
using Microsoft.Phone.Controls;
using System.Windows.Data;


namespace AgFx.Controls
{

    /// <summary>
    /// Provides replacement for the stock PhoneApplicationFrame that supports a
    /// header area and a popup.
    /// 
    /// The header is at the top of the screen and is good for a loading animation.
    /// 
    /// The popoup, when IsPopupVisible is set will cover the existing content and is good for a login dialog or other
    /// UI that doesn't require a page transition.
    /// 
    /// </summary>
    [TemplateVisualState(Name="Normal", GroupName="Popup")]
    [TemplateVisualState(Name = "PopupVisible", GroupName = "Popup")]    
	public class PhoneApplicationFrameEx : PhoneApplicationFrame
	{
        

        public object HeaderContent
        {
            get { return (object)GetValue(HeaderContentProperty); }
            set { SetValue(HeaderContentProperty, value); }
        }
                
        public static readonly DependencyProperty HeaderContentProperty =
            DependencyProperty.Register("HeaderContent", typeof(object), typeof(PhoneApplicationFrameEx), new PropertyMetadata(new PropertyChangedCallback(HeaderContent_Changed)));


        private static void HeaderContent_Changed(DependencyObject d, DependencyPropertyChangedEventArgs de)
        {
            var owner = (PhoneApplicationFrameEx)d;
        }



        public DataTemplate HeaderTemplate {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HeaderTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderTemplateProperty =
            DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(PhoneApplicationFrameEx), new PropertyMetadata(new PropertyChangedCallback(HeaderTemplate_Changed)));


        private static void HeaderTemplate_Changed(DependencyObject d, DependencyPropertyChangedEventArgs de) {
            var owner = (PhoneApplicationFrameEx)d;
        }

        
        

        public object PopupContent
        {
            get { return (object)GetValue(PopupContentProperty); }
            set { SetValue(PopupContentProperty, value); }
        }

        
        public static readonly DependencyProperty PopupContentProperty =
            DependencyProperty.Register("PopupContent", typeof(object), typeof(PhoneApplicationFrameEx), new PropertyMetadata(null));



        public bool IsPopupVisible
        {
            get { return (bool)GetValue(IsPopupVisibleProperty); }
            set { SetValue(IsPopupVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsPopupVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsPopupVisibleProperty =
            DependencyProperty.Register("IsPopupVisible", typeof(bool), typeof(PhoneApplicationFrameEx), new PropertyMetadata(false, new PropertyChangedCallback(IsPopupVisible_Changed)));


        private static void IsPopupVisible_Changed(DependencyObject d, DependencyPropertyChangedEventArgs de)
        {
            var owner = (PhoneApplicationFrameEx)d;
            owner.UpdateState();
        }

		public PhoneApplicationFrameEx()
		{
            DefaultStyleKey = typeof(PhoneApplicationFrameEx);
            LayoutUpdated += PhoneApplicationFrameEx_LayoutUpdated;
		}
        
        bool _runAnimations;

        void PhoneApplicationFrameEx_LayoutUpdated(object sender, EventArgs e)
        {
            _runAnimations = true;
            LayoutUpdated -= PhoneApplicationFrameEx_LayoutUpdated;
        }

        public override void OnApplyTemplate()
        {
            UpdateState();
            base.OnApplyTemplate();
        }

        private void UpdateState()
        {
            if (IsPopupVisible)
            {
                VisualStateManager.GoToState(this, "PopupVisible", _runAnimations);
            }
            else
            {
                VisualStateManager.GoToState(this, "Normal", _runAnimations);
            }
        }       
	}
}