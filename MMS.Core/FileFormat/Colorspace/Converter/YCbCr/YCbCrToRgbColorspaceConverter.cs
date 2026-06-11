namespace MMS.Core.FileFormat.Colorspace.Converter.YCbCr;

public class YCbCrToRgbColorspaceConverter
    : IMmsColorspaceConverter
{
    public byte[] Convert(byte[] pixels, int width, int height, int channels)
    {
        if (channels != 3 || pixels.Length != checked(width * height * channels))
        {
            throw new ArgumentException("Invalid channel count.");
        }

        var result = new byte[pixels.Length];
        for (int i = 0; i < pixels.Length; i += channels)
        {
            double y = pixels[i];
            double cb = pixels[i + 1] - 128;
            double cr = pixels[i + 2] - 128;

            var r = y + 1.402 * cr;
            var g = y - 0.344136 * cb - 0.714136 * cr;
            var b = y + 1.772 * cb;

            result[i] = (byte)Math.Clamp(r, 0, 255);
            result[i + 1] = (byte)Math.Clamp(g, 0, 255);
            result[i + 2] = (byte)Math.Clamp(b, 0, 255);

        }

        return result;
    }
}
