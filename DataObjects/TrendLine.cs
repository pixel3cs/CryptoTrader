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

    }
}
