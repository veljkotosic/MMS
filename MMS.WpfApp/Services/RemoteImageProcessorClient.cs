using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using MMS.Contracts;

namespace MMS.WpfApp.Services;

public sealed class RemoteImageProcessorClient
{
    private const int BytesPerPixel = 4;
    private const int ChunkSize = 64 * 1024;
    
    private readonly GrpcChannel _channel;
    private readonly ImageProcessor.ImageProcessorClient _client;

    public RemoteImageProcessorClient(string address = "http://localhost:5011")
    {
        _channel = GrpcChannel.ForAddress(address);
        _client = new ImageProcessor.ImageProcessorClient(_channel);
    }

    public async Task<RemoteImageProcessingResult> ProcessAsync(
        Bitmap bitmap,
        IEnumerable<ImageFilter> filters,
        CancellationToken cancellationToken = default)
    {
        using var call = _client.ProcessImage(cancellationToken: cancellationToken);
        
        var header = new ImageHeader
        {
            Width = bitmap.Width,
            Height = bitmap.Height
        };
        
        header.Filters.Add(filters);

        await call.RequestStream.WriteAsync(new ProcessImageRequest { Header = header }, cancellationToken);

        var imageBytes = ExtractBitmapBytes(bitmap);
        
        for (var offset = 0; offset < imageBytes.Length; offset += ChunkSize)
        {
            var length = Math.Min(ChunkSize, imageBytes.Length - offset);
            
            await call.RequestStream.WriteAsync(new ProcessImageRequest
            {
                Chunk = new ImageChunk
                {
                    Data = ByteString.CopyFrom(imageBytes, offset, length)
                }
            }, cancellationToken);
        }

        await call.RequestStream.CompleteAsync();

        ProcessImageResult? processingResult = null;
        using var responseBytes = new MemoryStream();

        await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            switch (response.PayloadCase)
            {
                case ProcessImageResponse.PayloadOneofCase.Result:
                    processingResult = response.Result;
                    break;
                case ProcessImageResponse.PayloadOneofCase.Chunk:
                    await responseBytes.WriteAsync(response.Chunk.Data.Memory, cancellationToken);
                    break;
            }
        }

        var expectedSize = checked(bitmap.Width * bitmap.Height * BytesPerPixel);
        
        if (responseBytes.Length != expectedSize)
        {
            throw new InvalidDataException($"Invalid image size.");
        }

        if (processingResult == null)
        {
            throw new InvalidDataException("Invalid processing result.");
        }

        return new RemoteImageProcessingResult(CreateBitmap(bitmap.Width, bitmap.Height, responseBytes.ToArray()), processingResult);
    }

    private static byte[] ExtractBitmapBytes(Bitmap bitmap)
    {
        var bytes = new byte[bitmap.Width * bitmap.Height * BytesPerPixel];
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        
        var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        try
        {
            for (var y = 0; y < bitmap.Height; y++)
            {
                Marshal.Copy(data.Scan0 + y * data.Stride, bytes, y * bitmap.Width * BytesPerPixel, bitmap.Width * BytesPerPixel);
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return bytes;
    }

    private static Bitmap CreateBitmap(int width, int height, byte[] bytes)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var rect = new Rectangle(0, 0, width, height);
        
        var data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            for (var y = 0; y < height; y++)
            {
                Marshal.Copy(bytes, y * width * BytesPerPixel, data.Scan0 + y * data.Stride, width * BytesPerPixel);
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return bitmap;
    }
}

public sealed record RemoteImageProcessingResult(Bitmap Bitmap, ProcessImageResult Timing);
