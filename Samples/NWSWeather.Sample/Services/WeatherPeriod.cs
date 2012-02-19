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

namespace NWSWeather.Sample.Services
{
    /// <summary>
    /// The description of the weather during a given period of time.
    /// </summary>
    public class WeatherPeriod
    {
        /// <summary>
        /// Title like "Friday Night"
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The start time of the period, relative to the weather location
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The end time, relative to the weather location
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Something like "Rain Likely"
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// 0-100, chance of precip as a percentage.
        /// </summary>
        public int PrecipChancePercent { get; set; }

        public int? MaxTemperature { get; set; }
        public int? MinTemperature { get; set; }

        /// <summary>
        /// A URI to an image that describes the weather forecast.
        /// </summary>
        public Uri WeatherImage { get; set; }

        /// <summary>
        /// Units of the min/max temperature (C/F)
        /// </summary>
        public string Units { get; set; }
    }
}
