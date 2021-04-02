using Binance.Net.Interfaces;
using CryptoTrader.Code;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;
using static CryptoTrader.Code.Utils;
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

        private bool movingChart = false;
        private Point cursorPosition = new Point();

        private double lowestPrice, highestPrice;

        private LiveData liveData = null;
        private TradeHelper realTimeTrades = null;

        public string Symbol { get { return (string)symbolsPanel.Tag; } }
        public string Interval { get { return (string)intervalsPanel.Tag; } }
        public string CandleType { get { return (string)candelTypesPanel.Tag; } }

        public double Leverage { get; private set; }
        public double TargetROE { get; private set; }
        

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

            liveData = new LiveData(ServerDataHandler);
        }

        public void SwitchData(string newSymbol, string newInterval, RequestType requestType)
        {
            if (newSymbol != Symbol || newInterval != Interval)
            {
                lock (viewControlsLock)
                {
                    // clear view
                    klinesView.Children.Clear();
                    klinesDecoration.Children.Clear();
                    dataLoadTime.Text = renderLoadTime.Text = cursorPrice.Text = profitTB.Text = intervalAVG.Text = "";

                    if (TradeSimulationChangedEvent != null)
                        TradeSimulationChangedEvent(null);
                }

                selectPanelUIOption(symbolsPanel, newSymbol);
                selectPanelUIOption(intervalsPanel, newInterval);

                liveData.SetSymbolAndInterval(newSymbol, newInterval);

                if (requestType == RequestType.LoadData)
                    liveData.LoadFromServer();
            }
        }

        private void ServerDataHandler(IEnumerable<IBinanceKline> newKlines, bool erasePreviousData)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    dataLoadTime.Text = string.Format("{0:0} ms", liveData.LastDataLoadTime.TotalMilliseconds);

                    lock (viewControlsLock)
                    {
                        if (erasePreviousData)
                        {
                            klinesView.Children.Clear();
                            klinesDecoration.Children.Clear();
                        }

                        if (newKlines != null)
                        {
                            foreach (var kline in newKlines)
                            {
                                CandleStick candleStick = new CandleStick(kline);
                                klinesView.Children.Add(candleStick);
                            }
                        }

                        if (liveData.IsTick)
                        {
                            int maxLines = 500;
                            if (klinesView.Children.Count > maxLines)
                                klinesView.Children.RemoveRange(0, klinesView.Children.Count - maxLines);
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

        public void SetTrendReversals(double leverage, double targetROE)
        {
            this.Leverage = leverage;
            this.TargetROE = targetROE;
            DrawKLines();
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
            klinesDecoration.RenderTransform = new MatrixTransform(matrix);

            if(resetScale)
                zoomFactor = 2;                       
        }

        private void UpdateCursorPrice()
        {
            cursorPrice.Margin = new Thickness(cursorPosition.X + 3, cursorPosition.Y + 3, 0, 0);
            Point klinesViewPosition = Mouse.GetPosition(klinesView);
            double currentRelativePrice = lowestPrice + (((double)klinesView.ActualHeight - (double)klinesViewPosition.Y) * (highestPrice - lowestPrice) / klinesView.ActualHeight);
            cursorPrice.Text = currentRelativePrice.ToString("0.00000");
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            DrawKLines();
        }

        int drawings = 0;
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

                List<CandleStick> klines = new List<CandleStick>(klinesView.Children.Count);
                CandleStick firstKline = (CandleStick)klinesView.Children[0];
                CandleStick lastKline = (CandleStick)klinesView.Children[klinesView.Children.Count - 1];
                double viewWidth = klinesView.ActualWidth;
                double viewHeight = klinesView.ActualHeight;
                decimal averageFluctuationPerCandlestick = 0;
                double reversal = 0;
                lowestPrice = double.MaxValue;
                highestPrice = 0;

                // initial calculations
                foreach (CandleStick candleStick in klinesView.Children)
                {
                    klines.Add(candleStick);

                    candleStick.RestoreToOriginal(); // make sure we restore candlestick to original values

                    if (candleStick.Low < lowestPrice) lowestPrice = candleStick.Low;
                    if (candleStick.High > highestPrice) highestPrice = candleStick.High;

                    averageFluctuationPerCandlestick += (candleStick.OriginalKLine.High / klinesView.Children.Count - candleStick.OriginalKLine.Low / klinesView.Children.Count);
                }

                reversal = (double)(averageFluctuationPerCandlestick / lastKline.OriginalKLine.Close) * 4 * 100;

                // view calculations
                int index = 0;
                foreach (CandleStick candleStick in klines)
                {
                    candleStick.SetWidthPositions(viewWidth, 0, klines.Count, index);
                    candleStick.SetHeightPositions(viewHeight, lowestPrice, highestPrice);
                    index++;
                }

                // calculate Heikin Ashi
                if (CandleType == "HA")
                {
                    CandleStick prevCS = (CandleStick)klines[0];
                    prevCS.CalculateHeikinAshi(prevCS);

                    foreach(CandleStick candleStick in klines)
                    {
                        candleStick.CalculateHeikinAshi(prevCS);
                        candleStick.Fill(candleStick.Up ? greenBrush : redBrush);
                        prevCS = candleStick;
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

                // add decorative lines
                klinesDecoration.Children.Clear();

                double targetPrice = ((TargetROE / Leverage) / 100d) * lastKline.Close;
                double longClose = Utils.CalculateViewHeight(viewHeight, lowestPrice, highestPrice, lastKline.Close + targetPrice);
                double shortClose = Utils.CalculateViewHeight(viewHeight, lowestPrice, highestPrice, lastKline.Close - targetPrice);

                Line line = new Line() { X1 = viewWidth - 20, Y1 = viewHeight - lastKline.ViewClose, X2 = viewWidth, Y2 = viewHeight - lastKline.ViewClose };
                line.StrokeThickness = 1;
                line.Stroke = Brushes.Orange;
                klinesDecoration.Children.Add(line);
                line = new Line() { X1 = viewWidth - 20, Y1 = viewHeight - longClose, X2 = viewWidth, Y2 = viewHeight - longClose };
                line.StrokeThickness = 2;
                line.Stroke = greenBrush;
                klinesDecoration.Children.Add(line);
                line = new Line() { X1 = viewWidth - 20, Y1 = viewHeight - shortClose, X2 = viewWidth, Y2 = viewHeight - shortClose };
                line.StrokeThickness = 2;
                line.Stroke = redBrush;
                klinesDecoration.Children.Add(line);

                // display parameters
                udCandleType.ToolTip = string.Format("{0:0.00} %", reversal);
                intervalAVG.Text = string.Format("AVG ${0:0.00000}", averageFluctuationPerCandlestick);

                UpdateCursorPrice();
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

        private void klinesView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Point cursorPreviousPosition = cursorPosition;
            cursorPosition = e.GetPosition(mainGrid);

            if (movingChart)
            {
                Matrix m = klinesView.RenderTransform.Value;                
                m.Translate(cursorPosition.X - cursorPreviousPosition.X, cursorPosition.Y - cursorPreviousPosition.Y);
                setRenderTransform(m);
            }
            
            UpdateCursorPrice();
        }

        private void klinesView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            liveData.ProcessMouseDownEvents(e, klinesView.Children.Count > 0);

            if (e.ChangedButton == MouseButton.Left)
            {
                movingChart = true;
            }

            if (e.ChangedButton == MouseButton.Right)
            {
                setRenderTransform(Matrix.Identity, true);
            }
        }

        private void klinesView_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                movingChart = false;
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
            liveData.SafelyClose();
        }

    }
}
