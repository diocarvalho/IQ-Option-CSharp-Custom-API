using System;
namespace IqApiNetCore.Models
{
    public class Candle
    {
        public decimal min { get; set; }
        public decimal max { get; set; }
        public decimal open { get; set; }
        public decimal close { get; set; }
        public long from { get; set; }
        public long to { get; set; }
        public decimal volume { get; set; }
        public decimal dir { get; set; }
        public DateTime fromDateTime { get; set; }
        public DateTime toDateTime { get; set; }
    }
}
