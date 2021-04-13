using CryptoTrader.UserControls;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace CryptoTrader
{
    public class TrendLineData
    {
        private TrendLineData()
        {
        }

        private static string GetFilePath(string symbol, string interval)
        {
            string folder = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            folder = Path.Combine(folder, "TrendLines");
            
            if (Directory.Exists(folder) == false)
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, string.Format("{0}_{1}.txt", symbol, interval));
        }

        public static List<TrendLine> LoadFromDisk(string symbol, string interval)
        {
            string path = GetFilePath(symbol, interval);

            if (File.Exists(path) == false)
                return new List<TrendLine>();

            string text = File.ReadAllText(path);
            List<TrendLine> tlList = JsonConvert.DeserializeObject<List<TrendLine>>(text);

            return tlList;
        }

        public static void SaveToDisk(Canvas klinesView, string symbol, string interval)
        {
            string path = GetFilePath(symbol, interval);

            List<TrendLine> tlList = klinesView.Children.OfType<TrendLineStick>().Where(tls => tls.OriginalTrendLine.ForSaving).Select(tls => tls.OriginalTrendLine).ToList();
            string text = JsonConvert.SerializeObject(tlList);

            File.WriteAllText(path, text);
        }

    }
}
