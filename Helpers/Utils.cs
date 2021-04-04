using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.MarketStream;
using System.Collections.Generic;

namespace CryptoTrader
{
    public delegate void ServerDataProcessDelegate(IEnumerable<IBinanceKline> newKlines, bool erasePreviousData, bool isTick);
    public delegate void LastTickValueDelegate(BinanceStreamAggregatedTrade trade);

    public class Utils
    {
        public static string InitialSymbol = "BTCUSDT";
        public static List<string> ViewIntervals = new List<string>() { "1d", "4h", "15m", "1m" };

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

        public enum TrendLineType
        {
            Normal,
            TargetLong,
            TargetCurrent,
            TargetShort
        }

        public static double CalculateViewWidth(double viewWidth, double lowestLeft, double highestRight, double priceWidth)
        {
            if (highestRight == lowestLeft) return 0;
            return ((priceWidth - lowestLeft) * viewWidth) / (highestRight - lowestLeft);
        }

        public static double CalculateViewHeight(double viewHeight, double lowestLow, double highestHigh, double priceHeight)
        {
            if (highestHigh == lowestLow) return 0;
            return ((priceHeight - lowestLow) * viewHeight) / (highestHigh - lowestLow);
        }

        public static int IntervalInMinutes(string interval)
        {
            return 0;
        }
    }

    
}
