using System;
using System.Collections.Generic;
using static CryptoTrader.Utils;

namespace CryptoTrader
{
    public class TradeHelper
    {
        private double riskAmount = 100, boughtShares = 0;
        private double marketOpenPrice, marketClosePrice;
        private double swingProfit = 0;
        private TradingType TradingType;

        public double CurrentMax = int.MinValue, CurrentMin = int.MaxValue, CurrentPrice = 0;
        
        public string Symbol;
        public double TrendReversalDown, TrendReservalUP; // in %
        public bool LastTrendIsDown;
        public double LastTrendOpenPrice; // used to start AutoTrade
        public List<Transaction> Transactions;

        private DateTime[] greenStartViewHelpers = new DateTime[500];
        private DateTime[] greenEndViewHelpers = new DateTime[500];

        public bool LastTrendIsUp { get { return LastTrendIsDown == false; } }
        public double SwingProfitPercent { get { return (swingProfit / riskAmount) * 100; } }
        public double HoldProfitPercent { get { return (marketClosePrice / marketOpenPrice - 1) * 100; } }

        public TradeHelper(double marketOpenPrice, double marketClosePrice, double trendReversalDown, double trendReservalUP, string symbol, TradingType tradingType)
        {
            this.marketOpenPrice = marketOpenPrice;
            this.marketClosePrice = marketClosePrice;
            
            LastTrendIsDown = (marketClosePrice < marketOpenPrice);
            LastTrendOpenPrice = marketOpenPrice;
            TrendReversalDown = trendReversalDown;
            TrendReservalUP = trendReservalUP;
            Symbol = symbol;
            TradingType = tradingType;

            Transactions = new List<Transaction>();
        }

        public bool HasChangedUp()
        {
            double upChange = (CurrentPrice / CurrentMin) - 1d;
            return upChange > TrendReservalUP / 100d;
        }

        public bool HasChangedDown()
        {
            double downChange = 1d - (CurrentPrice / CurrentMax);
            return downChange > TrendReversalDown / 100d;
        }

        public Transaction Buy(DateTime time)
        {
            if (boughtShares > 0)
                return null;

            double buyPrice = CurrentPrice;
            if(TradingType == TradingType.CandleStickSimulation)
                buyPrice = CurrentMin * (1d + TrendReservalUP / 100d);

            Transaction transaction = AddTransaction(true, buyPrice, time, false);

            LastTrendOpenPrice = buyPrice;
            boughtShares = riskAmount / buyPrice;
            
            CurrentMin = buyPrice;
            CurrentMax = buyPrice;
            LastTrendIsDown = false;

            return transaction;
        }

        public Transaction Sell(DateTime time)
        {
            if (boughtShares == 0)
                return null;

            double sellPrice = CurrentPrice;
            if (TradingType == TradingType.CandleStickSimulation)
                sellPrice = CurrentMax * (1d - TrendReversalDown / 100d);

            Transaction transaction = AddTransaction(false, sellPrice, time, false);

            LastTrendOpenPrice = sellPrice;
            swingProfit = swingProfit + sellPrice * boughtShares - riskAmount;
            boughtShares = 0;

            CurrentMin = sellPrice;
            CurrentMax = sellPrice;
            LastTrendIsDown = true;

            return transaction;
        }

        public void SellLastOpenPosition(DateTime time)
        {
            if (boughtShares == 0)
                return;

            double sellPrice = marketClosePrice;
            AddTransaction(false, sellPrice, time, true);

            swingProfit = swingProfit + sellPrice * boughtShares - riskAmount;
            boughtShares = 0;            
        }

        private Transaction AddTransaction(bool isBuy, double price, DateTime time, bool isFinalToKeep)
        {
            Transaction transaction = new Transaction()
            {
                IsBuy = isBuy,
                BaseAsset = Symbol,
                Amount = riskAmount,
                Price = price,
                Time = time,
                IsFinalToKeep = isFinalToKeep,
                Prev = (Transactions.Count > 0) ? Transactions[Transactions.Count - 1] : null
            };

            Transactions.Add(transaction);

            // process view helpers
            int index = (Transactions.Count - 1) / 2;
            if (index >= greenStartViewHelpers.Length) index = 0;
            if (isBuy)
                greenStartViewHelpers[index] = time;
            else
                greenEndViewHelpers[index] = time;

            return transaction;
        }

        public bool IsGreenCandle(DateTime time)
        {
            for (int i = 0; i < greenEndViewHelpers.Length; i++)
                if (greenStartViewHelpers[i].Year > 1000)
                {
                    if (time >= greenStartViewHelpers[i] && greenEndViewHelpers[i].Year < 1000)
                        return true; // last green trend

                    if (time >= greenStartViewHelpers[i] && time <= greenEndViewHelpers[i])
                        return true; // inside green trend
                }
                else
                    break;

            return false;
        }

    }
}
