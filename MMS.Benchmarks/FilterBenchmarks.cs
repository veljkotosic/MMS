using System.Drawing;
using System.Drawing.Imaging;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using MMS.Core.FileManager;
using MMS.Core.Filters;
using MMS.Core.Filters.BillAtkinson;
using MMS.Core.Filters.EdgeDetection;
using MMS.Core.Filters.Gamma;
using MMS.Core.Filters.Grayscale;
using MMS.Core.Filters.Halftone;
using MMS.Core.Filters.Pixelate;
using MMS.Core.Filters.Sharpen;
using MMS.Core.Filters.TimeWarp;

namespace MMS.Benchmarks;

[SimpleJob(RunStrategy.Throughput, launchCount: 1, warmupCount: 1, iterationCount: 10, invocationCount: 1)]
public class FilterBenchmarks
{
    private readonly Random _random = new();
    private readonly IImageFilter _grayscale = new GrayscaleFilter(new GrayscaleFilterOptions());
    private readonly IImageFilter _gamma = new GammaFilter(new GammaFilterOptions());
    private readonly IImageFilter _sharpen = new SharpenFilter(new SharpenFilterOptions());
    private readonly IImageFilter _edgeDetect = new EdgeDetectFilter(new EdgeDetectFilterOptions());
    private readonly IImageFilter _timeWarp = new TimeWarpFilter(new TimeWarpFilterOptions());
    private readonly IImageFilter _pixelate = new PixelateFilter(new PixelateFilterOptions());
    private readonly IImageFilter _billAtkinson = new BillAtkinsonFilter(new BillAtkinsonFilterOptions());
    private readonly IImageFilter _halftone = new HalftoneFilter(new HalftoneFilterOptions());

    private List<Bitmap> _images = [];
    private IImageFilter[] _filters = [];
    private IImageFilter[] _batch = [];
    private Bitmap? _workingBitmap;

    [GlobalSetup]
    public void LoadImages()
    {
        var imageDirectory = Path.Combine(FindRootFolder(), "benchmark", "1024x768");
        var imagePaths = Directory
            .EnumerateFiles(imageDirectory)
            .Where(IsSupportedImage)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _images = imagePaths.Select(LoadNormalizedBitmap).ToList();
        _filters =
        [
            _grayscale,
            _gamma,
            _sharpen,
            _edgeDetect,
            _timeWarp,
            _pixelate,
            _billAtkinson,
            _halftone
        ];
    }

    [IterationSetup]
    public void SelectRandomImage()
    {
        var source = _images[_random.Next(_images.Count)];
        
        _workingBitmap = source.Clone(new Rectangle(0, 0, source.Width, source.Height), PixelFormat.Format32bppArgb);
        _batch =
        [
            _filters[_random.Next(_filters.Length)],
            _filters[_random.Next(_filters.Length)],
            _filters[_random.Next(_filters.Length)]
        ];
    }

    [Benchmark]
    public void Grayscale() => _grayscale.Execute(_workingBitmap!);

    [Benchmark]
    public void Gamma() => _gamma.Execute(_workingBitmap!);

    [Benchmark]
    public void Sharpen() => _sharpen.Execute(_workingBitmap!);

    [Benchmark]
    public void EdgeDetect() => _edgeDetect.Execute(_workingBitmap!);

    [Benchmark]
    public void TimeWarp() => _timeWarp.Execute(_workingBitmap!);

    [Benchmark]
    public void Pixelate() => _pixelate.Execute(_workingBitmap!);

    [Benchmark]
    public void BillAtkinson() => _billAtkinson.Execute(_workingBitmap!);

    [Benchmark]
    public void Halftone() => _halftone.Execute(_workingBitmap!);

    [Benchmark]
    public void FilterBatch()
    {
        foreach (var filter in _batch)
        {
            filter.Execute(_workingBitmap!);
        }
    }

    [IterationCleanup]
    public void DisposeWorkingImage()
    {
        _workingBitmap?.Dispose();
        _workingBitmap = null;
    }

    [GlobalCleanup]
    public void DisposeImages()
    {
        foreach (var image in _images)
        {
            image.Dispose();
        }
    }

    private static Bitmap LoadNormalizedBitmap(string path)
    {
        using var loaded = FileManagerFactory.GetFileManager(path).LoadImage(path).GetBitmap();

        return loaded.Clone(new Rectangle(0, 0, loaded.Width, loaded.Height), PixelFormat.Format32bppArgb);
    }

    private static bool IsSupportedImage(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() is ".mms" or ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp";
    }

    private static string FindRootFolder()
    {
        for (var directory = new DirectoryInfo(Environment.CurrentDirectory); directory != null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "MMS.Benchmarks")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException();
    }
}
