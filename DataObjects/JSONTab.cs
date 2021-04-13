using CryptoTrader.UserControls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CryptoTrader.DataObjects
{
    public class JSONTab
    {
        public string Symbol;
        public bool ShowTargetLines;
        public double Leverage;
        public double TargetROE;

        private static string GetFilePath()
        {
            string folder = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            return Path.Combine(folder, "tabs.txt");
        }

        public static List<JSONTab> LoadFromDisk()
        {
            string path = GetFilePath();

            if (File.Exists(path) == false)
                return new List<JSONTab>();

            string text = File.ReadAllText(path);
            List<JSONTab> tabsList = JsonConvert.DeserializeObject<List<JSONTab>>(text);

            return tabsList;
        }

        public static void SaveToDisk(JSONTab jsonTab)
        {
            List<JSONTab> tabsList = LoadFromDisk();

            if (tabsList.Any(t => t.Symbol == jsonTab.Symbol) == false)
                tabsList.Add(jsonTab);

            string path = GetFilePath();
            string text = JsonConvert.SerializeObject(tabsList);
            File.WriteAllText(path, text);
        }

        public static void RemoveFromDisk(string symbol)
        {
            List<JSONTab> tabsList = LoadFromDisk();

            JSONTab jsonTab = tabsList.Where(t => t.Symbol == symbol).FirstOrDefault();
            tabsList.Remove(jsonTab);

            string path = GetFilePath();
            string text = JsonConvert.SerializeObject(tabsList);
            File.WriteAllText(path, text);
        }

    }
}
