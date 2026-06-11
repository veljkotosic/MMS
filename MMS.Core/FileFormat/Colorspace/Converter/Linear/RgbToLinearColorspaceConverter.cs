namespace MMS.Core.FileFormat.Colorspace.Converter.Linear;

public class RgbToLinearColorspaceConverter
    : IMmsColorspaceConverter
{
    public byte[] Convert(byte[] pixels, int width, int height, int channels)
    {
        if (channels != 3 || pixels.Length != checked(width * height * channels))
        {
            throw new ArgumentException("Invalid channel count.");
        }

        var result = new byte[checked(width * height)];

        for (var pixel = 0; pixel < result.Length; pixel++)
        {
            var offset = pixel * 3;
            var gray =
                0.299 * pixels[offset]
                + 0.587 * pixels[offset + 1]
                + 0.114 * pixels[offset + 2];

            result[pixel] = (byte)Math.Clamp((int)Math.Round(gray), 0, 255);
        }

        return result;
    }
}
