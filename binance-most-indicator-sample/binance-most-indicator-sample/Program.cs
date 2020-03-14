using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Market;
using MostDLL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Trady.Core;
using Trady.Core.Infrastructure;

namespace binance_most_indicator_sample
{
    class Program
    {
        private static readonly DateTime StartUnixTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        static void Main(string[] args)
        {
            var binanceClient = new BinanceClient(
                new ApiClient(
                    "Binance_Api_Key",
                    "Binance_Api_Secret"
                ));

            Console.BufferHeight = Int16.MaxValue - 1;

            TekSembolTaramaOrnegi(binanceClient);

            //CokSembolTaramaOrnegi(binanceClient);

            Console.ReadLine();
        }

        private static void TekSembolTaramaOrnegi(BinanceClient binanceClient)
        {
            //Bu örnekte tek sembolü 1 saatlik periyotta tarayacağız.
            var sembol = "BTCUSDT";

            //bu listeye denemek istediğiniz ema değerlerini girebilirsiniz { 1, 2, 3, 5, 8, 13, 21, 34 } gibi
            var emaUzunlukList = new[] { 3 };

            //test etmek istediğiniz % miktarlarını girebilirsiniz { 0.01, 0.02, 0.03, 0.05, 0.08, 0.13 } gibi. 0.01 = 1%
            var yuzdeList = new[] { 0.02 };

            //Binance api üzerinden bar datalarını çekiyoruz. Where koşulu kullanmazsanız tüm sembolleri verir. 
            //Where şartında USDT kullanırsanız sadece Tether paritesinde olanları verir. Sadece btc-usdt verisini çekmek için BTCUSDT şeklinde bıraktım.
            var tickerPrices = binanceClient.GetAllPrices().Result
                .Where(x => x.Symbol.EndsWith(sembol))
                .Select(i => new SymbolPrice { Symbol = i.Symbol, Price = i.Price })
                .OrderBy(x => x.Symbol).ToList();

            //Sembolümüzün 1 saatlik periyotta 500 barlık datasını çekiyoruz. Max 1000 bar veriyor api.
            var candlestick = binanceClient.GetCandleSticks(sembol, TimeInterval.Hours_1, limit: 500).Result;

            //Henüz kapanmamış olan barı dikkate almaması için where koşulu uyguluyoruz.
            var candlesticks = candlestick as Candlestick[] ?? candlestick.Where(x => StartUnixTime.AddMilliseconds(x.CloseTime).ToUniversalTime() < DateTime.Now.ToUniversalTime()).ToArray();
            if (candlesticks.Length <= 360) Console.WriteLine("360 bar ve altındaki datalarda işlem doğru sonuçlar vermediği için hesaplama yapılmadı.");

            List<IOhlcv> tradyCandles = candlesticks.Select(candle => new Candle(StartUnixTime.AddMilliseconds(candle.OpenTime).ToUniversalTime(), candle.Open, candle.High, candle.Low, candle.Close, candle.Volume)).Cast<IOhlcv>().ToList();

            foreach (var e in emaUzunlukList)
            {
                foreach (var y in yuzdeList)
                {
                    var mostSonucList = MostHesapla.Hesapla(tradyCandles, e, y);

                    if (mostSonucList.Count == 0) Console.WriteLine("360 bar ve altındaki datalarda işlem doğru sonuçlar vermediği için hesaplama yapılmadı.");

                    for (int i = 0; i < mostSonucList.Count; i++)
                    {
                        DateTime utc = mostSonucList[i].Bar.DateTime.UtcDateTime;
                        Console.WriteLine(i + " - Bar: " + utc.ToLocalTime() + " Ema: " + $"{mostSonucList[i].EmaDegeri:N8}" + " Most: " + $"{mostSonucList[i].MostDegeri:N8}" + " Durum: " + mostSonucList[i].MostDurum);
                    }
                }
            }
        }

        private static void CokSembolTaramaOrnegi(BinanceClient binanceClient)
        {
            //bu listeye denemek istediğiniz ema değerlerini girebilirsiniz { 1, 2, 3, 5, 8, 13, 21, 34 } gibi
            var emaUzunlukList = new[] { 3 };

            //test etmek istediğiniz % miktarlarını girebilirsiniz { 0.01, 0.02, 0.03, 0.05, 0.08, 0.13 } gibi. 0.01 = 1%
            var yuzdeList = new[] { 0.02 };

            //Binance api üzerinden bar datalarını çekiyoruz. Where koşulu kullanmazsanız tüm sembolleri verir. 
            //Where şartında USDT kullanırsanız sadece Tether paritesinde olanları verir. Sadece btc-usdt verisini çekmek için BTCUSDT şeklinde bıraktım.
            var tickerPrices = binanceClient.GetAllPrices().Result
                .Where(x => x.Symbol.EndsWith("USDT"))
                .Select(i => new SymbolPrice { Symbol = i.Symbol, Price = i.Price })
                .OrderBy(x => x.Symbol).ToList();

            foreach (var coin in tickerPrices)
            {
                //Sembolümüzün 1 saatlik periyotta 500 barlık datasını çekiyoruz. Max 1000 bar veriyor api.
                var candlestick = binanceClient.GetCandleSticks(coin.Symbol, TimeInterval.Hours_1, limit: 500).Result;

                //Henüz kapanmamış olan barı dikkate almaması için where koşulu uyguluyoruz.
                var candlesticks = candlestick as Candlestick[] ?? candlestick.Where(x => StartUnixTime.AddMilliseconds(x.CloseTime).ToUniversalTime() < DateTime.Now.ToUniversalTime()).ToArray();
                if (candlesticks.Length <= 360)
                {
                    Console.WriteLine(coin.Symbol + " 360 bar ve altındaki datalarda işlem doğru sonuçlar vermediği için hesaplama yapılmadı.");
                    continue;
                }

                List<IOhlcv> tradyCandles = candlesticks.Select(candle => new Candle(StartUnixTime.AddMilliseconds(candle.OpenTime).ToUniversalTime(), candle.Open, candle.High, candle.Low, candle.Close, candle.Volume)).Cast<IOhlcv>().ToList();

                foreach (var e in emaUzunlukList)
                {
                    foreach (var y in yuzdeList)
                    {
                        var mostSonucList = MostHesapla.Hesapla(tradyCandles, e, y);

                        if (mostSonucList.Count == 0) Console.WriteLine(coin.Symbol + " 360 bar ve altındaki datalarda işlem doğru sonuçlar vermediği için hesaplama yapılmadı.");

                        Console.WriteLine("=============== " + coin.Symbol + " BAŞLADI ===============");

                        for (int i = 0; i < mostSonucList.Count; i++)
                        {
                            DateTime utc = mostSonucList[i].Bar.DateTime.UtcDateTime;
                            Console.WriteLine(i + " - Sembol: " + coin.Symbol + " Bar: " + utc.ToLocalTime() + " Ema: " + $"{mostSonucList[i].EmaDegeri:N8}" + " Most: " + $"{mostSonucList[i].MostDegeri:N8}" + " Durum: " + mostSonucList[i].MostDurum);
                        }

                        Console.WriteLine("=============== " + coin.Symbol + " BİTTİ ===============");
                    }
                }
            }


        }

    }
}
