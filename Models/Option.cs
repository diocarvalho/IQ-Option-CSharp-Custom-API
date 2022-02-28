using System;
using System.Collections.Generic;

namespace IqApiNetCore.Models
{
    public class Option
    {
        public Profit profit { get; set; }
        public int exp_time { get; set; }
        public int count { get; set; }

        public Dictionary<string, Special> special { get; set; }
        public long startTime { get; set; }
        public int sum { get; set; }
        public bool enabled { get; set; }
        public int deadtime { get; set; }
    }
}
