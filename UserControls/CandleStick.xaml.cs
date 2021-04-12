using Binance.Net.Interfaces;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CryptoTrader.UserControls
{
    public partial class CandleStick : UserControl
    {
        public IBinanceKline OriginalKLine { get; private set; }
        public double Low { get { return (double)OriginalKLine.Low; } }
        public double High { get { return (double)OriginalKLine.High; } }
        public double Open { get { return (double)OriginalKLine.Open; } }
        public double Close { get { return (double)OriginalKLine.Close; } }
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
        }

        //public void CalculateHeikinAshi(CandleStick prevCandleStick)
        //{
        //    this.Close = (double)(OriginalKLine.Open + OriginalKLine.Close + OriginalKLine.Low + OriginalKLine.High) / 4d;
        //    this.Open = (double)(prevCandleStick.Open + prevCandleStick.Close) / 2d;
        //    this.High = Math.Max(Math.Max((double)OriginalKLine.High, this.Open), this.Close);
        //    this.Low = Math.Min(Math.Min((double)OriginalKLine.Low, this.Open), this.Close);
        //}

        //public void RestoreToOriginal()
        //{
        //    this.Close = (double)OriginalKLine.Close;
        //    this.Open = (double)OriginalKLine.Open;
        //    this.High = (double)OriginalKLine.High;
        //    this.Low = (double)OriginalKLine.Low;
        //}

        public void SetWidthPositions(double viewWidth, CandleStick firstKline, CandleStick lastKline)
        {
            double xViewLeft = Utils.CalculateViewWidth(viewWidth, firstKline.OriginalKLine.OpenTime.Ticks, lastKline.OriginalKLine.CloseTime.Ticks, this.OriginalKLine.OpenTime.Ticks);
            double xViewRight = Utils.CalculateViewWidth(viewWidth, firstKline.OriginalKLine.OpenTime.Ticks, lastKline.OriginalKLine.CloseTime.Ticks, this.OriginalKLine.CloseTime.Ticks) - 0.25d;

            Canvas.SetLeft(this, xViewLeft);
            this.Width = Math.Abs(xViewRight - xViewLeft);
        }

        //private static double lastY = 0;
        //private static CandleStick lastC = null;
        public void SetHeightPositions(double viewHeight, double lowestLowPrice, double highestHighPrice)
        {
            //if (lastC != null && (this.Up != lastC.Up))
            //    lastY = 0;

            double ViewHigh = Utils.CalculateViewHeight(viewHeight, lowestLowPrice, highestHighPrice, this.High);
            double ViewLow = Utils.CalculateViewHeight(viewHeight, lowestLowPrice, highestHighPrice, this.Low);
            double ViewOpen = Utils.CalculateViewHeight(viewHeight, lowestLowPrice, highestHighPrice, this.Open);
            double ViewClose = Utils.CalculateViewHeight(viewHeight, lowestLowPrice, highestHighPrice, this.Close);

            Canvas.SetTop(this, viewHeight - ViewHigh);
            this.Height = ViewHigh - ViewLow;

            body.Height = Math.Abs(ViewOpen - ViewClose);
            body.Margin = new Thickness(0, ViewHigh - (this.Up ? ViewClose : ViewOpen), 0, 0);

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
