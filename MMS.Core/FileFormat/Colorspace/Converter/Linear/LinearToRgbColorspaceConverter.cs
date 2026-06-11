namespace MMS.Core.FileFormat.Colorspace.Converter.Linear;

public class LinearToRgbColorspaceConverter
    : IMmsColorspaceConverter
{
    public byte[] Convert(byte[] pixels, int width, int height, int channels)
    {
        if (channels != 1 || pixels.Length != checked(width * height))
        {
            throw new ArgumentException("Invalid channel count.");
        }

        var result = new byte[checked(width * height * 3)];

        for (var pixel = 0; pixel < pixels.Length; pixel++)
        {
            var offset = pixel * 3;
            result[offset] = pixels[pixel];
            result[offset + 1] = pixels[pixel];
            result[offset + 2] = pixels[pixel];
        }

        return result;
    }
}
