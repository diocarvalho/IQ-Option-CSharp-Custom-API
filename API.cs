using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using IqApiNetCore.Models;
using IqApiNetCore.Utilities;
using System.Text.Json;
using System.Collections.Specialized;
namespace IqApiNetCore
{    
    public class Message
    {
        public string id { get; set; }
        public string message { get; set; }
    }
    public class Response
    {
        public object id { get; set; }
    }
    public class API
    {
        //used to show console if debbuging;
        //variable used to received account profile data
        public Profile profile;
        //if true enables logging all message received (excludes timesync heartbeat)
        public bool debugging;
        //Class with synced time with remoteServer
        public ServerTime serverTime;
        //Used to show log
        public Logger logger;
        //socket used to process WebSocket conn
        private WebSocketClient webSocketClient;
        //socket used to process http conn;
        private WebClient httpWebClient;
        //account ssid, used to auth with webSocket
        private string ssid;
        //tasker used to process requests and responses
        private ConcurrentDictionary<string, TaskCompletionSource<Response>> tasker;
        //used to check if time are synced with servers
        public API(bool debug, bool showConsole)
        {
            tasker = new ConcurrentDictionary<string, TaskCompletionSource<Response>>();
            serverTime = new ServerTime();
            debugging = debug;
            logger = new Logger(showConsole);
        }
        public API()
        {
            tasker = new ConcurrentDictionary<string, TaskCompletionSource<Response>>();
            serverTime = new ServerTime();
            debugging = false;
            logger = new Logger(false);
        }
        //default options to buy
        public enum BuyDirection
        {
            put,
            call,
            draw
        }    
        void InitHTTP()
        {
            httpWebClient = new WebClient();
        }
        
        void InitWebSocket()
        {
            webSocketClient = new WebSocketClient();

        }
        public async Task<bool> ConnectAsync(string username, string password)
        {
            //create  a taskCompletionSouce to receive result for main tasker
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            InitHTTP();
            InitWebSocket();
            //firs connect with HTTP using username and password
            await SendHTTPRequestAsync(username, password).ContinueWith(async t =>
            {
                LoginMessage r = t.Result;
                if (r.code == "success")
                {
                    //after auth get Account/Conn ssid code
                    ssid = r.ssid;
                    try
                    {
                        //add listener funct to process all messages received
                        webSocketClient.OnMessageReceived += MessageListener;
                        await webSocketClient.ConnectAsync("wss://iqoption.com/echo/websocket");
                        //use ssid to request profile info
                        profile = await GetProfileAsync();
                        logger.Log("Connected");
                        logger.Log("Waiting Time sync");

                        while (!serverTime.synced)
                        {
                            Thread.Sleep(100);
                        }
                        tcs.TrySetResult(true); 

                    }
                    catch (Exception err)
                    {
                        logger.Log("Failed to connect webSocket server, error: " + err.ToString());
                        tcs.TrySetResult(false);
                    }
                }
                else
                {
                    logger.Log("Failed to connect HTTP server, error: " + r.code);
                    tcs.TrySetResult(false);
                }
            });
            return tcs.Task.Result;         
        }       

        async Task<LoginMessage> SendHTTPRequestAsync(string username, string password)
        {
            var values = new NameValueCollection();
            values["identifier"] = username;
            values["password"] = password;
            var response = await httpWebClient.UploadValuesTaskAsync("https://auth.iqoption.com/api/v2/login", values);

            var responseString = Encoding.Default.GetString(response);
            try
            {
                return JsonSerializer.Deserialize<LoginMessage>(responseString);
            }
            catch
            {
                return new LoginMessage() { code = responseString };
            }
        }
      
        public bool SendWebSocketMessage(string msg)
        {
            if (debugging)
            {
                logger.Log("Sending message: " + msg + "\n");
                Console.WriteLine();
            }
            try
            {
                webSocketClient.SendMessageAsync(msg);
                return true;
            }catch
            {
                return false;
            }
        }   
        
        //This function process all message received
        void MessageListener(object data, EventArgs e)
        {
            try
            {
                //Receive message and deserialize
                IQMessage<object> msg = JsonSerializer.Deserialize<IQMessage<object>>(data.ToString());

                if (debugging)
                {
                    logger.Log("Message received: " + msg.name + " | " + msg.status + "\n" + msg.msg.ToString() + "\n");
                    Console.WriteLine();
                }
                
                TaskCompletionSource<Response> tcs;
                switch (msg.name)
                {
                    case MessageType.Heartbeat:
                        string heartBeatMessage = "{\"request_id\":\"2\",\"name\":\"heartbeat\",\"msg\":{\"userTime\":1617500473,\"heartbeatTime\":1617500453},\"microserviceName\":null,\"version\":\"1.0\",\"status\":0}";
                        SendWebSocketMessage(heartBeatMessage);
                        break;

                    case MessageType.TimeSync:
                        serverTime.SetServerTimeStamp(long.Parse(msg.msg.ToString()));
                        break;
                    case MessageType.Profile:
                        if (tasker.TryGetValue("GetProfile", out tcs))
                        {
                            Profile profile = JsonSerializer.Deserialize<Profile>(msg.msg.ToString());
                            tcs.SetResult(new Response { id = profile });
                        }
                        break;
                    case MessageType.Balances:
                        if (tasker.TryGetValue("GetBalances", out tcs))
                        {
                            Balance[] b = JsonSerializer.Deserialize<Balance[]>(msg.msg.ToString());
                            tcs.SetResult(new Response { id = b });
                        }
                        break;
                    case MessageType.Front:
                        break;
                    case MessageType.GetAllProfit:
                        if (tasker.TryGetValue("GetActiveOptionDetail", out tcs))
                        {
                            tcs.SetResult(new Response { id = msg.msg.ToString() });
                        }

                        break;
                    case MessageType.Candles:
                        if (tasker.TryGetValue("GetCandles", out tcs))
                        {
                            tcs.SetResult(new Response { id = msg.msg.ToString() });
                        }   
                        break;
                    
                    case MessageType.PlacedBinaryOptions:
                        if (tasker.TryGetValue("Buy", out tcs))
                        {
                            tcs.SetResult(new Response { id = msg.status.ToString() });
                        }
                        break;
                    case MessageType.SocketOptionOpened:
                        JsonDocument confirmSSearch = JsonDocument.Parse(msg.msg.ToString());
                        var conSfr = confirmSSearch.RootElement.GetProperty("id");
                        long buySId = long.Parse(conSfr.ToString());
                        if (tasker.TryGetValue("Buy", out tcs))
                        {
                            if(buySId == 0)
                            {

                            }
                            tcs.SetResult(new Response { id = buySId.ToString() });
                        }
                        break;
                    case MessageType.OptionOpened:
                        JsonDocument confirmSearch = JsonDocument.Parse(msg.msg.ToString());
                        var confr = confirmSearch.RootElement.GetProperty("option_id");
                        long buyId = long.Parse(confr.ToString());
                        if (tasker.TryGetValue("Buy", out tcs))
                        {
                            tcs.SetResult(new Response { id = buyId.ToString() });
                        }
                        break;
                    case MessageType.SocketOptionClosed:
                        OperationResult rs = JsonSerializer.Deserialize<OperationResult>(msg.msg.ToString());
                            JsonDocument opSSearch = JsonDocument.Parse(msg.msg.ToString());
                            var opSch = opSSearch.RootElement.GetProperty("id");
                            long opSchId = long.Parse(opSch.ToString());
                        if (tasker.TryGetValue("CheckWin:" + opSch, out tcs))
                        {
                            OperationResult or = JsonSerializer.Deserialize<OperationResult>(msg.msg.ToString());
                            tcs.SetResult(new Response { id = or });
                        }
                        break;
                    case MessageType.OptionChanged:
                        OperationResult r = JsonSerializer.Deserialize<OperationResult>(msg.msg.ToString());
                        if (r.result == "win" || r.result == "loose" || r.result == "equal")
                        {
                            JsonDocument opSearch = JsonDocument.Parse(msg.msg.ToString());
                            var opch = opSearch.RootElement.GetProperty("option_id");
                            long opchId = long.Parse(opch.ToString());
                            if (tasker.TryGetValue("CheckWin:" + opch, out tcs))
                            {
                                OperationResult or = JsonSerializer.Deserialize<OperationResult>(msg.msg.ToString());
                                tcs.SetResult(new Response { id = or });
                            }
                        }
                        break;

                    case MessageType.OptionArchived:
                        break;
                    case MessageType.Quotes:
                        JsonDocument rCandleSearch = JsonDocument.Parse(msg.msg.ToString());
                        var c = rCandleSearch.RootElement.GetProperty("active_id");
                        long rcId = long.Parse(c.ToString());
                        if (tasker.TryGetValue("RealTimeCandle:"+ rcId, out tcs))
                        {
                            if(tcs.Task.Status == TaskStatus.WaitingForActivation)
                            {
                                Candle candle = JsonSerializer.Deserialize<Candle>(msg.msg.ToString());
                                tcs.SetResult(new Response { id = candle });
                            }
                            break;
                        }
                        break;
                    case MessageType.BalanceChanged:
                        var bal = JsonDocument.Parse(msg.msg.ToString());
                        var currBal = bal.RootElement.GetProperty("current_balance");
                        
                        Balance currentBalance = JsonSerializer.Deserialize<Balance>(currBal.ToString());
                        for (int z = 0; z < profile.balances.Length; z++)
                        {
                            if (profile.balances[z].id == currentBalance.id)
                            {
                                profile.balances[z] = currentBalance;
                                if (profile.balance_id == profile.balances[z].id)
                                    profile.balance = profile.balances[z].amount;
                            }
                        }
                        foreach (Balance ba in profile.balances)
                        {
                            if (ba.id == currentBalance.id)
                                profile.balance = ba.amount;
                        }
                        break;

                    default:
                        if (debugging)
                        {
                            logger.Log("Unknown message: " + msg.name + " | " + msg.status + " | " + msg.msg.ToString());
                            Console.WriteLine();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                if (debugging)
                {
                    logger.Log("error: " + ex.Message + ", DATE:" + data);
                    Console.WriteLine();
                }
            }
        }

        Task<Profile> GetProfileAsync()
        {
            TaskCompletionSource<Response> tcs = new TaskCompletionSource<Response>();
            //tcs with receive profile and return
            TaskCompletionSource<Profile> tcs2 = new TaskCompletionSource<Profile>();
            Message msg = new Message { id = "GetProfile", message = "{\"request_id\":\"1\",\"name\":\"ssid\",\"msg\":\"" + ssid + "\",\"microserviceName\":null,\"version\":\"1.0\",\"status\":0}" };

            SendWebSocketMessage(msg.message);

            if (!tasker.TryAdd(msg.id, tcs))
                tcs2.TrySetCanceled();
            tcs.Task.ContinueWith(t =>
            {
                profile = (Profile)(tcs.Task.Result.id);
                tcs2.TrySetResult(profile);
            });
            return tcs2.Task;
        }
        //Change balance account type(demo, real, etc)
        public Task<bool> ChangeBalanceAsync(BalanceType balanceType)
        {
            var tcs = new TaskCompletionSource<bool>();
            logger.Log("Changing account type to: " + balanceType.ToString());
            //change balance id, used to perform buy
            long newBalanceId = 0;
            bool haveType = false;
            for (int i = 0; i < profile.balances.Length; i++)
            {
                if (profile.balances[i].type == balanceType)
                {
                    newBalanceId = profile.balances[i].id;
                    haveType = true;
                    break;
                }
            }
            if (!haveType)
            {
                logger.Log("Failed to change balance type,selected type does not exists");
                tcs.TrySetResult(false);
            }
            else
            {
                //informative message only, no response needed
                SendWebSocketMessage("{\"name\": \"unsubscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"cfd\", \"user_balance_id\":" + profile.balance_id + "}}}, \"request_id\":\"1\"");
                SendWebSocketMessage("{\"name\": \"unsubscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"forex\", \"user_balance_id\": " + profile.balance_id + "}}}, \"request_id\":\"1\"");
                SendWebSocketMessage("{\"name\": \"unsubscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"crypto\", \"user_balance_id\": " + profile.balance_id + "}}}, \"request_id\":\"1\"");
                SendWebSocketMessage("{\"name\": \"unsubscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"digital-option\", \"user_balance_id\": " + profile.balance_id + "}}}, \"request_id\":\"1\"");
                SendWebSocketMessage("{\"name\": \"unsubscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"turbo-option\", \"user_balance_id\": " + profile.balance_id + "}}}, \"request_id\":\"1\"");
                SendWebSocketMessage("{\"name\": \"unsubscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"binary-option\", \"user_balance_id\": " + profile.balance_id + "}}}, \"request_id\":\"1\"");

                SendWebSocketMessage("{\"name\": \"subscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"cfd\", \"user_balance_id\":" + newBalanceId + "}}}, \"request_id\":\"1\"");
                SendWebSocketMessage("{\"name\": \"subscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"forex\", \"user_balance_id\":" + newBalanceId + "}}}, \"request_id\":\"1\"");
                SendWebSocketMessage("{\"name\": \"subscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"crypto\", \"user_balance_id\":" + newBalanceId + "}}}, \"request_id\":\"1\"");
                SendWebSocketMessage("{\"name\": \"subscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"digital-option\", \"user_balance_id\":" + newBalanceId + "}}}, \"request_id\":\"1\"");
                SendWebSocketMessage("{\"name\": \"subscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"turbo-option\", \"user_balance_id\":" + newBalanceId + "}}}, \"request_id\":\"1\"");
                SendWebSocketMessage("{\"name\": \"subscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"binary-option\", \"user_balance_id\":" + newBalanceId + "}}}, \"request_id\":\"1\"");
                profile.balance_id = newBalanceId;
                logger.Log("Balance type changed.");
                tcs.TrySetResult(true);
            }
            return tcs.Task;
        }

        //Get Account balance (value)
        public Task<decimal> GetCurrentBalanceAsync()
        {
            var tcs = new TaskCompletionSource<Response>();
            var tcs2 = new TaskCompletionSource<decimal>();
            Message msg = new Message { id = "GetBalances", message = "{\"name\": \"sendMessage\", \"msg\": {\"name\": \"get-balances\", \"version\": \"1.0\"}, \"request_id\": \"\"}" };

            SendWebSocketMessage(msg.message);
            if (!tasker.TryAdd(msg.id, tcs))
                tcs2.TrySetCanceled();
            tcs.Task.ContinueWith(t =>
            {
                profile.balances = (Balance[])(tcs.Task.Result.id);
                tasker.TryRemove(msg.id, out TaskCompletionSource<Response> resp);
                foreach (Balance b in profile.balances)
                {
                    if (b.id == profile.balance_id)
                        profile.balance = b.amount;
                }
                tcs2.SetResult(profile.balance);

            });
            return tcs2.Task;
        }

        //Get Active list, binary and turbo
        public  Task<(List<Active>, List<Active>)> GetActiveOptionDetailAsync()
        {
            var tcs = new TaskCompletionSource<Response>();
            var tcs2 = new TaskCompletionSource<(List<Active>, List<Active>)>();
            Message msg = new Message { id = "GetActiveOptionDetail", message = "{\"name\": \"api_option_init_all\", \"msg\": \"\", \"request_id\": \"\"}" };
            SendWebSocketMessage(msg.message);
            if (!tasker.TryAdd(msg.id, tcs))
                tcs2.TrySetCanceled();
            tcs.Task.ContinueWith(t =>
            {
                var result = tcs.Task.Result; 
                tasker.TryRemove(msg.id, out TaskCompletionSource<Response> resp);
                List<Active> bActives = new List<Active>();
                List<Active> tActives = new List<Active>();

                var j1 = JsonDocument.Parse(result.id.ToString());
                var actsBin = j1.RootElement.GetProperty("result").GetProperty("binary").GetProperty("actives");
                foreach (var c in actsBin.EnumerateObject())
                {
                    var act = actsBin.GetProperty(c.Name);
                    Active active = JsonSerializer.Deserialize<Active>(act.ToString());
                    List<BetTime> betsTime = new List<BetTime>();
                    try
                    {
                        var bets = act.GetProperty("option").GetProperty("bet_close_time");
                        foreach (var b in bets.EnumerateObject())
                        {
                            var bt = bets.GetProperty(b.Name);
                            BetTime bet = JsonSerializer.Deserialize<BetTime>(bt.ToString());
                            bet.Date = long.Parse(b.Name);
                            betsTime.Add(bet);
                        }
                    }catch
                    {   }

                    var spec = act.GetProperty("option").GetProperty("special");
                    List<BetTime> specials = new List<BetTime>();
                    try
                    {
                        foreach (var b in spec.EnumerateObject())
                        {
                            var bt = spec.GetProperty(b.Name);
                            BetTime bet = JsonSerializer.Deserialize<BetTime>(bt.ToString());
                            bet.Date = long.Parse(b.Name);
                            specials.Add(bet);
                        }
                    }
                    catch { }
                    var sch = act.GetProperty("schedule");
                    List<Schedule> schedules = new List<Schedule>();
                    foreach (var sc in sch.EnumerateArray())
                    {
                        schedules.Add(new Schedule(int.Parse(sc[0].ToString()), int.Parse(sc[1].ToString())));
                    }
                    active.option.BetCloseTime = betsTime.ToArray();
                    active.option.Special = specials.ToArray();
                    active.Schedules = schedules.ToArray();
                    bActives.Add(active);
                }
                var actsTurbo = j1.RootElement.GetProperty("result").GetProperty("turbo").GetProperty("actives");
                foreach (var c in actsTurbo.EnumerateObject())
                {
                    var act = actsTurbo.GetProperty(c.Name);
                    Active active = JsonSerializer.Deserialize<Active>(act.ToString());
                    List<BetTime> betsTime = new List<BetTime>();
                    try
                    {
                        var bets = act.GetProperty("option").GetProperty("bet_close_time");
                        foreach (var b in bets.EnumerateObject())
                        {
                            var bt = bets.GetProperty(b.Name);
                            BetTime bet = JsonSerializer.Deserialize<BetTime>(bt.ToString());
                            //string[] pt = b.Path.Split(".");
                            bet.Date = long.Parse(b.Name);
                            betsTime.Add(bet);
                        }
                    }catch
                    {

                    }
                    var spec = act.GetProperty("option").GetProperty("special");
                    List<BetTime> specials = new List<BetTime>();
                    try
                    {
                        foreach (var b in spec.EnumerateObject())
                        {
                            var bt = spec.GetProperty(b.Name);
                            BetTime bet = JsonSerializer.Deserialize<BetTime>(bt.ToString());
                            //string[] pt = b.Path.Split(".");
                            bet.Date = long.Parse(b.Name);
                            specials.Add(bet);
                        }
                    }
                    catch { }
                    var sch = act.GetProperty("schedule");
                    List<Schedule> schedules = new List<Schedule>();
                    foreach (var sc in sch.EnumerateArray())
                    {
                        schedules.Add(new Schedule(int.Parse(sc[0].ToString()), int.Parse(sc[1].ToString())));
                    }
                    active.option.BetCloseTime = betsTime.ToArray();
                    active.option.Special = specials.ToArray();
                    active.Schedules = schedules.ToArray();
                    tActives.Add(active);
                }
                tcs2.TrySetResult((bActives, tActives));
            });
            return tcs2.Task;
        }
        //Get a list with candles from selected acive, period in seconds, date of most recent candle and candle total
        public Task<List<Candle>> GetCandlesAsync(Active active, int periodInSeconds, long lastCandleCloseDate, int candleCount)
        {
            var tcs = new TaskCompletionSource<Response>();
            var tcs2 = new TaskCompletionSource<List<Candle>>();
            Message msg = new Message { id = "GetCandles", message = "{\"name\": \"sendMessage\", \"msg\": {\"name\": \"get-candles\", \"version\": \"2.0\", \"body\": {\"active_id\": " + active.id + ", \"size\": " + periodInSeconds + ", \"to\": " + lastCandleCloseDate + ", \"count\": " + (candleCount + 1) + ", \"\": 80}}, \"request_id\": \"\"}" };
            int count = 0;
            while (!tasker.TryAdd(msg.id, tcs))
            {
                if (count < 10)
                {
                    Thread.Sleep(1000);
                    count++;
                }
                else
                {
                    tasker.TryRemove(msg.id, out TaskCompletionSource<Response> resp);
                }
            }
            SendWebSocketMessage(msg.message);
            tcs.Task.ContinueWith(t =>
            {
                tasker.TryRemove(msg.id, out TaskCompletionSource<Response> resp);
                List<Candle> candles = new List<Candle>();

                var jd = JsonDocument.Parse(tcs.Task.Result.id.ToString());
                var cSearch = jd.RootElement.GetProperty("candles");
                int count = 0;
                foreach (var c in cSearch.EnumerateArray())
                {
                    Candle candle = JsonSerializer.Deserialize<Candle>(cSearch[count].ToString());
                    candle.dir = (decimal)(candle.close - candle.open);
                    candle.fromDateTime = TimeStamp.FromTimeStamp(candle.from);
                    candle.toDateTime = TimeStamp.FromTimeStamp(candle.to);
                    if (candle.fromDateTime.Minute != serverTime.GetLocalServerDateTime().Minute || candle.fromDateTime.Hour != serverTime.GetLocalServerDateTime().Hour)
                        candles.Add(candle);
                    count++;
                }
                tcs2.TrySetResult(candles);
                return tcs2.Task;
            });
            tcs2.Task.Wait();
            return tcs2.Task;
        }      
        
        //Send message to activate candleStream
        public Task<bool> StartCandlesStream(Active active, int periodInSeconds)
        {           
            var msg = new Message { id = "RealTimeCandle", message = "{\"name\": \"subscribeMessage\", \"msg\": {\"name\": \"candle-generated\", \"params\": {\"routingFilters\": {\"" + active.id + "\": \"80\", \"size\": " + periodInSeconds + "}}}, \"request_id\": \"\"}" };
            var tcs = new TaskCompletionSource<bool>();

            if (SendWebSocketMessage(msg.message))
                tcs.SetResult(true);
            else
                tcs.SetResult(false);
            return tcs.Task;
        }
        //Send message to desactivate candleStream
        public Task<bool> StopCandlesStream(Active active, int periodInSeconds)
        {
            var msg = new Message { id = "RealTimeCandle", message = "{\"name\": \"unsubscribeMessage\", \"msg\": {\"name\": \"candle-generated\", \"params\": {\"routingFilters\": {\"" + active.id + "\": \"80\", \"size\": " + periodInSeconds + "}}}, \"request_id\": \"\"}" };
            SendWebSocketMessage(msg.message);
            var tcs = new TaskCompletionSource<bool>();

            if (SendWebSocketMessage(msg.message))
                tcs.SetResult(true);
            else
                tcs.SetResult(false);
            return tcs.Task;
        }

        //GetLastCandle (1 min) for selected active
        public Task<Candle> GetRealTimeCandlesAsync(Active active)
        {
            var tcs = new TaskCompletionSource<Response>();
            var tcs2 = new TaskCompletionSource<Candle>();
            Message msg = new Message { id = "RealTimeCandle:" + active.id, message = "" };
            if (!tasker.TryAdd(msg.id, tcs))
                tcs2.TrySetCanceled();
            tcs.Task.ContinueWith(t =>
            {
                tasker.TryRemove(msg.id, out TaskCompletionSource<Response> resp);
                var candles = new List<Candle>();
                Candle cd = (Candle)(tcs.Task.Result.id);
                cd.dir = (decimal)(cd.close - cd.open);
                tcs2.TrySetResult(cd);
            });
            tcs2.Task.Wait();
            return tcs2.Task;
        }
      
        //buy function, with selected active, period of candles in seconds, direction of buy and value
        public Task<(bool, long)> BuyAsync(Active active, int periodInSecond, BuyDirection direction, decimal value)
        {
            long expirationDateTimeStamp = GetExpirationTime(periodInSecond);
            value = Math.Round(value, 2);
            var tcs = new TaskCompletionSource<Response>();
            var tcs2 = new TaskCompletionSource<(bool, long)>();
            logger.Log("Buying on ative: " + active.name.Replace("front.", "") + " value: " + value.ToString("0.00").Replace(',', '.') + " interval: " + periodInSecond + " sec on buydirection: " + direction.ToString().ToUpper());
            Message msg = new Message { id = "Buy", message = "{\"name\": \"sendMessage\", \"msg\": {\"body\": {\"price\": " + value.ToString("0.00").Replace(',', '.') + ", \"active_id\": " + active.id + ", \"expired\": " + expirationDateTimeStamp + ", \"direction\": \"" + direction.ToString() + "\", \"option_type_id\": 3, \"user_balance_id\": " + profile.balance_id + "}, \"name\": \"binary-options.open-option\", \"version\": \"1.0\"}, \"request_id\": \"buy\"}" };
            SendWebSocketMessage(msg.message); 
            if (!tasker.TryAdd(msg.id, tcs))
                tcs2.TrySetCanceled();
            tcs.Task.ContinueWith(t =>
            {
                tasker.TryRemove(msg.id, out TaskCompletionSource<Response> resp);
                long code = Int64.Parse(tcs.Task.Result.id.ToString());
                if (code == 0)
                {

                }
                ErrorCode error = ErrorsType.ApplyBuyErrorsCode(code);
                if (error != null)
                {
                    logger.Log("Failed to complete purchase, error: " + error.description);
                    tcs2.TrySetResult((false, 0));
                }
                else
                {
                    logger.Log("Purchase completed, Id: " + code);
                    tcs2.TrySetResult((true, code));
                }
            });
            return tcs2.Task;

        }

        //used to calcule if expiration are on next candle or more
        long GetExpirationTime(int periodInSeconds)
        {
            if (periodInSeconds < 60)
                periodInSeconds = 60;
            DateTime serverDateTime = serverTime.GetRealServerDateTime();
            int seconds = serverDateTime.Second;
            if (seconds <= 30)
            {
                int dif = 30 - (seconds);
                serverDateTime = serverDateTime.AddSeconds(dif + 30);

                return TimeStamp.FromDateTime(serverDateTime) + (periodInSeconds - 60);
            }
            else
            {
                int dif = 60 - (seconds);
                serverDateTime = serverDateTime.AddSeconds(dif + 60);

                return TimeStamp.FromDateTime(serverDateTime) + (periodInSeconds - 60);
            }
        }

        //Check result of buy operation with id of operation and value
        public Task<(string, double)> CheckWinAsync(long id, double value)
        {
            logger.Log("waiting result");
            Message msg = new Message { id = "CheckWin:" + id, message = "" };

            var tcs = new TaskCompletionSource<Response>(); 
            var tcs2 = new TaskCompletionSource<(string, double)>();
            SendWebSocketMessage(msg.message);
            if (!tasker.TryAdd(msg.id, tcs))
                tcs2.TrySetCanceled();
            tcs.Task.ContinueWith(t =>
            {
                tasker.TryRemove(msg.id, out TaskCompletionSource<Response> resp);

                OperationResult op = (OperationResult)(tcs.Task.Result.id);
                double gain = 0;
                if (op.result == "win" || (op.result == null && op.win == "win"))
                {
                    gain = op.win_amount - value;
                    op.result = "win";
                    logger.Log("Resultado: " + op.result + ", Lucro: " + (gain), ConsoleColor.Green);
                }
                else if (op.result == "loose" || (op.result == null && op.win == "loose"))
                {
                    gain = value;
                    op.result = "loose";
                    logger.Log("Resultado: " + op.result + ", Prejuizo: " + (value), ConsoleColor.Red);
                }
                else
                {
                    op.result = "draw";
                    logger.Log("Resultado: " + op.result, ConsoleColor.Cyan);
                }
                tcs2.TrySetResult((op.result, gain));
            });
            return tcs2.Task;
        }
        
    }
}
