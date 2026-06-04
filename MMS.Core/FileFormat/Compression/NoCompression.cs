namespace MMS.Core.FileFormat.Compression;

public class NoCompression 
    : IMmsCompression
{
    public (byte[] Data, byte[] Metadata) Compress(byte[] data, int width, int height, int channels)
    {
        return (data, []);
    }

    public byte[] Decompress(byte[] data, byte[] metadata, int width, int height, int channels)
    {
        return data;
    }
}