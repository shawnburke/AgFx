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

namespace AgFx.Controls
{
    /// <summary>
    /// Derived TextBox class that provides a CurrentText property that will fire a change
    /// after each key up.  This is helpful for TwoWay bindings.
    /// 
    /// The default TextBox only fires changes on focus loss.
    /// </summary>
    public class TextBoxEx : System.Windows.Controls.TextBox
    {
        /// <summary>
        /// The current value for the text field.  This just passes through to Text, but a change
        /// will be raised on each KeyUp. 
        /// </summary>
        public string CurrentText
        {
            get { return (string)GetValue(CurrentTextProperty); }
            set { SetValue(CurrentTextProperty, value); }
        }

        public static readonly DependencyProperty CurrentTextProperty =
            DependencyProperty.Register("CurrentText", typeof(string), typeof(TextBoxEx), new PropertyMetadata(new PropertyChangedCallback(CurrentText_Changed)));


        private static void CurrentText_Changed(DependencyObject d, DependencyPropertyChangedEventArgs de)
        {
            var owner = (TextBoxEx)d;
            owner.Text = (string)de.NewValue;
        }

        protected override void OnKeyUp(System.Windows.Input.KeyEventArgs e)
        {
            base.OnKeyUp(e);
            CurrentText = Text;
        }
    }
}
