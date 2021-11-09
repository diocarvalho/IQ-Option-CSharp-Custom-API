using System;
using System.Collections.Generic;
using IqApiNetCore.Models;

namespace IqApiNetCore.Utilities
{
    //process returned error message
    public static class ErrorsType
    {
        private static ErrorCode operationExpired = new ErrorCode() { code = 4104, description = "The time to purchase this option is over, please try again." };

        public static ErrorCode ApplyBuyErrorsCode(long code)
        {
            if (code == operationExpired.code)
                return operationExpired;
            else
                return null;
        }
    }
}
