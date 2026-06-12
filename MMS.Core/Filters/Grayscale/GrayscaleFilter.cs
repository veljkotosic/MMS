using System.Drawing;
using System.Drawing.Imaging;

namespace MMS.Core.Filters.Grayscale;

public sealed class GrayscaleFilter : IImageFilter
{
    private readonly GrayscaleFilterOptions _options;

    public GrayscaleFilter(GrayscaleFilterOptions options)
    {
        ((IFilterOptions)options).ValidateAndThrow();
        _options = options;
    }

    public void Execute(Bitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;

        var rect = new Rectangle(0, 0, width, height);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

        try
        {
            unsafe
            {
                var basePointer = (byte*)data.Scan0;

                Parallel.For(0, height, y =>
                {
                    var row = basePointer + y * data.Stride;

                    for (var x = 0; x < width; x++)
                    {
                        var pixel = row + x * 4;

                        var gray = (byte)(
                            (pixel[2] * _options.RMul +
                             pixel[1] * _options.GMul +
                             pixel[0] * _options.BMul)
                            >> _options.Shift);

                        pixel[0] = gray;
                        pixel[1] = gray;
                        pixel[2] = gray;
                    }
                });
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }
}
