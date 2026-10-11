using System.Windows;
using System.Windows.Controls;
using WinTweaker.ViewModels;

namespace WinTweaker.Views
{
    public sealed class TweakTemplateSelector : DataTemplateSelector
    {
        public DataTemplate CheckTemplate { get; set; }
        public DataTemplate ComboTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var tweak = item as TweakVm;
            if (tweak != null && tweak.IsCombo)
                return ComboTemplate;
            return CheckTemplate;
        }
    }
}
