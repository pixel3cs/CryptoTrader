using Binance.Net;
using Binance.Net.Objects.Spot.MarketStream;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CryptoTrader
{
    // only one instance of BinanceTickClient per Symbol
    // multiple "data change events" for the same Interval (example: 2 TradeDataView controls having same Symbol + Interval)
    public class BinanceTickClient
    {
        private static Dictionary<string, BinanceTickClient> tickClients = new Dictionary<string, BinanceTickClient>();       

        private string selectedSymbol;
        private BinanceSocketClient socketClient = null;
        private object dataHandlersLock = new object();
        private Dictionary<int, List<ServerDataProcessDelegate>> serverDataHandlers = new Dictionary<int, List<ServerDataProcessDelegate>>();
        protected LastTickValueDelegate lastTickValueHandler = null;

        private BinanceTickClient(string selectedSymbol)
        {
            this.selectedSymbol = selectedSymbol;
        }

        public static BinanceTickClient GetInstance(string symbol)
        {
            if (tickClients.ContainsKey(symbol) == false)
            {
                BinanceTickClient newTickClient = new BinanceTickClient(symbol);
                tickClients.Add(symbol, newTickClient);
            }

            return tickClients[symbol];
        }

        public static void StartBroadcastingLastTickValue(string symbol, LastTickValueDelegate lastTickValueHandler)
        {
            ThreadStart ts = new ThreadStart(() => 
            {
                BinanceTickClient tickClient = GetInstance(symbol);
                tickClient.lastTickValueHandler = lastTickValueHandler;
                tickClient.StartBroadcastingData(null, null);
            });
            Thread thread = new Thread(ts);
            thread.IsBackground = true;
            thread.Start();
        }

        public void StartBroadcastingData(string selectedInterval, ServerDataProcessDelegate serverDataHandler)
        {
            lock (dataHandlersLock)
            {
                if (selectedInterval != null && serverDataHandler != null)
                {
                    int interval = (selectedInterval == "tick" ? 0 : int.Parse(selectedInterval.Replace("s", "")));
                    if (serverDataHandlers.ContainsKey(interval) == false)
                        serverDataHandlers.Add(interval, new List<ServerDataProcessDelegate>());
                    serverDataHandlers[interval].Add(serverDataHandler);
                }

                if (socketClient == null)
                {
                    var socketClient = new Binance.Net.BinanceSocketClient();
                    var socketCall = socketClient.FuturesUsdt.SubscribeToAggregatedTradeUpdates(selectedSymbol, TradeUpdatesServerHandler);

                    socketCall.Data.ActivityPaused += SocketData_ActivityPaused;
                    socketCall.Data.ActivityUnpaused += SocketData_ActivityUnpaused;
                    socketCall.Data.ConnectionLost += SocketData_ConnectionLost;
                    socketCall.Data.ConnectionRestored += SocketData_ConnectionRestored;
                    socketCall.Data.Exception += SocketData_Exception;
                }
            }
        }

        public void StopBroadcastingData(ServerDataProcessDelegate serverDataHandler)
        {
            lock (dataHandlersLock)
            {
                // serverDataHandler instance is a different object for each TradeDataView
                int foundInterval = -1;
                foreach (int interval in serverDataHandlers.Keys)
                    if (serverDataHandlers[interval].Contains(serverDataHandler))
                        foundInterval = interval;

                if (foundInterval >= 0)
                {
                    serverDataHandlers[foundInterval].Remove(serverDataHandler);
                    if (serverDataHandlers[foundInterval].Count == 0)
                        serverDataHandlers.Remove(foundInterval);
                }
            }
        }

        // SOCKET CONNECTION CODE
        private BinanceKLine s30, s15, s5, s3, s1, oneSecond;
        private int s30Count, s15Count, s5Count, s3Count;
        private List<BinanceKLine> ticks = new List<BinanceKLine>();
        private BinanceStreamAggregatedTrade lastTrade;

        private void TradeUpdatesServerHandler(BinanceStreamAggregatedTrade trade)
        {
            lock (dataHandlersLock)
            {
                if (lastTrade != null && trade.TradeTime < lastTrade.TradeTime)
                    return; // drop older messages

                if (lastTrade != null && trade.TradeTime == lastTrade.TradeTime && trade.Price == lastTrade.Price)
                    return; // drop similar messages

                if (lastTickValueHandler != null)
                    lastTickValueHandler(trade); // handle real time trading

                if (serverDataHandlers.Keys.Count == 0)
                    return; // cancel view calculations if no subscribers

                // tick
                BinanceKLine tick = new BinanceKLine();
                tick.Open = tick.Close = tick.Low = tick.High = trade.Price;
                tick.OpenTime = tick.CloseTime = trade.TradeTime;
                if (lastTrade != null)
                {
                    tick.Open = lastTrade.Price;
                    tick.Low = Math.Min(trade.Price, lastTrade.Price);
                    tick.High = Math.Max(trade.Price, lastTrade.Price);
                }
                ticks.Add(tick);

                // 1 second
                if (oneSecond == null)
                {
                    oneSecond = InitFromTrade(trade);
                }
                else if (trade.TradeTime.Second == oneSecond.OpenTime.Second)
                {
                    oneSecond.High = Math.Max(oneSecond.High, trade.Price);
                    oneSecond.Low = Math.Min(oneSecond.Low, trade.Price);
                    oneSecond.BaseVolume += trade.Quantity;
                }
                else
                {
                    oneSecond.Close = lastTrade.Price;
                    oneSecond.CloseTime = lastTrade.TradeTime;
                    s1 = oneSecond;
                    oneSecond = InitFromTrade(trade);
                }

                // 3 seconds
                if (s1 != null)
                {
                    s3 = CombineSeconds(s3, s1);
                    s3Count++;
                }

                // 5 seconds
                if (s1 != null)
                {
                    s5 = CombineSeconds(s5, s1);
                    s5Count++;
                }

                // 15 seconds
                if (s1 != null)
                {
                    s15 = CombineSeconds(s15, s1);
                    s15Count++;
                }

                // 30 seconds
                if (s1 != null)
                {
                    s30 = CombineSeconds(s30, s1);
                    s30Count++;
                }

                // send data to each interval handler
                if (ticks.Count > 0 && (trade.TradeTime - ticks[0].OpenTime).TotalMilliseconds >= 250)
                {
                    if (serverDataHandlers.ContainsKey(0))
                    {
                        foreach (var serverDataHandler in serverDataHandlers[0])
                            serverDataHandler(ticks, null, null, null, false, true);
                    }
                    ticks.Clear();
                }

                if (s1 != null)
                {
                    if (serverDataHandlers.ContainsKey(1))
                    {
                        List<BinanceKLine> newKLines = new List<BinanceKLine>() { s1 };
                        foreach (var serverDataHandler in serverDataHandlers[1])
                            serverDataHandler(newKLines, null, null, null, false, true);
                    }
                    s1 = null;
                }

                if (s3Count == 3)
                {
                    if (serverDataHandlers.ContainsKey(3))
                    {
                        List<BinanceKLine> newKLines = new List<BinanceKLine>() { s3 };
                        foreach (var serverDataHandler in serverDataHandlers[3])
                            serverDataHandler(newKLines, null, null, null, false, true);
                    }
                    s3 = null;
                    s3Count = 0;
                }

                if (s5Count == 5)
                {
                    if (serverDataHandlers.ContainsKey(5))
                    {
                        List<BinanceKLine> newKLines = new List<BinanceKLine>() { s5 };
                        foreach (var serverDataHandler in serverDataHandlers[5])
                            serverDataHandler(newKLines, null, null, null, false, true);
                    }
                    s5 = null;
                    s5Count = 0;
                }

                if (s15Count == 15)
                {
                    if (serverDataHandlers.ContainsKey(15))
                    {
                        List<BinanceKLine> newKLines = new List<BinanceKLine>() { s15 };
                        foreach (var serverDataHandler in serverDataHandlers[15])
                            serverDataHandler(newKLines, null, null, null, false, true);
                    }
                    s15 = null;
                    s15Count = 0;
                }

                if (s30Count == 30)
                {
                    if (serverDataHandlers.ContainsKey(30))
                    {
                        List<BinanceKLine> newKLines = new List<BinanceKLine>() { s30 };
                        foreach (var serverDataHandler in serverDataHandlers[30])
                            serverDataHandler(newKLines, null, null, null, false, true);
                    }
                    s30 = null;
                    s30Count = 0;
                }

                lastTrade = trade;
            }
        }

        private static BinanceKLine CloneSecond(BinanceKLine second)
        {
            BinanceKLine clone = new BinanceKLine();
            clone.Open = second.Open;
            clone.Close = second.Close;
            clone.Low = second.Low;
            clone.High = second.High;
            clone.BaseVolume = second.BaseVolume;
            clone.OpenTime = second.OpenTime;
            clone.CloseTime = second.CloseTime;
            return clone;
        }

        private static BinanceKLine CombineSeconds(BinanceKLine a, BinanceKLine b)
        {
            if (a == null)
            {
                return CloneSecond(b);
            }
            else
            {
                a.Close = b.Close;
                a.Low = Math.Min(a.Low, b.Low);
                a.High = Math.Max(a.High, b.High);
                a.BaseVolume += b.BaseVolume;
                a.CloseTime = b.CloseTime;
                return a;
            }
        }

        private static BinanceKLine InitFromTrade(BinanceStreamAggregatedTrade trade)
        {
            return new BinanceKLine()
            {
                Open = trade.Price,
                Close = trade.Price,
                High = trade.Price,
                Low = trade.Price,
                OpenTime = trade.TradeTime,
                CloseTime = trade.TradeTime,
                BaseVolume = trade.Quantity
            };
        }

        private void SocketData_Exception(Exception obj)
        {
        }

        private void SocketData_ConnectionRestored(TimeSpan obj)
        {
        }

        private void SocketData_ConnectionLost()
        {
        }

        private void SocketData_ActivityUnpaused()
        {
        }

        private void SocketData_ActivityPaused()
        {
        }


    }
}
