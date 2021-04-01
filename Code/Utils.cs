using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.MarketStream;
using System.Collections.Generic;

namespace CryptoTrader.Code
{
    public delegate void ServerDataProcessDelegate(IEnumerable<IBinanceKline> newKlines, bool erasePreviousData);
    public delegate void LastTickValueDelegate(BinanceStreamAggregatedTrade trade);

    public class Utils
    {
        public static List<string> ViewIntervals = new List<string>() { "1d", "2h", "15m", "1m" };

        public enum RequestType
        {
            LoadData,
            DoNotLoad
        }

        public enum TradingType
        {
            CandleStickSimulation,
            LiveSimulation,
            LiveTrading
        }

        public enum PriceSortType
        {
            PriceDownFromHigh24H = 1,
            PriceUpFromLow24H = 2,
            PriceDownSinceWatching = 3,
            PriceUpSinceWatching = 4
        }
    }

    
}
