using Binance.Net.Interfaces;
using CryptoTrader.UserControls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static CryptoTrader.Utils;

namespace CryptoTrader
{
    public class TrendLineHelper
    {
        private static TrendLineStick movingLine = null;
        private static bool movingStartPoint = false;        

        public static List<TrendLineStick> GetTempTargetLines(string interval, double targetMovePercent, bool showTargetLines, IBinanceKline lastKline)
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

            return new List<TrendLineStick> 
            {
                new TrendLineStick(longTrendLine) { Visibility = showTargetLines ? Visibility.Visible : Visibility.Hidden },
                new TrendLineStick(currentTrendLine) { Visibility = showTargetLines ? Visibility.Visible : Visibility.Hidden },
                new TrendLineStick(shortTrendLine) { Visibility = showTargetLines ? Visibility.Visible : Visibility.Hidden }
            };
        }

        public static void UpdateTempTargetLines(Canvas klinesView, double targetMovePercent, bool showTargetLines, double closePrice, CandleStick firstKline, CandleStick lastKline, double lowestLowPrice, double highestHighPrice)
        {
            double targetPrice = (targetMovePercent / 100d) * closePrice;
            
            TrendLineStick tlLong = klinesView.Children.OfType<TrendLineStick>().FirstOrDefault(tl => tl.OriginalTrendLine.LineType == TrendLineType.TargetLong.ToString() && tl.OriginalTrendLine.ForSaving == false);
            tlLong.OriginalTrendLine.StartPrice = tlLong.OriginalTrendLine.EndPrice = closePrice + targetPrice;
            tlLong.SetXPositions(klinesView.ActualWidth, firstKline, lastKline);
            tlLong.SetYPositions(klinesView.ActualHeight, lowestLowPrice, highestHighPrice);

            TrendLineStick tlCurrent = klinesView.Children.OfType<TrendLineStick>().FirstOrDefault(tl => tl.OriginalTrendLine.LineType == TrendLineType.TargetCurrent.ToString() && tl.OriginalTrendLine.ForSaving == false);
            tlCurrent.OriginalTrendLine.StartPrice = tlCurrent.OriginalTrendLine.EndPrice = closePrice;
            tlCurrent.SetXPositions(klinesView.ActualWidth, firstKline, lastKline);
            tlCurrent.SetYPositions(klinesView.ActualHeight, lowestLowPrice, highestHighPrice);

            TrendLineStick tlShort = klinesView.Children.OfType<TrendLineStick>().FirstOrDefault(tl => tl.OriginalTrendLine.LineType == TrendLineType.TargetShort.ToString() && tl.OriginalTrendLine.ForSaving == false);
            tlShort.OriginalTrendLine.StartPrice = tlShort.OriginalTrendLine.EndPrice = closePrice - targetPrice;
            tlShort.SetXPositions(klinesView.ActualWidth, firstKline, lastKline);
            tlShort.SetYPositions(klinesView.ActualHeight, lowestLowPrice, highestHighPrice);

            tlLong.Visibility = tlCurrent.Visibility = tlShort.Visibility = showTargetLines ? Visibility.Visible : Visibility.Hidden;
        }

        public static void KeepTempTargetLines(Canvas klinesView)
        {
            TrendLineStick tlLong = klinesView.Children.OfType<TrendLineStick>().FirstOrDefault(tl => tl.OriginalTrendLine.LineType == TrendLineType.TargetLong.ToString() && tl.OriginalTrendLine.ForSaving == false);
            TrendLineStick tlNormal = klinesView.Children.OfType<TrendLineStick>().FirstOrDefault(tl => tl.OriginalTrendLine.LineType == TrendLineType.TargetCurrent.ToString() && tl.OriginalTrendLine.ForSaving == false);
            TrendLineStick tlShort = klinesView.Children.OfType<TrendLineStick>().FirstOrDefault(tl => tl.OriginalTrendLine.LineType == TrendLineType.TargetShort.ToString() && tl.OriginalTrendLine.ForSaving == false);

            tlLong = new TrendLineStick(tlLong.OriginalTrendLine.Clone(true));
            tlNormal = new TrendLineStick(tlNormal.OriginalTrendLine.Clone(true));
            tlShort = new TrendLineStick(tlShort.OriginalTrendLine.Clone(true));

            klinesView.Children.Add(tlLong);
            klinesView.Children.Add(tlNormal);
            klinesView.Children.Add(tlShort);
        }

        public static TrendLineStick MouseDown(double priceAtCursorPosition, Canvas klinesView, CandleStick firstKline, CandleStick lastKline, double viewWidth)
        {
            if (movingLine != null)
                return null;

            TrendLine trendLine = new TrendLine();
            trendLine.LineType = Utils.TrendLineType.Normal.ToString();
            trendLine.ForSaving = true;
            trendLine.StartPrice = trendLine.EndPrice = priceAtCursorPosition;

            Point klinesViewPosition = Mouse.GetPosition(klinesView);
            double minutesAtXPosition = (klinesViewPosition.X * (lastKline.OriginalKLine.CloseTime - firstKline.OriginalKLine.OpenTime).TotalMinutes) / viewWidth;

            trendLine.StartTime = trendLine.EndTime = firstKline.OriginalKLine.OpenTime.AddMinutes(minutesAtXPosition);
            movingStartPoint = false;

            movingLine = new TrendLineStick(trendLine);
            klinesView.Children.Add(movingLine);

            return movingLine;
        }

        public static void MouseDownOnLine(TrendLineStick tls, Canvas klinesView)
        {
            Point klinesViewPosition = Mouse.GetPosition(klinesView);
            movingStartPoint = tls.IsNearStart(klinesViewPosition, NearDistance);
            movingLine = tls;
        }

        public static TrendLineStick MouseMove(double priceAtCursorPosition, Canvas klinesView, CandleStick firstKline, CandleStick lastKline, double viewWidth)
        {
            if (movingLine == null)
                return null;

            Point klinesViewPosition = Mouse.GetPosition(klinesView);
            double minutesAtXPosition = (klinesViewPosition.X * (lastKline.OriginalKLine.CloseTime - firstKline.OriginalKLine.OpenTime).TotalMinutes) / viewWidth;

            if (movingStartPoint)
            {
                movingLine.OriginalTrendLine.StartPrice = priceAtCursorPosition;
                movingLine.OriginalTrendLine.StartTime = firstKline.OriginalKLine.OpenTime.AddMinutes(minutesAtXPosition);
            }
            else
            {
                movingLine.OriginalTrendLine.EndPrice = priceAtCursorPosition;
                movingLine.OriginalTrendLine.EndTime = firstKline.OriginalKLine.OpenTime.AddMinutes(minutesAtXPosition);
            }

            return movingLine;
        }

        public static bool MouseUp(Canvas klinesView)
        {
            if (movingLine == null)
                return false;

            Point startPoint = new Point(movingLine.line.X1, movingLine.line.Y1);
            if (movingLine.IsNearEnd(startPoint, NearDistance)) // remove line if it's too short (double click or accidental line)
            {
                klinesView.Children.Remove(movingLine);
                movingLine = null;
                return false;
            }

            movingLine = null;
            return true;
        }

        public static TrendLineStick TrendLineNearMouse(Canvas klinesView)
        {
            Point klinesViewPosition = Mouse.GetPosition(klinesView);

            TrendLineStick trendLineStick = klinesView.Children.OfType<TrendLineStick>().Where(tls => 
                    tls.OriginalTrendLine.ForSaving &&
                    (
                        tls.IsNearStart(klinesViewPosition, NearDistance) || 
                        tls.IsNearEnd(klinesViewPosition, NearDistance))
                    ).FirstOrDefault();

            return trendLineStick;
        }

        public static void RemoveTrendLine(TrendLineStick tls, Canvas klinesView)
        {
            klinesView.Children.Remove(tls);
        }

    }
}
