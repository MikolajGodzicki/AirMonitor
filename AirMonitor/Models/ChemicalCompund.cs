using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirMonitor.Models {
    public class ChemicalCompund {
        public required string Name { get; set; }
        public required string Unit { get; set; }
        public double Limit { get; set; }
    }
}
