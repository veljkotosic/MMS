namespace MMS.Core.FileFormat.Colorspace.Converter;

public interface IMmsColorspaceConverter
{
    byte[] Convert(byte[] pixels, int width, int height, int channels);
}