namespace MMS.Core.FileFormat.Colorspace.Converter.Linear;

public class LinearToRgbColorspaceConverter
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

            var value = pixels[i] / 255.0;
            var rgbValue = value <= 0.0031308
                ? value * 12.92
                : 1.055 * Math.Pow(value, 1.0 / 2.4) - 0.055;

            result[i] = (byte)Math.Clamp(Math.Round(rgbValue * 255.0), 0, 255);
        }

        return result;
    }
}
