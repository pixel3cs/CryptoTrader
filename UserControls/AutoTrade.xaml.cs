using Binance.Net.Objects.Spot.MarketStream;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CryptoTrader.UserControls
{
    public partial class AutoTrade : UserControl
    {
        private int tradeCount;
        private BinanceStreamAggregatedTrade lastDisplayedTrade;

        public AutoTrade()
        {
            InitializeComponent();
        }

        public void SetControlsTradeData(string symbol, bool showTargetLines, double leverage, double targetROE)
        {
            leverageTB.Value = leverage;
            targetROETb.Value = targetROE;
            showTargetLinesCB.IsChecked = showTargetLines;
            currentSymbolTb.Text = string.Format("Current Symbol: {0}", symbol);

            int interval = 0;
            foreach (TradeDataView tdv in tradeDataViews.Children.OfType<TradeDataView>())
            {
                tdv.SetTargetPrice(targetROETb.Value / leverageTB.Value, showTargetLines);
                tdv.SwitchData(symbol, Utils.ViewIntervals[interval++], Utils.RequestType.DoNotLoad);
            }

            BinanceTickClient.StartBroadcastingLastTickValue(symbol, LastTickValueHandler);
        }

        public void LastTickValueHandler(BinanceStreamAggregatedTrade trade)
        {
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
                        lastValueTB.Text = trade.Price.ToString("$0.00000");
                        lastTimeTB.Text = trade.TradeTime.ToString("HH:mm:ss.fff");
                        tradeCountTB.Text = tradeCount.ToString();

                        decimal moveTargetPercent = (decimal)targetROETb.Value / (decimal)leverageTB.Value;
                        currentPriceTb.Text = string.Format("Current Price: ${0:0.00000}", trade.Price);
                        targetPriceTb.Text = string.Format("Target Price: ±${0:0.00000}", (moveTargetPercent / 100m) * trade.Price);
                    });
                    
                }
                catch { }

                lastDisplayedTrade = trade;
            }
        }

        private void leverageTB_OnValueChangedEvent()
        {
            RecalculateTargets();
        }

        private void targetROETb_OnValueChangedEvent()
        {
            RecalculateTargets();
        }

        private void showTargetLines_Checked(object sender, RoutedEventArgs e)
        {
            RecalculateTargets();
        }

        private void RecalculateTargets()
        {
            if (targetROETb == null || leverageTB == null || targetMoveTb == null)
                return;

            double targetMovePercent = targetROETb.Value / leverageTB.Value;
            targetMoveTb.Text = string.Format("Target Move: {0:0.00}%", targetMovePercent);

            if (tradeDataViews != null)
            {
                foreach (TradeDataView tdv in tradeDataViews.Children.OfType<TradeDataView>())
                    tdv.SetTargetPrice(targetMovePercent, showTargetLinesCB.IsChecked.Value);
            }
        }

    }
}
