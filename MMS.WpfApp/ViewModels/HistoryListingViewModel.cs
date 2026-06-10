using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace MMS.WpfApp.ViewModels;

public sealed class HistoryListingViewModel : INotifyPropertyChanged, IDisposable
{
    public string Title { get; }
    public Bitmap SnapshotBitmap { get; }
    public ObservableCollection<string> Details { get; }
    public bool IsBatch { get; }

    public bool IsCurrent
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            
            OnPropertyChanged();
        }
    }

    public bool IsFuture
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            
            OnPropertyChanged();
        }
    }

    public HistoryListingViewModel(
        string title,
        Bitmap snapshotBitmap,
        IEnumerable<string> details,
        bool isBatch)
    {
        Title = title;
        SnapshotBitmap = snapshotBitmap;
        Details = new ObservableCollection<string>(details);
        IsBatch = isBatch;
    }

    public void Dispose()
    {
        SnapshotBitmap.Dispose();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
