using System;
using System.Collections.Generic;
using System.Text;

namespace IqApiNetCore.Models
{
    public class DefaultMessage
    {
        public bool isSuccessful { get; set; }
        public int statusCode { get; set; }
        public Result result { get; set; }
        public Candle[] candles { get; set; }
        public Profile profile { get; set; }
        public List<Balance> balances { get; set; }
    }
}
