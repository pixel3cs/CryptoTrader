using Binance.Net.Objects.Futures.MarketData;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static CryptoTrader.Utils;

namespace CryptoTrader.UserControls
{
    public partial class OpenInterestStick : UserControl
    {
        public BinanceFuturesOpenInterestHistory OriginalOpenInterest { get; private set; }

        public double Open { get; private set; }
        public double Close { get; private set; }
        public DateTime OpenTime { get; private set; }
        public DateTime CloseTime { get; private set; }

        public bool Up { get { return Close >= Open; } }

        private OpenInterestStick()
        {
            InitializeComponent();
        }

        public OpenInterestStick(BinanceFuturesOpenInterestHistory current, BinanceFuturesOpenInterestHistory prev)
        {
            InitializeComponent();
            this.OriginalOpenInterest = current;
            Open = (double)prev.SumOpenInterestValue;
            Close = (double)current.SumOpenInterestValue;
            OpenTime = prev.Timestamp.Value;
            CloseTime = current.Timestamp.Value;
        }

        public void SetXPositions(double viewWidth, CandleStick firstKline, CandleStick lastKline)
        {
            double X1 = Utils.CalculateViewWidth(viewWidth, firstKline.OriginalKLine.OpenTime.Ticks, lastKline.OriginalKLine.CloseTime.Ticks, OpenTime.Ticks);
            double X2 = Utils.CalculateViewWidth(viewWidth, firstKline.OriginalKLine.OpenTime.Ticks, lastKline.OriginalKLine.CloseTime.Ticks, CloseTime.Ticks);
            Canvas.SetLeft(this, X1);
            this.Width = Math.Abs(X2 - X1);
        }

        public void SetYPositions(double viewHeight, double lowestLowPrice, double highestHighPrice)
        {
            viewHeight = viewHeight / 3;
            double high = Math.Max(Open, Close);
            double low = Math.Min(Open, Close);
            double Y1 = viewHeight - Utils.CalculateViewHeight(viewHeight, lowestLowPrice, highestHighPrice, high);
            double Y2 = viewHeight - Utils.CalculateViewHeight(viewHeight, lowestLowPrice, highestHighPrice, low);
            
            Canvas.SetTop(this, Y1);
            this.Height = Math.Abs(Y1 - Y2);
        }

        public void Fill(Brush greenBrush, Brush redBrush)
        {
            this.Background = this.Up ? greenBrush : redBrush;
        }

    }
}
