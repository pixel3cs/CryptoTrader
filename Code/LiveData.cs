using Binance.Net;
using Binance.Net.Enums;
using System;
using System.Threading;
using System.Windows.Input;

namespace CryptoTrader.Code
{
    public class LiveData 
    {
        private BinanceClient klinesClient = null;
        private BinanceTickClient tickClient = null;
           
        private string selectedSymbol, selectedInterval;
        private KlineInterval klineInterval = KlineInterval.OneMinute;
        public bool IsTick;        
        public TimeSpan LastDataLoadTime { get; private set; }
        
        private ServerDataProcessDelegate serverDataHandler = null;

        public LiveData(ServerDataProcessDelegate ServerDataHandler)
        {
            this.serverDataHandler = ServerDataHandler;
        }

        public void SetSymbolAndInterval(string symbol, string interval)
        {
            // stop receiving data when symbol or interval of TradeDataView is changed
            if (IsTick && tickClient != null)
                tickClient.StopBroadcastingData(selectedInterval, serverDataHandler);

            // set symbols and intervals
            selectedSymbol = symbol;
            selectedInterval = interval;

            IsTick = (interval == "tick" || interval.EndsWith("s"));

            switch (selectedInterval)
            {
                case "1m": klineInterval = KlineInterval.OneMinute; break;
                case "3m": klineInterval = KlineInterval.ThreeMinutes; break;
                case "5m": klineInterval = KlineInterval.FiveMinutes; break;
                case "15m": klineInterval = KlineInterval.FifteenMinutes; break;
                case "30m": klineInterval = KlineInterval.ThirtyMinutes; break;
                case "1h": klineInterval = KlineInterval.OneHour; break;
                case "2h": klineInterval = KlineInterval.TwoHour; break;
                case "4h": klineInterval = KlineInterval.FourHour; break;
                case "1d": klineInterval = KlineInterval.OneDay; break;
                default: klineInterval = KlineInterval.OneMinute; break;
            }
        }

        public void LoadFromServer()
        {
            ThreadStart ts = new ThreadStart(LoadFromServerThread);
            Thread thread = new Thread(ts);
            thread.IsBackground = true;
            thread.Start();
        }

        private void LoadFromServerThread()
        {
            DateTime dateStart = DateTime.Now;

            if (IsTick == true && selectedSymbol != null && selectedInterval != null && serverDataHandler != null)
            {
                if (tickClient == null)
                    tickClient = BinanceTickClient.GetInstance(selectedSymbol);

                tickClient.StartBroadcastingData(selectedInterval, serverDataHandler);
            }

            if (IsTick == false && selectedSymbol != null && selectedInterval != null && serverDataHandler != null)
            {
                if (klinesClient == null)
                    klinesClient = new Binance.Net.BinanceClient();

                var serverResponse = klinesClient.FuturesUsdt.Market.GetKlines(selectedSymbol, klineInterval);
                MainWindow.UpdateWeightUsage(serverResponse.ResponseHeaders);

                if (serverResponse != null && serverResponse.Success)
                    serverDataHandler(serverResponse.Data, true);
            }

            LastDataLoadTime = DateTime.Now - dateStart;
        }

        public void ProcessMouseDownEvents(MouseButtonEventArgs e, bool hasData)
        {
            if (e.ChangedButton == MouseButton.Middle)
                this.LoadFromServer();

            if ((e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right) && hasData == false)
                this.LoadFromServer();
        }

        public void SafelyClose()
        {
            //foreach (var client in socketClients)
            //{
            //    socketCalls[client.Key].Data.ActivityPaused -= SocketData_ActivityPaused;
            //    socketCalls[client.Key].Data.ActivityUnpaused -= SocketData_ActivityUnpaused;
            //    socketCalls[client.Key].Data.ConnectionLost -= SocketData_ConnectionLost;
            //    socketCalls[client.Key].Data.ConnectionRestored -= SocketData_ConnectionRestored;
            //    socketCalls[client.Key].Data.Exception -= SocketData_Exception;

            //    client.Value.Unsubscribe(socketCalls[client.Key].Data);
            //}

            //socketClients.Clear();
            //socketCalls.Clear();
        }

    }
}
