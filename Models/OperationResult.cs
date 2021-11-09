using System;


namespace IqApiNetCore.Models
{
    public class OperationResult
    {
        public long index { get; set; }
        public double value { get; set; }
        public string active { get; set; }
        public bool is_demo { get; set; }
        public double amount { get; set; }
        public string result { get; set; }
        public double balance { get; set; }
        public string currency { get; set; }
        public long option_id { get; set; }
        public long balance_id { get; set; }
        //public string win_enrolled_amount { get; set; }
        public double win_amount { get; set; }
        public double loose_amount { get; set; }
        public string win { get; set; }
    }
}
