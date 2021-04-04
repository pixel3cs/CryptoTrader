using System.Windows.Controls;
using System.Windows.Media;

namespace CryptoTrader.UserControls
{
    public partial class TrendLineStick : UserControl
    {
        public TrendLine OriginalTrendLine { get; private set; }

        public bool Up;

        private TrendLineStick()
        {
            InitializeComponent();
        }

        public TrendLineStick(TrendLine trendLine)
        {
            InitializeComponent();
            this.OriginalTrendLine = trendLine;
        }

        public void SetPositions(double viewWidth, double viewHeight, double lowestLow, double highestHigh, CandleStick firstKline, CandleStick lastKline)
        {
            //double targetPrice = ((TargetROE / Leverage) / 100d) * lastKline.Close;
            //double longClose = Utils.CalculateViewHeight(viewHeight, lowestPrice, highestPrice, lastKline.Close + targetPrice);
            //double shortClose = Utils.CalculateViewHeight(viewHeight, lowestPrice, highestPrice, lastKline.Close - targetPrice);

            //X1 = viewWidth - mleft
            //Y1 = viewHeight - lastKline.ViewClose
            //X2 = viewWidth + mright
            //Y2 = viewHeight - lastKline.ViewClose

            line.Y1 = viewHeight - Utils.CalculateViewHeight(viewHeight, lowestLow, highestHigh, OriginalTrendLine.StartPrice);
            line.Y2 = viewHeight - Utils.CalculateViewHeight(viewHeight, lowestLow, highestHigh, OriginalTrendLine.EndPrice);
            line.X1 = 0;
            line.X2 = viewWidth;
        }

        public void Fill(Brush brush)
        {
            line.Stroke = brush;
        }
    }
}
