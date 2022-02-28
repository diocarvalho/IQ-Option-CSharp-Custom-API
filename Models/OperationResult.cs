using System;


namespace IqApiNetCore.Models
{
    public class OperationResult
    {
        public long index { get; set; }
        public decimal value { get; set; }
        public decimal amount { get; set; }
        public string result { get; set; }
        public long user_id { get; set; }
        public string currency { get; set; }
        public long active_id { get; set; }
        public string direction { get; set; }
        public long open_time { get; set; }
        public long option_id { get; set; }
        public long balance_id { get; set; }
        public string option_type { get; set; }
        public object actual_expire { get; set; }
        public object profit_amount { get; set; }
        public long user_group_id { get; set; }
        public int option_type_id { get; set; }
        public int profit_percent { get; set; }
        public object enrolled_amount { get; set; }
        public long expiration_time { get; set; }
        public object expiration_value { get; set; }
        public int user_balance_type { get; set; }
        public object win_enrolled_ammount { get; set; }

        public long open_time_millisecond { get; set; }

        public IQMessage<object> errorInfo { get; set; }
    }
}
