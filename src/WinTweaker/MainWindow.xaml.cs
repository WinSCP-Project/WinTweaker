using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using WinTweaker.ViewModels;

namespace WinTweaker
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            try
            {
                int round = 2;
                DwmSetWindowAttribute(hwnd, 33, ref round, sizeof(int));
            }
            catch
            {
            }

            try
            {
                var margins = new Margins { Left = 1, Right = 1, Top = 1, Bottom = 1 };
                DwmExtendFrameIntoClientArea(hwnd, ref margins);
            }
            catch
            {
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null && vm.IsApplying)
            {
                e.Cancel = true;
                return;
            }
            if (vm != null)
                vm.SaveIfDirty();
            base.OnClosing(e);
        }

        void Menu_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null || button.ContextMenu == null)
                return;
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
        }

        void Log_TextChanged(object sender, TextChangedEventArgs e)
        {
            var box = sender as TextBox;
            if (box != null)
                box.ScrollToEnd();
        }

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        [DllImport("dwmapi.dll")]
        static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

        [StructLayout(LayoutKind.Sequential)]
        struct Margins
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }
    }
}
