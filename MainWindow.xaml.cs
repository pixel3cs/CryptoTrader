using CryptoTrader.DataObjects;
using CryptoTrader.UserControls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static CryptoTrader.Utils;

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
                tdv.SetTargetPrice(targetROETb.Value / leverageTB.Value, showTargetLinesCB.IsChecked.Value);
                tdv.SwitchData(Utils.InitialSymbol, ViewIntervals[interval++], RequestType.DoNotLoad);                
            }

            currentSymbolTb.Text = string.Format("Current Symbol: {0}", Utils.InitialSymbol);

            // load tabs
            List<JSONTab> tabsList = JSONTab.LoadFromDisk();
            foreach (JSONTab jsonTab in tabsList)
                AddAutoTradeTab(jsonTab);

            // broadcast all prices
            AllPricesTickClient.PricesAvailableEvent += AllPricesTickClient_PricesAvailableEvent; // fired once per second
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

                    if (price.Symbol.Name == tradeDataView.Symbol)
                    {
                        decimal moveTargetPercent = (decimal)targetROETb.Value / (decimal)leverageTB.Value;
                        currentPriceTb.Text = string.Format("Current Price: ${0:0.00000}", price.Price.LastPrice);
                        targetPriceTb.Text = string.Format("Target Price: ±${0:0.00000}", (moveTargetPercent / 100m) * price.Price.LastPrice);
                    }
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
            currentSymbolTb.Text = string.Format("Current Symbol: {0}", livePrice.Symbol.Name);

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

        private void autoTradeButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TabItem tab = null;

            // select tab if exists
            tab = tabControl.Items.OfType<TabItem>().FirstOrDefault(t => t.Header.ToString() == tradeDataView.Symbol);
            if (tab != null)
            {
                Dispatcher.BeginInvoke((Action)(() => tabControl.SelectedItem = tab));
                return;
            }

            // create new tab
            JSONTab jsonTab = new JSONTab() { Symbol = tradeDataView.Symbol, ShowTargetLines = showTargetLinesCB.IsChecked.Value, Leverage = leverageTB.Value, TargetROE = targetROETb.Value };
            tab = AddAutoTradeTab(jsonTab);
            JSONTab.SaveToDisk(jsonTab);

            tab.IsSelected = true;
            Dispatcher.BeginInvoke((Action)(() => tabControl.SelectedItem = tab));
        }

        private TabItem AddAutoTradeTab(JSONTab jsonTab)
        {
            AutoTrade autoTrade = new AutoTrade();
            autoTrade.CloseTabEvent += AutoTrade_CloseTabEvent;
            autoTrade.SetControlsTradeData(jsonTab);

            TabItem tab = new TabItem();
            tab.Header = jsonTab.Symbol;
            tab.Content = autoTrade;
            tabControl.Items.Add(tab);

            return tab;
        }

        private void AutoTrade_CloseTabEvent(AutoTrade autoTrade)
        {
            TabItem tab = autoTrade.Parent as TabItem;
            tabControl.Items.Remove(tab);
            JSONTab.RemoveFromDisk(autoTrade.currentSymbolTb.Tag.ToString());
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
                if (string.IsNullOrEmpty(general) == false || string.IsNullOrEmpty(for1m) == false)
                {
                    MainWindow main = Application.Current.MainWindow as MainWindow;
                    main.usedWeight.Text = string.Format("{0}, {1} / 1m", general, for1m);
                }
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
