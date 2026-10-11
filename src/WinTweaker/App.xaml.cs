using System;
using System.Windows;
using System.Windows.Controls;

namespace WinTweaker
{
    public partial class App : Application
    {
        public App()
        {
            try
            {
                ToolTipService.ShowDurationProperty.OverrideMetadata(
                    typeof(FrameworkElement),
                    new FrameworkPropertyMetadata(30000));
                ToolTipService.InitialShowDelayProperty.OverrideMetadata(
                    typeof(FrameworkElement),
                    new FrameworkPropertyMetadata(350));
            }
            catch (ArgumentException)
            {
            }
        }
    }
}
