// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.



using System;
using System.Globalization;
using System.Windows.Data;

namespace AgFx.Converters {
    /// <summary>
    /// Converter for applying a string format to a value.
    /// 
    /// Example usages:
    /// 
    /// Format a value as hex: {Binding Foo, Converter={StaticResource myStringFormatConverter}, Parameter={0:x}}
    /// Format a date time value to just the date {Binding Date, Converter={StaticResource myStringFormatConverter}, Parameter={0:d}}
    /// Format a float to two decimal places {Binding Ratio, Converter={StaticResource myStringFormatConverter}, Parameter={0:0.00}}
    /// </summary>
    public class StringFormatConverter : IValueConverter
    {
        /// <summary>
        /// Converts the given value to the specified format, in parameter.
        /// </summary>
        /// <param name="value"/>
        /// <param name="culture"/>
        /// <param name="targetType"/>
        /// <param name="parameter">A string format specifier like {0:x} or {0:0.00}</param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || (value is string && ((string)value).Length == 0)) {
                return "";
            }
            
            if (culture == null)
            {
                culture = CultureInfo.CurrentCulture;
            }

            return String.Format(culture, (string)parameter, value);
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
