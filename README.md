# IQOptionNetCoreCustomAPI
This is my custom API for IQ OPTION, created using C# NETCORE 3.1
## For now, it's working:
* Get Server Time
* Set Local Time
* Get Profile info
* Get Active list
* Get Profile Info
* Set Balance account type
* Get Candle List
* Get Realtime Candles
* Request Buy
* Check Buy Result
## Working Indicators
* Sma
* Ema
* RSI
* MACD
* Bollinger Band
* Fractal
## Usage
## Basic Functions

### Libraries
```
using IqApiNetCore;
using IqApiNetCore.Models;
using IqApiNetCore.Utilities;
```
### Connect
```
API api = new API();
connected = await api.ConnectAsync("username@username.com" "password");
```
### Change Balance Type
```
api.ChangeBalanceAsync(BalanceType.Real);
api.ChangeBalanceAsync(BalanceType.Practice);
```
### Get Balance Value
```
decimal balance = api.profile.balance;
```
### Get Server Time
```
DateTime serverDateTime = api.serverTime.GetRealServerDateTime();
```
### Convert Server Time to TimeStamp
```
long lastCandleTimeStamp = TimeConverter.FromDateTime(serverDateTime);
```
### Get Active List(true for online actives, false for all
```
(List<Active> binaryActives, List<Active> turboActives) = await api.GetActiveList(true);
```
### Get Candle List
```
Active active = turboActives[0];
int periodInSeconds = 60
long lastCandleTimeStamp = TimeConverter.FromDateTime(api.serverTime.GetRealServerDateTime());
int candleCount = 100;
List<Candle> candles = await api.GetCandlesAsync(active, periodInSeconds, lastCandleTimeStamp, candleCount);
```
### Start Candle Stream, all new candles changes be call added function
```
int periodInSeconds = 60;
Active active = turboActives[0];
candles = new List<Candle>();
api.StartCandlesStream(periodInSeconds,OnReceiveCandle); //used to start receiving candle realtime

//function example
void OnReceiveCandle(object data, EventArgs e)
{  
	Candle candle = (Candle)data;	
	if(candle.active_id == active.id)
	{
		if (candles[candles.Count - 1].fromDateTime == candle.fromDateTime)
                    candles[candles.Count - 1] = candle;
                else
                    candles.Add(candle);
	}
}

```
### Stop Candle Stream
```
api.StopCandlesStream();

```
### Buy
```
API.BuyDirection direction = API.BuyDirection.put;  //API.BuyDirection.call to use CALL
decimal buyValue = 1;
(string status, Operation op) = await api.BuyAsync(active, periodInSeconds, direction, buyValue);
```
### GetResult
```
OperationStatus opStat = await api.CheckBuyResult(op.option_id)
```

## Indicators
### Simple Moving Averages
```
List<Candle> candleList = await api.GetCandlesAsync(active, periodInSeconds, lastCandleTimeStamp, candleCount);
decimal[] sma = Indicators.GetSMA(period, candleList)
```
### Exponential Moving Averages
```
int period 12;
decimal[] ema = Indicators.GetEMA(period, candleList)
```
### Bollinger Band
```
int deviation = 2;
BollingerBand[] bb = Indicators.GetBollingerBand(period, deviation, candleList);
```
### MACD
```
MACD macd = Indicators.GetMACD(fastEMAPeriod,slowEMAPeriod,signalPeriod,candleList);
```
### RSI
```
float[] rsi = Indicators.GetRSI(period, candleList);
```
### Fractal
```
(decimal[] fracUp, decimal[] fracDown) = Indicators.GetFractal(period, candlesList);
```
