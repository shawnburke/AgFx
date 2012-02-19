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
using System.Collections.Generic;

namespace NWSWeather.Sample.Services
{
    /// <summary>
    /// Describes the weather for a given location.
    /// </summary>
    public class WeatherLocation
    {
        // the ID for this weather location
        public string Zipcode { get; set; }

        // the location key in the returned data.
        public string Key { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // a URL for the full weather info page.
        public Uri InformationUrl { get; set; }

        /// <summary>
        /// The list of weather periods to display.
        /// </summary>
        public IEnumerable<WeatherPeriod> WeatherPeriods { get; set; }

    }
}
