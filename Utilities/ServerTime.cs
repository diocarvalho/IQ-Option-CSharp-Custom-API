using System;
using System.Timers;
using System.Threading.Tasks;
namespace IqApiNetCore.Utilities
{
    //Sync with remote server Time
    public class ServerTime
    {
        private Timer serverTimer = new Timer();
        private DateTime serverDateTime = DateTime.Now;
        public int GMT = 0;
        public TaskCompletionSource<bool> syncTask = new TaskCompletionSource<bool>();
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
            return TimeConverter.FromDateTime(serverDateTime);
        }
        public void SetSetverDateTime(DateTime time)
        {
            serverTimer.Stop();
            serverTimer = null;
            serverTimer = new Timer();
            serverTimer.Elapsed += ServerTimer_Elapsed;
            serverDateTime = time;
            serverTimer.Start();
            syncTask.SetResult(true);
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
                syncTask = new TaskCompletionSource<bool>();
                if (serverTimer != null)
                {
                    serverTimer.Stop();
                }
                DateTime st = TimeConverter.FromTimeStamp(time);
                serverDateTime = st;
                TimeSpan span = DateTime.Now - serverDateTime.AddSeconds(120);
                GMT = span.Hours;
                serverDateTime = serverDateTime.AddHours(GMT);
                serverDateTime = serverDateTime.AddSeconds(1);
                serverTimer = new Timer();
                serverTimer.Elapsed += ServerTimer_Elapsed;
                serverTimer.Interval = 1000;
                serverTimer.Start();
                syncTask.SetResult(true);
                synced = true;
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }

    }
}
