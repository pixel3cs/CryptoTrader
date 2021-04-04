using Binance.Net.Interfaces;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CryptoTrader.UserControls
{
    public partial class CandleStick : UserControl
    {
        public double Low, High, Open, Close;
        public double ViewHigh, ViewLow, ViewOpen, ViewClose;
        public IBinanceKline OriginalKLine { get; private set; }
        public bool Up { get { return this.Close >= this.Open; } }
        public bool Down { get { return this.Close < this.Open; } }

        private CandleStick()
        {
            InitializeComponent();
        }

        public CandleStick(IBinanceKline kline)
        {
            InitializeComponent();
            this.OriginalKLine = kline;
            this.Close = (double)kline.Close;
            this.Open = (double)kline.Open;
            this.High = (double)kline.High;
            this.Low = (double)kline.Low;
        }

        public void CalculateHeikinAshi(CandleStick prevCandleStick)
        {
            this.Close = (double)(OriginalKLine.Open + OriginalKLine.Close + OriginalKLine.Low + OriginalKLine.High) / 4d;
            this.Open = (double)(prevCandleStick.Open + prevCandleStick.Close) / 2d;
            this.High = Math.Max(Math.Max((double)OriginalKLine.High, this.Open), this.Close);
            this.Low = Math.Min(Math.Min((double)OriginalKLine.Low, this.Open), this.Close);
        }

        public void RestoreToOriginal()
        {
            this.Close = (double)OriginalKLine.Close;
            this.Open = (double)OriginalKLine.Open;
            this.High = (double)OriginalKLine.High;
            this.Low = (double)OriginalKLine.Low;
        }

        public void SetWidthPositions(double viewWidth, double lowestLeft, double highestRight, double priceWidth)
        {
            double xViewLeft = Utils.CalculateViewWidth(viewWidth, lowestLeft, highestRight, priceWidth + 0.25d);
            double xViewRight = Utils.CalculateViewWidth(viewWidth, lowestLeft, highestRight, priceWidth + 1d);
            
            Canvas.SetLeft(this, (double)xViewLeft);
            this.Width = (double)Math.Abs(xViewRight - xViewLeft);
        }

        //private static double lastY = 0;
        //private static CandleStick lastC = null;
        public void SetHeightPositions(double viewHeight, double lowestLow, double highestHigh)
        {
            //if (lastC != null && (this.Up != lastC.Up))
            //    lastY = 0;

            ViewHigh = Utils.CalculateViewHeight(viewHeight, lowestLow, highestHigh, this.High);
            ViewLow = Utils.CalculateViewHeight(viewHeight, lowestLow, highestHigh, this.Low);
            ViewOpen = Utils.CalculateViewHeight(viewHeight, lowestLow, highestHigh, this.Open);
            ViewClose = Utils.CalculateViewHeight(viewHeight, lowestLow, highestHigh, this.Close);

            Canvas.SetTop(this, (double)(viewHeight - ViewHigh));
            this.Height = (double)(ViewHigh - ViewLow);

            body.Height = (double)Math.Abs(ViewOpen - ViewClose);
            body.Margin = new Thickness(0, (double)(ViewHigh - (this.Up ? ViewClose : ViewOpen)), 0, 0);

            //if (lastC != null)
            //{
            //    lastY = lastY + (this.Up ? (double)Math.Abs(this.ViewHigh - lastC.ViewHigh) : (double)Math.Abs(this.ViewLow - lastC.ViewLow));
            //    this.Margin = new Thickness(this.Margin.Left, (double)(viewHeight - lastY), 0, 0);
            //}
            //lastC = this;
        }

        public void Fill(Brush brush)
        {
            body.Fill = brush;
            line.Fill = brush;
        }

    }
}
