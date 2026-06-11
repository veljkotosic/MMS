using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MMS.Core.Filters.Utility;

internal static class FilterUtility
{
    internal static byte ClampToByte(int value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 255)
        {
            return 255;
        }

        return (byte)value;
    }
    
    internal static unsafe void ClearPixel(byte* pixel)
    {
        pixel[0] = 0;
        pixel[1] = 0;
        pixel[2] = 0;
    }
    
    internal static unsafe void ClearRow(byte* row, int width, byte bytesPerPixel = 4)
    {
        for (var x = 0; x < width; x++)
        {
            ClearPixel(row + x * bytesPerPixel);
        }
    }
    
    internal static byte[] CopyBitmapBytes(BitmapData bitmapData, int width, int height, byte bytesPerPixel = 4)
    {
        var rowBytes = checked(width * bytesPerPixel);
        var pixels = new byte[checked(rowBytes * height)];

        for (var y = 0; y < height; y++)
        {
            Marshal.Copy(bitmapData.Scan0 + y * bitmapData.Stride, pixels, y * rowBytes, rowBytes);
        }

        return pixels;
    }
    
    internal static unsafe void CopyPixel(byte* source, byte* destination)
    {
        destination[0] = source[0];
        destination[1] = source[1];
        destination[2] = source[2];
        destination[3] = source[3];
    }
}