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
        public IBinanceKline OriginalKLine { get; set; }
        public bool Up { get { return this.Close >= this.Open; } }
        public bool Down { get { return this.Close < this.Open; } }

        public CandleStick()
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

        public void CopyFrom(CandleStick cs)
        {
            this.Close = (double)cs.Close;
            this.Open = (double)cs.Open;
            this.High = (double)cs.High;
            this.Low = (double)cs.Low;
        }

        //private static double lastY = 0;
        //private static CandleStick lastC = null;
        public void CalculateViewPosition(double viewHeight, double lowestLow, double highestHigh)
        {
            //if (lastC != null && (this.Up != lastC.Up))
            //    lastY = 0;

            ViewHigh = CalculateViewHeight(viewHeight, lowestLow, highestHigh, this.High);
            ViewLow = CalculateViewHeight(viewHeight, lowestLow, highestHigh, this.Low);
            ViewOpen = CalculateViewHeight(viewHeight, lowestLow, highestHigh, this.Open);
            ViewClose = CalculateViewHeight(viewHeight, lowestLow, highestHigh, this.Close);

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

        public void InverseTrend()
        {
            double tempClose = this.Close;
            this.Close = this.Open;
            this.Open = tempClose;
        }

        private double CalculateViewHeight(double viewHeight, double lowestLow, double highestHigh, double priceHeight)
        {
            if (highestHigh == lowestLow) return 0;
            return ((priceHeight - lowestLow) * viewHeight) / (highestHigh - lowestLow);
        }

    }
}
