using System.Drawing;
using System.Drawing.Imaging;

namespace MMS.Core.Filters.Gamma;

public sealed class GammaFilter : IImageFilter
{
    private readonly byte[] _lookupTable;

    public GammaFilter(GammaFilterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ((IFilterOptions)options).ValidateAndThrow();

        _lookupTable = CreateLookupTable(options.Gamma);
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
                var basePtr = (byte*)data.Scan0;

                Parallel.For(0, height, y =>
                {
                    var row = basePtr + y * data.Stride;

                    for (var x = 0; x < width; x++)
                    {
                        var pixel = row + x * 4;
                        pixel[0] = _lookupTable[pixel[0]];
                        pixel[1] = _lookupTable[pixel[1]];
                        pixel[2] = _lookupTable[pixel[2]];
                    }
                });
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static byte[] CreateLookupTable(double gamma)
    {
        var inverseGamma = 1.0 / gamma;
        var lookupTable = new byte[256];

        for (var value = 0; value < lookupTable.Length; value++)
        {
            var corrected = 255.0 * Math.Pow(value / 255.0, inverseGamma);
            
            lookupTable[value] = (byte)Math.Clamp((int)Math.Round(corrected), 0, 255);
        }

        return lookupTable;
    }
}
