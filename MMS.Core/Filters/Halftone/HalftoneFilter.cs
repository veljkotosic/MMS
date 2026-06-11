using System.Drawing;
using System.Drawing.Imaging;

namespace MMS.Core.Filters.Halftone;

public sealed class HalftoneFilter : IImageFilter
{
    private const int BytesPerPixel = 4;
    private readonly int _cellSize;

    public HalftoneFilter(HalftoneFilterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.CellSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options.CellSize), "Invalid cell size.");
        }

        _cellSize = options.CellSize;
    }

    public void Execute(Bitmap bitmap)
    {
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
                var cellRows = (height + _cellSize - 1) / _cellSize;

                Parallel.For(0, cellRows, cellRow =>
                {
                    var startY = cellRow * _cellSize;
                    var endY = Math.Min(startY + _cellSize, height);

                    for (var startX = 0; startX < width; startX += _cellSize)
                    {
                        var endX = Math.Min(startX + _cellSize, width);
                        DrawCell(bitmapBase, stride, startX, endX, startY, endY);
                    }
                });
            }
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }

    private static unsafe void DrawCell(
        byte* bitmapBase,
        int stride,
        int startX,
        int endX,
        int startY,
        int endY)
    {
        long brightnessSum = 0;
        var pixelCount = (endX - startX) * (endY - startY);

        for (var y = startY; y < endY; y++)
        {
            var row = bitmapBase + y * stride;

            for (var x = startX; x < endX; x++)
            {
                var pixel = row + x * BytesPerPixel;
                brightnessSum += (pixel[2] * 306 + pixel[1] * 601 + pixel[0] * 117) >> 10;
            }
        }

        var averageBrightness = brightnessSum / (double)pixelCount;
        var darkness = 1.0 - averageBrightness / 255.0;
        var maxRadius = Math.Min(endX - startX, endY - startY) * 0.5;
        var radius = maxRadius * Math.Sqrt(darkness);
        var radiusSquared = radius * radius;
        
        var centerX = (startX + endX) * 0.5;
        var centerY = (startY + endY) * 0.5;

        for (var y = startY; y < endY; y++)
        {
            var row = bitmapBase + y * stride;
            var deltaY = y + 0.5 - centerY;

            for (var x = startX; x < endX; x++)
            {
                var pixel = row + x * BytesPerPixel; 
                var deltaX = x + 0.5 - centerX;
                
                var value = deltaX * deltaX + deltaY * deltaY <= radiusSquared ? (byte)0 : (byte)255;

                pixel[0] = value;
                pixel[1] = value;
                pixel[2] = value;
            }
        }
    }
}
