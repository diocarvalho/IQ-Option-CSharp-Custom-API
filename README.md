# IQOptionNetCoreCustomAPI
I'm creating an custom IQ Option NetCore Api , to help peoples who want to create automated robots.
## For now, it's working:
* Get Server Time
* Set Local Time
* Get Profile info
* Get Active list
* Get Balance Info
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
* Horizontal Support and Resistance
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
await api.ChangeBalanceAsync(BalanceType.Real);
await api.ChangeBalanceAsync(BalanceType.Practice);
```
### Get Balance Value
```
decimal balance = await api.GetCurrentBalanceAsync();
```
### Get Server Time
```
DateTime serverDateTime = api.serverTime.GetRealServerDateTime();
```
### Convert Server Time to TimeStamp
```
long lastCandleTimeStamp = TimeStamp.FromDateTime(serverDateTime);
```
### Get Active List
```
(List<Active> binaryActives, List<Active> turboActives) = await api.GetActiveOptionDetailAsync();
```
### Get Candle List
```
Active active = turboActives[0];
int periodInSeconds = 60
long lastCandleTimeStamp = TimeStamp.FromDateTime(api.serverTime.GetRealServerDateTime());
int candleCount = 100;
List<Candle> candles = await api.GetCandlesAsync(active, periodInSeconds, lastCandleTimeStamp, candleCount);
```
### Get Current Candle
```
api.StartCandlesStream(); //used to start receiving candle realtime
Candle lastCandle = await api.GetRealTimeCandlesAsync(active);
//after enable, disable candlesStream if you are not using
//api.StopCandlesStream();
```
### Buy
```
API.BuyDirection direction = API.BuyDirection.put;  //API.BuyDirection.call to use CALL
decimal buyValue = 1;
(bool operationStatus, long operationId) = await api.BuyAsync(active, periodInSeconds, direction, buyValue);
```
### GetResult
```
(string result, decimal earnedValue) = await api.CheckWinAsync(operationId, buyValue); //result = win, loose and draw, if win return profit on earnedValue
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
### Support And Resistance
```
int startIndex = 0;
List<decimal> supports = Indicators.GetSupports(candlesList, startIndex);
List<decimal> resistances = Indicators.GetResistances(candlesList, startIndex);
```
