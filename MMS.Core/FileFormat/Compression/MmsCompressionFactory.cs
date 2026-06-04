using MMS.Core.FileFormat.Compression.Downsampling.Mpeg1;
using MMS.Core.FileFormat.Compression.ShannonFano;

namespace MMS.Core.FileFormat.Compression;

public static class MmsCompressionFactory
{
    public static IMmsCompression GetCompression(MmsCompression compression)
    {
        return compression switch
        {
            MmsCompression.None => new NoCompression(),
            MmsCompression.ShannonFano => new ShannonFanoCompression(),
            MmsCompression.Mpeg1 => new Mpeg1Compression(),
            _ => throw new ArgumentOutOfRangeException(nameof(compression), $"Unsupported compression type: {compression}")
        };
    }
}