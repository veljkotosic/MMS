namespace MMS.Core.FileFormat.Compression;

public interface IMmsCompression
{
    (byte[] Data, byte[] Metadata) Compress(byte[] data, int width, int height, int channels);
    byte[] Decompress(byte[] data, byte[] metadata, int width, int height, int channels);
}