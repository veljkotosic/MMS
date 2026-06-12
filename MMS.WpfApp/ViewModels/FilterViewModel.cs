using System.ComponentModel;
using System.Runtime.CompilerServices;
using MMS.Core.Filters;
using MMS.Core.Filters.BillAtkinson;
using MMS.Core.Filters.EdgeDetection;
using MMS.Core.Filters.Gamma;
using MMS.Core.Filters.Grayscale;
using MMS.Core.Filters.Halftone;
using MMS.Core.Filters.Pixelate;
using MMS.Core.Filters.Sharpen;
using MMS.Core.Filters.TimeWarp;

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

    public string? ValidationMessage => SelectedType == ImageFilterType.Unknown ? "Select a filter type." : (Options as IFilterOptions)?.Validate();

    public bool IsValid => ValidationMessage == null;

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
            ImageFilterType.Gamma => new GammaFilterOptions(),
            ImageFilterType.Sharpen => new SharpenFilterOptions(),
            ImageFilterType.EdgeDetect => new EdgeDetectFilterOptions(),
            ImageFilterType.TimeWarp => new TimeWarpFilterOptions(),
            ImageFilterType.Pixelate => new PixelateFilterOptions(),
            ImageFilterType.BillAtkinson => new BillAtkinsonFilterOptions(),
            ImageFilterType.Halftone => new HalftoneFilterOptions(),
            _ => new object() 
        };

        HasOptions = SelectedType switch
        {
            ImageFilterType.Grayscale => true,
            ImageFilterType.Gamma => true,
            ImageFilterType.Sharpen => true,
            ImageFilterType.EdgeDetect => true,
            ImageFilterType.TimeWarp => true,
            ImageFilterType.Pixelate => true,
            ImageFilterType.BillAtkinson => true,
            ImageFilterType.Halftone => true,
            _ => false
        };

        RefreshValidation();
    }

    public void RefreshValidation()
    {
        OnPropertyChanged(nameof(ValidationMessage));
        OnPropertyChanged(nameof(IsValid));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
