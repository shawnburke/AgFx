
// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.


using AgFx;

namespace NWSWeather.Sample.ViewModels {
    
    /// <summary>
    /// Load Context for something with just a zip code.  This doesn't add much value (no parameters, etc.)
    /// so we could easily just use the base LoadContext, but we're doing this for clarity.
    /// </summary>    
    public class ZipCodeLoadContext : LoadContext {
        public string ZipCode {
            get {
                return (string)Identity;
            }            
        }

        public ZipCodeLoadContext(string zipcode)
            : base(zipcode) {

        }
    }
}
