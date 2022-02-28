using System;
using System.Collections.Generic;
using System.Text;

namespace IqApiNetCore.Models
{
    public class BuyInfo
    {        
        public long id { get; set; }
        public string active { get; set; }
        public string dir { get; set; }
        public long created { get; set; }
        public long expired { get; set; }
        public decimal value { get; set; }
        public decimal profit_amount { get; set; }
        public decimal profit_income { get; set; }
    }
}
