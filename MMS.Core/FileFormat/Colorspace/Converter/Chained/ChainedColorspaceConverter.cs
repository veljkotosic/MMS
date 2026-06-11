namespace MMS.Core.FileFormat.Colorspace.Converter.Chained;

public class ChainedColorspaceConverter
    : IMmsColorspaceConverter
{
    private readonly IMmsColorspaceConverter _toRgbConverter;
    private readonly IMmsColorspaceConverter _fromRgbConverter;
    
    public ChainedColorspaceConverter(
        MmsColorspace source,
        MmsColorspace target)
    {
        _toRgbConverter = MmsColorspaceConverterFactory.GetConverter(source, MmsColorspace.Rgb);
        _fromRgbConverter = MmsColorspaceConverterFactory.GetConverter(MmsColorspace.Rgb, target);
    }
    
    public byte[] Convert(byte[] pixels, int width, int height, int channels)
    {
        var rgbPixels = _toRgbConverter.Convert(pixels, width, height, channels);
        return _fromRgbConverter.Convert(rgbPixels, width, height, 3);
    }
}
