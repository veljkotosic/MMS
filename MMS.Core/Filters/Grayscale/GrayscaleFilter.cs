using System.Drawing;
using System.Drawing.Imaging;

namespace MMS.Core.Filters.Grayscale;

public sealed class GrayscaleFilter(GrayscaleFilterOptions options) : IImageFilter
{
    public void Execute(Bitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        
        var rect = new Rectangle(0, 0, width, height);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

        try
        {
            var totalPixels = width * height;
            var chunkSize = totalPixels / Environment.ProcessorCount;

            unsafe
            {
                byte* basePtr = (byte*)data.Scan0;

                Parallel.For(0, Environment.ProcessorCount, worker =>
                {
                    int start = worker * chunkSize;
                    int end = (worker == Environment.ProcessorCount - 1) ? totalPixels : start + chunkSize;

                    for (int i = start; i < end; i++)
                    {
                        byte* pixel = basePtr + (i * 4);

                        byte gray = (byte)(
                            (pixel[2] * options.RMul +
                             pixel[1] * options.GMul +
                             pixel[0] * options.BMul)
                            >> options.Shift);

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
