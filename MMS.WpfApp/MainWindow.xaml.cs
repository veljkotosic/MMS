using System.Windows.Controls;
using System.Windows.Input;
using MMS.WpfApp.ViewModels;

namespace MMS.WpfApp;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void HistoryList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.SelectedItem is HistoryListingViewModel listing)
        {
            viewModel.RestoreHistoryListing(listing);
        }
    }
}
