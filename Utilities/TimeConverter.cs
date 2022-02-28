using System;

namespace IqApiNetCore
{
    //Used to Timestamp conversion;
    public static class TimeConverter
    {
        
        public static DateTime FromTimeStamp(long timeStamp)
        {
            DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timeStamp).UtcDateTime;
            if (dateTime.Year <= DateTime.UnixEpoch.Year)
            {
                //dt += DateTime.UnixEpoch;
                // Unix timestamp is seconds past epoch
                DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddSeconds((Int64)timeStamp).ToLocalTime();
                return dtDateTime;
            }else
            {
                return dateTime;
            }

        }
        public static DateTime TimeStampToDateTimeAdjusted(long timeStamp)
        {
            DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timeStamp).UtcDateTime;
            if (dateTime.Year <= DateTime.UnixEpoch.Year)
            {
                //dt += DateTime.UnixEpoch;
                // Unix timestamp is seconds past epoch
                DateTime dtDateTime = new DateTime(1970, 1, 1, 8, 0, 0, 0, System.DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddSeconds((Int64)timeStamp).ToLocalTime();
                return dtDateTime;
            }
            else
            {
                return dateTime;
            }

        }
        public static long FromDateTime(DateTime time)
        {
            TimeSpan elapsedTime = time - DateTime.UnixEpoch; 
            return (long)elapsedTime.TotalSeconds;
        }
    }
}
