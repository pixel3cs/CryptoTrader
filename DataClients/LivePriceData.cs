using Binance.Net;
using Binance.Net.Enums;
using System;
using System.Threading;
using System.Windows.Input;

namespace CryptoTrader
{
    public class LivePriceData 
    {
        private BinanceClient klinesClient = null;
        private BinanceTickClient tickClient = null;
       
        private ServerDataProcessDelegate serverDataHandler = null;

        public TimeSpan LastDataLoadTime { get; private set; }

        public LivePriceData(ServerDataProcessDelegate ServerDataHandler)
        {
            this.serverDataHandler = ServerDataHandler;
        }

        public void LoadFromServer(string symbol, string interval)
        {
            ParameterizedThreadStart ts = new ParameterizedThreadStart(LoadFromServerThread);
            Thread thread = new Thread(ts);
            thread.IsBackground = true;
            thread.Start(new string[] { symbol, interval });
        }

        private void LoadFromServerThread(object parameter)
        {
            // stop receiving data when symbol or interval of TradeDataView is changed
            if (tickClient != null)
                tickClient.StopBroadcastingData(serverDataHandler);

            // variables
            string[] strParams = (string[])parameter;
            string symbol = strParams[0];
            string interval = strParams[1];
            bool isTick = (interval == "tick" || interval.EndsWith("s"));
            KlineInterval klineInterval = Utils.ToKlineInterval(interval);

            // load data
            DateTime dateStart = DateTime.Now;

            if (isTick == true && symbol != null && interval != null && serverDataHandler != null)
            {
                if (tickClient == null)
                    tickClient = BinanceTickClient.GetInstance(symbol);

                tickClient.StartBroadcastingData(interval, serverDataHandler);
            }

            if (isTick == false && symbol != null && interval != null && serverDataHandler != null)
            {
                if (klinesClient == null)
                    klinesClient = new BinanceClient();

                var serverResponse = klinesClient.FuturesUsdt.Market.GetKlines(symbol, klineInterval);
                MainWindow.UpdateWeightUsage(serverResponse.ResponseHeaders);                

                if (serverResponse != null && serverResponse.Success)
                    serverDataHandler(serverResponse.Data, true, false);
            }

            LastDataLoadTime = DateTime.Now - dateStart;
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
