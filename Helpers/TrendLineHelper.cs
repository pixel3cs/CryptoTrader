using Binance.Net.Interfaces;
using CryptoTrader.UserControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using static CryptoTrader.Utils;

namespace CryptoTrader
{
    public class TrendLineHelper
    {
        private static TrendLine drawingLine = null;

        public static void MouseDown(string selectedTool, Canvas klinesView)
        {
            if (drawingLine != null)
                return;

            if (selectedTool == "trendline")
            {
                drawingLine = new TrendLine();

            }
        }

        public static void MouseMove(string selectedTool, Canvas klinesView)
        {

        }

        public static void MouseUp(string selectedTool, Canvas klinesView)
        {

        }

        public static List<TrendLine> GetTargetLines(string interval, double targetMovePercent, IBinanceKline lastKline)
        {
            int intervalInMinutes = Utils.IntervalInMinutes(interval);
            double closePrice = (double)lastKline.Close;
            DateTime closeTime = lastKline.CloseTime;
            double targetPrice = (targetMovePercent / 100d) * closePrice;
            double mleft = -20, mright = 50;

            TrendLine longTrendLine = new TrendLine()
            {
                StartPrice = closePrice + targetPrice,
                StartTime = closeTime.AddMinutes(mleft * intervalInMinutes),
                EndPrice = closePrice + targetPrice,
                EndTime = closeTime.AddMinutes(mright * intervalInMinutes),
                ForSaving = false,
                LineType = TrendLineType.TargetLong.ToString()
            };

            TrendLine currentTrendLine = new TrendLine()
            {
                StartPrice = closePrice,
                StartTime = closeTime.AddMinutes(mleft * intervalInMinutes),
                EndPrice = closePrice,
                EndTime = closeTime.AddMinutes(mright * intervalInMinutes),
                ForSaving = false,
                LineType = TrendLineType.TargetCurrent.ToString()
            };

            TrendLine shortTrendLine = new TrendLine()
            {
                StartPrice = closePrice - targetPrice,
                StartTime = closeTime.AddMinutes(mleft * intervalInMinutes),
                EndPrice = closePrice - targetPrice,
                EndTime = closeTime.AddMinutes(mright * intervalInMinutes),
                ForSaving = false,
                LineType = TrendLineType.TargetShort.ToString()
            };

            return new List<TrendLine> { longTrendLine, currentTrendLine, shortTrendLine };
        }

        public static void UpdateTargetLines(Canvas klinesView, double targetMovePercent)
        {
            double closePrice = (double)klinesView.Children.OfType<CandleStick>().Last().OriginalKLine.Close;
            double targetPrice = (targetMovePercent / 100d) * closePrice;
            
            TrendLineStick tlLong = klinesView.Children.OfType<TrendLineStick>().FirstOrDefault(tl => tl.OriginalTrendLine.LineType == TrendLineType.TargetLong.ToString() && tl.OriginalTrendLine.ForSaving == false);
            tlLong.OriginalTrendLine.StartPrice = tlLong.OriginalTrendLine.EndPrice = closePrice + targetPrice;

            TrendLineStick tlShort = klinesView.Children.OfType<TrendLineStick>().FirstOrDefault(tl => tl.OriginalTrendLine.LineType == TrendLineType.TargetShort.ToString() && tl.OriginalTrendLine.ForSaving == false);
            tlShort.OriginalTrendLine.StartPrice = tlShort.OriginalTrendLine.EndPrice = closePrice - targetPrice;
        }

    }
}
