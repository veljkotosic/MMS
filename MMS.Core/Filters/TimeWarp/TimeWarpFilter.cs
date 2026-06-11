using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using MMS.Core.Filters.Utility;

namespace MMS.Core.Filters.TimeWarp;

public sealed class TimeWarpFilter : IImageFilter
{
    private const int BytesPerPixel = 4;
    
    private readonly double _strength;
    private readonly double _radius;
    private readonly double _u;
    private readonly double _v;

    public TimeWarpFilter(TimeWarpFilterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (double.IsNaN(options.Radius) || double.IsNaN(options.U) || double.IsNaN(options.V) || double.IsNaN(options.Strength) || double.IsInfinity(options.Strength))
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Invalid option parameters.");
        }

        _strength = options.Strength;
        _radius = options.Radius;
        _u = options.U;
        _v = options.V;
    }

    public void Execute(Bitmap bitmap)
    {
        if (_strength == 0 || _radius == 0 || bitmap.Width < 2 || bitmap.Height < 2)
        {
            return;
        }

        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

        try
        {
            var sourcePixels = FilterUtility.CopyBitmapBytes(bitmapData, bitmap.Width, bitmap.Height);
            Warp(sourcePixels, bitmapData, bitmap.Width, bitmap.Height);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }

    private void Warp(byte[] sourcePixels, BitmapData destination, int width, int height)
    {
        var sourceHandle = GCHandle.Alloc(sourcePixels, GCHandleType.Pinned);

        try
        {
            Warp(sourceHandle.AddrOfPinnedObject(), destination, width, height);
        }
        finally
        {
            sourceHandle.Free();
        }
    }

    private unsafe void Warp(IntPtr sourceAddress, BitmapData destination, int width, int height)
    {
        var sourceBase = (byte*)sourceAddress;
        var destinationBase = (byte*)destination.Scan0;
        
        var sourceStride = width * BytesPerPixel;
        
        var centerX = _u * (width - 1);
        var centerY = _v * (height - 1);
        
        var farthestX = Math.Max(centerX, width - 1 - centerX);
        var farthestY = Math.Max(centerY, height - 1 - centerY);
        
        var radius = Math.Sqrt(farthestX * farthestX + farthestY * farthestY) * _radius;

        Parallel.For(0, height, y =>
        {
            var destinationRow = destinationBase + y * destination.Stride;

            for (var x = 0; x < width; x++)
            {
                var deltaX = x - centerX;
                var deltaY = y - centerY;
                
                var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

                if (distance >= radius)
                {
                    FilterUtility.CopyPixel(sourceBase + y * sourceStride + x * BytesPerPixel, destinationRow + x * BytesPerPixel);
                    continue;
                }

                var angle = _strength * (1.0 - distance / radius);
                
                var cos = Math.Cos(angle);
                var sin = Math.Sin(angle);
                
                var sourceX = centerX + cos * deltaX - sin * deltaY;
                var sourceY = centerY + sin * deltaX + cos * deltaY;

                SampleBilinear(sourceBase, sourceStride, width, height, sourceX, sourceY, destinationRow + x * BytesPerPixel);
            }
        });
    }

    private static unsafe void SampleBilinear(
        byte* sourceBase,
        int sourceStride,
        int width,
        int height,
        double sourceX,
        double sourceY,
        byte* destination)
    {
        sourceX = Math.Clamp(sourceX, 0, width - 1);
        sourceY = Math.Clamp(sourceY, 0, height - 1);

        var left = Math.Clamp((int)Math.Floor(sourceX), 0, width - 1);
        var top = Math.Clamp((int)Math.Floor(sourceY), 0, height - 1);
        var right = Math.Min(left + 1, width - 1);
        var bottom = Math.Min(top + 1, height - 1);
        
        var horizontalWeight = sourceX - left;
        var verticalWeight = sourceY - top;

        var topLeft = sourceBase + top * sourceStride + left * BytesPerPixel;
        var topRight = sourceBase + top * sourceStride + right * BytesPerPixel;
        var bottomLeft = sourceBase + bottom * sourceStride + left * BytesPerPixel;
        var bottomRight = sourceBase + bottom * sourceStride + right * BytesPerPixel;

        for (var channel = 0; channel < BytesPerPixel; channel++)
        {
            var topValue = topLeft[channel] + (topRight[channel] - topLeft[channel]) * horizontalWeight;
            var bottomValue = bottomLeft[channel] + (bottomRight[channel] - bottomLeft[channel]) * horizontalWeight;
            
            destination[channel] = (byte)Math.Clamp((int)Math.Round(topValue + (bottomValue - topValue) * verticalWeight), 0, 255);
        }
    }
}
