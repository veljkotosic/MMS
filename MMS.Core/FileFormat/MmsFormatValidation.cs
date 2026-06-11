using MMS.Core.FileFormat.Colorspace;
using MMS.Core.FileFormat.Compression;

namespace MMS.Core.FileFormat;

internal static class MmsFormatValidation
{
    public static byte GetChannelCount(MmsColorspace colorspace)
    {
        return colorspace switch
        {
            MmsColorspace.Linear => 1,
            MmsColorspace.Rgb or MmsColorspace.YCbCr => 3,
            _ => throw new InvalidDataException("Invalid colorspace.")
        };
    }

    public static void ValidateHeader(MmsHeader header)
    {
        if (header.Width == 0 || header.Height == 0)
        {
            throw new InvalidDataException("Invalid dimensions.");
        }

        var expectedChannels = GetChannelCount(header.Colorspace);

        if (header.Channels != expectedChannels)
        {
            throw new InvalidDataException("Invalid channel number");
        }

        if (!Enum.IsDefined(header.Compression))
        {
            throw new InvalidDataException($"Unsupported compression: {header.Compression}.");
        }

        if (header.Compression == MmsCompression.Mpeg1 && header.Colorspace != MmsColorspace.YCbCr)
        {
            throw new InvalidDataException("MPEG-1 compression requires the YCbCr colorspace.");
        }
    }
}
