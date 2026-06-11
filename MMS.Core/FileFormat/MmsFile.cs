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
        MmsFormatValidation.ValidateHeader(Header);

        var width = checked((int)Header.Width);
        var height = checked((int)Header.Height);
        
        var compression = MmsCompressionFactory.GetCompression(Header.Compression);
        var decompressedPixels = compression.Decompress(Pixels, Metadata, width, height, Header.Channels);
        
        var expectedLength = checked(width * height * Header.Channels);

        if (decompressedPixels.Length != expectedLength)
        {
            throw new InvalidDataException("Invalid data length");
        }

        var colorspaceConverter = MmsColorspaceConverterFactory.GetConverter(Header.Colorspace, MmsColorspace.Rgb);
        var rgbPixels = colorspaceConverter.Convert(decompressedPixels, width, height, Header.Channels);
        var bgraPixels = RgbToBgra(rgbPixels, width, height);

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

        try
        {
            var rowLength = checked(width * 4);

            for (var y = 0; y < height; y++)
            {
                Marshal.Copy(
                    bgraPixels,
                    y * rowLength,
                    bitmapData.Scan0 + y * bitmapData.Stride,
                    rowLength);
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
        ArgumentNullException.ThrowIfNull(bitmap);

        var channels = MmsFormatValidation.GetChannelCount(Header.Colorspace);

        Header = Header with
        {
            Magic = MmsHeader.MagicValue,
            Version = 1,
            HeaderLength = (ushort)Marshal.SizeOf<MmsHeader>(),
            Width = (uint)bitmap.Width,
            Height = (uint)bitmap.Height,
            Channels = channels
        };

        MmsFormatValidation.ValidateHeader(Header);

        var width = bitmap.Width;
        var height = bitmap.Height;
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        
        using var normalizedBitmap = bitmap.Clone(rect, PixelFormat.Format32bppArgb);
        var normalizedBitmapData = normalizedBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        var rgbPixels = new byte[checked(width * height * 3)];
        
        try
        {
            unsafe
            {
                var sourceBase = (byte*)normalizedBitmapData.Scan0;

                Parallel.For(0, height, y =>
                {
                    var sourceRow = sourceBase + y * normalizedBitmapData.Stride;
                    var destinationOffset = y * width * 3;

                    for (var x = 0; x < width; x++)
                    {
                        var sourceOffset = x * 4;
                        var rgbOffset = destinationOffset + x * 3;

                        rgbPixels[rgbOffset] = sourceRow[sourceOffset + 2];
                        rgbPixels[rgbOffset + 1] = sourceRow[sourceOffset + 1];
                        rgbPixels[rgbOffset + 2] = sourceRow[sourceOffset];
                    }
                });
            }
        }
        finally
        {
            normalizedBitmap.UnlockBits(normalizedBitmapData);
        }

        var colorspaceConverter = MmsColorspaceConverterFactory.GetConverter(MmsColorspace.Rgb, Header.Colorspace);
        var convertedPixels = colorspaceConverter.Convert(rgbPixels, width, height, 3);

        var compression = MmsCompressionFactory.GetCompression(Header.Compression);
        var (compressedPixels, metadata) = compression.Compress(convertedPixels, width, height, Header.Channels);
        
        Pixels = compressedPixels;
        Metadata = metadata;

        Header = Header with
        {
            PixelsLength = (uint)Pixels.Length,
            MetadataLength = (uint)Metadata.Length
        };
    }

    private static byte[] RgbToBgra(byte[] rgbPixels, int width, int height)
    {
        var pixelCount = checked(width * height);

        if (rgbPixels.Length != checked(pixelCount * 3))
        {
            throw new InvalidDataException("Conversion failed.");
        }

        var bgraPixels = new byte[checked(pixelCount * 4)];

        Parallel.For(0, pixelCount, pixel =>
        {
            var rgbOffset = pixel * 3;
            var bgraOffset = pixel * 4;

            bgraPixels[bgraOffset] = rgbPixels[rgbOffset + 2];
            bgraPixels[bgraOffset + 1] = rgbPixels[rgbOffset + 1];
            bgraPixels[bgraOffset + 2] = rgbPixels[rgbOffset];
            bgraPixels[bgraOffset + 3] = 255;
        });

        return bgraPixels;
    }
}
