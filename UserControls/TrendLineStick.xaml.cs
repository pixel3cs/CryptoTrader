using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static CryptoTrader.Utils;

namespace CryptoTrader.UserControls
{
    public partial class TrendLineStick : UserControl
    {
        public TrendLine OriginalTrendLine { get; private set; }

        public bool Up { get { return OriginalTrendLine.EndPrice >= OriginalTrendLine.StartPrice; } }

        private TrendLineStick()
        {
            InitializeComponent();
        }

        public TrendLineStick(TrendLine trendLine)
        {
            InitializeComponent();
            this.OriginalTrendLine = trendLine;
        }

        public void SetXPositions(double viewWidth, CandleStick firstKline, CandleStick lastKline)
        {
            line.X1 = Utils.CalculateViewWidth(viewWidth, firstKline.OriginalKLine.CloseTime.Ticks, lastKline.OriginalKLine.CloseTime.Ticks, OriginalTrendLine.StartTime.Ticks);
            line.X2 = Utils.CalculateViewWidth(viewWidth, firstKline.OriginalKLine.CloseTime.Ticks, lastKline.OriginalKLine.CloseTime.Ticks, OriginalTrendLine.EndTime.Ticks);
        }

        public void SetYPositions(double viewHeight, double lowestLowPrice, double highestHighPrice)
        {
            line.Y1 = viewHeight - Utils.CalculateViewHeight(viewHeight, lowestLowPrice, highestHighPrice, OriginalTrendLine.StartPrice);
            line.Y2 = viewHeight - Utils.CalculateViewHeight(viewHeight, lowestLowPrice, highestHighPrice, OriginalTrendLine.EndPrice);
        }

        public void Fill(Brush greenBrush, Brush redBrush)
        {
            if (OriginalTrendLine.LineType == TrendLineType.Normal.ToString())
            {
                line.Stroke = this.Up ? greenBrush : redBrush;
            }
            else
            {
                if (OriginalTrendLine.LineType == TrendLineType.TargetLong.ToString()) line.Stroke = greenBrush;
                if (OriginalTrendLine.LineType == TrendLineType.TargetCurrent.ToString()) line.Stroke = Brushes.Orange;
                if (OriginalTrendLine.LineType == TrendLineType.TargetShort.ToString()) line.Stroke = redBrush;
            }

            if (OriginalTrendLine.ForSaving == false)
                line.StrokeDashArray = new DoubleCollection(new double[] { 2, 1 });
        }

        public bool IsNearStart(Point point, int nearDistnace)
        {
            return ((point.X - line.X1) * (point.X - line.X1) + (point.Y - line.Y1) * (point.Y - line.Y1)) < nearDistnace * nearDistnace;
        }

        public bool IsNearEnd(Point point, int nearDistnace)
        {
            return ((point.X - line.X2) * (point.X - line.X2) + (point.Y - line.Y2) * (point.Y - line.Y2)) < nearDistnace * nearDistnace;
        }

    }
}
