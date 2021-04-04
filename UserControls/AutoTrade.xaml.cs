using Binance.Net.Objects.Spot.MarketStream;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CryptoTrader.UserControls
{
    public partial class AutoTrade : UserControl
    {
        private TradeHelper tradeHelper;
        private int tradeCount;
        private BinanceStreamAggregatedTrade lastDisplayedTrade;
        private bool enterWithBuy;

        public AutoTrade()
        {
            InitializeComponent();
        }

        public void SetControlsTradeData(TradeHelper simulation, bool enterWithBuy, double trendReversalDown, double trendReversalUP)
        {
            tradeHelper = new TradeHelper(simulation.CurrentPrice, simulation.CurrentPrice, trendReversalDown, trendReversalUP, simulation.Symbol, Utils.TradingType.LiveSimulation);
            tradeHelper.LastTrendIsDown = (enterWithBuy == false);
            this.enterWithBuy = enterWithBuy;

            List<string> intervals = new List<string>() { "1h", "15m", "3m", "30s", "5s" };
            foreach (UIElement ui in mainGrid.Children)
                if (ui is TradeDataView)
                {
                    TradeDataView tdv = ui as TradeDataView;
                    tdv.SetTargetPrice(0);
                    tdv.SetRealTimeTradesList(tradeHelper);
                    tdv.SwitchData(Utils.InitialSymbol, intervals[0], Utils.RequestType.DoNotLoad);
                    tdv.SwitchData(simulation.Symbol, intervals[0], Utils.RequestType.DoNotLoad);
                    intervals.RemoveAt(0);
                }

            this.trendReversalUP.Value = trendReversalUP;
            this.trendReversalDown.Value = trendReversalDown;

            BinanceTickClient.StartBroadcastingLastTickValue(simulation.Symbol, LastTickValueHandler);
        }

        public void LastTickValueHandler(BinanceStreamAggregatedTrade trade)
        {
            tradeHelper.CurrentPrice = (double)trade.Price;
            if (tradeHelper.CurrentPrice < tradeHelper.CurrentMin) tradeHelper.CurrentMin = tradeHelper.CurrentPrice;
            if (tradeHelper.CurrentPrice > tradeHelper.CurrentMax) tradeHelper.CurrentMax = tradeHelper.CurrentPrice;

            // do initial buy
            if (enterWithBuy)
            {
                Transaction transaction = tradeHelper.Buy(trade.TradeTime);
                AddTransactionToListView(transaction);
                enterWithBuy = false;
            }

            //// up > trendReservalUP % => trend reversal
            //if (tradeHelper.LastTrendIsDown && tradeHelper.HasChangedUp())
            //{
            //    Transaction transaction = tradeHelper.Buy(trade.TradeTime); // buy here
            //    AddTransactionToListView(transaction);
            //}

            // down > trendReversalDown % => trend reversal
            if (tradeHelper.LastTrendIsUp && tradeHelper.HasChangedDown())
            {
                Transaction transaction = tradeHelper.Sell(trade.TradeTime); // sell here
                AddTransactionToListView(transaction);
            }

            // update view
            UpdateView(trade);
        }

        private void UpdateView(BinanceStreamAggregatedTrade trade)
        {
            tradeCount++;

            if (lastDisplayedTrade == null)
                lastDisplayedTrade = trade;

            if ((trade.TradeTime - lastDisplayedTrade.TradeTime).TotalMilliseconds > 100)
            {
                try
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        lastValueTB.Text = trade.Price.ToString("0.00000");
                        lastTimeTB.Text = trade.TradeTime.ToString("HH:mm:ss.fff");
                        tradeCountTB.Text = tradeCount.ToString();
                    });
                    
                }
                catch { }

                lastDisplayedTrade = trade;
            }
        }

        public void AddTransactionToListView(Transaction transaction)
        {
            if (transaction == null)
                return;

            this.Dispatcher.Invoke(() =>
            {
                profitTB.Text = string.Format("{0:0.00}%", tradeHelper.SwingProfitPercent);

                ListViewItem listViewItem = new ListViewItem();
                listViewItem.Content = transaction;
                listViewItem.Foreground = (transaction.IsBuy || transaction.IsFinalToKeep ? TradeDataView.green2Brush : TradeDataView.red2Brush);
                listViewItem.FontWeight = FontWeights.Bold;
                listViewItem.FontSize = 16;

                tradesList.Items.Add(listViewItem);
            });
        }

        private void trendReserval_OnValueChangedEvent()
        {
            tradeHelper.TrendReversalDown = trendReversalDown.Value;
            tradeHelper.TrendReservalUP = trendReversalUP.Value;

            foreach (UIElement ui in mainGrid.Children)
                if (ui is TradeDataView)
                {
                    TradeDataView tdv = ui as TradeDataView;
                    tdv.SetTargetPrice(0);
                }
        }
    }
}
