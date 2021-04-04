using Binance.Net.Interfaces;
using System;

namespace CryptoTrader
{
    public class BinanceKLine : IBinanceKline
    {
        public DateTime OpenTime { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal BaseVolume { get; set; }
        public DateTime CloseTime { get; set; }
        public decimal QuoteVolume { get; set; }
        public int TradeCount { get; set; }
        public decimal TakerBuyBaseVolume { get; set; }
        public decimal TakerBuyQuoteVolume { get; set; }

        public decimal CommonHigh { get; }

        public decimal CommonLow { get; }

        public decimal CommonOpen { get; }

        public decimal CommonClose { get; }

        public DateTime CommonOpenTime { get; }

        public decimal CommonVolume { get; }
    }
}
