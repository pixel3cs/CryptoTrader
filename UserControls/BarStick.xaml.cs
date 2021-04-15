using Binance.Net.Objects.Futures.MarketData;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static CryptoTrader.Utils;

namespace CryptoTrader.UserControls
{
    public partial class BarStick : UserControl
    {

        public double Open { get; private set; }
        public double Close { get; private set; }
        public DateTime OpenTime { get; private set; }
        public DateTime CloseTime { get; private set; }

        public bool Up { get { return Close >= 0; } }
        public int Type;

        private BarStick()
        {
            InitializeComponent();
        }

        public BarStick(double currentValue, DateTime currentTime, double prevValue, DateTime prevTime, int type)
        {
            InitializeComponent();
            Open = prevValue;
            Close = currentValue;
            OpenTime = prevTime;
            CloseTime = currentTime;
            this.Type = type;
        }

        public void SetXPositions(double viewWidth, CandleStick firstKline, CandleStick lastKline)
        {
            double X1 = Utils.CalculateViewWidth(viewWidth, firstKline.OriginalKLine.OpenTime.Ticks, lastKline.OriginalKLine.CloseTime.Ticks, OpenTime.Ticks);
            double X2 = Utils.CalculateViewWidth(viewWidth, firstKline.OriginalKLine.OpenTime.Ticks, lastKline.OriginalKLine.CloseTime.Ticks, CloseTime.Ticks);
            Canvas.SetLeft(this, X1 + (X2 - X1) / 4d);
            this.Width = Math.Abs(X2 - X1 - 2 *((X2 - X1) / 4d));
        }

        public void SetYPositions(double viewHeight, double lowestLowPrice, double highestHighPrice)
        {
            //viewHeight = viewHeight / 3;
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
