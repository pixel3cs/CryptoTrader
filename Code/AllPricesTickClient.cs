using Binance.Net;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Futures.MarketData;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CryptoTrader.Code
{
    public class AllPricesTickClient
    {
        private static BinanceClient bclient = null;
        private static BinanceSocketClient allPricesSocketClient = null;
        private static Dictionary<string, LivePrice> livePrices = new Dictionary<string, LivePrice>();

        public delegate void PricesAvailableDelegate(Dictionary<string, LivePrice>.ValueCollection prices);
        public static event PricesAvailableDelegate PricesAvailableEvent = null;

        public static void StartBroadcastingAllNewPrices()
        {
            ThreadStart ts = new ThreadStart(StartBroadcastingAllNewPricesThread);
            Thread thread = new Thread(ts);
            thread.IsBackground = true;
            thread.Start();
        }

        private static void StartBroadcastingAllNewPricesThread()
        {
            if (bclient != null)
                return;

            bclient = new Binance.Net.BinanceClient();

            var exchangeInfo = bclient.FuturesUsdt.System.GetExchangeInfo();
            MainWindow.UpdateWeightUsage(exchangeInfo.ResponseHeaders);

            var prices24h = bclient.FuturesUsdt.Market.Get24HPrices();
            MainWindow.UpdateWeightUsage(prices24h.ResponseHeaders);

            // process data
            List<string> ignoreList = new List<string> { };

            // get only coins that can be traded on futures and are USDT
            foreach (BinanceFuturesUsdtSymbol symbol in exchangeInfo.Data.Symbols)
                if (symbol.Status == Binance.Net.Enums.SymbolStatus.Trading && 
                    symbol.ContractType == Binance.Net.Enums.ContractType.Perpetual && 
                    symbol.OrderTypes.ToList().Contains(Binance.Net.Enums.OrderType.Market) &&
                    symbol.MarginAsset == "USDT" && 
                    ignoreList.Contains(symbol.Name) == false)
                {
                    LivePrice viewPrice = new LivePrice();
                    viewPrice.Symbol = symbol;
                    livePrices.Add(symbol.Name, viewPrice);
                }

            // set prices
            foreach (IBinanceTick price in prices24h.Data)
                if (livePrices.ContainsKey(price.Symbol))
                {
                    livePrices[price.Symbol].SetPrice(price);
                }

            if (PricesAvailableEvent != null)
                PricesAvailableEvent(livePrices.Values);

            // start monitoring price changes
            allPricesSocketClient = new BinanceSocketClient();
            var socketCall = allPricesSocketClient.FuturesUsdt.SubscribeToAllSymbolTickerUpdates(allSymbolMiniTickerServerHandler);
        }

        // fired once per second
        private static void allSymbolMiniTickerServerHandler(IEnumerable<IBinanceTick> ticks)
        {
            bool changed = false;
            foreach (IBinanceTick tick in ticks)
                if (livePrices.ContainsKey(tick.Symbol))
                {
                    livePrices[tick.Symbol].SetPrice(tick);
                    changed = true;
                }

            if (changed && PricesAvailableEvent != null)
                PricesAvailableEvent(livePrices.Values);
        }

    }
}
