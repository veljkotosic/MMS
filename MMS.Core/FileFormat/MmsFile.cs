using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using MMS.Core.FileFormat.Colorspace;
using MMS.Core.FileFormat.Colorspace.Converter;
using MMS.Core.FileFormat.Compression;
using MMS.Core.ImageResource;

namespace MMS.Core.FileFormat;

public sealed class MmsFile
    : IImageResource
{
    public MmsHeader Header { get; set; }
    public byte[] Metadata { get; set; } = [];
    public byte[] Pixels { get; set; } = [];
    public uint Crc32 { get; set; }

    public Bitmap GetBitmap()
    {
        var compression = MmsCompressionFactory.GetCompression(Header.Compression);
        var decompressedPixels = compression.Decompress(Pixels, Metadata, (int)Header.Width, (int)Header.Height, Header.Channels);

        var colorspaceConverter = MmsColorspaceConverterFactory.GetConverter(Header.Colorspace, MmsColorspace.Rgb);
        var convertedPixels = colorspaceConverter.Convert(decompressedPixels, (int)Header.Width, (int)Header.Height, Header.Channels);

        for (int i = 0; i < convertedPixels.Length; i += Header.Channels)
        {
            (convertedPixels[i], convertedPixels[i + 2]) = (convertedPixels[i + 2], convertedPixels[i]);
        }

        var pixelFormat = Header.Channels == 4 ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb;
        var bitmap = new Bitmap((int)Header.Width, (int)Header.Height, pixelFormat);
        var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

        try
        {
            for (int y = 0; y < Header.Height; y++)
            {
                Marshal.Copy(
                    convertedPixels,
                    y * (int)Header.Width * Header.Channels,
                    bitmapData.Scan0 + y * bitmapData.Stride,
                    (int)Header.Width * Header.Channels);
            }
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return bitmap;
    }

    public void SetBitmap(Bitmap bitmap)
    {
        Header = Header with
        {
            Magic = MmsHeader.MagicValue,
            Version = 1,
            HeaderLength = (ushort)Marshal.SizeOf<MmsHeader>(),
            Width = (uint)bitmap.Width,
            Height = (uint)bitmap.Height,
            Channels = (byte)(bitmap.PixelFormat == PixelFormat.Format32bppArgb ? 4 : 3)
        };

        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var pixelFormat = Header.Channels == 4 ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb;
        var bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, pixelFormat);

        byte[] rawBytes = new byte[Header.Width * Header.Height * Header.Channels];
        try
        {
            for (int y = 0; y < Header.Height; y++)
            {
                Marshal.Copy(
                    bmpData.Scan0 + y * bmpData.Stride,
                    rawBytes,
                    y * (int)Header.Width * Header.Channels,
                    (int)Header.Width * Header.Channels);
            }
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }

        for (int i = 0; i < rawBytes.Length; i += Header.Channels)
        {
            (rawBytes[i], rawBytes[i + 2]) = (rawBytes[i + 2], rawBytes[i]);
        }

        var colorspaceConverter = MmsColorspaceConverterFactory.GetConverter(MmsColorspace.Rgb, Header.Colorspace);
        var convertedPixels = colorspaceConverter.Convert(rawBytes, (int)Header.Width, (int)Header.Height, Header.Channels);

        var compression = MmsCompressionFactory.GetCompression(Header.Compression);
        var (compressedPixels, metadata) = compression.Compress(convertedPixels, (int)Header.Width, (int)Header.Height, Header.Channels);
        
        Pixels = compressedPixels;
        Metadata = metadata;

        Header = Header with
        {
            PixelsLength = (uint)Pixels.Length,
            MetadataLength = (uint)Metadata.Length
        };
    }
}