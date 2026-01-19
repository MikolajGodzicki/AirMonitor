using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirMonitor.Models {
    public class ChemicalMeasurement {
        public ChemicalCompund ChemicalCompund { get; set; }
        public double Value { get; set; }
        public bool IsExceeded {
            get {
                return Value > ChemicalCompund.Limit;
            }
        }
    }
}
