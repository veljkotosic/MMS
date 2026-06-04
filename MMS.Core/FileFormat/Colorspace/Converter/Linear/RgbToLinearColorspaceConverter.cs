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

            var value = pixels[i] / 255.0;
            var linearValue = value <= 0.04045
                ? value / 12.92
                : Math.Pow((value + 0.055) / 1.055, 2.4);

            result[i] = (byte)Math.Clamp(Math.Round(linearValue * 255.0), 0, 255);
        }

        return result;
    }
}