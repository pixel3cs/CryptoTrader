using System.Windows.Controls;
using System.Windows.Media;

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

        public void SetXPositions(string interval, CandleStick lastKline, double viewWidth, double highestRightX)
        {
            double intervalInMinutes = Utils.IntervalInMinutes(interval);
            double startMinutesFromLastKLine = (lastKline.OriginalKLine.CloseTime - OriginalTrendLine.StartTime).TotalMinutes;
            double endMinutesFromLastKLine = (lastKline.OriginalKLine.CloseTime - OriginalTrendLine.EndTime).TotalMinutes;

            line.X1 = Utils.CalculateViewWidth(viewWidth, 0, highestRightX, highestRightX - (startMinutesFromLastKLine / intervalInMinutes));
            line.X2 = Utils.CalculateViewWidth(viewWidth, 0, highestRightX, highestRightX - (endMinutesFromLastKLine / intervalInMinutes));
        }

        public void SetYPositions(double viewHeight, double lowestLowPrice, double highestHighPrice)
        {
            line.Y1 = viewHeight - Utils.CalculateViewHeight(viewHeight, lowestLowPrice, highestHighPrice, OriginalTrendLine.StartPrice);
            line.Y2 = viewHeight - Utils.CalculateViewHeight(viewHeight, lowestLowPrice, highestHighPrice, OriginalTrendLine.EndPrice);
        }

        public void Fill(Brush brush)
        {
            line.Stroke = brush;
        }
    }
}
