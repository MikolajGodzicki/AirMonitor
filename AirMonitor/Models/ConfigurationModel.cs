using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirMonitor.Models {
    internal class ConfigurationModel {
        public double HCHO_Limit { get; set; } = 0.0;
        public double HCL_Limit { get; set; } = 0.0;
        public double H2S_Limit { get; set; } = 0.0;
        public double NH3_Limit { get; set; } = 0.0;
        public double PM1_Limit { get; set; } = 0.0;
        public double PM2_5_Limit { get; set; } = 0.0;
        public double PM10_Limit { get; set; } = 0.0;
    }
}
