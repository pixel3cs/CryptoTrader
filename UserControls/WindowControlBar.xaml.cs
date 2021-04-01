using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CryptoTrader.UserControls
{
    public partial class WindowControlBar : UserControl
    {
        private bool maximized = false;
        private double normalLeft, normalTop, normalWidth, normalHeight;

        public WindowControlBar()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this) == false)
            {
                Application.Current.MainWindow.SizeChanged += MainWindow_SizeChanged;
                Application.Current.MainWindow.LocationChanged += MainWindow_LocationChanged;
                this.Loaded += WindowControlBar_Loaded;
            }
        }

        private void WindowControlBar_Loaded(object sender, RoutedEventArgs e)
        {
            MaximizeRestoreWindow(null, null);
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            if (maximized == false)
            {
                normalLeft = Application.Current.MainWindow.Left;
                normalTop = Application.Current.MainWindow.Top;
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (maximized == false)
            {
                normalWidth = Application.Current.MainWindow.Width;
                normalHeight = Application.Current.MainWindow.Height;
            }
        }

        private void CloseWindow(object sender, MouseButtonEventArgs e)
        {
            Application.Current.MainWindow.Close();
        }

        private void MaximizeRestoreWindow(object sender, MouseButtonEventArgs e)
        {
            if (maximized)
            {                
                Application.Current.MainWindow.Left = normalLeft;
                Application.Current.MainWindow.Top = normalTop;
                Application.Current.MainWindow.Width = normalWidth;
                Application.Current.MainWindow.Height = normalHeight;
                maximized = false;
            }
            else
            {
                maximized = true;
                Application.Current.MainWindow.Left = 0;
                Application.Current.MainWindow.Top = 0;
                Application.Current.MainWindow.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
                Application.Current.MainWindow.Height = System.Windows.SystemParameters.WorkArea.Height;
            }
        }

        private void MinimizeWindow(object sender, MouseButtonEventArgs e)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                Application.Current.MainWindow.DragMove();
        }
    }
}
