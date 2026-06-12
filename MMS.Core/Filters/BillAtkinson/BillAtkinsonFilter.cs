using System.Drawing;
using System.Drawing.Imaging;

namespace MMS.Core.Filters.BillAtkinson;

public sealed class BillAtkinsonFilter : IImageFilter
{
    private const int BytesPerPixel = 4;
    private readonly int _threshold;

    public BillAtkinsonFilter(BillAtkinsonFilterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ((IFilterOptions)options).ValidateAndThrow();
        _threshold = options.Threshold;
    }

    public void Execute(Bitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        var rect = new Rectangle(0, 0, width, height);
        
        var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

        try
        {
            var channels = SeparateChannels(bitmapData, width, height);
            ApplyDithering(channels.Blue, channels.Green, channels.Red, bitmapData, width, height, _threshold);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }

    private static unsafe (int[] Blue, int[] Green, int[] Red) SeparateChannels(BitmapData bitmapData, int width, int height)
    {
        var pixelCount = checked(width * height);
        
        var blue = new int[pixelCount];
        var green = new int[pixelCount];
        var red = new int[pixelCount];
        
        var bitmapBase = (byte*)bitmapData.Scan0;

        Parallel.For(0, height, y =>
        {
            var row = bitmapBase + y * bitmapData.Stride;
            var rowOffset = y * width;

            for (var x = 0; x < width; x++)
            {
                var pixel = row + x * BytesPerPixel;
                var index = rowOffset + x;
                
                blue[index] = pixel[0];
                green[index] = pixel[1];
                red[index] = pixel[2];
            }
        });

        return (blue, green, red);
    }

    private static unsafe void ApplyDithering(
        int[] blue,
        int[] green,
        int[] red,
        BitmapData bitmapData,
        int width,
        int height,
        int threshold)
    {
        var bitmapBase = (byte*)bitmapData.Scan0;

        for (var y = 0; y < height; y++)
        {
            var row = bitmapBase + y * bitmapData.Stride;

            for (var x = 0; x < width; x++)
            {
                var index = y * width + x;
                var pixel = row + x * BytesPerPixel;

                pixel[0] = DitherChannel(blue, index, width, height, x, y, threshold);
                pixel[1] = DitherChannel(green, index, width, height, x, y, threshold);
                pixel[2] = DitherChannel(red, index, width, height, x, y, threshold);
            }
        }
    }

    private static byte DitherChannel(int[] channel, int index, int width, int height, int x, int y, int threshold)
    {
        var output = channel[index] >= threshold ? 255 : 0;
        var error = (channel[index] - output) / 8;

        AddError(channel, width, height, x + 1, y, error);
        AddError(channel, width, height, x + 2, y, error);
        AddError(channel, width, height, x - 1, y + 1, error);
        AddError(channel, width, height, x, y + 1, error);
        AddError(channel, width, height, x + 1, y + 1, error);
        AddError(channel, width, height, x, y + 2, error);

        return (byte)output;
    }

    private static void AddError(int[] channel, int width, int height, int x, int y, int error)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            channel[y * width + x] += error;
        }
    }
}
