using Binance.Net.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;
using static CryptoTrader.Utils;
using System.Windows.Shapes;

namespace CryptoTrader.UserControls
{
    public partial class TradeDataView : UserControl
    {
        public static readonly Brush greenBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#62B985"));
        public static readonly Brush redBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D04A57"));
        public static readonly Brush green2Brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B8B0B"));
        public static readonly Brush red2Brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BB2C30"));
        public static readonly Brush grayBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));
        public static readonly Brush buyBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD800"));
        public static readonly Brush sellBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0094FF"));
        public static readonly Brush alteredBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        public static readonly Brush realRedBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0000"));
        public static readonly Brush realGreenBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00"));

        protected object viewControlsLock = new object();
        protected bool isDrawing = false;

        private int row = 0, column = 0;
        private bool maximized = false;

        private double zoomFactor = 2;

        private bool movingObjectsWithMouse = false;
        private Point cursorPosition = new Point();
        private double priceAtCursorPosition = 0;

        private double lowestLowPrice, highestHighPrice;
        private CandleStick firstKline, lastKline;

        private LivePriceData livePriceData = null;
        private TradeHelper realTimeTrades = null;

        public string Symbol { get { return (string)symbolsPanel.Tag; } }
        public string Interval { get { return (string)intervalsPanel.Tag; } }
        public string CandleType { get { return (string)candelTypesPanel.Tag; } }

        public double TargetMovePercent { get; private set; }
        

        private bool showTicks = false;
        public bool ShowTicks { get { return showTicks; } set { showTicks = value; showHideTicksIntervals(); } }

        public delegate void TradeSimulationChangedDelegate(TradeHelper tradeSimulation);
        public event TradeSimulationChangedDelegate TradeSimulationChangedEvent;

        public TradeDataView()
        {
            InitializeComponent();

            greenBrush.Freeze();
            redBrush.Freeze();
            green2Brush.Freeze();
            red2Brush.Freeze();
            grayBrush.Freeze();
            buyBrush.Freeze();
            sellBrush.Freeze();
            alteredBrush.Freeze();
            realRedBrush.Freeze();
            realGreenBrush.Freeze();

            livePriceData = new LivePriceData(ServerDataHandler);
        }

        public void SwitchData(string newSymbol, string newInterval, RequestType requestType)
        {
            if (newSymbol != Symbol || newInterval != Interval)
            {
                lock (viewControlsLock)
                {
                    // clear view
                    klinesView.Children.Clear();

                    dataLoadTime.Text = renderLoadTime.Text = cursorPrice.Text = profitTB.Text = intervalAVG.Text = "";

                    if (TradeSimulationChangedEvent != null)
                        TradeSimulationChangedEvent(null);
                }

                selectPanelUIOption(symbolsPanel, newSymbol);
                selectPanelUIOption(intervalsPanel, newInterval);

                if (requestType == RequestType.LoadData)
                    livePriceData.LoadFromServer(newSymbol, newInterval);
            }
        }

        public void ServerDataHandler(IEnumerable<IBinanceKline> newKlines, bool erasePreviousData, bool isTick)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    dataLoadTime.Text = string.Format("{0:0} ms", livePriceData.LastDataLoadTime.TotalMilliseconds);

                    lock (viewControlsLock)
                    {
                        if (erasePreviousData)
                            klinesView.Children.Clear();

                        if (isTick == false && klinesView != null)
                        {
                            // add new klines
                            foreach (var kline in newKlines)
                                klinesView.Children.Add(new CandleStick(kline));

                            // add target trend lines
                            foreach (var targetTrendLine in TrendLineHelper.GetTargetLines(Interval, TargetMovePercent, newKlines.Last()))
                                klinesView.Children.Add(new TrendLineStick(targetTrendLine));

                            // add new trend lines
                            foreach (var trendLine in TrendLineData.LoadTrendLines(Symbol, Interval))
                                klinesView.Children.Add(new TrendLineStick(trendLine));
                        }

                        if (isTick && newKlines != null)
                        {
                            // add new klines
                            foreach (var kline in newKlines)
                                klinesView.Children.Add(new CandleStick(kline));

                            // remove extra klines
                            int maxLines = 500;
                            int candleSticksCount = klinesView.Children.OfType<CandleStick>().Count();
                            if (candleSticksCount > maxLines)
                            {
                                List<CandleStick> removedCSList = klinesView.Children.OfType<CandleStick>().Take(candleSticksCount - maxLines).ToList();
                                foreach(CandleStick removedCS in removedCSList)
                                    klinesView.Children.Remove(removedCS);
                            }
                        }
                    }

                    setRenderTransform(Matrix.Identity, true);

                    DrawKLines();
                });
            }
            catch
            {
            }
        }

        public void SetTargetPrice(double targetMovePercent)
        {
            this.TargetMovePercent = targetMovePercent;

            if (klinesView.Children.Count > 0)
            {
                TrendLineHelper.UpdateTargetLines(klinesView, targetMovePercent);
                DrawKLines();
            }
        }

        public void SetRealTimeTradesList(TradeHelper tradeHelper)
        {
            realTimeTrades = tradeHelper;
        }

        private void selectPanelUIOption(StackPanel panel, string selectedValue, int option = 0)
        {
            bool found = false;
            foreach (TextBlock tb in panel.Children)
                found = (tb.Text == selectedValue) || found;

            if (found == false)
            {
                TextBlock tb = new TextBlock();
                tb.Text = selectedValue;                
                tb.Style = (Style)FindResource("ChooseDisplayStyle");

                if (panel == symbolsPanel)
                    tb.MouseDown += selectSymbol_MouseDown;

                panel.Children.Add(tb);
                if (panel.Children.Count > 1 && panel == symbolsPanel)
                    panel.Children.RemoveAt(0);
            }

            foreach (TextBlock tb in panel.Children)
                tb.Style = (Style)FindResource("ChooseDisplayStyle" + (tb.Text == selectedValue ? "Selected" : ""));

            string currentValue = panel.Tag as string;
            panel.Tag = selectedValue;
        }

        private void showHideTicksIntervals()
        {
            foreach (TextBlock tb in intervalsPanel.Children)
                if (tb.Text == "tick" || tb.Text.EndsWith("s"))
                    tb.Visibility = showTicks ? Visibility.Visible : Visibility.Collapsed;
                else
                    tb.Visibility = Visibility.Visible;
        }
        
        private void setRenderTransform(Matrix matrix, bool resetScale = false)
        {
            klinesView.RenderTransform = new MatrixTransform(matrix);

            if(resetScale)
                zoomFactor = 2;                       
        }

        private void UpdateCursorPrice()
        {
            Point klinesViewPosition = Mouse.GetPosition(klinesView);
            priceAtCursorPosition = lowestLowPrice + ((klinesView.ActualHeight - klinesViewPosition.Y) * (highestHighPrice - lowestLowPrice) / klinesView.ActualHeight);

            cursorPrice.Margin = new Thickness(cursorPosition.X + 3, cursorPosition.Y + 3, 0, 0);
            cursorPrice.Text = string.Format("{0:0.00000}", priceAtCursorPosition);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            DrawKLines();
        }

        protected void DrawKLines()
        {
            if (isDrawing)
                return;

            isDrawing = true;
            DateTime startDrawingTime = DateTime.Now;

            lock (viewControlsLock)
            {
                if (klinesView.Children.Count == 0 || Symbol == null || Interval == null || CandleType == null)
                {
                    isDrawing = false;
                    return;
                }

                List<CandleStick> klines = klinesView.Children.OfType<CandleStick>().ToList();
                firstKline = klines[0];
                lastKline = klines[klines.Count - 1];
                double viewWidth = klinesView.ActualWidth;
                double viewHeight = klinesView.ActualHeight;
                decimal averageFluctuationPerCandlestick = 0;
                double reversal = 0;
                lowestLowPrice = double.MaxValue;
                highestHighPrice = 0;

                // initial calculations
                foreach (CandleStick candleStick in klines)
                {
                    if (candleStick.Low < lowestLowPrice) lowestLowPrice = candleStick.Low;
                    if (candleStick.High > highestHighPrice) highestHighPrice = candleStick.High;

                    averageFluctuationPerCandlestick += (candleStick.OriginalKLine.High / klines.Count - candleStick.OriginalKLine.Low / klines.Count);
                }

                reversal = (double)(averageFluctuationPerCandlestick / lastKline.OriginalKLine.Close) * 4 * 100;

                // set CandleStick positions
                int xPosition = 0;
                foreach (CandleStick candleStick in klines)
                {
                    candleStick.SetWidthPositions(viewWidth, 0, klines.Count, xPosition);
                    candleStick.SetHeightPositions(viewHeight, lowestLowPrice, highestHighPrice);
                    xPosition++;
                }

                // set TrendLines positions
                List<TrendLineStick> tlines = klinesView.Children.OfType<TrendLineStick>().ToList();
                foreach (TrendLineStick trendLine in tlines)
                {
                    trendLine.SetXPositions(Interval, lastKline, viewWidth, klines.Count);
                    trendLine.SetYPositions(viewHeight, lowestLowPrice, highestHighPrice);

                    if (trendLine.OriginalTrendLine.LineType == TrendLineType.Normal.ToString())
                    {
                        trendLine.Fill(trendLine.Up ? greenBrush : redBrush);
                    }
                    else
                    {
                        if (trendLine.OriginalTrendLine.LineType == TrendLineType.TargetLong.ToString()) trendLine.Fill(greenBrush);
                        if (trendLine.OriginalTrendLine.LineType == TrendLineType.TargetCurrent.ToString()) trendLine.Fill(Brushes.Orange);
                        if (trendLine.OriginalTrendLine.LineType == TrendLineType.TargetShort.ToString()) trendLine.Fill(redBrush);
                    }
                        
                }

                // calculate normal bars
                if (CandleType == "BAR")
                {
                    foreach (CandleStick candleStick in klines)
                        candleStick.Fill(candleStick.Up ? greenBrush : redBrush);
                }

                // calculate volume
                if (CandleType == "VOL")
                {
                    List<double> sizes = new List<double>();
                    List<decimal> volumes = new List<decimal>();

                    foreach (CandleStick candleStick in klines)
                    {
                        sizes.Add(candleStick.High - candleStick.Low);
                        volumes.Add(candleStick.OriginalKLine.BaseVolume);
                    }

                    sizes.Sort();
                    volumes.Sort();

                    double size80 = sizes[(80 * klines.Count) / 100]; // show candles with size over 80%
                    decimal volume80 = volumes[(80 * klines.Count) / 100]; // show candles with volume over 80%

                    CandleStick prevCs = klines[0];
                    foreach (CandleStick candleStick in klines)
                    {
                        if (candleStick.High - candleStick.Low >= size80 && candleStick.OriginalKLine.BaseVolume >= volume80)
                        {
                            candleStick.Fill((candleStick.High > prevCs.High || candleStick.Low > prevCs.Low) ? realGreenBrush : realRedBrush);
                            prevCs = candleStick;
                        }
                        else
                            candleStick.Fill(Brushes.Black);
                    }
                }

                // calculate trends
                if (CandleType == "UD%" && realTimeTrades == null)
                {
                    TradeHelper simulation = new TradeHelper(firstKline.Open, lastKline.Close, reversal, reversal, Symbol, TradingType.CandleStickSimulation);
                    simulation.LastTrendIsDown = true; // always assume we are going down

                    foreach (CandleStick candleStick in klines)
                    {
                        simulation.CurrentPrice = candleStick.Close;
                        if (candleStick.Low < simulation.CurrentMin) simulation.CurrentMin = candleStick.Low;
                        if (candleStick.High > simulation.CurrentMax) simulation.CurrentMax = candleStick.High;

                        // up > trendReservalUP % => trend reversal
                        if (simulation.LastTrendIsDown && simulation.HasChangedUp())
                            simulation.Buy(candleStick.OriginalKLine.CloseTime); // buy here

                        // down > reversal % => trend reversal
                        if (simulation.LastTrendIsUp && simulation.HasChangedDown())
                            simulation.Sell(candleStick.OriginalKLine.CloseTime); // sell here

                        candleStick.Fill(simulation.LastTrendIsDown ? redBrush : greenBrush);
                    }

                    // sell on last candlestick
                    simulation.CurrentPrice = lastKline.Close;
                    simulation.SellLastOpenPosition(lastKline.OriginalKLine.CloseTime);

                    // simulation end
                    profitTB.Text = string.Format("{0:0.00} %", simulation.SwingProfitPercent);
                    if (TradeSimulationChangedEvent != null)
                        TradeSimulationChangedEvent(simulation);
                }

                if(CandleType == "UD%" && realTimeTrades != null)
                {
                    foreach (CandleStick candleStick in klines)
                    {
                        candleStick.Fill(realTimeTrades.IsGreenCandle(candleStick.OriginalKLine.CloseTime) ? greenBrush : redBrush);
                    }
                }

                if(CandleType == "TA")
                {
                    foreach (CandleStick candleStick in klines)
                    {
                        candleStick.Fill(candleStick.Up ? greenBrush : redBrush);
                    }
                }

                // display parameters
                udCandleType.ToolTip = string.Format("{0:0.00} %", reversal);
                intervalAVG.Text = string.Format("AVG ${0:0.00000}", averageFluctuationPerCandlestick);

                UpdateCursorPrice(); // refresh cursor price after lowestLowPrice, highestHighPrice are recalculated
            }            

            TimeSpan drawingTS = DateTime.Now - startDrawingTime;
            renderLoadTime.Text = string.Format("{0:0} ms", drawingTS.TotalMilliseconds);

            isDrawing = false;
        }
        
        private void selectSymbol_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            TextBlock tbSender = sender as TextBlock;
            SwitchData(tbSender.Text, this.Interval, RequestType.LoadData);
        }

        private void selectInterval_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            TextBlock tbSender = sender as TextBlock;
            SwitchData(this.Symbol, tbSender.Text, RequestType.LoadData);
        }

        private void selectCandleType_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            TextBlock tbSender = sender as TextBlock;
            selectPanelUIOption(candelTypesPanel, tbSender.Text);
            DrawKLines();
        }

        private void selectTool_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            foreach (TextBlock tb in toolsPanel.Children)
                tb.Style = (Style)FindResource("ChooseToolStyle");

            TextBlock tbSender = sender as TextBlock;
            bool tbSenderIsCurrentlySelected = (tbSender.Tag == toolsPanel.Tag);

            if (tbSenderIsCurrentlySelected == false)
            {
                toolsPanel.Tag = tbSender.Tag;
                tbSender.Style = (Style)FindResource("ChooseToolStyleSelected");
            }
            else
            {
                toolsPanel.Tag = null;
                tbSender.Style = (Style)FindResource("ChooseToolStyle");
            }
        }

        private void klinesView_MouseLeave(object sender, MouseEventArgs e)
        {
            cursorPrice.Text = "";
        }

        private void klinesView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string selectedTool = (string)toolsPanel.Tag;

            if (selectedTool != null && klinesView.Children.Count > 0)
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    TrendLine trendLine = TrendLineHelper.MouseDown(selectedTool, priceAtCursorPosition, klinesView, firstKline, lastKline, klinesView.ActualWidth);
                    if (trendLine == null)
                        return;
                    klinesView.Children.Add(new TrendLineStick(trendLine));
                    movingObjectsWithMouse = true;
                }
            }

            if (selectedTool == null)
            {
                if (e.ChangedButton == MouseButton.Middle)
                    livePriceData.LoadFromServer(Symbol, Interval);

                if ((e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right) && klinesView.Children.Count == 0)
                    livePriceData.LoadFromServer(Symbol, Interval);

                if (e.ChangedButton == MouseButton.Left)
                    movingObjectsWithMouse = true;

                if (e.ChangedButton == MouseButton.Right)
                    setRenderTransform(Matrix.Identity, true);
            }
        }

        private void klinesView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            string selectedTool = (string)toolsPanel.Tag;
            Point cursorPreviousPosition = cursorPosition;
            cursorPosition = e.GetPosition(mainGrid);

            UpdateCursorPrice(); // refresh cursor price after mouse position is changed

            if (selectedTool != null && movingObjectsWithMouse)
            {
                bool objectMoved = TrendLineHelper.MouseMove(selectedTool, priceAtCursorPosition, klinesView, firstKline, lastKline, klinesView.ActualWidth);
                if (objectMoved)
                    DrawKLines();
            }

            if (selectedTool == null && movingObjectsWithMouse)
            {
                Matrix m = klinesView.RenderTransform.Value;
                m.Translate(cursorPosition.X - cursorPreviousPosition.X, cursorPosition.Y - cursorPreviousPosition.Y);
                setRenderTransform(m);
            }
        }

        private void klinesView_MouseUp(object sender, MouseButtonEventArgs e)
        {
            string selectedTool = (string)toolsPanel.Tag;

            if (selectedTool != null && e.ChangedButton == MouseButton.Left)
            {
                TrendLineHelper.MouseUp(selectedTool);
                movingObjectsWithMouse = false;
            }

            if (selectedTool == null && e.ChangedButton == MouseButton.Left)
                movingObjectsWithMouse = false;
        }

        private void klinesView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point klinesViewPosition = Mouse.GetPosition(klinesView);
            klinesViewPosition = klinesView.RenderTransform.Transform(klinesViewPosition);

            double edelta = Math.Sign(e.Delta);

            if (edelta > 0)
            {
                // zoom in
                zoomFactor = zoomFactor + 0.5;

                Matrix m = klinesView.RenderTransform.Value;
                m.ScaleAt(1 + 1 / zoomFactor, 1 + 1 / zoomFactor, klinesViewPosition.X, klinesViewPosition.Y);
                setRenderTransform(m);
            }
            else
            {
                // zoom out
                zoomFactor = zoomFactor - 0.5;

                Matrix m = klinesView.RenderTransform.Value;
                m.ScaleAt(1 - 1 / zoomFactor, 1 - 1 / zoomFactor, klinesViewPosition.X, klinesViewPosition.Y);

                if (m.Determinant < 1)
                    setRenderTransform(Matrix.Identity, true);
                else
                    setRenderTransform(m);
            }            
        }

        private void klinesView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Grid parentGrid = this.Parent as Grid;

            if (maximized)
            {
                Grid.SetRow(this, row);
                Grid.SetColumn(this, column);
                Grid.SetRowSpan(this, 1);
                Grid.SetColumnSpan(this, 1);
                Grid.SetZIndex(this, 0);
                maximized = false;
            }
            else
            {
                row = Grid.GetRow(this);
                column = Grid.GetColumn(this);
                Grid.SetRow(this, 0);
                Grid.SetColumn(this, 0);
                Grid.SetRowSpan(this, parentGrid.RowDefinitions.Count);
                Grid.SetColumnSpan(this, parentGrid.ColumnDefinitions.Count);
                Grid.SetZIndex(this, 1);
                maximized = true;
            }

            setRenderTransform(Matrix.Identity, true);
        }

        public void SafelyClose()
        {
            livePriceData.SafelyClose();
        }

    }
}
