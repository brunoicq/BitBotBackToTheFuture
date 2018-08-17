﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace BitBotBackToTheFuture
{
    class MainClass
    {


        //REAL NET
        static string bitmexKey = "";
        static string bitmexSecret = "";
        static int qtdyContacts = 0;
        static double profit = 0;
        static double fee = 0.075;
        private static string bitmexDomain = "https://www.bitmex.com";
        static BitMEX.BitMEXApi bitMEXApi = null;
        public static void Main(string[] args)
        {
            //Config
            String key = System.IO.File.ReadAllText(@"c:\bitmex\key.txt");
            bitmexKey = key.Split(';')[0].Trim();
            bitmexSecret = key.Split(';')[1].Trim();
            bitmexDomain = key.Split(';')[2].Trim();
            qtdyContacts = int.Parse(key.Split(';')[3].Trim());
            profit = double.Parse(key.Split(';')[4].Trim());

            Console.WriteLine("Deleron - Back to the future - v0.0.0.2 - Bitmex version");
            bitMEXApi = new BitMEX.BitMEXApi(bitmexKey, bitmexSecret, bitmexDomain);

            Console.WriteLine("Total open orders: " + bitMEXApi.GetOpenOrders("XBTUSD").Count);
            Console.WriteLine("");

            //makeOrder("Sell");
            while (true)
            {
                List<BitMEX.Candle> lstCandle = bitMEXApi.GetCandleHistory("XBTUSD", 100, "1m");

                double[] arrayPriceClose = new double[100];
                int i = 0;
                foreach (var candle in lstCandle)
                {
                    arrayPriceClose[i] = (double)candle.Close;
                    i++;
                }

                Array.Reverse(arrayPriceClose);

                double[] rsi = Indicators.RSI(arrayPriceClose, 7);
                double[] ema3 = Indicators.EMA(arrayPriceClose, 3);
                double[] ema5 = Indicators.EMA(arrayPriceClose, 5);
                double[] cog = Indicators.COG(arrayPriceClose, 10);

                Console.WriteLine(DateTime.Now);
                Console.WriteLine("CLOSE PRICE " + lstCandle[0].Close);
                Console.WriteLine("RSI " + rsi[99]);
                Console.WriteLine("EMA 5 " + ema5[99]);
                Console.WriteLine("EMA 3 " + ema3[99]);
                Console.WriteLine("COG " + cog[99]);
                Console.WriteLine("");


                if (ema3[98] < ema5[98] && ema3[99] > ema5[99] && rsi[99] > 40 && rsi[98] > rsi[97])
                {
                    makeOrder("Buy");
                }
                if (ema3[98] > ema5[98] && ema3[99] < ema5[99] && rsi[99] < 40 && rsi[98] < rsi[97])
                {
                    makeOrder("Sell");
                }

                Console.WriteLine("wait 10s");
                Thread.Sleep(10000);
            }






        }


        static void makeOrder(string side)
        {

            Console.WriteLine("Make order " + side);
            if (side == "Sell")
            {
                String json = bitMEXApi.MarketOrder("XBTUSD", "Sell", qtdyContacts);
                Console.WriteLine(json);
                JContainer jCointaner = (JContainer)JsonConvert.DeserializeObject(json, (typeof(JContainer)));
                double price = double.Parse(jCointaner["price"].ToString().Replace(".", "."));
                price -= (price * (fee + profit)) / 100;
                price = Math.Floor(price);
                json = bitMEXApi.PostOrderPostOnly("XBTUSD", "Buy", price, qtdyContacts);
                Console.WriteLine(json);
            }
            if (side == "Buy")
            {
                String json = bitMEXApi.MarketOrder("XBTUSD", "Buy", qtdyContacts);
                Console.WriteLine(json);
                JContainer jCointaner = (JContainer)JsonConvert.DeserializeObject(json, (typeof(JContainer)));
                double price = double.Parse(jCointaner["price"].ToString().Replace(".", "."));
                price += (price * (fee + profit)) / 100;
                price = Math.Ceiling(price);
                json = bitMEXApi.PostOrderPostOnly("XBTUSD", "Sell", price, qtdyContacts);
                Console.WriteLine(json);
            }

            Console.WriteLine("wait 60s");
            Thread.Sleep(60000);

        }

        class Indicators
        {
            public static double[] COG(double[] price, int period)
            {

                var cog = new double[price.Length];
                for (int i = period - 1; i < price.Length; ++i)
                {
                    var weightedSum = 0.0;
                    var sum = 0.0;
                    for (int j = 0; j < period; ++j)
                    {
                        weightedSum += price[i - period + j + 1] * (period - j);
                        sum += price[i - period + j + 1];
                    }

                    cog[i] = -weightedSum / sum;
                }

                return cog;
            }
            public static double[] EMA(double[] price, int period)
            {
                var ema = new double[price.Length];
                double sum = price[0];
                double coeff = 2.0 / (1.0 + period);

                for (int i = 0; i < price.Length; i++)
                {
                    sum += coeff * (price[i] - sum);
                    ema[i] = sum;
                }

                return ema;
            }

            public static double[] RSI(double[] price, int period)
            {
                var rsi = new double[price.Length];

                double gain = 0.0;
                double loss = 0.0;

                // first RSI value
                rsi[0] = 0.0;
                for (int i = 1; i <= period; ++i)
                {
                    var diff = price[i] - price[i - 1];
                    if (diff >= 0)
                    {
                        gain += diff;
                    }
                    else
                    {
                        loss -= diff;
                    }
                }

                double avrg = gain / period;
                double avrl = loss / period;
                double rs = gain / loss;
                rsi[period] = 100 - (100 / (1 + rs));

                for (int i = period + 1; i < price.Length; ++i)
                {
                    var diff = price[i] - price[i - 1];

                    if (diff >= 0)
                    {
                        avrg = ((avrg * (period - 1)) + diff) / period;
                        avrl = (avrl * (period - 1)) / period;
                    }
                    else
                    {
                        avrl = ((avrl * (period - 1)) - diff) / period;
                        avrg = (avrg * (period - 1)) / period;
                    }

                    rs = avrg / avrl;

                    rsi[i] = 100 - (100 / (1 + rs));
                }

                return rsi;
            }
        }

    }
}
