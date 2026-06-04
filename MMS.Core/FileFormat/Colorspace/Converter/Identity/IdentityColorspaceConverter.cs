namespace MMS.Core.FileFormat.Colorspace.Converter.Identity;

public class IdentityColorspaceConverter
    : IMmsColorspaceConverter
{
    public byte[] Convert(byte[] pixels, int width, int height, int channels)
    {
        return pixels;
    }
}