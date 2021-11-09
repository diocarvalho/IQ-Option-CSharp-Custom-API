using System;
using System.Collections.Generic;
using System.Text;

namespace IqApiNetCore.Models
{
    //message received with login data
    public class LoginMessage
    {
        public string code { get; set; }
        public string ssid { get; set; }
    }
}
