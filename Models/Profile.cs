using System.Collections.Generic;
using System;
using IqApiNetCore.Models;
namespace IqApiNetCore
{
    public class Profile
    {
        public string account_status { get; set; }
        public string avatar { get; set; }
        public long confirmation_required { get; set; }
        public Money money { get; set; }        
        public string user_group { get; set; }
        public long welcome_splash { get; set; }
        public string finance_state { get; set; }
        public decimal balance { get; set; }
        public long bonus_wager { get; set; }
        public long bonus_total_wager { get; set; }
        public long balance_id { get; set; }
        public BalanceType balance_type { get; set; }
        public long messages { get; set; }
        public long id { get; set; }
        public long Demo { get; set; }
        public long group_id { get; set; }
        public string name { get; set; }
        public object nickname { get; set; }
        public string currency { get; set; }
        public string currency_char { get; set; }
        public string mask { get; set; }
        public string city { get; set; }
        public long user_id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public bool last_visit { get; set; }
        public long site_id { get; set; }
        public string tz { get; set; }
        public string locale { get; set; }
        public long birthdate { get; set; }
        public long country_id { get; set; }
        public long currency_id { get; set; }
        public string gender { get; set; }
        public string address { get; set; }
        public string postal_index { get; set; }
        public long timediff { get; set; }
        public long tz_offset { get; set; }
        public Balance[] balances { get; set; }
        public long infeed { get; set; }
        public object[] confirmed_phones { get; set; }
        public bool? need_phone_confirmation { get; set; }
        public bool rate_in_one_click { get; set; }
        public bool deposit_in_one_click { get; set; }
        public bool kyc_confirmed { get; set; }
        public bool trade_restricted { get; set; }
        public object auth_two_factor { get; set; }
        public long deposit_count { get; set; }
        public bool is_activated { get; set; }
        public string new_email { get; set; }
        public bool tc { get; set; }
        public bool trial { get; set; }
        public bool is_islamic { get; set; }
        public string tin { get; set; }
        public object[] tournaments_ids { get; set; }
        public string flag { get; set; }
        public string user_circle { get; set; }
        public long deposit_amount { get; set; }
       
    }

}
