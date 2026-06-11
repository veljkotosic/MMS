using System.Drawing;
using System.Drawing.Imaging;

namespace MMS.Core.Filters.Pixelate;

public sealed class PixelateFilter : IImageFilter
{
    private const int BytesPerPixel = 4;
    private readonly int _blockSize;

    public PixelateFilter(PixelateFilterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.BlockSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options.BlockSize), "Invalid block size.");
        }

        _blockSize = options.BlockSize;
    }

    public void Execute(Bitmap bitmap)
    {
        if (_blockSize == 1)
        {
            return;
        }

        var width = bitmap.Width;
        var height = bitmap.Height;
        var rect = new Rectangle(0, 0, width, height);
        
        var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

        try
        {
            unsafe
            {
                var bitmapBase = (byte*)bitmapData.Scan0;
                var stride = bitmapData.Stride;
                
                var blockRows = (height + _blockSize - 1) / _blockSize;

                Parallel.For(0, blockRows, blockRow =>
                {
                    var startY = blockRow * _blockSize;
                    var endY = Math.Min(startY + _blockSize, height);

                    for (var startX = 0; startX < width; startX += _blockSize)
                    {
                        var endX = Math.Min(startX + _blockSize, width);
                        
                        PixelateBlock(bitmapBase, stride, startX, endX, startY, endY);
                    }
                });
            }
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }

    private static unsafe void PixelateBlock(
        byte* bitmapBase,
        int stride,
        int startX,
        int endX,
        int startY,
        int endY)
    {
        long blue = 0;
        long green = 0;
        long red = 0;
        long alpha = 0;
        
        var pixelCount = (endX - startX) * (endY - startY);

        for (var y = startY; y < endY; y++)
        {
            var row = bitmapBase + y * stride;

            for (var x = startX; x < endX; x++)
            {
                var pixel = row + x * BytesPerPixel;
                blue += pixel[0];
                green += pixel[1];
                red += pixel[2];
                alpha += pixel[3];
            }
        }

        var averageBlue = (byte)(blue / pixelCount);
        var averageGreen = (byte)(green / pixelCount);
        var averageRed = (byte)(red / pixelCount);
        var averageAlpha = (byte)(alpha / pixelCount);

        for (var y = startY; y < endY; y++)
        {
            var row = bitmapBase + y * stride;

            for (var x = startX; x < endX; x++)
            {
                var pixel = row + x * BytesPerPixel;
                
                pixel[0] = averageBlue;
                pixel[1] = averageGreen;
                pixel[2] = averageRed;
                pixel[3] = averageAlpha;
            }
        }
    }
}
