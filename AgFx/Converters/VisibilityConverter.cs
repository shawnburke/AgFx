// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.

using System;
using System.Collections;
using System.Windows;
using System.Windows.Data;

namespace AgFx.Converters {

    /// <summary>
    /// Converts a value into a Visibility so parts of the UI can be shown/hidden in response to view model values.
    /// 
    /// The parameter can be nothing or "!" which reverses the resulting value.
    /// 
    /// This converter supports bool, numeric values (non-zero is true), and reference types (non-null is true).    
    /// </summary>
    public class VisibilityConverter : IValueConverter
    {

        /// <summary>
        /// Converts a value into a Visibility.
        /// </summary>
        /// <param name="value">The value which can be bool, numeric (non-zero is true), string (non-zero length), or reference (non-null is true).</param>
        /// <param name="targetType">System.Windows.Visibility</param>
        /// <param name="parameter">Passing ! reverses Visible/Collapsed on the return value.</param>
        /// <param name="culture"></param>
        /// <returns>System.Windows.Visibility</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool visible = true;

            if (value is bool)
            {
                visible = (bool)value;
            }
            else if (value is int || value is short || value is long)
            {
                visible = 0 != (int)value;
            }
            else if (value is float || value is double)
            {
                visible = 0.0 != (double)value;
            }
            else if (value is string) {
                visible = ((string)value).Length > 0;
            }
            else if (value as ICollection != null)
            {
                visible = (value as ICollection).Count > 0;
            }
            else if (value == null) {
                visible = false;
            }

            if ((string)parameter == "!")
            {
                visible = !visible;
            }
        
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
