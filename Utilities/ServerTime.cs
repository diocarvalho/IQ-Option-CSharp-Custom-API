using System;
using System.Timers;
namespace IqApiNetCore.Utilities
{
    //Sync with remote server Time
    public class ServerTime
    {
        private Timer serverTimer = new Timer();
        private DateTime serverDateTime = DateTime.Now;
        public int GMT = 0;
        public bool synced = false;
        public DateTime GetLocalServerDateTime()
        {
            return serverDateTime;
        }
        public DateTime GetRealServerDateTime()
        {
            DateTime temp = serverDateTime.AddHours(-GMT);
            return temp;
        }
        public long GetServerTimeStamp()
        {
            return TimeStamp.FromDateTime(serverDateTime);
        }
        public void SetSetverDateTime(DateTime time)
        {
            serverTimer.Stop();
            serverTimer = null;
            serverTimer = new Timer();
            serverTimer.Elapsed += ServerTimer_Elapsed;
            serverDateTime = time;
            serverTimer.Start();
            synced = true;
        }

        private void ServerTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            serverDateTime = serverDateTime.AddSeconds(1);
        }

        public bool SetServerTimeStamp(long time)
        {
            try
            {
                if (serverTimer != null)
                {
                    serverTimer.Stop();
                }
                DateTime st = TimeStamp.FromTimeStamp(time);
                serverDateTime = st;
                TimeSpan span = DateTime.Now - serverDateTime.AddSeconds(120);
                GMT = span.Hours;
                serverDateTime = serverDateTime.AddHours(GMT);
                serverDateTime = serverDateTime.AddSeconds(1);
                serverTimer = new Timer();
                serverTimer.Elapsed += ServerTimer_Elapsed;
                serverTimer.Interval = 1000;
                serverTimer.Start();
                synced = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
