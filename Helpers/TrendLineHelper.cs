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
        private static int nearDistance = 15; // pixels

        public static List<TrendLine> GetTempTargetLines(string interval, double targetMovePercent, IBinanceKline lastKline)
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

        public static void UpdateTempTargetLines(Canvas klinesView, double targetMovePercent)
        {
            double closePrice = (double)klinesView.Children.OfType<CandleStick>().Last().OriginalKLine.Close;
            double targetPrice = (targetMovePercent / 100d) * closePrice;
            
            TrendLineStick tlLong = klinesView.Children.OfType<TrendLineStick>().FirstOrDefault(tl => tl.OriginalTrendLine.LineType == TrendLineType.TargetLong.ToString() && tl.OriginalTrendLine.ForSaving == false);
            tlLong.OriginalTrendLine.StartPrice = tlLong.OriginalTrendLine.EndPrice = closePrice + targetPrice;

            TrendLineStick tlShort = klinesView.Children.OfType<TrendLineStick>().FirstOrDefault(tl => tl.OriginalTrendLine.LineType == TrendLineType.TargetShort.ToString() && tl.OriginalTrendLine.ForSaving == false);
            tlShort.OriginalTrendLine.StartPrice = tlShort.OriginalTrendLine.EndPrice = closePrice - targetPrice;
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

        public static TrendLineStick MouseDown(string selectedTool, double priceAtCursorPosition, Canvas klinesView, CandleStick firstKline, CandleStick lastKline, double viewWidth)
        {
            if (movingLine != null)
                return null;

            if (selectedTool == "trendline")
            {
                TrendLine trendLine = new TrendLine();
                trendLine.LineType = Utils.TrendLineType.Normal.ToString();
                trendLine.ForSaving = true;
                trendLine.StartPrice = trendLine.EndPrice = priceAtCursorPosition;

                Point klinesViewPosition = Mouse.GetPosition(klinesView);
                double minutesAtXPosition = (klinesViewPosition.X * (lastKline.OriginalKLine.CloseTime - firstKline.OriginalKLine.CloseTime).TotalMinutes) / viewWidth;

                trendLine.StartTime = trendLine.EndTime = firstKline.OriginalKLine.CloseTime.AddMinutes(minutesAtXPosition);
                movingStartPoint = false;

                movingLine = new TrendLineStick(trendLine);
                klinesView.Children.Add(movingLine);

                return movingLine;
            }

            return null;
        }

        public static void MouseDownOnLine(TrendLineStick tls, Canvas klinesView)
        {
            Point klinesViewPosition = Mouse.GetPosition(klinesView);
            movingStartPoint = tls.IsNearStart(klinesViewPosition, nearDistance);
            movingLine = tls;
        }

        public static TrendLineStick MouseMove(string selectedTool, double priceAtCursorPosition, Canvas klinesView, CandleStick firstKline, CandleStick lastKline, double viewWidth)
        {
            if (movingLine == null)
                return null;

            if (selectedTool == "trendline")
            {
                Point klinesViewPosition = Mouse.GetPosition(klinesView);
                double minutesAtXPosition = (klinesViewPosition.X * (lastKline.OriginalKLine.CloseTime - firstKline.OriginalKLine.CloseTime).TotalMinutes) / viewWidth;

                if (movingStartPoint)
                {
                    movingLine.OriginalTrendLine.StartPrice = priceAtCursorPosition;
                    movingLine.OriginalTrendLine.StartTime = firstKline.OriginalKLine.CloseTime.AddMinutes(minutesAtXPosition);
                }
                else
                {
                    movingLine.OriginalTrendLine.EndPrice = priceAtCursorPosition;
                    movingLine.OriginalTrendLine.EndTime = firstKline.OriginalKLine.CloseTime.AddMinutes(minutesAtXPosition);
                }

                return movingLine;
            }

            return null;
        }

        public static bool MouseUp(string selectedTool, Canvas klinesView)
        {
            if (movingLine == null)
                return false;

            Point startPoint = new Point(movingLine.line.X1, movingLine.line.Y1);
            if (movingLine.IsNearEnd(startPoint, nearDistance)) // remove line if it's too short (double click or accidental line)
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
                        tls.IsNearStart(klinesViewPosition, nearDistance) || 
                        tls.IsNearEnd(klinesViewPosition, nearDistance))
                    ).FirstOrDefault();

            return trendLineStick;
        }

        public static void RemoveTrendLine(TrendLineStick tls, Canvas klinesView)
        {
            klinesView.Children.Remove(tls);
        }



    }
}
