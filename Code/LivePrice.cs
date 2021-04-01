using Binance.Net.Interfaces;
using Binance.Net.Objects.Futures.MarketData;
using static CryptoTrader.Code.Utils;

namespace CryptoTrader.Code
{
    public class LivePrice
    {
        public PriceSortType SortType = PriceSortType.PriceDownFromHigh24H;
        public bool Displayed = false;

        public int Index;
        public decimal PriceChange24H;

        private decimal PriceMaxSinceWatching = decimal.MinValue, PriceMinSinceWatching = decimal.MaxValue;
        public decimal PriceDownSinceWatching, PriceUpSinceWatching;

        public IBinanceTick Price { get; private set; }
        public BinanceFuturesUsdtSymbol Symbol;
        
        public LivePrice()
        {
        }

        public void SetPrice(IBinanceTick newPrice)
        {
            PriceChange24H = newPrice.PriceChangePercent;

            if (newPrice.LastPrice > PriceMaxSinceWatching) PriceMaxSinceWatching = newPrice.LastPrice;
            if (newPrice.LastPrice < PriceMinSinceWatching) PriceMinSinceWatching = newPrice.LastPrice;
            PriceDownSinceWatching = (1 - newPrice.LastPrice / PriceMaxSinceWatching) * 100;
            PriceUpSinceWatching = (1 - PriceMinSinceWatching / newPrice.LastPrice) * 100;

            Price = newPrice;
        }

        public override string ToString()
        {
            string upChar = char.ConvertFromUtf32(0x1F879);
            string downChar = char.ConvertFromUtf32(0x1F87B);
            decimal displayedPercentage = 0;
            string displayedCharacter = "";

            switch (SortType)
            {
                case PriceSortType.PriceDownFromHigh24H:
                    displayedPercentage = PriceChange24H;
                    displayedCharacter = downChar;
                    break;
                case PriceSortType.PriceUpFromLow24H:
                    displayedPercentage = PriceChange24H;
                    displayedCharacter = upChar;
                    break;
                case PriceSortType.PriceDownSinceWatching:
                    displayedPercentage = PriceDownSinceWatching;
                    displayedCharacter = downChar;
                    break;
                case PriceSortType.PriceUpSinceWatching:
                    displayedPercentage = PriceUpSinceWatching;
                    displayedCharacter = upChar;
                    break;
                default:break;
            }
                        
            return string.Format("{2}{3:0.00}% {0} ${1:0.0}m {4}", 
                Symbol.BaseAsset,
                Price.QuoteVolume / 1000000,
                displayedCharacter,
                displayedPercentage,
                Displayed ? "----------" : ""
                );
        }
    }
}
