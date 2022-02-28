using System;
using System.Collections.Generic;
using System.Text;

namespace IqApiNetCore.Models
{
    public class OperationState
    {
        public enum OPState
        {
            Opened,
            Win,
            Loose,
            Draw
        }
        public OPState state = OPState.Opened;
        public decimal valueReturned = 0;
        public long contractId;
    }
}
