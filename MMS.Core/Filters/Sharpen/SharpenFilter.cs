using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using MMS.Core.Filters.Utility;

namespace MMS.Core.Filters.Sharpen;

public sealed class SharpenFilter : IImageFilter
{
    private const int BytesPerPixel = 4;
    private const int Scale = 1024;

    private readonly int _strength;
    private readonly int _centerWeight;

    public SharpenFilter(SharpenFilterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Strength < 0 || double.IsNaN(options.Strength) || double.IsInfinity(options.Strength))
        {
            throw new ArgumentOutOfRangeException(nameof(options.Strength), "Invalid sharpen strength.");
        }

        _strength = (int)Math.Round(options.Strength * Scale);
        _centerWeight = Scale + 4 * _strength;
    }

    public void Execute(Bitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;

        if (_strength == 0 || width < 3 || height < 3)
        {
            return;
        }

        var rect = new Rectangle(0, 0, width, height);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

        try
        {
            var sourcePixels = FilterUtility.CopyBitmapBytes(data, width, height);
            Sharpen(sourcePixels, data, width, height);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private void Sharpen(
        byte[] sourcePixels,
        BitmapData destination,
        int width,
        int height)
    {
        var sourceHandle = GCHandle.Alloc(sourcePixels, GCHandleType.Pinned);

        try
        {
            ApplyKernel(sourceHandle.AddrOfPinnedObject(), destination, width, height);
        }
        finally
        {
            sourceHandle.Free();
        }
    }

    private unsafe void ApplyKernel(IntPtr sourceAddress, BitmapData destination, int width, int height)
    {
        var sourceBase = (byte*)sourceAddress;
        var destinationBase = (byte*)destination.Scan0;
        var sourceRowBytes = width * BytesPerPixel;

        Parallel.For(1, height - 1, y =>
        {
            var rowAbove = sourceBase + (y - 1) * sourceRowBytes;
            var currentRow = sourceBase + y * sourceRowBytes;
            var rowBelow = sourceBase + (y + 1) * sourceRowBytes;
            var destinationRow = destinationBase + y * destination.Stride;

            for (var x = 1; x < width - 1; x++)
            {
                var blueOffset = x * BytesPerPixel;

                destinationRow[blueOffset] = SharpenChannel(rowAbove, currentRow, rowBelow, blueOffset);
                destinationRow[blueOffset + 1] = SharpenChannel(rowAbove, currentRow, rowBelow, blueOffset + 1);
                destinationRow[blueOffset + 2] = SharpenChannel(rowAbove, currentRow, rowBelow, blueOffset + 2);
            }
        });
    }

    private unsafe byte SharpenChannel(byte* rowAbove, byte* currentRow, byte* rowBelow, int channelOffset)
    {
        var value =
            currentRow[channelOffset] * _centerWeight
            - currentRow[channelOffset - BytesPerPixel] * _strength
            - currentRow[channelOffset + BytesPerPixel] * _strength
            - rowAbove[channelOffset] * _strength
            - rowBelow[channelOffset] * _strength;

        return FilterUtility.ClampToByte((value + Scale / 2) / Scale);
    }
}
