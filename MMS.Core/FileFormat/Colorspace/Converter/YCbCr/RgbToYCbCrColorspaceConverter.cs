namespace MMS.Core.FileFormat.Colorspace.Converter.YCbCr;

public class RgbToYCbCrColorspaceConverter
    : IMmsColorspaceConverter
{
    public byte[] Convert(byte[] pixels, int width, int height, int channels)
    {
        var result = new byte[pixels.Length];
        
        for (int i = 0; i < pixels.Length; i += channels)
        {
            double r = pixels[i];
            double g = pixels[i + 1];
            double b = pixels[i + 2];

            var y = 0.299 * r + 0.587 * g + 0.114 * b;
            var cb = 128 - 0.168736 * r - 0.331264 * g + 0.5 * b;
            var cr = 128 + 0.5 * r - 0.418688 * g - 0.081312 * b;

            result[i] = (byte)Math.Clamp(y, 0, 255);
            result[i + 1] = (byte)Math.Clamp(cb, 0, 255);
            result[i + 2] = (byte)Math.Clamp(cr, 0, 255);

            if (channels == 4)
            {
                result[i + 3] = pixels[i + 3];
            }
        }

        return result;
    }
}