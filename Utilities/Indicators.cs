using System;
using System.Collections.Generic;
using System.Text;
using IqApiNetCore.Utilities;
using IqApiNetCore.Models;
namespace IqApiNetCore
{
    //returns a indicators data
    public class Indicators
    {
        public static BollingerBand GetBollingerBand(int period, int factor, List<Candle> candles)
        {
            BollingerBand bollingerBand = new BollingerBand();
            decimal totalAverage = 0;
            decimal totalSquares = 0;
            for (int i = 0; i < candles.Count; i++)
            {
                totalAverage += candles[i].close;
                totalSquares += (decimal)Math.Pow((double)candles[i].close, 2);
                if (i >= period - 1)
                {
                    decimal average = totalAverage / period;
                    decimal stdev = (decimal)Math.Sqrt(((double)totalSquares - Math.Pow((double)totalAverage, 2) / period) / period);

                    bollingerBand.MidBand.Add(average);
                    decimal up = average + factor * stdev;
                    bollingerBand.UpperBand.Add(up);
                    decimal down = average - factor * stdev;
                    bollingerBand.LowerBand.Add(down);
                    decimal bandWidth = (up - down) / average;
                    bollingerBand.BandWidth.Add(bandWidth);
                    decimal bPercent = (candles[i].close - down) / (up - down);
                    bollingerBand.BPercent.Add(bPercent);

                    totalAverage -= candles[i - period + 1].close;
                    totalSquares -= (decimal)Math.Pow((double)candles[i - period + 1].close, 2);
                }
                else
                {
                    bollingerBand.MidBand.Add(null);
                    bollingerBand.UpperBand.Add(null);
                    bollingerBand.LowerBand.Add(null);
                    bollingerBand.BandWidth.Add(null);
                    bollingerBand.BPercent.Add(null);
                }
            }
            return bollingerBand;
        }

        public static (decimal[],decimal[]) GetFractal(int period, List<Candle> candles)
        {
            decimal[] fracUp = new decimal[candles.Count];
            decimal[] fracDown = new decimal[candles.Count];
            for (int i = period; i< candles.Count;i++)
            {
                int per = period % 2 == 0 ? period - 1 : period;
                int middleIndex = i - per / 2;
                decimal middleValue = candles[middleIndex].max;

                bool up = true;

                for (int x = 0; x < per; x++)
                {
                    if (middleValue < candles[i - x].max)
                    {
                        up = false;
                        break;
                    }
                }
                if (up)
                    fracUp[middleIndex] = middleValue;

                middleValue = candles[middleIndex].min;

                bool down = true;

                for (int x = 0; x < per; x++)
                {
                    if (middleValue < candles[i - x].min)
                    {
                        down = false;
                        break;
                    }
                }
                if (down)
                    fracDown[middleIndex] = middleValue;

            }
            return (fracUp, fracDown);
        }
        public static float[] GetRSI(int period, List<Candle> candles)
        {
            float[] rsArray = new float[candles.Count];
            float[] rsiArray = new float[candles.Count];
            decimal gainSum = 0;
            decimal lossSum = 0;
            rsArray[0] = 0;
            rsiArray[0] = 0;
            for (int i = 1; i < period; i++)
            {
                decimal thisChange = candles[i].close - candles[i - 1].close;
                if (thisChange > 0)
                {
                    gainSum += thisChange;
                }
                else
                {
                    lossSum += (-1) * thisChange;
                }
                rsArray[i] = 0;
                rsiArray[i] = 0;
            }
            var averageGain = gainSum / period;
            var averageLoss = lossSum / period;
            var rs = averageGain / averageLoss;
            var rsi = 100 - (100 / (1 + rs));


            for (int i = period; i < candles.Count; i++)
            {
                decimal thisChange = candles[i].close - candles[i - 1].close;
                if (thisChange > 0)
                {
                    averageGain = (averageGain * (period - 1) + thisChange) / period;
                    averageLoss = (averageLoss * (period - 1)) / period;
                }
                else
                {
                    averageGain = (averageGain * (period - 1)) / period;
                    averageLoss = (averageLoss * (period - 1) + (-1) * thisChange) / period;
                }
                rs = averageGain / averageLoss;
                rsi = 100 - (100 / (1 + rs));

                rsArray[i] = (float)rs;
                rsiArray[i] = (float)rsi;
            }
            return rsiArray;
        }

        public static float[] GetEMA(int period, List<Candle> candles)
        {
            float multiplier = 2 / ((float)period + 1);
            float[] ema = new float[candles.Count];
            ema[0] = (float)candles[0].close;
            //float[] sma = GetSMA(period, candles);
            for (int i = 1; i < candles.Count; i++)
            {
                ema[i] = ((float)candles[i].close - ema[i - 1]) * multiplier + ema[i - 1];
            }
            return ema;
        }
        public static float[] GetSMA(int period, List<Candle> candles)
        {
            float[] smaArray = new float[candles.Count];
            for (int i = 0; i < candles.Count; i++)
            {
                if (i > period - 1)
                {
                    float sum = 0;
                    for (int x = period - 1; x >= 0; x--)
                    {
                        sum += (float)candles[i - x].close;
                    }
                    sum = sum / period;
                    smaArray[i] = sum;
                }
                else
                {
                    float sum = 0;
                    for (int x = 0; x < period; x++)
                    {
                        sum += (float)candles[i + x].close;
                    }
                    sum = sum / period;
                    smaArray[i] = sum;
                }
            }
            return smaArray;
        }

        public static MACD GetMACD(int fastEMAPeriod, int slowEMAPeriod, int signalPeriod, List<Candle> candles)
        {
            float[] fastEMA = GetEMA(fastEMAPeriod, candles);
            float[] slowEMA = GetEMA(slowEMAPeriod, candles);
            float[] macd = new float[candles.Count];
            float[] hist = new float[candles.Count];
            for (int i = 0; i < candles.Count; i++)
            {
                macd[i] = fastEMA[i] - slowEMA[i];
            }
            float[] signal = GetEMA(signalPeriod, macd);
            for (int i = 0; i < candles.Count; i++)
            {
                hist[i] = macd[i] - slowEMA[i];
            }
            MACD m = new MACD() { macd = macd, signal = signal, histogram = hist };
            return m;
        }
        public static float[] GetEMA(int period, float[] values)
        {
            float multiplier = 2 / ((float)period + 1);
            float[] ema = new float[values.Length];
            ema[0] = (float)values[0];
            //float[] sma = GetSMA(period, candles);
            for (int i = 1; i < values.Length; i++)
            {
                ema[i] = ((float)values[i] - ema[i - 1]) * multiplier + ema[i - 1];
            }
            return ema;
        }


        public static (List<decimal>, List<int>) GetLowPeaks(List<Candle> candles, int startIndex)
        {
            List<decimal> lowPeaks = new List<decimal>();
            List<decimal> tempPeaks = new List<decimal>();
            List<int> lowPeaksIndex = new List<int>();
            List<int> tempPeaksIndex = new List<int>();
            for (int i = 1; i < candles.Count - 1; i++)
            {
                bool isPeak = candles[i].min < candles[i - 1].min && candles[i].min < candles[i + 1].min;
                //bool isPeak = candles[i].min < candles[i - 1].min && candles[i].min < candles[i + 1].min && candles[i + 1].min < candles[i + 2].min && candles[i - 1].min < candles[i - 2].min;
                if (isPeak)
                {
                    tempPeaks.Add(candles[i].min);
                    tempPeaksIndex.Add(i);
                }

            }
            for (int i = 0; i < tempPeaksIndex.Count; i++)
            {
                for (int x = 0; x < tempPeaksIndex.Count; x++)
                {

                    if ((candles[tempPeaksIndex[x]].close >= candles[tempPeaksIndex[i]].min && candles[tempPeaksIndex[x]].close <= candles[tempPeaksIndex[i]].max || candles[tempPeaksIndex[x]].min >= candles[tempPeaksIndex[i]].min && candles[tempPeaksIndex[x]].max <= candles[tempPeaksIndex[i]].max) && x != i)
                    {
                        lowPeaks.Add(candles[tempPeaksIndex[x]].min);
                        lowPeaksIndex.Add(x);
                        lowPeaks.Add(candles[tempPeaksIndex[i]].min);
                        lowPeaksIndex.Add(i);
                        break;
                    }
                }
            }
          
            return (lowPeaks, lowPeaksIndex);
        }

        static (List<decimal>, List<int>) GetHighPeaks(List<Candle> candles, int startIndex)
        {
            List<decimal> highPeaks = new List<decimal>();
            List<decimal> tempPeaks = new List<decimal>();
            List<int> highPeaksIndex = new List<int>();
            List<int> tempPeaksIndex = new List<int>();
            for (int i = 1; i < candles.Count - 1; i++)
            {
                bool isPeak = candles[i].max > candles[i - 1].max && candles[i].max > candles[i + 1].max;
                //bool isPeak = candles[i].min < candles[i - 1].min && candles[i].min < candles[i + 1].min && candles[i + 1].min < candles[i + 2].min && candles[i - 1].min < candles[i - 2].min;
                if (isPeak)
                {
                    tempPeaks.Add(candles[i].max);
                    tempPeaksIndex.Add(i);
                }

            }
            for (int i = 0; i < tempPeaksIndex.Count; i++)
            {
                for (int x = 0; x < tempPeaks.Count; x++)
                {

                    if ((candles[tempPeaksIndex[x]].close >= candles[tempPeaksIndex[i]].min && candles[tempPeaksIndex[x]].close <= candles[tempPeaksIndex[i]].max || candles[tempPeaksIndex[x]].max >= candles[tempPeaksIndex[i]].min && candles[tempPeaksIndex[x]].max <= candles[tempPeaksIndex[i]].max) && x != i)
                    {
                        highPeaks.Add(candles[tempPeaksIndex[x]].max);
                        highPeaksIndex.Add(x);
                        highPeaks.Add(candles[tempPeaksIndex[i]].max);
                        highPeaksIndex.Add(i);
                        break;
                    }
                }
            }

            return (highPeaks, highPeaksIndex);
        }
        public static List<decimal> GetSupports(List<Candle> candles, int startIndex)
        {
            (List<decimal> lowPeaks, List<int> lowPeaksIndex) = GetLowPeaks(candles, startIndex);
            List<decimal> supports = new List<decimal>();
            for(int i = 0; i < lowPeaks.Count; i++)
            {
                bool breaked = false;
                for (int x = candles.Count - 1; x > lowPeaksIndex[i]; x--)
                {
                    if(candles[x].min < lowPeaks[i])
                    {
                        breaked = true;
                        break;
                    }    
                }
                if (!breaked)
                    supports.Add(lowPeaks[i]);
            }
            return supports;
        }
        public static List<decimal> GetResistances(List<Candle> candles, int startIndex)
        {
            (List<decimal> highPeaks, List<int> highPeaksIndex) = GetHighPeaks(candles, startIndex);
            List<decimal> resistances = new List<decimal>();
            for (int i = 0; i < highPeaks.Count; i++)
            {
                bool breaked = false;
                for (int x = candles.Count - 1; x > highPeaksIndex[i]; x--)
                {
                    if (candles[x].max > highPeaks[i])
                    {
                        breaked = true;
                        break;
                    }
                }
                if (!breaked)
                    resistances.Add(highPeaks[i]);
            }
            return resistances;
        }
      
    }
}
