using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace CryptoTrader.UserControls
{
    public partial class NumericUpDown : UserControl
    {
        public delegate void OnValueChangedDelegate();
        public event OnValueChangedDelegate OnValueChangedEvent = null;

        public NumericUpDown()
        {
            InitializeComponent();
        }

        private double value = 0;
        public double Value
        {
            get { return value; }
            set
            {
                this.value = value;
                if (this.value < 0.1) this.value = 0.1;
                RefreshUI();
                if (OnValueChangedEvent != null) OnValueChangedEvent();
            }
        }

        private double interval = 0.25;
        public double Interval 
        { 
            get { return interval; } 
            set { interval = value; RefreshUI(); } 
        }

        private string symbol = "%";
        public string Symbol 
        { 
            get { return symbol; } 
            set { symbol = value; RefreshUI(); } 
        }

        private void RefreshUI()
        {
            textBox.Text = string.Format("{0} {1}", this.value, Symbol);
        }

        private void textBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Value = Value + Math.Sign(e.Delta) * Interval;
        }
    }
}
