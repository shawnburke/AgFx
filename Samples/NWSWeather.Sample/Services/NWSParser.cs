using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NWSWeather.Sample.Services
{

    /// <summary>
    /// Helper class that parses an XML stream into weather objects.
    /// </summary>
    public class NWSParser
    {
        // example url:
        // http://www.weather.gov/forecasts/xml/sample_products/browser_interface/ndfdBrowserClientByDay.php?zipCodeList=98033+99223&format=12+hourly&numDays=7


        /// <summary>
        /// Given a set of zipcodes that were used to make the request, and the response XML stream,
        /// parse it to create a set of WeatherLocation object.
        /// </summary>
        /// <param name="zipcodes">identifiers for the data set, in this case zipcodes</param>
        /// <param name="xmlStream"></param>
        /// <returns></returns>
        public static IEnumerable<WeatherLocation> ParseWeatherXml(IEnumerable<string> zipcodes, Stream xmlStream)
        {
            XElement xml = XElement.Load(xmlStream);

            var dataElement = xml.Element("data");

            if (dataElement == null)
            {
                throw new ArgumentException("Data not available.");
            }

            // get the locations
            //
            var locations = (from l in dataElement.Elements("location")
                            select new WeatherLocation
                            {
                                Key = l.Element("location-key").Value,
                                Latitude = Double.Parse(l.Element("point").Attribute("latitude").Value),
                                Longitude = Double.Parse(l.Element("point").Attribute("longitude").Value),
                            }).ToList();

            var enumerator = zipcodes.GetEnumerator();

            // Match up the zips we pasted in with the data we got back.
            foreach (var l in locations)
            {
                enumerator.MoveNext();
                l.Zipcode = enumerator.Current;
            }

            // build the time maps.  each element has a different time layout, some are 12h, some are 24h.
            // but in either case, they'll match up with the different data items in a given element.  for example,
            // in "probability of precip", it's time map will be 12h, so it's 2 entries per day for 5 days.  the items under
            // "probability of precip" will match up with an equivelent number of time layouts that describe their start and end time.
            //
            Dictionary<string, List<TimeLayoutItem>> timeLookup = new Dictionary<string, List<TimeLayoutItem>>();

            foreach (var timeLayoutElement in dataElement.Elements("time-layout"))
            {
                string key = timeLayoutElement.Element("layout-key").Value;
                List<TimeLayoutItem> items = new List<TimeLayoutItem>();
                timeLookup[key] = items;

                // walk the elements in pairs of two.
                //
                TimeLayoutItem currentItem = null;
                foreach (XElement e in timeLayoutElement.Elements())
                {

                    switch (e.Name.ToString())
                    {
                        case "start-valid-time":
                            if (currentItem != null) throw new InvalidOperationException();

                            currentItem = new TimeLayoutItem();
                            currentItem.Start = DateTime.Parse(e.Value);
                            var pnAttr = e.Attribute("period-name");

                            if (pnAttr != null)
                            {
                                currentItem.Name = pnAttr.Value;
                            }
                            break;
                        case "end-valid-time":

                            if (currentItem == null) throw new InvalidOperationException();
                            currentItem.End = DateTime.Parse(e.Value);
                            items.Add(currentItem);
                            currentItem = null;
                            break;
                    }

                }
            }

            // Now walk through the weather data.  
            //
            foreach (var parms in dataElement.Elements("parameters"))
            {
                WeatherPeriodBuilder wpb = new WeatherPeriodBuilder();

                // proceess temps.
                //
                foreach (var temp in parms.Elements("temperature"))
                {
                    string type = temp.Attribute("type").Value;

                    var timeLayout = timeLookup[temp.Attribute("time-layout").Value];

                    int count = 0;

                    foreach (var tempValue in temp.Elements("value"))
                    {

                        int value;
                        if (Int32.TryParse(tempValue.Value, out value))
                        {
                            // for each value, we grab the correspoinding time layout
                            var timeItem = timeLayout[count++];

                            // we then look up the weather periiod that matches the time layout
                            // and set it's props
                            var wp = wpb.GetWeatherPeriod(timeItem.Start, timeItem.End);

                            if (type == "maximum")
                            {
                                wp.MaxTemperature = value;
                            }
                            else
                            {
                                wp.MinTemperature = value;
                            }

                            wp.Units = temp.Attribute("units").Value;
                        }


                    }
                }

                // precip chances.
                //
                var pp = parms.Element("probability-of-precipitation");
                {

                    var timeLayout = timeLookup[pp.Attribute("time-layout").Value];

                    int count = 0;

                    foreach (var ppValue in pp.Elements("value"))
                    {
                        int value;
                        if (Int32.TryParse(ppValue.Value, out value))
                        {
                            var timeItem = timeLayout[count++];

                            var wp = wpb.GetWeatherPeriod(timeItem.Start, timeItem.End);

                            wp.PrecipChancePercent = value;
                        }
                    }
                }

                // summary.
                //

                var ws = parms.Element("weather");
                {
                    var timeLayout = timeLookup[ws.Attribute("time-layout").Value];

                    int count = 0;

                    foreach (var ppValue in ws.Elements("weather-conditions"))
                    {
                        var timeItem = timeLayout[count++];

                        var wp = wpb.GetWeatherPeriod(timeItem.Start, timeItem.End);

                        var wsAttr = ppValue.Attribute("weather-summary");

                        if (wsAttr != null) {
                            wp.Summary = wsAttr.Value;
                        }
                        wp.Title = timeItem.Name;
                    }
                }

                // icons
                //
                var icons = parms.Element("conditions-icon");
                {
                    var timeLayout = timeLookup[icons.Attribute("time-layout").Value];

                    int count = 0;

                    foreach (var ppValue in icons.Elements("icon-link"))
                    {
                        var timeItem = timeLayout[count++];
                        var wp = wpb.GetWeatherPeriod(timeItem.Start, timeItem.End);

                        if (Uri.IsWellFormedUriString(ppValue.Value, UriKind.Absolute))
                        {
                            wp.WeatherImage = new Uri(ppValue.Value);
                        }
                    }
                }

                // now match up the data with the zipcodes and order
                // by start time.
                WeatherLocation wl = (from l in locations
                                      where l.Key == parms.Attribute("applicable-location").Value
                                      select l).First();

                wl.WeatherPeriods = wpb.Periods.OrderBy(wp => wp.StartTime);
            }
            return locations;
        }

        /// <summary>
        /// Helper class for building weather periods.  Because we get weather data in many different
        /// time layouts, this class tries to map a given time layouts start and end to a weather period.
        /// this allows us to process data then quickly apply it to the right weather period, which acts as a way to 
        /// coallate all the data together for the model to display.
        /// </summary>
        public class WeatherPeriodBuilder
        {

            public List<WeatherPeriod> Periods = new List<WeatherPeriod>();

            public WeatherPeriod GetWeatherPeriod(DateTime start, DateTime end)
            {
                var wp = (from w in Periods
                          where w.StartTime == start && w.EndTime == end
                          select w).FirstOrDefault();

                if (wp == null)
                {
                    wp = new WeatherPeriod();
                    wp.StartTime = start;
                    wp.EndTime = end;
                    Periods.Add(wp);
                }
                return wp;
            }
        }


        public class TimeLayoutItem
        {
            public string Name { get; set; }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
        }
    }
}
