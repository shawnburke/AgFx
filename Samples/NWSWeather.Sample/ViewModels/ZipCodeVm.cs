// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.


using System;
using AgFx;
using System.Xml.Linq;
using System.Linq;

namespace NWSWeather.Sample.ViewModels
{

    /// <summary>
    /// Our wrapper around fetching location information for a zipcode - city, state, etc.
    /// </summary>
    [CachePolicy(CachePolicy.Forever)] 
    public class ZipCodeVm : ModelItemBase<ZipCodeLoadContext>
    {
        public ZipCodeVm()
        {

        }
        
        public ZipCodeVm(string zipcode)
            : base(new ZipCodeLoadContext(zipcode))
        {

        }
                
        #region Property City
        private string _City;
        public string City
        {
            get
            {
                return _City;
            }
            set
            {
                if (_City != value)
                {
                    _City = value;
                    RaisePropertyChanged("City");
                }
            }
        }
        #endregion

        #region Property State
        private string _State;
        public string State
        {
            get
            {
                return _State;
            }
            set
            {
                if (_State != value)
                {
                    _State = value;
                    RaisePropertyChanged("State");
                }
            }
        }
        #endregion

        #region Property AreaCode
        private string _AreaCode;
        public string AreaCode
        {
            get
            {
                return _AreaCode;
            }
            set
            {
                if (_AreaCode != value)
                {
                    _AreaCode = value;
                    RaisePropertyChanged("AreaCode");
                }
            }
        }
        #endregion

        #region Property TimeZone
        private string _TimeZone;
        public string TimeZone
        {
            get
            {
                return _TimeZone;
            }
            set
            {
                if (_TimeZone != value)
                {
                    _TimeZone = value;
                    RaisePropertyChanged("TimeZone");
                }
            }
        }
        #endregion

        /// <summary>
        /// Loaders know how to do two things:
        /// 
        /// 1. Request new data
        /// 2. Process the response of that request into an object of the containing type (ZipCodeVm in this case)
        /// </summary>
        public class ZipCodeVmDataLoader : IDataLoader<ZipCodeLoadContext>
        {
            private const string ZipCodeUriFormat = "http://www.webservicex.net/uszip.asmx/GetInfoByZIP?USZip={0}";

            public LoadRequest GetLoadRequest(ZipCodeLoadContext loadContext, Type objectType)
            {
                // build the URI, return a WebLoadRequest.
                string uri = String.Format(ZipCodeUriFormat, loadContext.ZipCode);
                return new WebLoadRequest(loadContext, new Uri(uri));
            }

            public object Deserialize(ZipCodeLoadContext loadContext, Type objectType, System.IO.Stream stream)
            {

                // the XML will look like hte following, so we parse it.

                //<?xml version="1.0" encoding="utf-8"?>
                //<NewDataSet>
                //  <Table>
                //    <CITY>Kirkland</CITY>
                //    <STATE>WA</STATE>
                //    <ZIP>98033</ZIP>
                //    <AREA_CODE>425</AREA_CODE>
                //    <TIME_ZONE>P</TIME_ZONE>
                //  </Table>
                //</NewDataSet>                

                var xml = XElement.Load(stream);


                var table = (
                            from t in xml.Elements("Table")
                            select t).FirstOrDefault();

                if (table == null) {
                    throw new ArgumentException("Unknown zipcode " + loadContext.ZipCode);
                }

                ZipCodeVm vm = new ZipCodeVm(loadContext.ZipCode);
                vm.City = table.Element("CITY").Value;
                vm.State = table.Element("STATE").Value;
                vm.AreaCode = table.Element("AREA_CODE").Value;
                vm.TimeZone = table.Element("TIME_ZONE").Value;
                return vm;
            }
        }
        

        
        

        

        
    }
}
