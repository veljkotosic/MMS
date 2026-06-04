namespace MMS.Core.FileFormat.Colorspace.Converter.Linear;

public class RgbToLinearColorspaceConverter
    : IMmsColorspaceConverter
{
    public byte[] Convert(byte[] pixels, int width, int height, int channels)
    {
        var result = new byte[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            if (channels == 4 && (i + 1) % 4 == 0)
            {
                result[i] = pixels[i];
                continue;
            }

            double val = pixels[i] / 255.0;
            double linearVal = val <= 0.04045
                ? val / 12.92
                : Math.Pow((val + 0.055) / 1.055, 2.4);

            result[i] = (byte)Math.Clamp(Math.Round(linearVal * 255.0), 0, 255);
        }

        return result;
    }
}