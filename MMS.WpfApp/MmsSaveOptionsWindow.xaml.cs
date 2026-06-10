using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using MMS.Core.FileFormat.Colorspace;
using MMS.Core.FileFormat.Compression;

namespace MMS.WpfApp;

public partial class MmsSaveOptionsWindow : INotifyPropertyChanged
{
    public MmsColorspace SelectedColorspace { get; private set; }
    public MmsCompression SelectedCompression { get; private set; }

    public bool IsRgb
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = true;

    public bool IsYCbCr
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsLinear
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsNone
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = true;

    public bool IsShannonFano
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsMpeg1
    {
        get;
        set
        {
            field = value;

            if (field)
            {
                IsYCbCr = true;
                IsRgb = false;
                IsLinear = false;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotMpeg1));
        }
    }

    public bool IsNotMpeg1 => !IsMpeg1;

    public MmsSaveOptionsWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (IsRgb)
        {
            SelectedColorspace = MmsColorspace.Rgb;
        }
        else if (IsYCbCr)
        {
            SelectedColorspace = MmsColorspace.YCbCr;
        }
        else if (IsLinear)
        {
            SelectedColorspace = MmsColorspace.Linear;
        }

        if (IsNone)
        {
            SelectedCompression = MmsCompression.None;
        }
        else if (IsShannonFano)
        {
            SelectedCompression = MmsCompression.ShannonFano;
        }
        else if (IsMpeg1)
        {
            SelectedCompression = MmsCompression.Mpeg1;
        }

        DialogResult = true;
        Close();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}