using System;
using System.Collections.Generic;
using System.Text;
namespace IqApiNetCore.Models
{
    public class BollingerBand
    {
        public List<decimal?> LowerBand { get; set; }
        public List<decimal?> MidBand { get; set; }
        public List<decimal?> UpperBand { get; set; }
        public List<decimal?> BandWidth { get; set; }
        public List<decimal?> BPercent { get; set; }
        public BollingerBand()
        {
            LowerBand = new List<decimal?>();
            MidBand = new List<decimal?>();
            UpperBand = new List<decimal?>();
            BandWidth = new List<decimal?>();
            BPercent = new List<decimal?>();
        }
        
    }
}
