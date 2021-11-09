using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
namespace IqApiNetCore
{
    public enum BalanceType
    {
        [EnumMember(Value = "1")] Real = 1,


        [EnumMember(Value = "4")] Practice = 4,


        [EnumMember(Value = "5")] RealOption = 5,
        Unknow
    }
    public class Balance
    {
        public long id { get; set; }
        public BalanceType type { get; set; }
        public long index { get; set; }
        public decimal amount { get; set; }
        public bool is_fiat { get; set; }
        public string currency { get; set; }
        public decimal new_amount { get; set; }
        public bool is_marginal { get; set; }
        public long bonus_amount { get; set; }
        public double enrolled_amount { get; set; }
        public long bonus_total_amount { get; set; }
        public object description { get; set; }
    }
}
