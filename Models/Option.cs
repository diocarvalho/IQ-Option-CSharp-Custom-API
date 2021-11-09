using System;

namespace IqApiNetCore.Models
{
    public class Option
    {
        public Profit profit { get; set; }
        public int exp_time { get; set; }
        public BetTime[] BetCloseTime { get; set; }
        public BetTime[] Special { get; set; }
    }
}
