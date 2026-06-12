using System.Windows;
using MMS.WpfApp.ViewModels;

namespace MMS.WpfApp.Controls;

public partial class FilterStagingView
{
    public FilterStagingView()
    {
        InitializeComponent();
    }

    private void PropertyGrid_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: FilterViewModel filter }
            && DataContext is FilterStagingViewModel staging)
        {
            staging.NotifyFilterOptionsChanged(filter);
        }
    }
}
