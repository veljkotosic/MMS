using MMS.Core.FileFormat.Colorspace.Converter.Chained;
using MMS.Core.FileFormat.Colorspace.Converter.Identity;
using MMS.Core.FileFormat.Colorspace.Converter.Linear;
using MMS.Core.FileFormat.Colorspace.Converter.YCbCr;

namespace MMS.Core.FileFormat.Colorspace.Converter;

public static class MmsColorspaceConverterFactory
{
    public static IMmsColorspaceConverter GetConverter(MmsColorspace source, MmsColorspace target)
    {
        if (source == target) 
        {
            return new IdentityColorspaceConverter();
        }

        if (source == MmsColorspace.Rgb) {
            return target switch {
                MmsColorspace.YCbCr => new RgbToYCbCrColorspaceConverter(),
                MmsColorspace.Linear => new RgbToLinearColorspaceConverter(),
                _ => throw new NotSupportedException()
            };
        }

        if (target == MmsColorspace.Rgb) {
            return source switch {
                MmsColorspace.YCbCr => new YCbCrToRgbColorspaceConverter(),
                MmsColorspace.Linear => new LinearToRgbColorspaceConverter(),
                _ => throw new NotSupportedException()
            };
        }
        
        return new ChainedColorspaceConverter(source, target);
    }
}