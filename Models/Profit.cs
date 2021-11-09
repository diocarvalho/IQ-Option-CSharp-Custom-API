using System;

namespace IqApiNetCore.Models
{
    public class Profit
    {
        public int commission { get; set; }
        public int refund_min { get; set; }
        public int refund_max { get; set; }
    }
}
