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
        //socket used to process http conn;
        private WebClient httpWebClient;
        private CustomWebSocket webSocket;
        //account ssid, used to auth with webSocket
        private string ssid;
        //custom tasker used to process requests and responses
        private List<CustomTasker<Response>> tasks = new List<CustomTasker<Response>>();
        //all contact created by app in current session
        private List<OperationState> contractsCreated = new List<OperationState>();
        private int respTimeout = 5000;

        public event EventHandler<EventArgs> OnCandleStreamReceive;
        //used to check if time are synced with servers
        public API(bool debug, bool showConsole)
        {
            serverTime = new ServerTime();
            debugging = debug;
            InitHTTP();
            InitWebSocket();
            logger = new Logger(showConsole);
        }
        public API()
        {
            serverTime = new ServerTime();
            debugging = false;
            logger = new Logger(false);
        }
        public async Task<bool> Disconnect()
        {
            try
            {
                await webSocket.DisconnectAsync();
                tasks = null;
                return true;
            }
            catch
            {
                return false;
            }
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
            webSocket = new CustomWebSocket();

        }
        public async Task<string> ConnectAsync(string username, string password)
        {
            string resp = "";

            //firs connect with HTTP using username and password
            LoginMessage r = await SendRequestAsync(username, password);
            if (r.code == "success")
            {
                //after auth get Account/Conn ssid code
                ssid = r.ssid;
                try
                {
                    //add listener funct to process all messages received
                    webSocket.OnMessageReceived += MessageListener;
                    await webSocket.ConnectAsync("wss://iqoption.com/echo/websocket");
                    //use ssid to request profile info
                    profile = await GetProfileAsync();

                    while (!serverTime.synced)
                    {
                        Task.Delay(100).Wait();
                    }
                    resp = "ok";

                }
                catch (Exception err)
                {
                    resp = err.Message;
                }
            }
            else
            {
                resp = r.code;
            }
            return resp;
        }
        public async Task<LoginMessage> SendRequestAsync(string username, string password)
        {
            var values = new NameValueCollection();
            values["identifier"] = username;
            values["password"] = password;

            try
            {
                byte[] response = await httpWebClient.UploadValuesTaskAsync("https://auth.iqoption.com/api/v2/login", values);

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
            catch (Exception err)
            {
                return new LoginMessage() { code = err.Message };
            }
        }
      
        public bool SendMessageAsync(string msg)
        {
            if (debugging)
            {
                logger.Log("Sending message: " + msg + "\n");
                Console.WriteLine();
            }
            try
            {
                webSocket.SendMessageAsync(msg);
                return true;
            }catch
            {
                return false;
            }
        }

        //This function process all message received
        async void MessageListener(object data, EventArgs e)
        {
            try
            {
                //Receive message and deserialize
                IQMessage<object> msg = JsonSerializer.Deserialize<IQMessage<object>>(data.ToString());
                //logger.Log(msg.name);
                if (debugging)
                {
                    logger.Log("Message received: " + msg.name + " | " + msg.status + "\n" + msg.msg.ToString() + "\n");
                    Console.WriteLine();
                }

                switch (msg.name)
                {
                    //used to sync gmt with server
                    case MessageType.Heartbeat:
                        string heartBeatMessage = "{\"request_id\":\"2\",\"name\":\"heartbeat\",\"msg\":{\"userTime\":1617500473,\"heartbeatTime\":1617500453},\"microserviceName\":null,\"version\":\"1.0\",\"status\":0}";
                        SendMessageAsync(heartBeatMessage);
                        break;
                    //update timer controller
                    case MessageType.TimeSync:
                        serverTime.SetServerTimeStamp(long.Parse(msg.msg.ToString()));
                        break;
                    //receive the profile data
                    case MessageType.Profile:
                        int index = tasks.FindIndex(x => x.Request.id.ToString() == "GetProfile");
                        if (index > -1)
                            if (tasks[index].status == CustomTasker<Response>.Status.WaitingResult)
                            {
                                Profile profile = JsonSerializer.Deserialize<Profile>(msg.msg.ToString());
                                tasks[index].SetResult(new Response { id = profile });
                            }
                        break;
                    //receive list of all balances
                    case MessageType.Balances:
                        index = tasks.FindIndex(x => x.Request.id.ToString() == "GetBalances");
                        if (index > -1)
                            if (tasks[index].status == CustomTasker<Response>.Status.WaitingResult)
                            {
                                List<Balance> b = JsonSerializer.Deserialize<List<Balance>>(msg.msg.ToString());
                                tasks[index].SetResult(new Response { id = b });
                            }
                        break;
                    //receive any balance changes
                    case MessageType.BalanceChanged:
                        CurrentBalance bal = JsonSerializer.Deserialize<CurrentBalance>(msg.msg.ToString());
                        int bIndex = profile.balances.FindIndex(x => x.id == bal.current_balance.id);
                        if (bIndex > -1)
                        {
                            profile.balances[bIndex] = bal.current_balance;
                            if (profile.balance_id == bal.current_balance.id)
                            {
                                profile.balance = (decimal)bal.current_balance.amount;
                            }
                        }
                        break;
                    //receive a list off all active
                    case MessageType.GetAllProfit:
                        index = tasks.FindIndex(x => x.Request.id.ToString() == "GetActiveOptionDetail");
                        if (index > -1)
                            if (tasks[index].status == CustomTasker<Response>.Status.WaitingResult)
                                tasks[index].SetResult(new Response { id = msg.msg.ToString() });
                        break;
                    case MessageType.Candles:
                        index = tasks.FindIndex(x => x.Request.id.ToString() == "GetCandles");
                        if (index > -1)
                            if (tasks[index].status == CustomTasker<Response>.Status.WaitingResult)
                                tasks[index].SetResult(new Response { id = msg.msg.ToString() });
                        break;
                    //receive any operation changes
                    case MessageType.OptionChanged:
                        var rsn = JsonSerializer.Deserialize<Operation>(msg.msg.ToString());
                        if (rsn.result == "opened")
                        {
                            index = tasks.FindIndex(x => x.Request.id.ToString() == "Buy");
                            if (index > -1)
                                if (tasks[index].status == CustomTasker<Response>.Status.WaitingResult)
                                    tasks[index].SetResult(new Response { id = msg.msg.ToString() });
                        }
                        else if (rsn.result == "win" || rsn.result == "loose" || rsn.result == "equal")
                        {
                            int opIndex = await WaitContract(rsn.option_id, respTimeout);
                            if (opIndex > -1)
                            {
                                if (rsn.result == "win")
                                {
                                    contractsCreated[opIndex].state = OperationState.OPState.Win;
                                    contractsCreated[opIndex].valueReturned = decimal.Parse(rsn.profit_amount.ToString());
                                }
                                else if (rsn.result == "loose")
                                    contractsCreated[opIndex].state = OperationState.OPState.Loose;
                                else if (rsn.result == "equal")
                                    contractsCreated[opIndex].state = OperationState.OPState.Draw;
                            }
                        }
                        else
                        {
                            logger.Log(msg.msg.ToString());
                        }
                        break;
                    //receive errors on buy
                    case MessageType.Option:
                        index = tasks.FindIndex(x => x.Request.id.ToString() == "Buy");
                        if (index > -1)
                            if (msg.status != 2000 && tasks[index].status == CustomTasker<Response>.Status.WaitingResult) //2000 is buy confirmation code
                                tasks[index].SetResult(new Response { id = msg });
                        break;
                     //receive candleStream
                    case MessageType.Quotes:
                        JsonDocument rCandleSearch = JsonDocument.Parse(msg.msg.ToString());

                        if (OnCandleStreamReceive != null)
                        {
                            long rcId = long.Parse(rCandleSearch.RootElement.GetProperty("active_id").ToString());

                            Candle cd = JsonSerializer.Deserialize<Candle>(msg.msg.ToString());
                            cd.dir = (decimal)(cd.close - cd.open);
                            cd.fromDateTime = TimeConverter.FromTimeStamp(cd.from);
                            cd.toDateTime = TimeConverter.FromTimeStamp(cd.to);
                            cd.active_id = rcId;

                            Task.Factory.StartNew(() =>
                            {
                                OnCandleStreamReceive(cd, new EventArgs());
                            });
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

        private async Task<int> WaitContract(long id, int delay)
        {
            int index = -1;
            Task tks = Task.Run(() =>
            {
                while (index == -1)
                {
                    index = contractsCreated.FindIndex(x => x.contractId == id);
                    Task.Delay(50).Wait();
                }
            });
            await Task.WhenAny(tks, Task.Delay(delay));
            return index;
        }

        async Task<Profile> GetProfileAsync()
        {
            CustomTasker<Response> tk = new CustomTasker<Response>();

            Profile profile = null;
            Message msg = new Message { id = "GetProfile", message = "{\"request_id\":\"1\",\"name\":\"ssid\",\"msg\":\"" + ssid + "\",\"microserviceName\":null,\"version\":\"1.0\",\"status\":0}" };

            tk.SetRequest(new Response() { id = msg.id });
            tasks.Add(tk);

            SendMessageAsync(msg.message);
            Task task = tk.task;
            if (await Task.WhenAny(task, Task.Delay(respTimeout)) == task)
            {
                profile = (Profile)tk.Result.id;
            }
            else
            {

            }
            tasks.Remove(tk);
            return profile;
        }
        //Change balance account type(demo, real, etc)

        public bool ChangeBalance(BalanceType balanceType)
        {
            bool changed = false;
            logger.Log("Changing account type to: " + balanceType.ToString());
            //change balance id, used to perform buy
            long newBalanceId = 0;
            int index = profile.balances.FindIndex(x => x.type == balanceType);

            if (index == -1)
            {
                logger.Log("Failed to change balance type,selected type does not exists");
            }
            else
            {
                newBalanceId = profile.balances[index].id;
                //informative message only, no response needed
                SendMessageAsync("{\"name\": \"unsubscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"cfd\", \"user_balance_id\":" + profile.balance_id + "}}}, \"request_id\":\"1\"");
                SendMessageAsync("{\"name\": \"unsubscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"forex\", \"user_balance_id\": " + profile.balance_id + "}}}, \"request_id\":\"1\"");
                SendMessageAsync("{\"name\": \"unsubscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"crypto\", \"user_balance_id\": " + profile.balance_id + "}}}, \"request_id\":\"1\"");
                SendMessageAsync("{\"name\": \"unsubscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"digital-option\", \"user_balance_id\": " + profile.balance_id + "}}}, \"request_id\":\"1\"");
                SendMessageAsync("{\"name\": \"unsubscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"turbo-option\", \"user_balance_id\": " + profile.balance_id + "}}}, \"request_id\":\"1\"");
                SendMessageAsync("{\"name\": \"unsubscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"binary-option\", \"user_balance_id\": " + profile.balance_id + "}}}, \"request_id\":\"1\"");

                SendMessageAsync("{\"name\": \"subscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"cfd\", \"user_balance_id\":" + newBalanceId + "}}}, \"request_id\":\"1\"");
                SendMessageAsync("{\"name\": \"subscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"forex\", \"user_balance_id\":" + newBalanceId + "}}}, \"request_id\":\"1\"");
                SendMessageAsync("{\"name\": \"subscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"crypto\", \"user_balance_id\":" + newBalanceId + "}}}, \"request_id\":\"1\"");
                SendMessageAsync("{\"name\": \"subscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"digital-option\", \"user_balance_id\":" + newBalanceId + "}}}, \"request_id\":\"1\"");
                SendMessageAsync("{\"name\": \"subscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"turbo-option\", \"user_balance_id\":" + newBalanceId + "}}}, \"request_id\":\"1\"");
                SendMessageAsync("{\"name\": \"subscribeMessage\", \"msg\": {\"name\": \"portfolio.position-changed\", \"version\": \"2.0\", \"params\": {\"routingFilters\": {\"instrument_type\": \"binary-option\", \"user_balance_id\":" + newBalanceId + "}}}, \"request_id\":\"1\"");
                profile.balance_id = newBalanceId;
                profile.balance = (decimal)profile.balances[index].amount;
                logger.Log("Balance type changed.");
                changed = true;
            }
            return changed;
        }

        //Get Account balance (value)
        public async Task<decimal> UpdateBalanceAsync()
        {
            CustomTasker<Response> tk = new CustomTasker<Response>();
            decimal balance = 0;
            Message msg = new Message { id = "GetBalances", message = "{\"name\": \"sendMessage\", \"msg\": {\"name\": \"get-balances\", \"version\": \"1.0\"}, \"request_id\": \"\"}" };

            tk.SetRequest(new Response() { id = msg.id });
            tasks.Add(tk);

            SendMessageAsync(msg.message);

            Task task = tk.task;
            if (await Task.WhenAny(task, Task.Delay(respTimeout)) == task)
            {
                profile.balances = (List<Balance>)tk.Result.id;
                foreach (Balance b in profile.balances)
                {
                    if (b.id == profile.balance_id)
                        profile.balance = (decimal)b.amount;
                }
                balance = profile.balance;

            }
            else
            {
                logger.Log(msg.id + " Task timeout");
            }
            tasks.Remove(tk);
            return balance;
        }
        public async Task<(List<Active>, List<Active>)> GetActiveList(bool online)
        {
            CustomTasker<Response> tk = new CustomTasker<Response>();
            List<Active> bActives = new List<Active>();
            List<Active> tActives = new List<Active>();
            Message msg = new Message { id = "GetActiveOptionDetail", message = "{\"name\": \"api_option_init_all\", \"msg\": \"\", \"request_id\": \"\"}" };


            tk.SetRequest(new Response() { id = msg.id });
            tasks.Add(tk);

            SendMessageAsync(msg.message);

            Task task = tk.task;
            if (await Task.WhenAny(task, Task.Delay(respTimeout)) == task)
            {
                DefaultMessage msgDec = JsonSerializer.Deserialize<DefaultMessage>(tk.Result.id.ToString());
                DateTime time = serverTime.GetLocalServerDateTime();
                foreach (var m in msgDec.result.binary.actives)
                {
                    if (online)
                    {
                        if (m.Value.opened_at <= time && m.Value.close_at > time)
                            bActives.Add(m.Value);
                    }
                    else
                        bActives.Add(m.Value);
                }
                foreach (var m in msgDec.result.turbo.actives)
                {
                    if (online)
                    {
                        if (m.Value.opened_at <= time && m.Value.close_at > time)
                            tActives.Add(m.Value);
                    }
                    else
                        tActives.Add(m.Value);
                }
            }
            else
            {
                logger.Log(msg.id + " Task timeout");
            }

            tasks.Remove(tk);
            return (bActives, tActives);
        }

        //Get a list with candles from selected acive, period in seconds, date of most recent candle and candle total

        public async Task<List<Candle>> GetCandlesAsync(Active active, int periodInSeconds, long lastCandleCloseDate, int candleCount)
        {
            List<Candle> candles = new List<Candle>();
            CustomTasker<Response> tk = new CustomTasker<Response>();

            Message msg = new Message { id = "GetCandles", message = "{\"name\": \"sendMessage\", \"msg\": {\"name\": \"get-candles\", \"version\": \"2.0\", \"body\": {\"active_id\": " + active.id + ", \"size\": " + periodInSeconds + ", \"to\": " + lastCandleCloseDate + ", \"count\": " + (candleCount + 1) + ", \"\": 80}}, \"request_id\": \"\"}" };

            tk.SetRequest(new Response() { id = msg.id });
            tasks.Add(tk);

            SendMessageAsync(msg.message);

            Task task = tk.task;
            if (await Task.WhenAny(task, Task.Delay(respTimeout)) == task)
            {
                CandleResult res = JsonSerializer.Deserialize<CandleResult>(tk.Result.id.ToString());
                for (int i = 0; i < res.candles.Count; i++)
                {
                    res.candles[i].UpdateData(active.id);
                }
                candles = res.candles;
            }
            else
            {
                logger.Log(msg.id + " Task timeout");
            }
            tasks.Remove(tk);


            return candles;
        }


        //Start candleStream and assign a event to receive
        public bool StartCandlesStream(int periodInSeconds, EventHandler<EventArgs> OnCandleStreamReceive)
        {
            if (OnCandleStreamReceive != null)
            {
                this.OnCandleStreamReceive = OnCandleStreamReceive;
                var msg = new Message { id = "RealTimeCandle", message = "{\"name\": \"subscribeMessage\", \"msg\": {\"name\": \"candle-generated\", \"params\": {\"routingFilters\": {\"" + 0 + "\": \"80\", \"size\": " + periodInSeconds + "}}}, \"request_id\": \"\"}" };


                if (SendMessageAsync(msg.message))
                {
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        //Stop candle stream;

        public bool StopCandlesStream()
        {
            var msg = new Message { id = "RealTimeCandle", message = "{\"name\": \"unsubscribeMessage\", \"msg\": {\"name\": \"candle-generated\", \"params\": {\"routingFilters\": {\"" + 0 + "\": \"80\", \"size\": " + 0 + "}}}, \"request_id\": \"\"}" };
            SendMessageAsync(msg.message);
            OnCandleStreamReceive = null;
            if (SendMessageAsync(msg.message))
                return true;
            else
                return false;

        }
        //GetLastCandle (1 min) for selected active


        //buy function, with selected active, period of candles in seconds, direction of buy and value
        public async Task<(string, Operation)> BuyAsync(Active active, int periodInSecond, BuyDirection direction, decimal value)
        {
            long expirationDateTimeStamp = GetExpirationTime(periodInSecond);
            value = Math.Round(value, 2);
            CustomTasker<Response> tk = new CustomTasker<Response>();

            Operation or = null;
            string status = "";

            int optionType = 3;
            if (periodInSecond >= 300)
                optionType = 1;
            logger.Log("Buying on ative: " + active.name.Replace("front.", "") + " value: " + value.ToString("0.00").Replace(',', '.') + " interval: " + periodInSecond + " sec on buydirection: " + direction.ToString().ToUpper() + ", Option Type: " + optionType);
            Message msg = new Message { id = "Buy", message = "{\"name\": \"sendMessage\", \"msg\": {\"body\": {\"price\": " + value.ToString("0.00").Replace(',', '.') + ", \"active_id\": " + active.id + ", \"expired\": " + expirationDateTimeStamp + ", \"direction\": \"" + direction.ToString() + "\", \"option_type_id\": " + optionType + ", \"user_balance_id\": " + profile.balance_id + "}, \"name\": \"binary-options.open-option\", \"version\": \"1.0\"}, \"request_id\": \"buy\"}" };

            tk.SetRequest(new Response() { id = msg.id });
            tasks.Add(tk);
            SendMessageAsync(msg.message);
            Task task = tk.task;
            if (await Task.WhenAny(task, Task.Delay(respTimeout)) == task)
            {
                if (tk.Result.id is IQMessage<object>)
                {
                    var errorMsg = (IQMessage<Object>)tk.Result.id;
                    Message m = JsonSerializer.Deserialize<Message>(errorMsg.msg.ToString());
                    logger.Log("Failed to complete purchase, error: " + m.message);
                    status = m.message;
                }
                else
                {
                    or = JsonSerializer.Deserialize<Operation>(tk.Result.id.ToString());
                    {
                        logger.Log("Purchase completed, operation id: " + or.option_id);
                        status = "sucess";
                        contractsCreated.Add(new OperationState() { state = OperationState.OPState.Opened, contractId = or.option_id });
                        tasks.Remove(tk);

                    }
                }

            }
            else
            {
                logger.Log(msg.id + " Task timeout");
            }
            tasks.Remove(tk);
            return (status, or);
        }

            //calcule if expiration are on next candle or more
            long GetExpirationTime(int periodInSeconds)
        {
            if (periodInSeconds < 60)
                periodInSeconds = 60;
            DateTime serverDateTime = serverTime.GetRealServerDateTime();

            int seconds = serverDateTime.Second;
            if (periodInSeconds == 60)
            {
                if (seconds <= 30)
                {
                    int dif = 30 - (seconds);
                    serverDateTime = serverDateTime.AddSeconds(dif + 30);

                    return TimeConverter.FromDateTime(serverDateTime) + (periodInSeconds - 60);
                }
                else
                {
                    int dif = 60 - (seconds);
                    serverDateTime = serverDateTime.AddSeconds(dif + 60);

                    return TimeConverter.FromDateTime(serverDateTime) + (periodInSeconds - 60);
                }
            }
            else if (periodInSeconds == 300)
            {
                if (serverDateTime.AddMinutes(1).Minute % 5 == 0 && seconds > 30)
                {
                    int dif = 60 - seconds;
                    serverDateTime = serverDateTime.AddSeconds(periodInSeconds + dif);
                    logger.Log("Expiration Time: " + serverDateTime.ToLongTimeString());
                    return TimeConverter.FromDateTime(serverDateTime);
                }
                else if (serverDateTime.AddMinutes(1).Minute % 5 == 0 && seconds <= 30)
                {
                    int dif = 60 - (seconds);
                    serverDateTime = serverDateTime.AddSeconds(dif);
                    logger.Log("Expiration Time: " + serverDateTime.ToLongTimeString());
                    return TimeConverter.FromDateTime(serverDateTime);
                }
                else
                {
                    //calcular corretamente
                    DateTime newTime = new DateTime(serverDateTime.Year, serverDateTime.Month, serverDateTime.Day, serverDateTime.Hour, serverDateTime.Minute, 0, 0);
                    if (serverDateTime.AddMinutes(5).Minute % 5 == 0)
                        newTime = newTime.AddMinutes(5);
                    else if (serverDateTime.AddMinutes(4).Minute % 5 == 0)
                        newTime = newTime.AddMinutes(4);
                    else if (serverDateTime.AddMinutes(3).Minute % 5 == 0)
                        newTime = newTime.AddMinutes(3);
                    else if (serverDateTime.AddMinutes(2).Minute % 5 == 0)
                        newTime = newTime.AddMinutes(2);
                    logger.Log("Expiration Time: " + newTime.ToLongTimeString());
                    return TimeConverter.FromDateTime(newTime);
                }
            }
            else
            {
                logger.Log("No Expiration time avaliable");
            }
            return TimeConverter.FromDateTime(serverDateTime);
        }

        //return result for a buyID
        public async Task<OperationState> CheckBuyResult(long buyId)
        {
            OperationState opState = null;
            int index = contractsCreated.FindIndex(x => x.contractId == buyId);
            if (index > -1)
            {
                opState = contractsCreated[index];
                await Task.Run(() =>
                {
                    while (opState.state == OperationState.OPState.Opened) { Task.Delay(10).Wait(); }
                });
                logger.Log("Contract result: " + opState.state);
            }
            else
            {
                logger.Log("Error checking result, contract not found: " + buyId);
            }
            return opState;
        }
        
        
    }
}
