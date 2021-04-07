using CryptoTrader.UserControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace CryptoTrader
{
    public class TrendLine
    {
        public DateTime StartTime;
        public double StartPrice;

        public DateTime EndTime;
        public double EndPrice;

        public bool ForSaving;
        public string LineType;

        public TrendLine Clone(bool forSaving)
        {
            TrendLine trendLine = new TrendLine();
            trendLine.StartPrice = this.StartPrice;
            trendLine.StartTime = this.StartTime;
            trendLine.EndPrice = this.EndPrice;
            trendLine.EndTime = this.EndTime;
            trendLine.LineType = this.LineType;
            trendLine.ForSaving = forSaving;
            return trendLine;
        }
    }
}
