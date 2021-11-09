using System;
namespace IqApiNetCore.Models
{
    public class Money
    {
        public Deposit deposit { get; set; }
        public Deposit withdraw { get; set; }
    }
}
