using System.ComponentModel;
using System.Runtime.CompilerServices;
using MMS.Core.Filters;
using MMS.Core.Filters.Grayscale;

namespace MMS.WpfApp.ViewModels;

public sealed class FilterViewModel : INotifyPropertyChanged
{
    public ImageFilterType SelectedType
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
            UpdateOptions();
        }
    }

    public object Options
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    } = new();

    public bool HasOptions
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    }

    public FilterViewModel(ImageFilterType type = ImageFilterType.Unknown)
    {
        SelectedType = type;
        UpdateOptions();
    }

    private void UpdateOptions()
    {
        Options = SelectedType switch
        {
            ImageFilterType.Grayscale => new GrayscaleFilterOptions(),
            _ => new object() 
        };

        HasOptions = SelectedType switch
        {
            ImageFilterType.Grayscale => true,
            _ => false
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
