using System;
using System.Collections.Generic;
using System.Text;

namespace IqApiNetCore.Models
{
    public class Schedule
    {
        public long StartDate { get; set; }
        public long EndDate { get; set; }
        public Schedule(long StartDate, long EndDate)
        {
            this.StartDate = StartDate;
            this.EndDate = EndDate;
        }
    }
}
