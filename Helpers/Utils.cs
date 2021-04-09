using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.MarketStream;
using System.Collections.Generic;
using System.Windows;

namespace CryptoTrader
{
    public delegate void ServerDataProcessDelegate(IEnumerable<IBinanceKline> newKlines, bool erasePreviousData, bool isTick);
    public delegate void LastTickValueDelegate(BinanceStreamAggregatedTrade trade);

    public class Utils
    {
        public static readonly string InitialSymbol = "BTCUSDT";
        public static readonly List<string> ViewIntervals = new List<string>() { "4h", "2h", "1h", "30m", "15m", "5m", "3m", "1m" };
        public static readonly int NearDistance = 15; // pixels

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

        public static double CalculateViewWidth(double viewWidth, double lowestLeftX, double highestRightX, double xPosition)
        {
            if (highestRightX == lowestLeftX) return 0;
            return ((xPosition - lowestLeftX) * viewWidth) / (highestRightX - lowestLeftX);
        }

        public static double CalculateViewHeight(double viewHeight, double lowestPrice, double highestPrice, double price)
        {
            if (highestPrice == lowestPrice) return 0;
            return ((price - lowestPrice) * viewHeight) / (highestPrice - lowestPrice);
        }

        public static int IntervalInMinutes(string interval)
        {
            switch (interval)
            {
                case "1m": return 1;
                case "3m": return 3;
                case "5m": return 5;
                case "15m": return 15;
                case "30m": return 30;
                case "1h": return 60;
                case "2h": return 2 * 60;
                case "4h": return 4 * 60;
                case "12h": return 12 * 60;
                case "1d": return 24 * 60;
                default: return 0;
            }
        }

        public static KlineInterval ToKlineInterval(string interval)
        {
            switch (interval)
            {
                case "1m": return KlineInterval.OneMinute;
                case "3m": return KlineInterval.ThreeMinutes;
                case "5m": return KlineInterval.FiveMinutes;
                case "15m": return KlineInterval.FifteenMinutes;
                case "30m": return KlineInterval.ThirtyMinutes;
                case "1h": return KlineInterval.OneHour;
                case "2h": return KlineInterval.TwoHour;
                case "4h": return KlineInterval.FourHour;
                case "12h": return KlineInterval.TwelveHour;
                case "1d": return KlineInterval.OneDay;
                default: return KlineInterval.OneMinute;
            }
        }

        public static bool AreClosePoints(Point p1, Point p2, double nearDistance)
        {
            return ((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y)) < nearDistance * nearDistance;
        }

    }

    
}
