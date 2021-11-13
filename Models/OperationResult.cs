using System;


namespace IqApiNetCore.Models
{
    public class OperationResult
    {
        public long index { get; set; }
        public decimal value { get; set; }
        public string active { get; set; }
        public bool is_demo { get; set; }
        public decimal amount { get; set; }
        public string result { get; set; }
        public decimal balance { get; set; }
        public string currency { get; set; }
        public long option_id { get; set; }
        public long balance_id { get; set; }
        //public string win_enrolled_amount { get; set; }
        public decimal win_amount { get; set; }
        public decimal loose_amount { get; set; }
        public string win { get; set; }
    }
}
