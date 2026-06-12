using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using MMS.Core.Filters.Utility;

namespace MMS.Core.Filters.EdgeDetection;

public sealed class EdgeDetectFilter : IImageFilter
{
    private const int BytesPerPixel = 4;
    private readonly EdgeDetectDirection _direction;

    public EdgeDetectFilter(EdgeDetectFilterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ((IFilterOptions)options).ValidateAndThrow();

        _direction = options.Direction;
    }

    public void Execute(Bitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;

        if (width < 3 || height < 3)
        {
            return;
        }

        var rect = new Rectangle(0, 0, width, height);
        var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

        try
        {
            var sourcePixels = FilterUtility.CopyBitmapBytes(bitmapData, width, height);
            ApplySobelKernel(sourcePixels, bitmapData, width, height);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }

    private void ApplySobelKernel(byte[] sourcePixels, BitmapData destination, int width, int height)
    {
        var sourceHandle = GCHandle.Alloc(sourcePixels, GCHandleType.Pinned);

        try
        {
            ApplySobelKernel(sourceHandle.AddrOfPinnedObject(), destination, width, height);
        }
        finally
        {
            sourceHandle.Free();
        }
    }

    private unsafe void ApplySobelKernel(IntPtr sourceAddress, BitmapData destination, int width, int height)
    {
        var sourceBase = (byte*)sourceAddress;
        var destinationBase = (byte*)destination.Scan0;
        var sourceRowBytes = width * BytesPerPixel;

        ClearBorders(destinationBase, destination.Stride, width, height);

        Parallel.For(1, height - 1, y =>
        {
            var rowAbove = sourceBase + (y - 1) * sourceRowBytes;
            var currentRow = sourceBase + y * sourceRowBytes;
            var rowBelow = sourceBase + (y + 1) * sourceRowBytes;
            var destinationRow = destinationBase + y * destination.Stride;

            for (var x = 1; x < width - 1; x++)
            {
                var offset = x * BytesPerPixel;
                var edge = _direction switch
                {
                    EdgeDetectDirection.Horizontal => DetectHorizontalEdge(rowAbove, rowBelow, offset),
                    EdgeDetectDirection.Vertical => DetectVerticalEdge(rowAbove, currentRow, rowBelow, offset),
                    EdgeDetectDirection.Both => DetectBothEdges(rowAbove, currentRow, rowBelow, offset),
                    _ => throw new InvalidOperationException("Unsupported edge detection direction.")
                };

                destinationRow[offset] = edge;
                destinationRow[offset + 1] = edge;
                destinationRow[offset + 2] = edge;
            }
        });
    }

    private static unsafe byte DetectHorizontalEdge(byte* rowAbove, byte* rowBelow, int offset)
    {
        var gradient = GetHorizontalGradient(rowAbove, rowBelow, offset);

        return FilterUtility.ClampToByte(Math.Abs(gradient));
    }

    private static unsafe byte DetectVerticalEdge(byte* rowAbove, byte* currentRow, byte* rowBelow, int offset)
    {
        var gradient = GetVerticalGradient(rowAbove, currentRow, rowBelow, offset);

        return FilterUtility.ClampToByte(Math.Abs(gradient));
    }

    private static unsafe byte DetectBothEdges(byte* rowAbove, byte* currentRow, byte* rowBelow, int offset)
    {
        var horizontal = GetHorizontalGradient(rowAbove, rowBelow, offset);
        var vertical = GetVerticalGradient(rowAbove, currentRow, rowBelow, offset);
        
        var magnitude = Math.Abs(horizontal) + Math.Abs(vertical);

        return FilterUtility.ClampToByte(magnitude);
    }

    private static unsafe int GetHorizontalGradient(byte* rowAbove, byte* rowBelow, int offset)
    {
        var top = Gray(rowAbove, offset - BytesPerPixel)
                  + 2 * Gray(rowAbove, offset)
                  + Gray(rowAbove, offset + BytesPerPixel);

        var bottom = Gray(rowBelow, offset - BytesPerPixel)
                     + 2 * Gray(rowBelow, offset)
                     + Gray(rowBelow, offset + BytesPerPixel);

        return bottom - top;
    }

    private static unsafe int GetVerticalGradient(byte* rowAbove, byte* currentRow, byte* rowBelow, int offset)
    {
        var left = Gray(rowAbove, offset - BytesPerPixel)
                   + 2 * Gray(currentRow, offset - BytesPerPixel)
                   + Gray(rowBelow, offset - BytesPerPixel);

        var right = Gray(rowAbove, offset + BytesPerPixel)
                    + 2 * Gray(currentRow, offset + BytesPerPixel)
                    + Gray(rowBelow, offset + BytesPerPixel);

        return right - left;
    }

    private static unsafe int Gray(byte* row, int offset)
    {
        return (row[offset + 2] * 306 + row[offset + 1] * 601 + row[offset] * 117) >> 10;
    }

    private static unsafe void ClearBorders(byte* destinationBase, int stride, int width, int height)
    {
        FilterUtility.ClearRow(destinationBase, width);
        FilterUtility.ClearRow(destinationBase + (height - 1) * stride, width);

        for (var y = 1; y < height - 1; y++)
        {
            var row = destinationBase + y * stride;
            FilterUtility.ClearPixel(row);
            FilterUtility.ClearPixel(row + (width - 1) * BytesPerPixel);
        }
    }
}
