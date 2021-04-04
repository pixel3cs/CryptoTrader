using System.Windows.Controls;
using System.Windows.Media;

namespace CryptoTrader.UserControls
{
    public partial class TrendLineStick : UserControl
    {
        public double X1, Y1, X2, Y2;
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

        public void SetPositions(double viewWidth, double viewHeight)
        {
            //double targetPrice = ((TargetROE / Leverage) / 100d) * lastKline.Close;
            //double longClose = Utils.CalculateViewHeight(viewHeight, lowestPrice, highestPrice, lastKline.Close + targetPrice);
            //double shortClose = Utils.CalculateViewHeight(viewHeight, lowestPrice, highestPrice, lastKline.Close - targetPrice);

            //Line line = new Line() { X1 = viewWidth - mleft, Y1 = viewHeight - lastKline.ViewClose, X2 = viewWidth + mright, Y2 = viewHeight - lastKline.ViewClose };
            //line.StrokeThickness = 1;
            //line.Stroke = Brushes.Orange;
            //canvas.Children.Add(line);

            //line = new Line() { X1 = viewWidth - mleft, Y1 = viewHeight - longClose, X2 = viewWidth + mright, Y2 = viewHeight - longClose };
            //line.StrokeThickness = 2;
            //line.Stroke = TradeDataView.greenBrush;
            //canvas.Children.Add(line);

            //line = new Line() { X1 = viewWidth - mleft, Y1 = viewHeight - shortClose, X2 = viewWidth + mright, Y2 = viewHeight - shortClose };
            //line.StrokeThickness = 2;
            //line.Stroke = TradeDataView.redBrush;
            //canvas.Children.Add(line);
        }

        public void Fill(Brush brush)
        {
            line.Stroke = brush;
        }
    }
}
