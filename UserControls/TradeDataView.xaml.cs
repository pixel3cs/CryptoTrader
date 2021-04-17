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
using Binance.Net.Objects.Futures.MarketData;

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
        public static readonly Brush realGrayBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#777777"));

        protected object viewControlsLock = new object();
        protected bool isDrawing = false;

        private int row = 0, column = 0;
        private bool maximized = false;

        private double zoomFactor = 2;

        private bool movingLineWithMouse = false, movingChartWithMouse = false;
        private Point cursorPosition = new Point(), moveStartPosition = new Point();
        private double priceAtCursorPosition = 0;

        private double lowestLowPrice, highestHighPrice, averageFluctuationPerCandlestick, reversal;
        private CandleStick firstKline, lastKline;

        private LivePriceData livePriceData = null;

        public string Symbol { get { return (string)symbolsPanel.Tag; } }
        public string Interval { get { return (string)intervalsPanel.Tag; } }
        public string CandleType { get { return (string)candelTypesPanel.Tag; } }

        public double TargetMovePercent { get; private set; }
        public bool ShowTargetLines { get; private set; }

        private bool showTicks = false;
        public bool ShowTicks { get { return showTicks; } set { showTicks = value; showHideTicksIntervals(); } }


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
            realGrayBrush.Freeze();

            livePriceData = new LivePriceData(ServerDataHandler, ServerDataLongShortHandler);
        }

        public void SwitchData(string newSymbol, string newInterval, RequestType requestType)
        {
            if (newSymbol != Symbol || newInterval != Interval)
            {
                lock (viewControlsLock)
                {
                    // clear view
                    klinesView.Children.Clear();

                    renderLoadTime.Text = cursorPrice.Text = intervalAVG.Text = "";
                    cursorLine.Visibility = Visibility.Collapsed;
                    longShortTb.Visibility = Visibility.Visible;
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
                    lock (viewControlsLock)
                    {
                        if (erasePreviousData)
                        {
                            klinesView.Children.Clear();
                            longShortTb.Visibility = Visibility.Visible;
                        }

                        if (isTick == false && klinesView != null)
                        {
                            // add new klines
                            foreach (var kline in newKlines)
                                klinesView.Children.Add(new CandleStick(kline));

                            // add target trend lines
                            foreach (var tls in TrendLineHelper.GetTempTargetLines(Interval, TargetMovePercent, ShowTargetLines, newKlines.Last()))
                                klinesView.Children.Add(tls);

                            // add new trend lines
                            foreach (var trendLine in TrendLineData.LoadFromDisk(Symbol, Interval))
                                klinesView.Children.Add(new TrendLineStick(trendLine));
                        }

                        if (isTick && newKlines != null)
                        {
                            bool firstTime = (klinesView.Children.Count == 0);

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

                            if (firstTime)
                            {
                                // add target trend lines
                                foreach (var tls in TrendLineHelper.GetTempTargetLines(Interval, TargetMovePercent, ShowTargetLines, newKlines.Last()))
                                    klinesView.Children.Add(tls);
                            }
                            else
                            {
                                double closePrice = klinesView.Children.OfType<CandleStick>().Last().Close;
                                TrendLineHelper.UpdateTempTargetLines(klinesView, TargetMovePercent, ShowTargetLines, closePrice, firstKline, lastKline, lowestLowPrice, highestHighPrice);
                            }
                        }
                    }

                    setRenderTransform(Matrix.Identity, true);

                    RenderPositions(); // when new data is loaded                    
                });
            }
            catch
            {
            }
        }

        public void ServerDataLongShortHandler(IEnumerable<BinanceFuturesLongShortRatio> ttlsRatioPositions)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    lock (viewControlsLock)
                    {
                        // top traders long/short positions
                        if (ttlsRatioPositions != null)
                        {                            
                            BinanceFuturesLongShortRatio prev2 = ttlsRatioPositions.First();
                            foreach (var var2 in ttlsRatioPositions)
                            {
                                klinesView.Children.Add(new BarStick((double)var2.LongShortRatio, var2.Timestamp.Value, (double)prev2.LongShortRatio, prev2.Timestamp.Value, 1));
                                prev2 = var2;
                            }
                        }
                    }

                    setRenderTransform(Matrix.Identity, true);

                    RenderPositions(); // when new data is loaded
                });
            }
            catch
            {
            }            
        }

        public void SetTargetPrice(double targetMovePercent, bool showTargetLines)
        {
            this.TargetMovePercent = targetMovePercent;
            this.ShowTargetLines = showTargetLines;

            if (klinesView.Children.Count > 0)
                TrendLineHelper.UpdateTempTargetLines(klinesView, targetMovePercent, showTargetLines, lastKline.Close, firstKline, lastKline, lowestLowPrice, highestHighPrice);
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
            if (klinesView.Children.Count > 0) // if no tool selected and data is loaded
            {
                cursorPrice.Text = string.Format("{0:0.00000}", priceAtCursorPosition);
                cursorLine.Visibility = Visibility.Visible;
            }
            else
            {
                cursorPrice.Text = string.Empty;
                cursorLine.Visibility = Visibility.Collapsed;
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            RenderPositions(); // on windows resize
        }

        protected void RenderPositions()
        {
            if (isDrawing)
                return;

            isDrawing = true;

            lock (viewControlsLock)
            {
                if (klinesView.Children.Count == 0 || Symbol == null || Interval == null || CandleType == null)
                {
                    isDrawing = false;
                    return;
                }

                List<CandleStick> klines = klinesView.Children.OfType<CandleStick>().ToList();
                double viewWidth = klinesView.ActualWidth;
                double viewHeight = klinesView.ActualHeight;
                firstKline = klines[0];
                lastKline = klines[klines.Count - 1];
                lowestLowPrice = double.MaxValue;
                highestHighPrice = 0;
                averageFluctuationPerCandlestick = 0;
                reversal = 0;

                // initial calculations
                foreach (CandleStick candleStick in klines)
                {
                    if (candleStick.Low < lowestLowPrice) lowestLowPrice = candleStick.Low;
                    if (candleStick.High > highestHighPrice) highestHighPrice = candleStick.High;

                    averageFluctuationPerCandlestick += (double)(candleStick.OriginalKLine.High / klines.Count - candleStick.OriginalKLine.Low / klines.Count);
                }

                reversal = (averageFluctuationPerCandlestick / (double)lastKline.OriginalKLine.Close) * 4 * 100;

                // CandleStick positions
                foreach (CandleStick candleStick in klines)
                {
                    candleStick.SetWidthPositions(viewWidth, firstKline, lastKline);
                    candleStick.SetHeightPositions(viewHeight, lowestLowPrice, highestHighPrice);
                }

                // TrendLines positions
                List<TrendLineStick> tlines = klinesView.Children.OfType<TrendLineStick>().ToList();
                foreach (TrendLineStick trendLine in tlines)
                {
                    trendLine.SetXPositions(viewWidth, firstKline, lastKline);
                    trendLine.SetYPositions(viewHeight, lowestLowPrice, highestHighPrice);
                    trendLine.Fill(greenBrush, redBrush);
                }

                // top traders long/short positions
                List<BarStick> ttlsRP = klinesView.Children.OfType<BarStick>().Where(lsr => lsr.Type == 1).ToList();
                if (ttlsRP != null && ttlsRP.Count > 0)
                {
                    double lowestLSR = ttlsRP.Min(o => o.Close);
                    double highestLSR = ttlsRP.Max(o => o.Close);
                    foreach (BarStick lsrline in ttlsRP)
                    {
                        lsrline.SetXPositions(viewWidth, firstKline, lastKline);
                        lsrline.SetYPositions(viewHeight, lowestLSR, highestLSR);
                        lsrline.Fill(green2Brush, green2Brush);
                    }
                }               

                // display parameters
                udCandleType.ToolTip = string.Format("{0:0.00} %", reversal);
                intervalAVG.Text = string.Format("AVG ${0:0.00000}", averageFluctuationPerCandlestick);

                UpdateCursorPrice(); // refresh cursor price after lowestLowPrice, highestHighPrice are recalculated
            }            

            isDrawing = false;

            RenderColors(); // on RenderPositions event
        }

        protected void RenderColors()
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

                    double size70 = sizes[(70 * klines.Count) / 100]; // show candles with size over 80%
                    decimal volume70 = volumes[(70 * klines.Count) / 100]; // show candles with volume over 80%

                    CandleStick prevCs = klines[0];
                    foreach (CandleStick candleStick in klines)
                    {
                        if (candleStick.High - candleStick.Low >= size70 && candleStick.OriginalKLine.BaseVolume >= volume70)
                            candleStick.Fill(candleStick.Up ? realGreenBrush : realRedBrush);
                        else
                            candleStick.Fill(realGrayBrush);
                        prevCs = candleStick;
                    }
                }

                // calculate trends
                if (CandleType == "UD%")
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
                    //profitTB.Text = string.Format("{0:0.00} %", simulation.SwingProfitPercent);
                }

                // technical analysis with trend lines
                if(CandleType == "TA")
                {
                    List<TrendLineStick> tlines = klinesView.Children.OfType<TrendLineStick>().ToList();

                    foreach (CandleStick candleStick in klines)
                    {
                        double csX = Canvas.GetLeft(candleStick);

                        // take the corresponding trend line (candle stick between its positions)
                        TrendLineStick[] tlsList = tlines.Where(tl => csX >= tl.line.X1 && csX <= tl.line.X2 &&
                            tl.OriginalTrendLine.LineType == TrendLineType.Normal.ToString()).ToArray();

                        if (tlsList != null && tlsList.Length > 0)
                        {
                            TrendLineStick tls = (tlsList.Length == 1) ? tlsList[0] : null;

                            // if there are more trend lines, take the most suitable one
                            if (tls == null)
                            {
                                int upTlsCount = tlsList.Where(tl => tl.Up == true).Count();
                                int downTlsCount = tlsList.Where(tl => tl.Up == false).Count();

                                // if there are more trend lines with different positions, take the most left one
                                if (upTlsCount > 0 && downTlsCount > 0)
                                    tls = tlsList.OrderBy(tl => tl.line.X1).FirstOrDefault();

                                // if there are only up trend lines, take the lowest one
                                if (upTlsCount > 0 && downTlsCount == 0)
                                    tls = tlsList.OrderByDescending(tl => tl.line.Y1).FirstOrDefault();

                                // if there are only down trend lines, take the highest one
                                if (upTlsCount == 0 && downTlsCount > 0)
                                    tls = tlsList.OrderBy(tl => tl.line.Y1).FirstOrDefault();
                            }

                            // find distance from candleStick to trend line
                            double csHighY = Canvas.GetTop(candleStick);
                            double csLowY = csHighY + candleStick.Height;
                            double distanceHigh = tls.DistanceToPoint(csX, csHighY); // distance to cs.High
                            double distanceLow = tls.DistanceToPoint(csX, csLowY); // distance to cs.Low
                            double distance = tls.Up ? distanceHigh : distanceLow;

                            // if candleStick cross the trend line, take the higher distance
                            if (Math.Sign(distanceHigh) != Math.Sign(distanceLow))
                            {
                                if(tls.Up)
                                    distance = Math.Abs(distanceHigh) / Math.Abs(distanceLow) >= 0.67 ? distanceHigh : distanceLow; // 0.67 = two thirds
                                else
                                    distance = Math.Abs(distanceLow) / Math.Abs(distanceHigh) >= 0.67 ? distanceLow : distanceHigh;
                            }

                            // calculate color based on distance sign: 0/+1/-1
                            candleStick.Fill(distance < 0 ? greenBrush : redBrush);
                        }
                        else
                            candleStick.Fill(candleStick.Up ? greenBrush : redBrush);
                    }
                }

                // gray, without wick
                if (CandleType == "GRY")
                {
                    foreach (CandleStick candleStick in klines)
                    {
                        candleStick.Fill(grayBrush, true);
                    }
                }
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
            RenderColors(); // when CandleType is changed (colors are changed)
        }

        private void tools_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            TextBlock tb = sender as TextBlock;

            if(tb.Tag.ToString() == "savemark" && klinesView.Children.Count > 0)
            {
                TrendLineHelper.KeepTempTargetLines(klinesView);
                TrendLineData.SaveToDisk(klinesView, Symbol, Interval); // when temp target lines are keep
                MessageBox.Show("Target lines have been saved.", Application.ResourceAssembly.GetName().Name, MessageBoxButton.OK, MessageBoxImage.Information);              
            }

            if (tb.Tag.ToString() == "longshort" && klinesView.Children.Count > 0)
            {
                tb.Visibility = Visibility.Collapsed;
                livePriceData.LoadLongShortIndicators(Symbol, Interval, 500);
            }
        }

        private void klinesView_MouseLeave(object sender, MouseEventArgs e)
        {
            cursorPrice.Text = string.Empty;
            cursorLine.Visibility = Visibility.Collapsed;
        }

        private void klinesView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (klinesView.Children.Count > 0)
            {
                TrendLineStick nearTLS = TrendLineHelper.TrendLineNearMouse(klinesView);

                if (e.ChangedButton == MouseButton.Left && nearTLS != null) // Left + near line
                {
                    TrendLineHelper.MouseDownOnLine(nearTLS, klinesView);
                    movingLineWithMouse = true;
                    klinesView_PreviewMouseMove(null, e); // update line position to cursor
                }

                if (e.ChangedButton == MouseButton.Left && nearTLS == null) // Left + no line
                {
                    TrendLineStick tls = TrendLineHelper.MouseDown(priceAtCursorPosition, klinesView, firstKline, lastKline, klinesView.ActualWidth);
                    movingLineWithMouse = (tls != null);
                }

                if (e.ChangedButton == MouseButton.Right && nearTLS != null) // Right + near line
                {
                    TrendLineHelper.RemoveTrendLine(nearTLS, klinesView);
                    TrendLineData.SaveToDisk(klinesView, Symbol, Interval); // when a trend line is removed
                    klinesView_PreviewMouseMove(null, e); // update cursor in relation with remaining lines
                    RenderColors(); // update candle stick colors after trend line deletion
                }

                if (e.ChangedButton == MouseButton.Right && nearTLS == null) // Right + no line
                {
                    moveStartPosition = cursorPosition;
                    movingChartWithMouse = true;
                }
            }

            if (e.ChangedButton == MouseButton.Middle)
                livePriceData.LoadFromServer(Symbol, Interval);

            if ((e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right) && klinesView.Children.Count == 0)
                livePriceData.LoadFromServer(Symbol, Interval);
        }

        private void klinesView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Point cursorPreviousPosition = cursorPosition;
            cursorPosition = e.GetPosition(mainGrid);

            cursorLine.X1 = cursorLine.X2 = cursorPosition.X;
            cursorLine.Y2 = this.ActualHeight;
            UpdateCursorPrice(); // refresh cursor price after mouse position is changed

            if (klinesView.Children.Count > 0)
            {
                if (movingLineWithMouse)
                {
                    TrendLineStick movedTLS = TrendLineHelper.MouseMove(priceAtCursorPosition, klinesView, firstKline, lastKline, klinesView.ActualWidth);
                    if (movedTLS != null) // update view
                    {
                        movedTLS.SetXPositions(klinesView.ActualWidth, firstKline, lastKline);
                        movedTLS.SetYPositions(klinesView.ActualHeight, lowestLowPrice, highestHighPrice);
                        movedTLS.Fill(greenBrush, redBrush);
                    }
                }

                if(movingLineWithMouse == false && movingChartWithMouse == false)
                {
                    TrendLineStick nearTLS = TrendLineHelper.TrendLineNearMouse(klinesView);
                    this.Cursor = (nearTLS == null) ? Cursors.Cross : Cursors.Hand;
                }

                if (movingChartWithMouse)
                {
                    Matrix m = klinesView.RenderTransform.Value;
                    m.Translate(cursorPosition.X - cursorPreviousPosition.X, cursorPosition.Y - cursorPreviousPosition.Y);
                    setRenderTransform(m);
                }
            }
        }

        private void klinesView_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (klinesView.Children.Count > 0)
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    bool lineMoved = TrendLineHelper.MouseUp(klinesView);
                    if (lineMoved)
                    {
                        TrendLineData.SaveToDisk(klinesView, Symbol, Interval); // when mouse is up and a new line is completed/updated
                        RenderColors(); // update candle stick colors after trend line creation/move is ended
                    }
                    movingLineWithMouse = false;
                    klinesView_PreviewMouseMove(null, e); // update mouse cursor
                }

                if (e.ChangedButton == MouseButton.Right)
                {
                    movingChartWithMouse = false;

                    if (Utils.AreClosePoints(moveStartPosition, cursorPosition, Utils.NearDistance))
                        setRenderTransform(Matrix.Identity, true);
                }
            }
        }

        private void klinesView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //Utils.startindex -= e.Delta;
            //RenderPositions();
            //return;

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

            // toggle size (full screen / normal size)
            Grid parentGrid = this.Parent as Grid;            

            if (maximized)
            {
                maximized = false;
                this.Visibility = Visibility.Hidden;

                Grid.SetRow(this, row);
                Grid.SetColumn(this, column);
                Grid.SetRowSpan(this, 1);
                Grid.SetColumnSpan(this, 1);

                setRenderTransform(Matrix.Identity, true);
                parentGrid.Children.OfType<UIElement>().All(ui => { ui.Visibility = Visibility.Visible; return true; });                
            }
            else
            {
                maximized = true;
                parentGrid.Children.OfType<UIElement>().All(ui => { ui.Visibility = Visibility.Hidden; return true; });

                row = Grid.GetRow(this);
                column = Grid.GetColumn(this);
                Grid.SetRow(this, 0);
                Grid.SetColumn(this, 0);
                Grid.SetRowSpan(this, parentGrid.RowDefinitions.Count);
                Grid.SetColumnSpan(this, parentGrid.ColumnDefinitions.Count);

                setRenderTransform(Matrix.Identity, true);
                this.Visibility = Visibility.Visible;
            }

            
        }

        public void SafelyClose()
        {
            livePriceData.SafelyClose();
        }

    }
}
