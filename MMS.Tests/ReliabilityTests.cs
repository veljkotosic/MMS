using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Runtime.InteropServices;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MMS.Contracts;
using MMS.Core.FileManager;
using MMS.Core.Utility;
using MMS.GrpcService.Services;
using MMS.Infrastructure.DependencyInjection;

namespace MMS.Tests;

[TestFixture]
[NonParallelizable]
public sealed class ReliabilityTests
{
    private const int ConcurrentRequestCount = 10;
    private const int FiltersPerRequest = 3;
    private const int ChunkSize = 64 * 1024;
    
    private static readonly TimeSpan MaximumLatency = TimeSpan.FromSeconds(4.25);

    private WebApplication _server = null!;
    private GrpcChannel _channel = null!;
    private ImageProcessor.ImageProcessorClient _client = null!;
    private byte[] _imageBytes = null!;
    private int _imageWidth;
    private int _imageHeight;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        LoadTestImage();

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0, endpoint => endpoint.Protocols = HttpProtocols.Http2);
        });
        builder.Services.AddMms();
        builder.Services.AddGrpc(options =>
        {
            options.MaxReceiveMessageSize = checked((int)ApplicationLimits.MaxDecodedImageBytes);
            options.MaxSendMessageSize = checked((int)ApplicationLimits.MaxDecodedImageBytes);
        });

        _server = builder.Build();
        _server.MapGrpcService<ImageProcessorService>();
        await _server.StartAsync();

        var addresses = _server.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses;
        
        var address = addresses.Single();

        _channel = GrpcChannel.ForAddress(address);
        _client = new ImageProcessor.ImageProcessorClient(_channel);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _channel?.Dispose();

        await _server.StopAsync();
        await _server.DisposeAsync();
    }

    [Test]
    public async Task TenConcurrentRequests_ShouldCompleteWithinMaximumLatency()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        
        var sync = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = Enumerable
            .Range(0, ConcurrentRequestCount)
            .Select(_ => CreateRequestTask(sync.Task, timeout.Token))
            .ToArray();

        var stopwatch = Stopwatch.StartNew();
        sync.SetResult();
        var results = await Task.WhenAll(requests);
        stopwatch.Stop();

        await TestContext.Progress.WriteLineAsync($"Requests completed in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");

        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Length.EqualTo(ConcurrentRequestCount));
            Assert.That(results, Has.All.Matches<ProcessImageResult>(
                result => result.FilterTimes.Count == FiltersPerRequest));
            Assert.That(stopwatch.Elapsed, Is.LessThanOrEqualTo(MaximumLatency));
        });
    }

    private async Task<ProcessImageResult> CreateRequestTask(Task sync, CancellationToken cancellationToken)
    {
        var filters = Enumerable.Range(0, FiltersPerRequest).Select(_ => CreateRandomFilter()).ToArray();
        await sync;

        using var call = _client.ProcessImage(cancellationToken: cancellationToken);
        
        var header = new ImageHeader
        {
            Width = _imageWidth,
            Height = _imageHeight
        };
        
        header.Filters.Add(filters);

        await call.RequestStream.WriteAsync(new ProcessImageRequest { Header = header }, cancellationToken);

        for (var offset = 0; offset < _imageBytes.Length; offset += ChunkSize)
        {
            var length = Math.Min(ChunkSize, _imageBytes.Length - offset);
            await call.RequestStream.WriteAsync(new ProcessImageRequest
            {
                Chunk = new ImageChunk
                {
                    Data = ByteString.CopyFrom(_imageBytes, offset, length)
                }
            }, cancellationToken);
        }

        await call.RequestStream.CompleteAsync();

        ProcessImageResult? result = null;
        var receivedBytes = 0L;

        await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            if (response.PayloadCase == ProcessImageResponse.PayloadOneofCase.Result)
            {
                result = response.Result;
            }
            else if (response.PayloadCase == ProcessImageResponse.PayloadOneofCase.Chunk)
            {
                receivedBytes += response.Chunk.Data.Length;
            }
        }

        Assert.That(receivedBytes, Is.EqualTo(_imageBytes.LongLength));
        return result ?? throw new InvalidDataException("Invalid result.");
    }

    private void LoadTestImage()
    {
        var imagePath = Path.Combine(FindRootFolder(), "test", "testImage.jpg");
        
        using var loaded = new StandardFileManager().LoadImage(imagePath).GetBitmap();
        using var bitmap = loaded.Clone(new Rectangle(0, 0, loaded.Width, loaded.Height), PixelFormat.Format32bppArgb);

        _imageWidth = bitmap.Width;
        _imageHeight = bitmap.Height;
        _imageBytes = ExtractBitmapBytes(bitmap);
    }

    private static ImageFilter CreateRandomFilter()
    {
        return Random.Shared.Next(8) switch
        {
            0 => new ImageFilter { Grayscale = new GrayscaleFilter { RMul = 306, GMul = 601, BMul = 117 } },
            1 => new ImageFilter { Gamma = new GammaFilter { Gamma = 1.2 } },
            2 => new ImageFilter { Sharpen = new SharpenFilter { Strength = 1 } },
            3 => new ImageFilter { EdgeDetect = new EdgeDetectFilter { Direction = EdgeDetectDirection.Both } },
            4 => new ImageFilter { TimeWarp = new TimeWarpFilter { Strength = 1, U = 0.5, V = 0.5, Radius = 0.5 } },
            5 => new ImageFilter { Pixelate = new PixelateFilter { BlockSize = 10 } },
            6 => new ImageFilter { BillAtkinson = new BillAtkinsonFilter { Threshold = 128 } },
            _ => new ImageFilter { Halftone = new HalftoneFilter { CellSize = 8 } }
        };
    }

    private static byte[] ExtractBitmapBytes(Bitmap bitmap)
    {
        var bytes = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        try
        {
            for (var y = 0; y < bitmap.Height; y++)
            {
                Marshal.Copy(data.Scan0 + y * data.Stride, bytes, y * bitmap.Width * 4, bitmap.Width * 4);
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return bytes;
    }

    private static string FindRootFolder()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             directory != null;
             directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "MMS.Tests")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException();
    }
}
