using CryptoTrader.UserControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using static CryptoTrader.Utils;

namespace CryptoTrader
{
    public class TrendLineData
    {
        private TrendLineData()
        {
        }

        public static IEnumerable<TrendLine> LoadTrendLines(string selectedSymbol, string selectedInterval)
        {
            List<TrendLine> trendLines = new List<TrendLine>();
            return trendLines;
        }

        public static void SaveTrendLines(List<TrendLineStick> tradeLineSticks, string selectedSymbol, string selectedInterval)
        {

        }






    }
}
