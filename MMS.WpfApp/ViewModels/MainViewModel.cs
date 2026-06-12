using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Grpc.Core;
using MMS.Contracts;
using MMS.Core.Filters.Grayscale;
using MMS.Core.Filters.BillAtkinson;
using MMS.Core.Filters.Halftone;
using MMS.Core.FileFormat;
using MMS.Core.FileManager;
using MMS.Core.Filters;
using MMS.Core.Filters.Gamma;
using MMS.Core.Filters.Pixelate;
using MMS.Core.ImageResource;
using MMS.Core.Filters.Sharpen;
using MMS.Core.Filters.TimeWarp;
using MMS.WpfApp.Controls;
using MMS.WpfApp.Services;

namespace MMS.WpfApp.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private const int MaxHistoryEntries = 5;

    private Bitmap? _currentBitmap;
    private int _currentHistoryIndex = -1;
    private readonly RemoteImageProcessorClient _imageProcessorClient = new();

    public BitmapSource? MainImage
    {
        get;
        private set
        {
            if (Equals(field, value))
            {
                return;
            }

            field = value;

            OnPropertyChanged(nameof(MainImage));
        }
    }

    public bool IsApplyingFilters
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            
            OnPropertyChanged(nameof(IsApplyingFilters));
            RefreshCommandStates();
        }
    }

    public ObservableCollection<HistoryListingViewModel> HistoryListings { get; } = [];
    public ObservableCollection<ClientLogEntry> ClientLogs { get; } = [];

    public MmsCommand LoadCommand { get; }
    public MmsCommand SaveAsCommand { get; }
    public MmsCommand ApplyCommand { get; }
    public MmsCommand UndoCommand { get; }
    public MmsCommand RedoCommand { get; }
    public MmsCommand CenterImageCommand { get; }
    public MmsCommand CloseCommand { get; }

    public FilterStagingViewModel FilterStaging { get; }

    public MainViewModel()
    {
        FilterStaging = new FilterStagingViewModel();

        LoadCommand = new MmsCommand(OnLoadImage, _ => !IsApplyingFilters);
        SaveAsCommand = new MmsCommand(OnSaveImage, _ => _currentBitmap != null && !IsApplyingFilters);
        ApplyCommand = new MmsCommand(OnApplyFilters, _ => CanApplyFilters());
        UndoCommand = new MmsCommand(OnUndo, _ => CanUndo());
        RedoCommand = new MmsCommand(OnRedo, _ => CanRedo());
        CenterImageCommand = new MmsCommand(OnCenterImage);
        CloseCommand = new MmsCommand(OnCloseImage, _ => _currentBitmap != null && !IsApplyingFilters);

        FilterStaging.SetApplyCommand(ApplyCommand);
        RefreshCommandStates();
    }

    private void OnLoadImage(object? obj)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "MMS Files (*.mms)|*.mms|Image Files (*.png;*.jpg;*.bmp;*.gif)|*.png;*.jpg;*.bmp;*.gif|All Files (*.*)|*.*",
        };

        if (openFileDialog.ShowDialog() != true)
        {
            return;
        }

        var filePath = openFileDialog.FileName;
        Bitmap? normalizedBitmap = null;

        try
        {
            var fileManager = FileManagerFactory.GetFileManager(filePath);
            using var loadedBitmap = fileManager.LoadImage(filePath).GetBitmap();
            normalizedBitmap = NormalizeBitmap(loadedBitmap);
            var mainImage = CreateBitmapSource(normalizedBitmap);

            DisposeCurrentBitmap();
            ClearHistory();

            _currentBitmap = normalizedBitmap;
            normalizedBitmap = null;
            MainImage = mainImage;

            AddHistoryListing("Image Loaded From Disk", [], false);
            AddLog($"Loaded image from disk: {Path.GetFileName(filePath)}");
            RefreshCommandStates();
        }
        catch (Exception ex)
        {
            normalizedBitmap?.Dispose();
            ShowOperationError(
                "Load Error",
                "The image could not be loaded. Verify that it is a valid supported image under 25 MB.",
                ex);
        }
    }

    private void OnSaveImage(object? obj)
    {
        var currentBitmap = _currentBitmap;

        if (currentBitmap == null)
        {
            return;
        }

        var saveFileDialog = new SaveFileDialog
        {
            Filter = "MMS Files (*.mms)|*.mms|Image Files (*.png;*.jpg;*.bmp;*.gif)|*.png;*.jpg;*.bmp;*.gif|All Files (*.*)|*.*",
            DefaultExt = "mms"
        };

        if (saveFileDialog.ShowDialog() != true)
        {
            return;
        }

        var filePath = saveFileDialog.FileName;
        var extension = Path.GetExtension(filePath).ToLower();

        try
        {
            if (extension == ".mms")
            {
                var optionsWindow = new MmsSaveOptionsWindow
                {
                    Owner = Application.Current.MainWindow
                };

                if (optionsWindow.ShowDialog() != true)
                {
                    return;
                }

                var mmsFile = new MmsFile
                {
                    Header = new MmsHeader
                    {
                        Colorspace = optionsWindow.SelectedColorspace,
                        Compression = optionsWindow.SelectedCompression
                    }
                };

                mmsFile.SetBitmap(currentBitmap);

                var fileManager = new MmsFileManager();
                fileManager.SaveImage(filePath, mmsFile);
                AddLog($"Saved MMS file: {Path.GetFileName(filePath)}");
            }
            else
            {
                var fileManager = new StandardFileManager();
                var resource = new StandardImageResource(currentBitmap);
                fileManager.SaveImage(filePath, resource);
                AddLog($"Saved image file: {Path.GetFileName(filePath)}");
            }
        }
        catch (Exception ex)
        {
            ShowOperationError(
                "Save Error",
                "The image could not be saved. Verify the destination and selected format.",
                ex);
        }
    }

    private async void OnApplyFilters(object? obj)
    {
        try
        {
            if (!CanApplyFilters())
            {
                return;
            }

            var stagedFilters = FilterStaging.Filters.ToList();
            var filterRequests = stagedFilters.Select(CreateFilterRequest).ToList();
            var executedFilters = stagedFilters.Select(filter => filter.SelectedType.ToString()).ToList();

            IsApplyingFilters = true;
            AddLog($"Sending {executedFilters.Count} filter(s) for processing.");

            try
            {
                var response = await _imageProcessorClient.ProcessAsync(_currentBitmap!, filterRequests);
                BitmapSource mainImage;

                try
                {
                    mainImage = CreateBitmapSource(response.Bitmap);
                }
                catch
                {
                    response.Bitmap.Dispose();
                    throw;
                }

                RemoveFutureHistory();
                DisposeCurrentBitmap();
            
                _currentBitmap = response.Bitmap;
                MainImage = mainImage;

                UpdateProcessingTimings(response.Timing);
                AddHistoryListing(executedFilters.Count == 1 ? executedFilters[0] : "Filter Batch", executedFilters, executedFilters.Count > 1);

                FilterStaging.ClearFilters();
            }
            catch (Exception ex)
            {
                AddLog($"Filter processing failed: {ex.Message}", ClientLogLevel.Error);
                MessageBox.Show(
                    ex is RpcException { StatusCode: StatusCode.InvalidArgument }
                        ? "The staged filters or image are invalid. Review the filter messages and try again."
                        : "The image could not be processed. Verify that the processing service is running and try again.",
                    "Processing Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsApplyingFilters = false;
            }
        }
        catch (Exception e)
        {
            AddLog($"Failed to prepare filters: {e.Message}", ClientLogLevel.Error);
            MessageBox.Show(
                "The filters could not be prepared. Review the staged filter options.",
                "Filter Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public void RestoreHistoryListing(HistoryListingViewModel listing)
    {
        var index = HistoryListings.IndexOf(listing);
        RestoreHistoryAt(index);
    }

    private void RestoreHistoryAt(int index)
    {
        if (index < 0)
        {
            return;
        }

        if (_currentHistoryIndex == index)
        {
            return;
        }

        var listing = HistoryListings[index];
        var restoredBitmap = new Bitmap(listing.SnapshotBitmap);

        DisposeCurrentBitmap();
        
        _currentBitmap = restoredBitmap;
        
        MainImage = CreateBitmapSource(_currentBitmap);

        _currentHistoryIndex = index;

        UpdateHistoryState();
        RefreshCommandStates();
    }

    private void OnUndo(object? obj)
    {
        if (!CanUndo())
        {
            return;
        }

        RestoreHistoryAt(_currentHistoryIndex - 1);
    }

    private void OnRedo(object? obj)
    {
        if (!CanRedo())
        {
            return;
        }

        RestoreHistoryAt(_currentHistoryIndex + 1);
    }

    private static void OnCenterImage(object? parameter)
    {
        if (parameter is ZoomBorder zoomBorder)
        {
            zoomBorder.Reset();
        }
    }

    private void OnCloseImage(object? obj)
    {
        DisposeCurrentBitmap();
        ClearHistory();
        
        FilterStaging.ClearFilters();
        MainImage = null;
        
        AddLog("Closed image.");
        
        RefreshCommandStates();
    }

    private bool CanApplyFilters()
    {
        return _currentBitmap != null
            && !IsApplyingFilters
            && FilterStaging.Filters.Count > 0
            && FilterStaging.Filters.All(filter => filter.IsValid);
    }

    private bool CanUndo()
    {
        return !IsApplyingFilters && _currentHistoryIndex > 0;
    }

    private bool CanRedo()
    {
        return !IsApplyingFilters
            && _currentHistoryIndex >= 0
            && _currentHistoryIndex < HistoryListings.Count - 1;
    }

    private void AddHistoryListing(string title, IEnumerable<string> details, bool isBatch)
    {
        var listing = CreateHistoryListing(title, details, isBatch);

        HistoryListings.Add(listing);
        _currentHistoryIndex = HistoryListings.Count - 1;
        
        TrimHistory();
        UpdateHistoryState();
    }

    private HistoryListingViewModel CreateHistoryListing(string title, IEnumerable<string> details, bool isBatch)
    {
        var snapshotBitmap = new Bitmap(_currentBitmap!);

        return new HistoryListingViewModel(title, snapshotBitmap, details, isBatch);
    }

    private void RemoveFutureHistory()
    {
        if (_currentHistoryIndex < 0)
        {
            return;
        }

        for (var i = HistoryListings.Count - 1; i > _currentHistoryIndex; i--)
        {
            RemoveHistoryAt(i);
        }
    }

    private void TrimHistory()
    {
        while (HistoryListings.Count > MaxHistoryEntries)
        {
            RemoveHistoryAt(0);

            if (_currentHistoryIndex > 0)
            {
                _currentHistoryIndex--;
            }
        }
    }

    private void RemoveHistoryAt(int index)
    {
        if (index < 0 || index >= HistoryListings.Count)
        {
            return;
        }

        var listing = HistoryListings[index];
        HistoryListings.RemoveAt(index);
        listing.Dispose();

        if (_currentHistoryIndex > index)
        {
            _currentHistoryIndex--;
        }
    }

    private void ClearHistory()
    {
        foreach (var listing in HistoryListings)
        {
            listing.Dispose();
        }

        HistoryListings.Clear();
        
        _currentHistoryIndex = -1;
    }

    private void UpdateHistoryState()
    {
        for (var i = 0; i < HistoryListings.Count; i++)
        {
            HistoryListings[i].IsCurrent = i == _currentHistoryIndex;
            HistoryListings[i].IsFuture = _currentHistoryIndex >= 0 && i > _currentHistoryIndex;
        }
    }

    private void DisposeCurrentBitmap()
    {
        _currentBitmap?.Dispose();
        _currentBitmap = null;
    }

    private void RefreshCommandStates()
    {
        LoadCommand.RaiseCanExecuteChanged();
        SaveAsCommand.RaiseCanExecuteChanged();
        ApplyCommand.RaiseCanExecuteChanged();
        UndoCommand.RaiseCanExecuteChanged();
        RedoCommand.RaiseCanExecuteChanged();
        CloseCommand.RaiseCanExecuteChanged();
    }

    private static ImageFilter CreateFilterRequest(FilterViewModel filterVm)
    {
        return filterVm.SelectedType switch
        {
            ImageFilterType.Grayscale => new ImageFilter
            {
                Grayscale = new Contracts.GrayscaleFilter
                {
                    RMul = ((GrayscaleFilterOptions)filterVm.Options).RMul,
                    GMul = ((GrayscaleFilterOptions)filterVm.Options).GMul,
                    BMul = ((GrayscaleFilterOptions)filterVm.Options).BMul
                }
            },
            ImageFilterType.Gamma => new ImageFilter
            {
                Gamma = new Contracts.GammaFilter
                {
                    Gamma = ((GammaFilterOptions)filterVm.Options).Gamma
                }
            },
            ImageFilterType.Sharpen => new ImageFilter
            {
                Sharpen = new Contracts.SharpenFilter
                {
                    Strength = ((SharpenFilterOptions)filterVm.Options).Strength
                }
            },
            ImageFilterType.EdgeDetect => new ImageFilter
            {
                EdgeDetect = new EdgeDetectFilter
                {
                    Direction = ((Core.Filters.EdgeDetection.EdgeDetectFilterOptions)filterVm.Options).Direction switch
                    {
                        Core.Filters.EdgeDetection.EdgeDetectDirection.Horizontal =>
                            EdgeDetectDirection.Horizontal,
                        Core.Filters.EdgeDetection.EdgeDetectDirection.Vertical =>
                            EdgeDetectDirection.Vertical,
                        Core.Filters.EdgeDetection.EdgeDetectDirection.Both =>
                            EdgeDetectDirection.Both,
                        _ => throw new InvalidOperationException("Unsupported edge detection direction.")
                    }
                }
            },
            ImageFilterType.TimeWarp => new ImageFilter
            {
                TimeWarp = new Contracts.TimeWarpFilter
                {
                    Strength = ((TimeWarpFilterOptions)filterVm.Options).Strength,
                    U = ((TimeWarpFilterOptions)filterVm.Options).U,
                    V = ((TimeWarpFilterOptions)filterVm.Options).V,
                    Radius = ((TimeWarpFilterOptions)filterVm.Options).Radius
                }
            },
            ImageFilterType.Pixelate => new ImageFilter
            {
                Pixelate = new Contracts.PixelateFilter
                {
                    BlockSize = ((PixelateFilterOptions)filterVm.Options).BlockSize
                }
            },
            ImageFilterType.BillAtkinson => new ImageFilter
            {
                BillAtkinson = new Contracts.BillAtkinsonFilter
                {
                    Threshold = ((BillAtkinsonFilterOptions)filterVm.Options).Threshold
                }
            },
            ImageFilterType.Halftone => new ImageFilter
            {
                Halftone = new Contracts.HalftoneFilter
                {
                    CellSize = ((HalftoneFilterOptions)filterVm.Options).CellSize
                }
            },
            _ => throw new InvalidOperationException($"Unsupported filter type: {filterVm.SelectedType}.")
        };
    }

    private void UpdateProcessingTimings(ProcessImageResult timing)
    {
        AddLog($"Filter batch processed in {timing.TotalProcessingTimeMs} ms.");

        foreach (var filterTime in timing.FilterTimes.OrderBy(item => item.FilterIndex))
        {
            AddLog($"Filter {filterTime.FilterIndex + 1} ({filterTime.FilterName}) completed in {filterTime.ProcessingTimeMs} ms.");
        }
    }

    private void AddLog(string message, ClientLogLevel level = ClientLogLevel.Info)
    {
        ClientLogs.Add(new ClientLogEntry(DateTime.Now, level, message));
    }

    private void ShowOperationError(string title, string userMessage, Exception exception)
    {
        AddLog($"{title}: {exception.Message}", ClientLogLevel.Error);
        MessageBox.Show(userMessage, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private static Bitmap NormalizeBitmap(Bitmap bitmap)
    {
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        return bitmap.Clone(rect, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
    }

    private static BitmapSource CreateBitmapSource(Bitmap bitmap)
    {
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        try
        {
            var source = BitmapSource.Create(
                bitmapData.Width,
                bitmapData.Height,
                bitmap.HorizontalResolution,
                bitmap.VerticalResolution,
                PixelFormats.Bgra32,
                null,
                bitmapData.Scan0,
                bitmapData.Stride * bitmapData.Height,
                bitmapData.Stride);

            source.Freeze();
            
            return source;
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public enum ClientLogLevel
{
    Info,
    Error
}

public sealed record ClientLogEntry(DateTime Timestamp, ClientLogLevel Level, string Message)
{
    public string DisplayMessage => $"[{Level}] {Message}";
}

public class MmsCommand(Action<object?> execute, Predicate<object?>? canExecute = null) : ICommand
{
    public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => execute(parameter);

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
