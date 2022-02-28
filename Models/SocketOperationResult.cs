using System;
using System.Collections.Generic;
using System.Text;

namespace IqApiNetCore.Models
{
    public class SocketOperationResult
    {
        public long id { get; set; }
        public float refund { get; set; }
        public string currency { get; set; }
        public string currency_char { get; set; }
        public long active_id { get; set; }
        public string active { get; set; }
        public decimal value { get; set; }
        public long exp_value { get; set; }
        public string dir { get; set; }
        public long created { get; set; }
        public long expired { get; set; }
        public string type_name { get; set; }
        public string type { get; set; }
        public int profit { get; set; }
        public object profit_amount { get; set; }
        public decimal win_amount { get; set; }
        public decimal loose_amount { get; set; }
        public object sum { get; set; }
        public string win { get; set; }
        public long now { get; set; }
        public long user_id { get; set; }
        public int game_state { get; set; }
        public int profit_income { get; set; }
        public int profit_return { get; set; }
        public int option_type_id { get; set; }
        public int site_id { get; set; }
        public bool is_demo { get; set; }
        public long user_balance_id { get; set; }
        public int platform_id { get; set; }
        public bool rate_finished { get; set; }

    }
}
