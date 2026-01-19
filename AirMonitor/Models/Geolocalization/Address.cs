using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirMonitor.Models.Geolocalization {
    public class Address {

        public string house_number { get; set; }
        public string village { get; set; }
        public string municipality { get; set; }
        public string county { get; set; }
        public string state { get; set; }
        public string ISO3166_2_lvl4 { get; set; } // zastępujemy "-" "_" bo C# nie pozwala
        public string postcode { get; set; }
        public string country { get; set; }
        public string country_code { get; set; }
    }
}
