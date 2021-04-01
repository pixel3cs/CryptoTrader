using CryptoTrader.Code;
using CryptoTrader.UserControls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static CryptoTrader.Code.Utils;

namespace CryptoTrader
{
    // https://fapi.binance.com/fapi/v1/exchangeInfo - limits, currently: 2400 req_weight / 1m, 300 orders / 10s, 1200 orders / 1m
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            int interval = 0;
            foreach (TradeDataView tdv in tradeDataViews.Children.OfType<TradeDataView>())
            {
                tdv.SwitchData("BTCUSDT", ViewIntervals[interval++], RequestType.DoNotLoad);
                tdv.SetTrendReversals(leverageTB.Value, targetROETb.Value);
            }

            AllPricesTickClient.PricesAvailableEvent += AllPricesTickClient_PricesAvailableEvent;
            AllPricesTickClient.StartBroadcastingAllNewPrices();
        }

        private void AllPricesTickClient_PricesAvailableEvent(Dictionary<string, LivePrice>.ValueCollection prices)
        {
            this.Dispatcher.Invoke(() =>
            {
                ComboBoxItem sortByItem = sortByCombo.SelectedItem as ComboBoxItem;
                PriceSortType priceSortType = (PriceSortType)int.Parse(sortByItem.Tag.ToString());                

                IOrderedEnumerable<LivePrice> orderedPrices = null;
                if (priceSortType == PriceSortType.PriceDownFromHigh24H) orderedPrices = prices.OrderBy(p => p.PriceChange24H); // fastest fall in 24 h
                if (priceSortType == PriceSortType.PriceUpFromLow24H) orderedPrices = prices.OrderByDescending(p => p.PriceChange24H); // fastest grow in 24 h
                if (priceSortType == PriceSortType.PriceDownSinceWatching) orderedPrices = prices.OrderByDescending(p => p.PriceDownSinceWatching); // fastest fall since start
                if (priceSortType == PriceSortType.PriceUpSinceWatching) orderedPrices = prices.OrderByDescending(p => p.PriceUpSinceWatching); // fastest grow since start

                usdt.Items.Clear();

                foreach (LivePrice price in orderedPrices)
                {
                    price.SortType = priceSortType;
                    usdt.Items.Add(price);
                }
            });
        }

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (usdt.SelectedIndex == -1)
                return;

            LivePrice livePrice = usdt.SelectedItem as LivePrice;
            if (livePrice.Symbol.Pair == tradeDataView.Symbol)
                return;

            livePrice.Displayed = true;
            int interval = 0;

            foreach (TradeDataView tdv in tradeDataViews.Children.OfType<TradeDataView>())
                tdv.SwitchData(livePrice.Price.Symbol, ViewIntervals[interval++], RequestType.DoNotLoad);
        }

        private void listBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == System.Windows.Input.Key.Up || e.Key == System.Windows.Input.Key.Down)
                e.Handled = true;
        }

        private void symbolSearch_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            foreach (LivePrice livePrice in usdt.Items)
            {
                if (livePrice.Symbol.BaseAsset.ToLower().StartsWith(usdtSearch.Text.ToLower()))
                    usdt.ScrollIntoView(livePrice);

                if (livePrice.Symbol.BaseAsset.ToLower() == usdtSearch.Text.ToLower() && usdt.SelectedItem != livePrice)
                {
                    usdt.SelectedItem = livePrice;
                    return;
                }
            }
        }

        private void tradeDataView_TradeSimulationChangedEvent(TradeHelper simulation)
        {
            tradeSimulationList.Items.Clear();

            if (simulation == null)
            {
                simulationProfitTB.Text = "";

                autoTradeBuyButton.Text = "Buy";
                autoTradeBuyButton.IsEnabled = false;
                autoTradeBuyButton.ClearValue(TextBlock.BackgroundProperty);
                autoTradeBuyButton.Tag = null;

                autoTradeWatchButton.Text = "Watch";
                autoTradeWatchButton.IsEnabled = false;
                autoTradeWatchButton.ClearValue(TextBlock.BackgroundProperty);
                autoTradeWatchButton.Tag = null;

                return;
            }

            string swingProfitStr = string.Format("Swing ({0:0.00}%)", simulation.SwingProfitPercent);
            string keepProfitStr = string.Format("Keep ({0:0.00}%)", simulation.HoldProfitPercent);
            simulationProfitTB.Text = string.Format("{0} > {1}", 
                simulation.SwingProfitPercent > simulation.HoldProfitPercent ? swingProfitStr : keepProfitStr,
                simulation.SwingProfitPercent > simulation.HoldProfitPercent ? keepProfitStr : swingProfitStr);

            autoTradeBuyButton.Text = "Buy " + tradeDataView.Symbol;
            autoTradeBuyButton.IsEnabled = true;
            autoTradeBuyButton.Background = Brushes.Green;
            autoTradeBuyButton.Tag = simulation;

            autoTradeWatchButton.Text = "Watch " + tradeDataView.Symbol;
            autoTradeWatchButton.IsEnabled = true;
            autoTradeWatchButton.Background = Brushes.DarkRed;
            autoTradeWatchButton.Tag = simulation;

            foreach (Transaction transaction in simulation.Transactions)
            {
                ListViewItem listViewItem = new ListViewItem();
                listViewItem.Content = transaction;
                listViewItem.Foreground = (transaction.IsBuy || transaction.IsFinalToKeep ? TradeDataView.green2Brush : TradeDataView.red2Brush);
                listViewItem.FontWeight = FontWeights.Bold;
                listViewItem.FontSize = 16;

                tradeSimulationList.Items.Add(listViewItem);
            }
        }

        private void autoTradeButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TextBlock autoTradeButton = sender as TextBlock;
            TradeHelper simulation = autoTradeButton.Tag as TradeHelper;
            bool enterWithBuy = (autoTradeButton == autoTradeBuyButton);
            TabItem tab = null;

            // select tab if exists
            tab = tabControl.Items.OfType<TabItem>().FirstOrDefault(t => t.Header.ToString() == simulation.Symbol);
            if (tab != null)
            {
                Dispatcher.BeginInvoke((Action)(() => tabControl.SelectedItem = tab));
                return;
            }

            // create new tab
            tab = new TabItem();
            AutoTrade autoTrade = new AutoTrade();
            autoTrade.SetControlsTradeData(simulation, enterWithBuy, 0, 0);

            tab.Header = simulation.Symbol;
            tab.IsSelected = true;
            tab.Content = autoTrade;
            tabControl.Items.Add(tab);

            Dispatcher.BeginInvoke((Action)(() => tabControl.SelectedItem = tab));
        }

        private void leverageTB_OnValueChangedEvent()
        {
            RecalculateTargets();
        }

        private void targetROETb_OnValueChangedEvent()
        {
            RecalculateTargets();
        }

        private void RecalculateTargets()
        {
            if (targetROETb == null || leverageTB == null || targetMoveTb == null)
                return;

            double moveTarget = targetROETb.Value / leverageTB.Value;
            targetMoveTb.Text = string.Format("Target Move: {0:0.00}%", moveTarget);

            if (tradeDataViews != null)
            {
                foreach (TradeDataView tdv in tradeDataViews.Children.OfType<TradeDataView>())
                    tdv.SetTrendReversals(leverageTB.Value, targetROETb.Value);                    
            }
        }

        public static void UpdateWeightUsage(IEnumerable<KeyValuePair<string, IEnumerable<string>>> responseHeaders)
        {
            if (responseHeaders == null)
                return;

            string general = string.Empty, for1m = string.Empty;
            foreach (var header in responseHeaders)
            {
                if (header.Key.ToLower() == "x-mbx-used-weight")
                    general = header.Value.ToList()[0];

                if (header.Key.ToLower() == "x-mbx-used-weight-1m")
                    for1m = header.Value.ToList()[0];
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow main = Application.Current.MainWindow as MainWindow;
                main.usedWeight.Text = string.Format("{0}, {1} / 1m", general, for1m);
            });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            foreach (TradeDataView tdv in tradeDataViews.Children.OfType<TradeDataView>())
                tdv.SafelyClose();

            base.OnClosing(e);
        }

    }
}
