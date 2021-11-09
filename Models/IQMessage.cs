using System;
using System.Collections.Generic;
using System.Text;

namespace IqApiNetCore.Models
{
    //default received message
    public class IQMessage<T>
    {
        public string name { get; set; }
        public object msg { get; set; }
        public int status { get; set; }
        public string version { get; set; }
        public string microserviceName { get; set; }

    }
}
