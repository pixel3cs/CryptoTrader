using System;

namespace CryptoTrader.Code
{
    public class Transaction
    {
        public double Price;
        public double Amount;
        public DateTime Time;
        public bool IsBuy;
        public string BaseAsset;
        public bool IsFinalToKeep;
        public Transaction Prev;

        public Transaction()
        {
        }

        public Transaction(Transaction prev)
        {
            this.Prev = prev;
        }

        public override string ToString()
        {
            string format = "{0} {1} ${2:0.00000}, {3}{4}";
            return string.Format(format,
                IsFinalToKeep ? "KEEP" : (IsBuy ? "BUY" : "SELL"),
                BaseAsset,
                Price,
                Time.ToString("dd.MM HH:mm:ss"),
                (IsBuy == false && Prev != null) ? string.Format(", {0:0.00}%", ((Price - Prev.Price) / Prev.Price) * 100) : "");
        }
    }
}
