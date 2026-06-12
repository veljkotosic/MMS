using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using MMS.Application.Abstract.Command;
using MMS.Core.Filters;
using MMS.Core.Utility;

namespace MMS.Application.Commands.ProcessImage;

public sealed class ProcessImageCommandHandler
    : ICommandHandler<ProcessImageCommand, ProcessImageCommandResult>
{
    private const int BytesPerPixel = 4;

    public Task<ProcessImageCommandResult> HandleAsync(
        ProcessImageCommand command,
        CancellationToken cancellationToken)
    {
        Validate(command);

        using var bitmap = CreateBitmap(command.Width, command.Height, command.ImageData);
        var timings = new List<FilterProcessingTimeResult>(command.Filters.Count);
        var batchTimer = Stopwatch.StartNew();

        for (int i = 0; i < command.Filters.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var filter = command.Filters[i];
            var filterTimer = Stopwatch.StartNew();

            filter.Execute(bitmap);
            filterTimer.Stop();

            timings.Add(new FilterProcessingTimeResult(
                i,
                GetFilterName(filter),
                filterTimer.ElapsedMilliseconds));
        }

        batchTimer.Stop();

        return Task.FromResult(new ProcessImageCommandResult(
            ExtractBitmapBytes(bitmap),
            batchTimer.ElapsedMilliseconds,
            timings));
    }

    private static string GetFilterName(IImageFilter filter)
    {
        const string suffix = "Filter";
        var typeName = filter.GetType().Name;

        return typeName.EndsWith(suffix, StringComparison.Ordinal)
            ? typeName[..^suffix.Length]
            : typeName;
    }

    private static void Validate(ProcessImageCommand command)
    {
        if (command.Width <= 0 || command.Height <= 0)
        {
            throw new ArgumentException("Invalid dimensions.");
        }

        if (command.Filters.Count == 0)
        {
            throw new ArgumentException("Invalid filters.");
        }

        var expectedSize = ApplicationLimits.ValidateProcessedImageSize(command.Width, command.Height);
        
        if (command.ImageData.LongLength != expectedSize)
        {
            throw new ArgumentException("Invalid image size.");
        }
    }

    private static Bitmap CreateBitmap(int width, int height, byte[] bytes)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var rect = new Rectangle(0, 0, width, height);
        
        var data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            for (var y = 0; y < height; y++)
            {
                Marshal.Copy(bytes, y * width * BytesPerPixel, data.Scan0 + y * data.Stride, width * BytesPerPixel);
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return bitmap;
    }

    private static byte[] ExtractBitmapBytes(Bitmap bitmap)
    {
        var bytes = new byte[bitmap.Width * bitmap.Height * BytesPerPixel];
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        
        var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        try
        {
            for (var y = 0; y < bitmap.Height; y++)
            {
                Marshal.Copy(data.Scan0 + y * data.Stride, bytes, y * bitmap.Width * BytesPerPixel, bitmap.Width * BytesPerPixel);
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return bytes;
    }
}
