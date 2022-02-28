using System;
using System.Collections.Generic;
using System.Text;

namespace IqApiNetCore.Models
{
    public class Signal
    {
        public int period_in_Seconds;
        public string active_name;
        public decimal buy_on_value;
        public API.BuyDirection buy_direction;
    }
}
